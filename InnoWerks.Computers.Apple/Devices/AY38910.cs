namespace InnoWerks.Computers.Apple
{
    /// <summary>
    /// AY-3-8910 Programmable Sound Generator (PSG) emulation.
    /// <para>The PSG has 16 registers (R0-R15):</para>
    /// <ul>
    /// <li>R0-R1: Channel A tone period (12-bit)</li>
    /// <li>R2-R3: Channel B tone period (12-bit)</li>
    /// <li>R4-R5: Channel C tone period (12-bit)</li>
    /// <li>R6: Noise period (5-bit)</li>
    /// <li>R7: Mixer control (tone/noise enable per channel, I/O port direction)</li>
    /// <li>R8: Channel A volume (4-bit + envelope mode bit)</li>
    /// <li>R9: Channel B volume</li>
    /// <li>R10: Channel C volume</li>
    /// <li>R11-R12: Envelope period (16-bit)</li>
    /// <li>R13: Envelope shape/cycle control</li>
    /// <li>R14: I/O Port A data</li>
    /// <li>R15: I/O Port B data</li>
    /// </ul>
    /// <para>
    /// Communication with the PSG is via two 8-bit ports on a 6522 VIA:
    /// Port A carries the 8-bit data bus (DA0-DA7), Port B carries control
    /// lines (~RESET on bit 2, BC1 on bit 0, BDIR on bit 1).
    /// </para>
    /// <para>Bus control truth table:</para>
    /// <ul>
    /// <li>BDIR=0 BC1=0: Inactive</li>
    /// <li>BDIR=0 BC1=1: Read from PSG register</li>
    /// <li>BDIR=1 BC1=0: Write to PSG register</li>
    /// <li>BDIR=1 BC1=1: Latch register address</li>
    /// </ul>
    /// </summary>
    public sealed class AY38910
    {
        // 16 internal registers
        private readonly byte[] registers = new byte[16];

        // currently latched register address
        private byte latchedRegister;

        // tone generator state
        private int toneCounterA;
        private int toneCounterB;
        private int toneCounterC;
        private bool toneOutputA;
        private bool toneOutputB;
        private bool toneOutputC;

        // noise generator state
        private int noiseCounter;
        private bool noiseOutput;
        private int noiseShiftRegister = 1; // LFSR, must be non-zero

        // envelope generator state
        private int envelopeCounter;
        private int envelopeStep;
        private bool envelopeHolding;
        private byte envelopeVolume;

        // DAC volume table (logarithmic, 16 levels)
        // Based on measurements of real AY-3-8910 output levels
        private static readonly float[] VolumeTable =
        [
            0.0000f, 0.0100f, 0.0145f, 0.0211f,
            0.0307f, 0.0455f, 0.0645f, 0.1074f,
            0.1266f, 0.2050f, 0.2922f, 0.3728f,
            0.4925f, 0.6353f, 0.8056f, 1.0000f,
        ];

        public void Reset()
        {
            for (var i = 0; i < 16; i++)
            {
                registers[i] = 0;
            }

            latchedRegister = 0;

            toneCounterA = 0;
            toneCounterB = 0;
            toneCounterC = 0;
            toneOutputA = false;
            toneOutputB = false;
            toneOutputC = false;

            noiseCounter = 0;
            noiseOutput = false;
            noiseShiftRegister = 1;

            envelopeCounter = 0;
            envelopeStep = 0;
            envelopeHolding = false;
            envelopeVolume = 0;
        }

        /// <summary>
        /// Called by the 6522 VIA when the PSG bus control lines change.
        /// The VIA's Port B low bits drive BDIR and BC1.
        /// </summary>
        public void SetBusControl(byte portB, byte portA)
        {
            var bc1 = (portB & 0x01) != 0;
            var bdir = (portB & 0x02) != 0;
            var reset = (portB & 0x04) == 0; // active-low

            if (reset)
            {
                Reset();
                return;
            }

            if (bdir && bc1)
            {
                // Latch register address
                latchedRegister = (byte)(portA & 0x0F);
            }
            else if (bdir && !bc1)
            {
                // Write data to latched register
                WriteRegister(latchedRegister, portA);
            }
        }

        /// <summary>
        /// Read from the currently latched register.
        /// </summary>
        public byte ReadData()
        {
            return latchedRegister < 16 ? registers[latchedRegister] : (byte)0xFF;
        }

        private void WriteRegister(byte reg, byte value)
        {
            if (reg >= 16)
            {
                return;
            }

            // mask register values to their valid bit widths
            registers[reg] = reg switch
            {
                1 or 3 or 5 => (byte)(value & 0x0F),   // tone period high (4-bit)
                6 => (byte)(value & 0x1F),              // noise period (5-bit)
                7 => value,                              // mixer (full byte)
                8 or 9 or 10 => (byte)(value & 0x1F),   // volume (5-bit)
                13 => (byte)(value & 0x0F),              // envelope shape (4-bit)
                _ => value,
            };

            // writing to envelope shape register resets the envelope generator
            if (reg == 13)
            {
                envelopeStep = 0;
                envelopeCounter = 0;
                envelopeHolding = false;
            }
        }

        /// <summary>
        /// Advance the PSG state by one clock tick (1.0227 MHz on the Mockingboard,
        /// which is the Apple II system clock). The PSG internally divides by 8,
        /// but we handle that in the tone/noise period counters.
        ///
        /// Returns a mono sample in the range [0.0, 1.0].
        /// </summary>
        public float Clock()
        {
            // tone generators
            UpdateToneGenerator(ref toneCounterA, ref toneOutputA, TonePeriodA);
            UpdateToneGenerator(ref toneCounterB, ref toneOutputB, TonePeriodB);
            UpdateToneGenerator(ref toneCounterC, ref toneOutputC, TonePeriodC);

            // noise generator
            UpdateNoiseGenerator();

            // envelope generator
            UpdateEnvelopeGenerator();

            // mixer: bit = 0 means enabled, bit = 1 means disabled
            var mixer = registers[7];

            var toneEnableA = (mixer & 0x01) == 0;
            var toneEnableB = (mixer & 0x02) == 0;
            var toneEnableC = (mixer & 0x04) == 0;
            var noiseEnableA = (mixer & 0x08) == 0;
            var noiseEnableB = (mixer & 0x10) == 0;
            var noiseEnableC = (mixer & 0x20) == 0;

            // channel output: OR of tone and noise (when enabled), then apply volume
            var outA = (toneEnableA && toneOutputA) || (noiseEnableA && noiseOutput) ||
                       (!toneEnableA && !noiseEnableA);
            var outB = (toneEnableB && toneOutputB) || (noiseEnableB && noiseOutput) ||
                       (!toneEnableB && !noiseEnableB);
            var outC = (toneEnableC && toneOutputC) || (noiseEnableC && noiseOutput) ||
                       (!toneEnableC && !noiseEnableC);

            // apply volume (envelope or fixed)
            var volA = GetChannelVolume(8);
            var volB = GetChannelVolume(9);
            var volC = GetChannelVolume(10);

            var sampleA = outA ? VolumeTable[volA] : 0f;
            var sampleB = outB ? VolumeTable[volB] : 0f;
            var sampleC = outC ? VolumeTable[volC] : 0f;

            // mix the three channels equally
            return (sampleA + sampleB + sampleC) / 3.0f;
        }

        private byte GetChannelVolume(int register)
        {
            var vol = registers[register];

            if ((vol & 0x10) != 0)
            {
                // envelope mode
                return envelopeVolume;
            }

            return (byte)(vol & 0x0F);
        }

        private static void UpdateToneGenerator(ref int counter, ref bool output, int period)
        {
            if (period == 0)
            {
                period = 1;
            }

            counter++;

            if (counter >= period)
            {
                counter = 0;
                output = !output;
            }
        }

        private void UpdateNoiseGenerator()
        {
            var period = registers[6] & 0x1F;
            if (period == 0)
            {
                period = 1;
            }

            noiseCounter++;

            if (noiseCounter >= period)
            {
                noiseCounter = 0;

                // 17-bit LFSR: new bit = XOR of bits 0 and 3
                var feedback = ((noiseShiftRegister ^ (noiseShiftRegister >> 3)) & 1) != 0;
                noiseShiftRegister = (noiseShiftRegister >> 1) | (feedback ? 0x10000 : 0);
                noiseOutput = (noiseShiftRegister & 1) != 0;
            }
        }

        private void UpdateEnvelopeGenerator()
        {
            if (envelopeHolding)
            {
                return;
            }

            var period = EnvelopePeriod;
            if (period == 0)
            {
                period = 1;
            }

            envelopeCounter++;

            if (envelopeCounter >= period)
            {
                envelopeCounter = 0;
                envelopeStep++;

                var shape = registers[13] & 0x0F;

                // the envelope shape is a 4-bit field:
                //   bit 3: continue
                //   bit 2: attack (direction)
                //   bit 1: alternate
                //   bit 0: hold

                var cont = (shape & 0x08) != 0;
                var attack = (shape & 0x04) != 0;
                var alt = (shape & 0x02) != 0;
                var hold = (shape & 0x01) != 0;

                if (envelopeStep >= 16)
                {
                    if (!cont)
                    {
                        // non-continue: drop to zero and hold
                        envelopeVolume = 0;
                        envelopeHolding = true;
                        return;
                    }

                    if (hold)
                    {
                        // hold at the final value
                        envelopeVolume = alt != attack ? (byte)15 : (byte)0;
                        envelopeHolding = true;
                        return;
                    }

                    if (alt)
                    {
                        // alternate: reverse direction
                        attack = !attack;
                    }

                    envelopeStep = 0;
                }

                envelopeVolume = attack
                    ? (byte)envelopeStep
                    : (byte)(15 - envelopeStep);
            }
        }

        // helper properties for tone/noise/envelope periods
        private int TonePeriodA => registers[0] | ((registers[1] & 0x0F) << 8);
        private int TonePeriodB => registers[2] | ((registers[3] & 0x0F) << 8);
        private int TonePeriodC => registers[4] | ((registers[5] & 0x0F) << 8);
        private int EnvelopePeriod => registers[11] | (registers[12] << 8);
    }
}

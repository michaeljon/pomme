using System;

namespace InnoWerks.Computers.Apple
{
    /// <summary>
    /// MOS 6522 Versatile Interface Adapter (VIA) emulation.
    ///
    /// Register map (accent accent accent accent accent accent accent accent accent RS3-RS0):
    ///   $0: ORB/IRB  - Output/Input Register B
    ///   $1: ORA/IRA  - Output/Input Register A (with handshake)
    ///   $2: DDRB     - Data Direction Register B
    ///   $3: DDRA     - Data Direction Register A
    ///   $4: T1C-L    - Timer 1 Counter Low (read) / Latch Low (write)
    ///   $5: T1C-H    - Timer 1 Counter High (read/write, starts timer)
    ///   $6: T1L-L    - Timer 1 Latch Low
    ///   $7: T1L-H    - Timer 1 Latch High
    ///   $8: T2C-L    - Timer 2 Counter Low (read) / Latch Low (write)
    ///   $9: T2C-H    - Timer 2 Counter High (read/write, starts timer)
    ///   $A: SR       - Shift Register
    ///   $B: ACR      - Auxiliary Control Register
    ///   $C: PCR      - Peripheral Control Register
    ///   $D: IFR      - Interrupt Flag Register
    ///   $E: IER      - Interrupt Enable Register
    ///   $F: ORA/IRA  - Same as $1 but without handshake
    ///
    /// For the Mockingboard, Port A carries data to/from the AY-3-8910 PSG,
    /// and Port B bits 0-2 carry the PSG bus control signals (BC1, BDIR, ~RESET).
    /// Timer 1 is typically used for interrupt-driven music playback.
    /// </summary>
    public sealed class Via6522
    {
        // I/O port state
        private byte outputRegisterA;
        private byte outputRegisterB;
        private byte dataDirectionA;
        private byte dataDirectionB;

        // Timer 1 state
        private ushort timer1Counter;
        private ushort timer1Latch;
        private bool timer1Running;
        private bool timer1Armed; // for one-shot mode: has the interrupt fired?

        // Timer 2 state
        private ushort timer2Counter;
        private byte timer2LatchLow;
        private bool timer2Running;
        private bool timer2Armed;

        // Shift register
        private byte shiftRegister;

        // Control registers
        private byte auxiliaryControlRegister; // ACR
        private byte peripheralControlRegister; // PCR

        // Interrupt state
        private byte interruptFlagRegister;  // IFR (active interrupt flags)
        private byte interruptEnableRegister; // IER (enabled interrupt mask)

        // IFR bit assignments
        private const byte IFR_CA2 = 0x01;
        private const byte IFR_CA1 = 0x02;
        private const byte IFR_SR = 0x04;
        private const byte IFR_CB2 = 0x08;
        private const byte IFR_CB1 = 0x10;
        private const byte IFR_T2 = 0x20;
        private const byte IFR_T1 = 0x40;
        private const byte IFR_IRQ = 0x80; // composite: set when any enabled flag is active

        /// <summary>
        /// Callback invoked when Port B is written, allowing the parent device
        /// to notify the connected PSG of bus control changes.
        /// </summary>
        public Action<byte, byte> OnPortBWrite { get; set; }

        /// <summary>
        /// Callback invoked when the IRQ output line changes state.
        /// </summary>
        public Action<bool> OnIrqChanged { get; set; }

        /// <summary>
        /// True if the composite IRQ output is asserted (any enabled interrupt flag is set).
        /// </summary>
        public bool IrqActive => (interruptFlagRegister & IFR_IRQ) != 0;

        public void Reset()
        {
            outputRegisterA = 0;
            outputRegisterB = 0;
            dataDirectionA = 0;
            dataDirectionB = 0;

            timer1Counter = 0;
            timer1Latch = 0;
            timer1Running = false;
            timer1Armed = false;

            timer2Counter = 0;
            timer2LatchLow = 0;
            timer2Running = false;
            timer2Armed = false;

            shiftRegister = 0;
            auxiliaryControlRegister = 0;
            peripheralControlRegister = 0;
            interruptFlagRegister = 0;
            interruptEnableRegister = 0;
        }

        public byte Read(byte register)
        {
            switch (register & 0x0F)
            {
                case 0x0: // ORB/IRB
                    // clear CB1/CB2 interrupt flags on read
                    ClearInterruptFlag(IFR_CB1 | IFR_CB2);
                    // read: input bits come from external (0 for now), output bits from ORB
                    return (byte)((outputRegisterB & dataDirectionB) | (~dataDirectionB & 0x00));

                case 0x1: // ORA/IRA (with handshake)
                    ClearInterruptFlag(IFR_CA1 | IFR_CA2);
                    return (byte)((outputRegisterA & dataDirectionA) | (~dataDirectionA & 0x00));

                case 0x2: // DDRB
                    return dataDirectionB;

                case 0x3: // DDRA
                    return dataDirectionA;

                case 0x4: // T1C-L (reading clears T1 interrupt flag)
                    ClearInterruptFlag(IFR_T1);
                    return (byte)(timer1Counter & 0xFF);

                case 0x5: // T1C-H
                    return (byte)((timer1Counter >> 8) & 0xFF);

                case 0x6: // T1L-L
                    return (byte)(timer1Latch & 0xFF);

                case 0x7: // T1L-H
                    return (byte)((timer1Latch >> 8) & 0xFF);

                case 0x8: // T2C-L (reading clears T2 interrupt flag)
                    ClearInterruptFlag(IFR_T2);
                    return (byte)(timer2Counter & 0xFF);

                case 0x9: // T2C-H
                    return (byte)((timer2Counter >> 8) & 0xFF);

                case 0xA: // Shift Register
                    ClearInterruptFlag(IFR_SR);
                    return shiftRegister;

                case 0xB: // ACR
                    return auxiliaryControlRegister;

                case 0xC: // PCR
                    return peripheralControlRegister;

                case 0xD: // IFR
                    return interruptFlagRegister;

                case 0xE: // IER
                    // bit 7 is always read as 1
                    return (byte)(interruptEnableRegister | 0x80);

                case 0xF: // ORA (no handshake)
                    return (byte)((outputRegisterA & dataDirectionA) | (~dataDirectionA & 0x00));

                default:
                    return 0xFF;
            }
        }

        public void Write(byte register, byte value)
        {
            switch (register & 0x0F)
            {
                case 0x0: // ORB
                    outputRegisterB = value;
                    ClearInterruptFlag(IFR_CB1 | IFR_CB2);
                    OnPortBWrite?.Invoke(
                        (byte)(outputRegisterB & dataDirectionB),
                        (byte)(outputRegisterA & dataDirectionA));
                    break;

                case 0x1: // ORA (with handshake)
                    outputRegisterA = value;
                    ClearInterruptFlag(IFR_CA1 | IFR_CA2);
                    break;

                case 0x2: // DDRB
                    dataDirectionB = value;
                    break;

                case 0x3: // DDRA
                    dataDirectionA = value;
                    break;

                case 0x4: // T1L-L (write goes to latch, not counter)
                    timer1Latch = (ushort)((timer1Latch & 0xFF00) | value);
                    break;

                case 0x5: // T1C-H (write starts timer)
                    timer1Latch = (ushort)((timer1Latch & 0x00FF) | (value << 8));
                    timer1Counter = timer1Latch;
                    timer1Running = true;
                    timer1Armed = true;
                    ClearInterruptFlag(IFR_T1);
                    break;

                case 0x6: // T1L-L
                    timer1Latch = (ushort)((timer1Latch & 0xFF00) | value);
                    break;

                case 0x7: // T1L-H
                    timer1Latch = (ushort)((timer1Latch & 0x00FF) | (value << 8));
                    ClearInterruptFlag(IFR_T1);
                    break;

                case 0x8: // T2L-L (write goes to latch)
                    timer2LatchLow = value;
                    break;

                case 0x9: // T2C-H (write starts timer)
                    timer2Counter = (ushort)((value << 8) | timer2LatchLow);
                    timer2Running = true;
                    timer2Armed = true;
                    ClearInterruptFlag(IFR_T2);
                    break;

                case 0xA: // Shift Register
                    shiftRegister = value;
                    ClearInterruptFlag(IFR_SR);
                    break;

                case 0xB: // ACR
                    auxiliaryControlRegister = value;
                    break;

                case 0xC: // PCR
                    peripheralControlRegister = value;
                    break;

                case 0xD: // IFR (writing 1s clears the corresponding flags)
                    ClearInterruptFlag((byte)(value & 0x7F));
                    break;

                case 0xE: // IER
                    if ((value & 0x80) != 0)
                    {
                        // set: enable the specified bits
                        interruptEnableRegister |= (byte)(value & 0x7F);
                    }
                    else
                    {
                        // clear: disable the specified bits
                        interruptEnableRegister &= (byte)~(value & 0x7F);
                    }
                    UpdateIrqLine();
                    break;

                case 0xF: // ORA (no handshake)
                    outputRegisterA = value;
                    break;
            }
        }

        /// <summary>
        /// Advance the VIA timers by the specified number of CPU cycles.
        /// </summary>
        public void Tick(int cycles)
        {
            if (timer1Running)
            {
                TickTimer1(cycles);
            }

            if (timer2Running)
            {
                TickTimer2(cycles);
            }
        }

        private void TickTimer1(int cycles)
        {
            var newValue = timer1Counter - cycles;

            if (newValue <= 0)
            {
                // timer underflowed
                var freeRunning = (auxiliaryControlRegister & 0x40) != 0;

                if (freeRunning)
                {
                    // continuous mode: reload from latch and keep running
                    timer1Counter = timer1Latch;
                    SetInterruptFlag(IFR_T1);
                }
                else
                {
                    // one-shot mode: fire interrupt once, then free-run without interrupts
                    if (timer1Armed)
                    {
                        SetInterruptFlag(IFR_T1);
                        timer1Armed = false;
                    }

                    // counter continues to count down from $FFFF
                    timer1Counter = (ushort)(0xFFFF + newValue);
                }
            }
            else
            {
                timer1Counter = (ushort)newValue;
            }
        }

        private void TickTimer2(int cycles)
        {
            // Timer 2 only operates in one-shot mode for the Mockingboard
            // (pulse counting mode via ACR bit 5 is not used)
            var newValue = timer2Counter - cycles;

            if (newValue <= 0)
            {
                if (timer2Armed)
                {
                    SetInterruptFlag(IFR_T2);
                    timer2Armed = false;
                }

                timer2Counter = (ushort)(0xFFFF + newValue);
            }
            else
            {
                timer2Counter = (ushort)newValue;
            }
        }

        private void SetInterruptFlag(byte flag)
        {
            interruptFlagRegister |= flag;
            UpdateIrqLine();
        }

        private void ClearInterruptFlag(byte flag)
        {
            interruptFlagRegister &= (byte)~flag;
            UpdateIrqLine();
        }

        private void UpdateIrqLine()
        {
            var wasActive = (interruptFlagRegister & IFR_IRQ) != 0;

            if ((interruptFlagRegister & interruptEnableRegister & 0x7F) != 0)
            {
                interruptFlagRegister |= IFR_IRQ;
            }
            else
            {
                interruptFlagRegister &= unchecked((byte)~IFR_IRQ);
            }

            var isActive = (interruptFlagRegister & IFR_IRQ) != 0;

            if (isActive != wasActive)
            {
                OnIrqChanged?.Invoke(isActive);
            }
        }
    }
}

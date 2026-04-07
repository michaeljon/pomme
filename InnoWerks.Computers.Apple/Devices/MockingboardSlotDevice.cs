using System;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
    /// <summary>
    /// Sweet Micro Systems Mockingboard A/B sound card emulation.
    /// <para>
    /// The Mockingboard contains two 6522 VIAs, each connected to an AY-3-8910 PSG:
    /// </para>
    /// <ul>
    /// <li>VIA1 + PSG1 at $Cn00-$Cn0F (channels A1, B1, C1)</li>
    /// <li>VIA2 + PSG2 at $Cn80-$Cn8F (channels A2, B2, C2)</li>
    /// </ul>
    /// <para>
    /// The VIA Port A carries the 8-bit data bus to the PSG.
    /// The VIA Port B low bits carry the PSG control signals:
    /// </para>
    /// <ul>
    /// <li>Bit 0: BC1</li>
    /// <li>Bit 1: BDIR</li>
    /// <li>Bit 2: ~RESET (active low)</li>
    /// </ul>
    /// <para>
    /// The Mockingboard A/B has no ROM. Detection is timer-based: software writes
    /// to VIA Timer 1, reads it back, and checks that it has decremented.
    /// Timer 1 on either VIA can generate IRQs for interrupt-driven music playback.
    /// </para>
    /// </summary>
    public sealed class MockingboardSlotDevice : SlotRomDevice
    {
        private readonly Via6522 via1 = new();
        private readonly Via6522 via2 = new();
        private readonly AY38910 psg1 = new();
        private readonly AY38910 psg2 = new();

        private readonly ICpu cpu;

        public MockingboardSlotDevice(
            int slot,
            Computer computer)
            : base(slot, "Mockingboard", computer)
        {
            ArgumentNullException.ThrowIfNull(computer, nameof(computer));

            this.cpu = computer.Processor;

            // wire up VIA port B writes to the PSGs
            via1.OnPortBWrite = (portB, portA) => psg1.SetBusControl(portB, portA);
            via2.OnPortBWrite = (portB, portA) => psg2.SetBusControl(portB, portA);

            // wire up VIA IRQ lines to the CPU
            via1.OnIrqChanged = (active) => UpdateIrq();
            via2.OnIrqChanged = (active) => UpdateIrq();
        }

        public override bool HandlesRead(ushort address)
        {
            // slot ROM space: $Cn00-$CnFF (VIA registers)
            return address >= RomBaseAddressLo && address <= RomBaseAddressHi;
        }

        public override bool HandlesWrite(ushort address)
        {
            // slot ROM space: $Cn00-$CnFF (VIA registers)
            return address >= RomBaseAddressLo && address <= RomBaseAddressHi;
        }

        protected override byte DoIo(MemoryAccessType ioType, ushort address, byte value)
        {
            return 0xFF;
        }

        protected override byte DoCx(MemoryAccessType ioType, ushort address, byte value)
        {
            // Address bit 7 selects VIA1 (0) or VIA2 (1), bits 0-3 select the register.
            var reg = (byte)(address & 0x0F);
            var via = (address & 0x80) != 0 ? via2 : via1;

            if (ioType == MemoryAccessType.Read)
            {
                return via.Read(reg);
            }
            else
            {
                via.Write(reg, value);
                return 0;
            }
        }

        public override void Tick()
        {
            via1.Tick();
            via2.Tick();
        }

        public override void Reset()
        {
            via1.Reset();
            via2.Reset();
            psg1.Reset();
            psg2.Reset();
        }

        /// <summary>
        /// Generate a mono audio sample by mixing both PSGs.
        /// Called by the audio renderer at the sample rate.
        /// </summary>
        public float GenerateSample()
        {
            var sample1 = psg1.Clock();
            var sample2 = psg2.Clock();

            return (sample1 + sample2) / 2.0f;
        }

        private void UpdateIrq()
        {
            var irqActive = via1.IrqActive || via2.IrqActive;

            if (irqActive)
            {
                cpu.InjectInterrupt(nmi: false);
            }
        }
    }
}

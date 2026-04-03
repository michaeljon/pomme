using System;
using System.IO;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
    /// <summary>
    /// Sweet Micro Systems Mockingboard sound card emulation.
    ///
    /// The Mockingboard contains two 6522 VIAs, each connected to an AY-3-8910 PSG:
    ///   VIA1 + PSG1 at $Cn00-$Cn0F (channels A1, B1, C1)
    ///   VIA2 + PSG2 at $Cn80-$Cn8F (channels A2, B2, C2)
    ///
    /// The VIA Port A carries the 8-bit data bus to the PSG.
    /// The VIA Port B low bits carry the PSG control signals:
    ///   Bit 0: BC1
    ///   Bit 1: BDIR
    ///   Bit 2: ~RESET (active low)
    ///
    /// The expansion ROM ($C800-$CFFF) contains the Mockingboard firmware.
    ///
    /// Timer 1 on either VIA can generate IRQs for interrupt-driven music playback.
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
            ICpu cpu,
            IAppleBus bus,
            MachineState machineState)
            : base(slot, "Mockingboard", cpu, bus, machineState)
        {
            ArgumentNullException.ThrowIfNull(cpu, nameof(cpu));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            this.cpu = cpu;

            // Load the Mockingboard D 2K ROM. The ROM occupies $Cn10-$Cn7F and
            // $Cn90-$CnFF in the slot ROM space (avoiding the VIA register ranges)
            // and the full $C800-$CFFF expansion ROM space.
            var romBytes = File.ReadAllBytes("roms/mockingboard.rom");

            HasRom = true;
            Rom = new byte[256];
            Array.Copy(romBytes, 0, Rom, 0, Math.Min(romBytes.Length, 256));

            HasAuxRom = true;
            ExpansionRom = new byte[2048];
            Array.Copy(romBytes, 0, ExpansionRom, 0, Math.Min(romBytes.Length, 2048));

            // wire up VIA port B writes to the PSGs
            via1.OnPortBWrite = (portB, portA) => psg1.SetBusControl(portB, portA);
            via2.OnPortBWrite = (portB, portA) => psg2.SetBusControl(portB, portA);

            // wire up VIA IRQ lines to the CPU
            via1.OnIrqChanged = (active) => UpdateIrq();
            via2.OnIrqChanged = (active) => UpdateIrq();

            bus.AddDevice(this);
        }

        public override bool HandlesRead(ushort address)
        {
            // slot I/O: $C0n0-$C0nF
            if (address >= IoBaseAddressLo && address <= IoBaseAddressHi)
            {
                return true;
            }

            // slot ROM: $Cn00-$CnFF (VIA registers and ROM)
            if (address >= RomBaseAddressLo && address <= RomBaseAddressHi)
            {
                return true;
            }

            // expansion ROM: $C800-$CFFF
            if (address >= ExpansionBaseAddressLo && address <= ExpansionBaseAddressHi)
            {
                return true;
            }

            return false;
        }

        public override bool HandlesWrite(ushort address)
        {
            // slot I/O: $C0n0-$C0nF
            if (address >= IoBaseAddressLo && address <= IoBaseAddressHi)
            {
                return true;
            }

            // slot ROM: $Cn00-$CnFF (VIA registers are writable)
            if (address >= RomBaseAddressLo && address <= RomBaseAddressHi)
            {
                return true;
            }

            return false;
        }

        protected override byte DoIo(CardIoType ioType, byte address, byte value)
        {
            // The Mockingboard doesn't use the standard $C0n0-$C0nF I/O space.
            // All register access goes through the $Cn00 ROM space.
            // Return floating bus value for any stray I/O access.
            return machineState.FloatingValue;
        }

        protected override byte DoCx(CardIoType ioType, ushort address, byte value)
        {
            var offset = address & 0xFF;

            // VIA registers: $Cn00-$Cn0F (VIA1) and $Cn80-$Cn8F (VIA2)
            var isViaRange = (offset & 0x70) == 0x00; // low nibble of high nibble is 0 → $Cn0x or $Cn8x
            if (isViaRange)
            {
                var reg = (byte)(offset & 0x0F);
                var via = (offset & 0x80) != 0 ? via2 : via1;

                if (ioType == CardIoType.Read)
                {
                    return via.Read(reg);
                }
                else
                {
                    via.Write(reg, value);
                    return 0;
                }
            }

            // Non-VIA addresses serve ROM
            if (ioType == CardIoType.Read && Rom != null)
            {
                return Rom[offset];
            }

            return machineState.FloatingValue;
        }

        protected override byte DoC8(CardIoType ioType, ushort address, byte value)
        {
            if (ioType == CardIoType.Read && ExpansionRom != null)
            {
                return ExpansionRom[address - ExpansionBaseAddressLo];
            }

            return machineState.FloatingValue;
        }

        public override void Tick(int cycles)
        {
            via1.Tick(cycles);
            via2.Tick(cycles);
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

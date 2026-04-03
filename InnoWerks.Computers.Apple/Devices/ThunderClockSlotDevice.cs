using System;
using System.Collections.Generic;
using System.IO;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
    // https://www.alldatasheet.com/datasheet-pdf/view/113488/NEC/UPD1990AC.html
    //
    // UPD1990AC 40 bit shift register layout:
    //
    // All values except month are BCD encoded. Month is a hex value. Each
    // slot is a 4-bit 0-based BCD value.
    //
    // month (1-12) -- 4-bit hex encoded
    // day of week (0-6)
    // day of month, tens digit (0-3)
    // day of month, ones digit (0-9)
    // hour, tens digit (0-2)
    // hour, ones digit (0-9)
    // minute, tens digit (0-5)
    // minute, ones digit (0-9)
    // second, tens digit (0-5)
    // second, ones digit (0-9)
    //
    public sealed class ThunderClockSlotDevice : SlotRomDevice
    {
        // TODO: fix this, it's shared all over the damned place
        private const int AppleClockSpeed = 1020484;

        private bool strobe;
        private bool clock;
        private bool irqEnabled;
        private bool irqAsserted;
        private bool timerEnabled;
        private int timerRate;
        private int ticks;

        private readonly ICpu cpu;
        private readonly IAppleBus bus;

        private readonly Stack<bool> bitBuffer = new Stack<bool>();

        public ThunderClockSlotDevice(int slot, ICpu cpu, IAppleBus bus, MachineState machineState)
            : base(slot, "ThunderClock Plus", cpu, bus, machineState)
        {
            ArgumentNullException.ThrowIfNull(cpu, nameof(cpu));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            this.cpu = cpu;
            this.bus = bus;

            // ROM is 2k on disk but only 0x700 bytes count. the first
            // 256 bytes are mirrored in the expansion rom
            var romBytes = File.ReadAllBytes("roms/thunderclock_plus.rom");

            HasRom = true;
            Rom = new byte[MemoryPage.PageSize];
            Array.Copy(romBytes, Rom, MemoryPage.PageSize);

            HasAuxRom = true;
            ExpansionRom = new byte[MemoryPage.ExpansionRomSize];
            Array.Copy(romBytes, ExpansionRom, MemoryPage.ExpansionRomSize);

            bus.AddDevice(this);
        }

        //
        // Reg 0: Command register
        // data in = 0x01
        // clock = 0x02
        // strobe = 0x04
        // register hold = 0x0
        // register shift = 0x08
        // time set = 0x010
        // time read = 0x018
        // Timer modes = 0x020 (64hz), 0x028 (256hz), 0x030 (2048hz)
        // Interrupt enable = 0x040 (IRQ assert is read as 0x020 in the status register)
        // data out = 0x080
        protected override byte DoIo(CardIoType ioType, ushort address, byte value)
        {
            if (ioType == CardIoType.Read && (byte)(address & 0x0F) == 0x00)
            {
                return (byte)(peekBit() | (irqAsserted ? 0x20 : 0x00));
            }

            if ((byte)(address & 0x0F) == 0x08)
            {
                irqAsserted = false;
                return 0x00;
            }
            else if ((byte)(address & 0x0F) != 0x00)
            {
                return 0x00;
            }

            bool isClock = (value & 0x02) != 0;
            bool isStrobe = (value & 0x04) != 0;
            bool isRead = (value & 0x18) != 0;

            if (isClock == false && clock)
            {
                bitBuffer?.Pop();
            }

            if (isStrobe == false && strobe && isRead)
            {
                // if (attemptYearPatch)
                // {
                //     _performProdosPatch();
                // }

                getTime();
            }

            timerEnabled = (value & 0x20) != 0;
            ticks = 0;

            if (timerEnabled)
            {
                switch (value & 0x38)
                {
                    case 0x20:
                        timerRate = AppleClockSpeed / 64;
                        break;
                    case 0x28:
                        timerRate = AppleClockSpeed / 256;
                        break;
                    case 0x30:
                        timerRate = AppleClockSpeed / 2048;
                        break;

                    default:
                        timerEnabled = false;
                        timerRate = 0;
                        break;
                }
            }
            else
            {
                timerRate = 0;
            }

            irqEnabled = (value & 0x40) != 0;
            clock = isClock;
            strobe = isStrobe;

            return 0x00;
        }

        public override bool HandlesRead(ushort address) =>
            address == IoBaseAddressLo;

        public override bool HandlesWrite(ushort address) =>
            address == IoBaseAddressLo;

        public override void Tick()
        {
            if (timerEnabled)
            {
                ticks++;
                if (ticks >= timerRate)
                {
                    ticks = 0;
                    irqAsserted = true;

                    if (irqEnabled)
                    {
                        cpu.InjectInterrupt(false);
                    }
                }
            }
        }

        public override void Reset()
        {
            irqAsserted = false;
            irqEnabled = false;
            ticks = 0;
            timerRate = 0;
        }

        private void getTime()
        {
            var now = DateTime.Now;

            clearBuffer();

            pushNibble(now.Month);
            pushNibble((int)now.DayOfWeek);
            pushNibble(now.Day / 10);
            pushNibble(now.Day % 10);
            pushNibble(now.Hour / 10);
            pushNibble(now.Hour % 10);
            pushNibble(now.Minute / 10);
            pushNibble(now.Minute % 10);
            pushNibble(now.Second / 10);
            pushNibble(now.Second % 10);
        }

        private void clearBuffer()
        {
            bitBuffer.Clear();
        }

        private void pushNibble(int value)
        {
            for (var i = 0; i < 4; i++)
            {
                bool val = (value & 8) != 0;
                bitBuffer.Push(val);
                value <<= 1;
            }
        }

        private int peekBit()
        {
            if (bitBuffer.Count == 0)
            {
                return 0;
            }

            return bitBuffer.Peek() ? 0x80 : 0x00;
        }
    }
}

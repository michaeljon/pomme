using System;
using System.Collections.Generic;

namespace InnoWerks.Computers.Apple
{
    /// <summary>
    /// Dallas Semiconductor DS1215 No-Slot-Clock (NSC) emulation.
    /// <para>
    /// The NSC sits between a ROM chip and its socket, monitoring address
    /// lines for a specific 64-bit unlock sequence. Once unlocked, subsequent
    /// reads return clock data instead of ROM data.
    /// </para>
    /// <para>
    /// The unlock sequence is a series of 64 reads where bit 0 of the address
    /// matches the comparison pattern. The NSC watches $C100-$CFFF but only
    /// intercepts when the internal ROM is being read (controlled by soft switches).
    /// </para>
    /// <para>Detection/unlock protocol:</para>
    /// <ol>
    /// <li>Write the 64-bit comparison register (fixed pattern: $5C A3 3A C5 $5C A3 3A C5)</li>
    /// <li>Write the 64-bit comparison register again to unlock</li>
    /// <li>Read 64 bits of clock data (8 bytes, LSB first)</li>
    /// </ol>
    /// <para>Clock data format (8 bytes, BCD):</para>
    /// <ul>
    /// <li>Byte 0: hundredths of seconds</li>
    /// <li>Byte 1: seconds</li>
    /// <li>Byte 2: minutes</li>
    /// <li>Byte 3: hours (24-hour format)</li>
    /// <li>Byte 4: day of week (1=Sunday)</li>
    /// <li>Byte 5: day of month</li>
    /// <li>Byte 6: month</li>
    /// <li>Byte 7: year</li>
    /// </ul>
    /// </summary>
    public sealed class NoSlotClockDevice : IAddressInterceptDevice
    {
        private readonly MachineState machineState;

        // the 64-bit comparison/unlock pattern
        private const ulong ComparisonRegister = 0x5ca33ac55ca33ac5;

        // current state
        private int bitIndex;
        private bool unlocked;

        // clock data loaded when unlocked
        private ulong clockData;

        private readonly IAppleBus bus;

        public string Name => "No-Slot-Clock";

        public IReadOnlyList<AddressRange> AddressRanges { get; } =
        [
            new AddressRange(0xC100, 0xCFFF, MemoryAccessType.Any)
        ];

        public NoSlotClockDevice(MachineState machineState, IAppleBus bus)
        {
            ArgumentNullException.ThrowIfNull(machineState, nameof(machineState));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            this.bus = bus;
            this.machineState = machineState;

            bus.AddDevice(this);
        }

        public bool TryRead(ushort address, out byte value)
        {
            value = 0;

            if (ShouldIntercept(address) == false)
            {
                return false;
            }

            if (unlocked)
            {
                // return clock data one bit at a time in bit 0,
                // leaving the other bits from the underlying ROM
                value = (byte)((clockData >> bitIndex) & 1);
                bitIndex++;

                if (bitIndex >= 64)
                {
                    // all 64 bits read, lock again
                    unlocked = false;
                    bitIndex = 0;
                }

                return true;
            }

            // not unlocked — feed the address bit into the shift register
            // to check for the unlock sequence
            var addressBit = (ulong)(address & 1);
            var expectedBit = (ComparisonRegister >> bitIndex) & 1;

            if (addressBit == expectedBit)
            {
                bitIndex++;

                if (bitIndex >= 64)
                {
                    // full comparison match — unlock and load clock data
                    unlocked = true;
                    bitIndex = 0;
                    clockData = LoadClockData();
                }
            }
            else
            {
                // mismatch — reset the sequence
                bitIndex = 0;
            }

            // not intercepting the read — let normal ROM read proceed
            return false;
        }

        public bool TryWrite(ushort address, byte value)
        {
            if (!ShouldIntercept(address))
            {
                return false;
            }

            // writes also feed the comparison sequence via address bit 0
            var addressBit = (ulong)(address & 1);
            var expectedBit = (ComparisonRegister >> bitIndex) & 1;

            if (addressBit == expectedBit)
            {
                bitIndex++;

                if (bitIndex >= 64)
                {
                    unlocked = true;
                    bitIndex = 0;
                    clockData = LoadClockData();
                }
            }
            else
            {
                bitIndex = 0;
            }

            // don't consume the write — let it pass through
            return false;
        }

        public void Tick() { }

        public void Reset()
        {
            unlocked = false;
            bitIndex = 0;
            clockData = 0;
        }

        private bool ShouldIntercept(ushort address)
        {
            // internal CX ROM covers everything
            if (machineState.State[SoftSwitch.IntCxRomEnabled] == true)
            {
                return true;
            }

            // $C300-$C3FF when slot 3 ROM is disabled (showing internal ROM)
            if (address >= 0xC300 && address <= 0xC3FF &&
                !machineState.State[SoftSwitch.SlotC3RomEnabled] == true)
            {
                return true;
            }

            // $C800-$CFFF when internal C8 ROM is enabled
            if (address >= 0xC800 && address <= 0xCFFF &&
                machineState.State[SoftSwitch.IntC8RomEnabled] == true)
            {
                return true;
            }

            return false;
        }

        private ulong LoadClockData()
        {
            var now = DateTime.Now;

            ulong data = 0;
            data |= ToBcd(0);                               // byte 0: hundredths
            data |= (ulong)ToBcd(now.Second) << 8;          // byte 1: seconds
            data |= (ulong)ToBcd(now.Minute) << 16;         // byte 2: minutes
            data |= (ulong)ToBcd(now.Hour) << 24;           // byte 3: hours (24h)
            data |= (ulong)ToBcd((int)now.DayOfWeek + 1) << 32;  // byte 4: day of week (1=Sun)
            data |= (ulong)ToBcd(now.Day) << 40;            // byte 5: day of month
            data |= (ulong)ToBcd(now.Month) << 48;          // byte 6: month
            data |= (ulong)ToBcd(now.Year % 100) << 56;     // byte 7: year

            if (ProDOSClockPatcher.PerformProDOSPatch)
            {
                ProDOSClockPatcher.PatchClockForProDOS(bus);
            }

            return data;
        }

        private static byte ToBcd(int value)
        {
            return (byte)(((value / 10) << 4) | (value % 10));
        }
    }
}

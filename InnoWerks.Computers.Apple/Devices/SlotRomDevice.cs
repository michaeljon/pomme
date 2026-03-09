using System;
using System.Reflection.PortableExecutable;
using InnoWerks.Processors;
using InnoWerks.Simulators;
using Microsoft.VisualBasic;

namespace InnoWerks.Computers.Apple
{
    public enum CardIoType
    {
        Read,
        Write
    }

    // Core soft switches
    //   $C000..C07F On Board Resources
    //
    // Slot soft switches
    //   $C080..C08F Slot 0 /DEVSEL area (16 byte register file)
    //   $C090..C09F Slot 1 /DEVSEL area
    //   ... repeated for Slot 2..6
    //   $C0F0..C0FF Slot 7 /DEVSEL
    //
    // Slot ROM
    //   $C100..C1FF Slot 1 /IOSEL area (256 bytes 'PROM')
    //   ... repeated for Slot 2..6
    //   $C700..C7FF Slot 7 /IOSEL area
    //
    // Shared ROM addresses
    //   $C800..CFFF Common area for all Slots (2 KiB 'ROM')

#pragma warning disable CA1716, CA1707, CA1822
    public abstract class SlotRomDevice : ISlotDevice
    {
        private readonly ICpu cpu;

        private readonly IBus bus;

        public const ushort IO_BASE_ADDR = 0xC080;

        public const ushort ROM_BASE_ADDR = 0xC000;

        public const ushort EXPANSION_ROM_BASE_ADDR = 0xC800;

        protected MachineState machineState { get; }

#pragma warning disable CA1819 // Properties should not return arrays
        public byte[] Rom { get; init; }
        public byte[] ExpansionRom { get; init; }

#pragma warning restore CA1819 // Properties should not return arrays

        protected SlotRomDevice(int slot, string name, ICpu cpu, IBus bus, MachineState machineState)
            : this(slot, name, cpu, bus, machineState, null, null) { }

        protected SlotRomDevice(int slot, string name, ICpu cpu, IBus bus, MachineState machineState, byte[] romImage)
            : this(slot, name, cpu, bus, machineState, romImage, null) { }

        protected SlotRomDevice(int slot, string name, ICpu cpu, IBus bus, MachineState machineState, byte[] cxRom, byte[] c8Rom)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(slot, 7, nameof(slot));
            ArgumentOutOfRangeException.ThrowIfLessThan(slot, 0, nameof(slot));

            ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
            ArgumentNullException.ThrowIfNull(cpu, nameof(cpu));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));
            ArgumentNullException.ThrowIfNull(machineState, nameof(machineState));

            Slot = slot;
            Name = name;

            this.cpu = cpu;
            this.bus = bus;
            this.machineState = machineState;

            if (cxRom != null)
            {
                HasRom = true;
                Rom = new byte[256];
                Array.Copy(cxRom, 0, Rom, 0, cxRom.Length);
            }

            if (c8Rom != null)
            {
                HasAuxRom = true;
                ExpansionRom = new byte[2048];
                Array.Copy(c8Rom, 0, ExpansionRom, 0, 2048);
            }
        }

        public int Slot { get; }

        public string Name { get; }

        public abstract bool HandlesRead(ushort address);

        public abstract bool HandlesWrite(ushort address);

        protected abstract byte DoIo(CardIoType ioType, byte address, byte value);

        protected abstract byte DoCx(CardIoType ioType, ushort address, byte value);

        protected abstract byte DoC8(CardIoType ioType, ushort address, byte value);

        public byte Read(ushort address)
        {
            if (address >= IoBaseAddressLo && address <= IoBaseAddressHi)
            {
                return DoIo(CardIoType.Read, (byte)(address & 0x0F), 0x00);
            }
            else if (address >= RomBaseAddressLo && address <= RomBaseAddressHi)
            {
                machineState.CurrentSlot = Slot;

                if (machineState.State[SoftSwitch.IntCxRomEnabled] == false)
                {
                    // return value from rom
                    return DoCx(CardIoType.Read, address, 0x00);
                }
            }
            else if (address >= ExpansionBaseAddressLo && address <= ExpansionBaseAddressHi)
            {
                if (machineState.State[SoftSwitch.IntCxRomEnabled] == false || machineState.State[SoftSwitch.IntC8RomEnabled] == false)
                {
                    // return value from rom
                    return DoC8(CardIoType.Read, address, 0x00);
                }
            }

            return machineState.FloatingValue;
        }

        public bool Write(ushort address, byte value)
        {
            if (address >= IoBaseAddressLo && address <= IoBaseAddressHi)
            {
                DoIo(CardIoType.Write, (byte)(address & 0x0F), value);
                return false;
            }
            else if (address >= RomBaseAddressLo && address <= RomBaseAddressHi)
            {
                machineState.CurrentSlot = Slot;

                if (machineState.State[SoftSwitch.IntCxRomEnabled] == false)
                {
                    // write to rom
                    DoCx(CardIoType.Write, address, value);
                }
            }
            else if (address >= ExpansionBaseAddressLo && address <= ExpansionBaseAddressHi)
            {
                if (machineState.State[SoftSwitch.IntCxRomEnabled] == false || machineState.State[SoftSwitch.IntC8RomEnabled] == false)
                {
                    // write to rom
                    DoC8(CardIoType.Write, address, value);
                }
            }

            return false;
        }

        public abstract void Tick(int cycles);

        public abstract void Reset();

        public bool HasRom { get; init; }

        public bool HasAuxRom { get; init; }

        // 16 bytes bytes
        protected ushort IoBaseAddressLo => (ushort)(IO_BASE_ADDR + (Slot * 0x10));

        protected ushort IoBaseAddressHi => (ushort)(IO_BASE_ADDR + (Slot * 0x10) + 0x0F);

        // 256 bytes
        protected ushort RomBaseAddressLo => (ushort)(ROM_BASE_ADDR + (Slot * 0x100));

        protected ushort RomBaseAddressHi => (ushort)(ROM_BASE_ADDR + (Slot * 0x100) + 0xFF);

        // 2048 bytes
        protected ushort ExpansionBaseAddressLo => EXPANSION_ROM_BASE_ADDR;

        protected ushort ExpansionBaseAddressHi => EXPANSION_ROM_BASE_ADDR + 0x7FF;

        protected virtual bool IsIoReadRequest(ushort address)
        {
            SimDebugger.Info("Slot {0} IsIoReadRequest({1:X4})\n", Slot, address);

            return IoBaseAddressLo <= address && address <= IoBaseAddressHi;
        }

        protected virtual bool IsRomReadRequest(ushort address)
        {
            SimDebugger.Info("Slot {0} IsRomReadRequest({1:X4})\n", Slot, address);

            if (Slot > 0 && Slot <= 4)
            {
                // allow for expansion rom
                return (RomBaseAddressLo <= address && address <= RomBaseAddressHi) || (ExpansionBaseAddressLo <= address && address <= ExpansionBaseAddressHi);
            }
            else
            {
                // this is just a regular rom read
                return RomBaseAddressLo <= address && address <= RomBaseAddressHi;
            }
        }
    }
#pragma warning restore CA1716, CA1707, CA1822
}

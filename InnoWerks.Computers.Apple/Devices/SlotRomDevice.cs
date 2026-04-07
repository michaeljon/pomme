using System;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
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

#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CA1822 // Mark members as static

    public abstract class SlotRomDevice : ISlotDevice
    {
        public const ushort IO_BASE_ADDR = 0xC080;

        public const ushort ROM_BASE_ADDR = 0xC000;

        public const ushort EXPANSION_ROM_BASE_ADDR = 0xC800;

        protected MachineState machineState { get; }

        public byte[] Rom { get; init; }
        public byte[] ExpansionRom { get; init; }

        protected SlotRomDevice(int slot, string name, Computer computer)
            : this(slot, name, computer, null, null) { }

        protected SlotRomDevice(int slot, string name, Computer computer, byte[] romImage)
            : this(slot, name, computer, romImage, null) { }

        protected SlotRomDevice(int slot, string name, Computer computer, byte[] cxRom, byte[] c8Rom)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(slot, 7, nameof(slot));
            ArgumentOutOfRangeException.ThrowIfLessThan(slot, 0, nameof(slot));

            ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

            ArgumentNullException.ThrowIfNull(computer, nameof(computer));

            Slot = slot;
            Name = name;

            machineState = computer.MachineState;

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

        /// <summary>
        /// Returns true if the device actively handles reads at this address.
        /// Default claims $C0n0-$C0nF only. Override to claim $Cn00 or $C800
        /// ranges for devices with active hardware in those spaces.
        /// </summary>
        public virtual bool HandlesRead(ushort address) =>
            address >= IoBaseAddressLo && address <= IoBaseAddressHi;

        /// <summary>
        /// Returns true if the device actively handles writes at this address.
        /// Default claims $C0n0-$C0nF only.
        /// </summary>
        public virtual bool HandlesWrite(ushort address) =>
            address >= IoBaseAddressLo && address <= IoBaseAddressHi;

        protected abstract byte DoIo(MemoryAccessType ioType, ushort address, byte value);

        /// <summary>
        /// Handles access in the $Cn00-$CnFF slot ROM range. Default serves ROM bytes.
        /// Override for devices with active hardware in this range.
        /// </summary>
        protected virtual byte DoCx(MemoryAccessType ioType, ushort address, byte value)
        {
            if (ioType == MemoryAccessType.Read && Rom != null)
            {
                return Rom[address & 0xFF];
            }

            return machineState.FloatingValue;
        }

        /// <summary>
        /// Handles access in the $C800-$CFFF expansion ROM range. Default serves
        /// expansion ROM bytes. Override for devices with active hardware in this range.
        /// </summary>
        protected virtual byte DoC8(MemoryAccessType ioType, ushort address, byte value)
        {
            if (ioType == MemoryAccessType.Read && ExpansionRom != null)
            {
                return ExpansionRom[address - ExpansionBaseAddressLo];
            }

            return machineState.FloatingValue;
        }

        public byte Read(ushort address)
        {
            if (address >= IoBaseAddressLo && address <= IoBaseAddressHi)
            {
                return DoIo(MemoryAccessType.Read, address, 0x00);
            }
            else if (address >= RomBaseAddressLo && address <= RomBaseAddressHi)
            {
                return DoCx(MemoryAccessType.Read, address, 0x00);
            }
            else if (address >= ExpansionBaseAddressLo && address <= ExpansionBaseAddressHi)
            {
                // AppleBus verifies soft switch state before routing here
                return DoC8(MemoryAccessType.Read, address, 0x00);
            }

            return machineState.FloatingValue;
        }

        public void Write(ushort address, byte value)
        {
            if (address >= IoBaseAddressLo && address <= IoBaseAddressHi)
            {
                DoIo(MemoryAccessType.Write, address, value);
            }
            else if (address >= RomBaseAddressLo && address <= RomBaseAddressHi)
            {
                DoCx(MemoryAccessType.Write, address, value);
            }
            else if (address >= ExpansionBaseAddressLo && address <= ExpansionBaseAddressHi)
            {
                // AppleBus verifies soft switch state before routing here
                DoC8(MemoryAccessType.Write, address, value);
            }
        }

        public abstract void Tick();

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
    }
}

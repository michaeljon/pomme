using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
#pragma warning disable CA1819 // Properties should not return arrays

    public sealed class EmptySlotDevice : ISlotDevice
    {
        public int Slot { get; }

        public string Name { get; }

        public byte[] Rom { get; }

        // 16 bytes bytes
        private ushort IoBaseAddressLo => (ushort)(SlotRomDevice.IO_BASE_ADDR + (Slot * 0x10));

        private ushort IoBaseAddressHi => (ushort)(SlotRomDevice.IO_BASE_ADDR + (Slot * 0x10) + 0x0F);

        // 256 bytes
        private ushort RomBaseAddressLo => (ushort)(SlotRomDevice.ROM_BASE_ADDR + (Slot * 0x100));

        private ushort RomBaseAddressHi => (ushort)(SlotRomDevice.ROM_BASE_ADDR + (Slot * 0x100) + 0xFF);

        /// <summary>
        /// Returns true if the device actively handles reads at this address.
        /// Default claims $C0n0-$C0nF only. Override to claim $Cn00 or $C800
        /// ranges for devices with active hardware in those spaces.
        /// </summary>
        public bool HandlesRead(ushort address) =>
            address >= IoBaseAddressLo && address <= IoBaseAddressHi ||
            address >= RomBaseAddressLo && address <= RomBaseAddressHi;

        /// <summary>
        /// Returns true if the device actively handles writes at this address.
        /// Default claims $C0n0-$C0nF only.
        /// </summary>
        public bool HandlesWrite(ushort address) =>
            address >= IoBaseAddressLo && address <= IoBaseAddressHi ||
            address >= RomBaseAddressLo && address <= RomBaseAddressHi;

        public EmptySlotDevice(int slot)
        {
            Slot = slot;
            Name = $"Empty slot {slot} device";
        }

        // this should really just be reading from machineState
        public byte Read(ushort address) => MachineState.FloatingValue;

        public void Write(ushort address, byte value) { /* NO-OP */ }

        public void Tick() { /* NO-OP */ }

        public void Reset() { /* NO-OP */ }
    }
}

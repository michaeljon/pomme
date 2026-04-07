using System;
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

        /// <summary>
        /// Returns true if the device actively handles reads at this address.
        /// Default claims $C0n0-$C0nF only. Override to claim $Cn00 or $C800
        /// ranges for devices with active hardware in those spaces.
        /// </summary>
        public bool HandlesRead(ushort address) =>
            address >= IoBaseAddressLo && address <= IoBaseAddressHi;

        /// <summary>
        /// Returns true if the device actively handles writes at this address.
        /// Default claims $C0n0-$C0nF only.
        /// </summary>
        public bool HandlesWrite(ushort address) =>
            address >= IoBaseAddressLo && address <= IoBaseAddressHi;

        public EmptySlotDevice(int slot)
        {
            Slot = slot;
            Name = $"Empty slot {slot} device";

            Rom = new byte[MemoryPage.PageSize];
            Array.Fill(Rom, (byte)0xFF);
        }

        public byte Read(ushort address) => 0xFF;

        public void Write(ushort address, byte value) { /* NO-OP */ }

        public void Tick() { /* NO-OP */ }

        public void Reset() { /* NO-OP */ }
    }
}

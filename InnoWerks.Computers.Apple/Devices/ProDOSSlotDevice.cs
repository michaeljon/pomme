using System;
using System.IO;
using InnoWerks.Processors;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
    public sealed class ProDOSSlotDevice : SlotRomDevice
    {
        private enum Command
        {
            Status,
            Read,
            Write,
            Format
        }

        private const byte JMP = 0x4C;
        private const byte RTS = 0x60;

        private FileStream fileStream;

        public ProDOSSlotDevice(
            int slot,
            ICpu cpu,
            IBus bus,
            MachineState machineState)
            : base(slot, "ProDOS Controller", cpu, bus, machineState)
        {
            HasRom = true;
            Rom = new byte[256];
            for (var i = 0; i < 256; i++)
            {
                Rom[i] = 0xEA;    // NOP
            }

            // signature
            Rom[0] = RTS; Rom[1] = 0x20; Rom[2] = (byte)(0xC0 | slot);
            Rom[3] = 0x00;
            Rom[5] = 0x03;
            Rom[6] = 0x4C; Rom[7] = 0x10; Rom[8] = (byte)(0xC0 | slot);

            // capability
            Rom[0xFE] = 0x0F;

            // drive entry point
            Rom[0xFF] = 0x10;

            ArgumentNullException.ThrowIfNull(bus, nameof(bus));
            bus.AddDevice(this);
        }

        public void InsertDisk(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            fileStream?.Close();

            // todo: bullet-proof this, it's sloppy
            if (File.Exists(path) == false)
            {
                // create a new, empty disk, 64k blocks of 512 bytes == 32mb
                File.WriteAllBytes(path, new byte[65535 * 512]);
            }

            // todo: allow for R/O disks
            fileStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite);

            Rom[0] = JMP;
        }

        protected override byte DoIo(CardIoType ioType, byte address, byte value)
        {
            if (ioType == CardIoType.Read)
            {
                SimDebugger.Info($"Read slot {Slot} I/O address {address:X4}\n");
            }
            else if (ioType == CardIoType.Write)
            {
                SimDebugger.Info($"Write slot {Slot} I/O address {address:X4}\n");
            }

            return 0x00;
        }

        public override bool HandlesRead(ushort address) =>
            (address >= IoBaseAddressLo && address <= IoBaseAddressHi) ||
            (address >= RomBaseAddressLo && address <= RomBaseAddressHi);

        public override bool HandlesWrite(ushort address) =>
            (address >= IoBaseAddressLo && address <= IoBaseAddressHi);

        protected override byte DoCx(CardIoType ioType, ushort address, byte value)
        {
            if (ioType == CardIoType.Read)
            {
                SimDebugger.Info($"Read slot {Slot} ({address:X4}) returns {Rom[address & 0xFF]:X2}\n");

                return Rom[address & 0xFF];
            }
            else if (ioType == CardIoType.Write)
            {
                SimDebugger.Info($"Write slot {Slot} ({address:X4}) writes {value:X2}\n");
            }

            return 0x00;
        }

        protected override byte DoC8(CardIoType ioType, ushort address, byte value) { return 0x00; }

        public override void Tick(int cycles) {/* NO-OP */ }

        public override void Reset() { }

        private void ReadBlock(int block, Span<byte> buffer)
        {
            fileStream.Seek(block * 512, SeekOrigin.Begin);
            fileStream.ReadExactly(buffer);
        }

        private void WriteBlock(int block, Span<byte> buffer)
        {
            fileStream.Seek(block * 512, SeekOrigin.Begin);
            fileStream.Write(buffer);
        }
    }
}

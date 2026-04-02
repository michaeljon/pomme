using System;
using System.IO;
using InnoWerks.Assemblers;
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

        private const int NumberOfDrives = 4;
        private const ushort NumberOfBlocks = 65535;
        private const ushort BlockSize = 512;

        private readonly FileStream[] fileStream = new FileStream[NumberOfDrives];
        private readonly string[] drivePaths = new string[NumberOfDrives];
        private readonly long[] fileStreamLength = new long[NumberOfDrives];

        private const byte Success = 0x00;
        private const byte IOError = 0x27;
        private const byte NoDevice = 0x28;
        private const byte WriteProtected = 0x29;

        // private const byte DiskDrive = 0x3C;
        // private const byte SmartPort = 0x00;
        // private const ushort DriveTypeOffset = 0x07;

        private readonly string[] rom1 = [
            "COMMAND  EQU   $42",
            "UNIT     EQU   $43",
            "ADDRLO   EQU   $44",
            "ADDRHI   EQU   $45",
            "BLKLO    EQU   $46",
            "BLKHI    EQU   $47",
            "",
            "         LDX   #$20    ; Apple IIe looks for magic bytes $20, $00, $03.",
            "         LDA   #$00    ; These indicate a disk drive or SmartPort device.",
            "         LDX   #$03",
            "         LDA   #$3C    ; $3C=disk drive, $00=SmartPort",
            "",
            "         BIT   $CFFF   ; Trigger all peripheral cards to turn off expansion ROMs",
            "",
            "         LDA   #$01    ; ProDOS command code = READ",
            "         STA   COMMAND ; Store ProDOS command code",
            "         LDA   #$4C    ; JMP inst",
            "         STA   $07FD",
            "         LDA   #$C0    ; jump address",
            "         STA   $07FE",
            "         LDA   #$60    ; RTS inst",
            "         STA   $07FF",
            "         JSR   $07FF",
            "         TSX",
            "         LDA   $0100,X ; High byte of slot address",
            "         STA   $07FF   ; Store this for the high byte of our JMP command",
            "         ASL           ; Shift $Cs up to $s0 (e.g. $C5 -> $50)",
            "         ASL           ; We need this for the ProDOS unit number (below).",
            "         ASL           ; Format = bits DSSS0000",
            "         ASL           ; D = drive number (0), SSS = slot number (1-7)",
            "         STA   UNIT    ; Store ProDOS unit number here",
            "         LDA   #$08    ; Store block (512 bytes) at address $0800",
            "         STA   ADDRHI  ; Address high byte",
            "         LDA   #$00",
            "         STA   ADDRLO  ; Address low byte",
            "         STA   BLKLO   ; Block 0 low byte",
            "         STA   BLKHI   ; Block 0 high byte",
            "         JSR   $07FD   ; Read the block (will JMP to our driver and trigger it)",
            "         BCS   ERROR",
            "         LDA   #$0A    ; Store block (512 bytes) at address $0A00",
            "         STA   ADDRHI  ; Address high byte",
            "         LDA   #$01",
            "         STA   BLKLO   ; Block 1 low byte",
            "         JSR   $07FD   ; Read",
            "         BCS   ERROR",
            "         LDA   $0801   ; Should be nonzero",
            "         BEQ   ERROR",
            "         LDA   #$01    ; Should always be 1",
            "         CMP   $0800",
            "         BNE   ERROR",
            "         LDX   UNIT    ; ProDOS block 0 code wants ProDOS unit number in X",
            "         JMP   $0801   ; Continue reading the disk",
            "ERROR    JMP   $E000   ; Out to BASIC on error",
        ];

        private readonly string[] rom2 = [
            "         NOP           ; Hard drive driver address",
            "         BRA   DONE",
            "         TSX           ; SmartPort driver address",
            "         INX",
            "         INC   $0100,X",
            "         INC   $0100,X",
            "         INC   $0100,X",
            "DONE     BCS   ERR",
            "         LDA   #$00    ; Success",
            "         RTS",
            "ERR      LDA   #$27    ; I/O Error",
            "         RTS",
        ];

        public ProDOSSlotDevice(
            int slot,
            ICpu cpu,
            IAppleBus bus,
            MachineState machineState)
            : base(slot, "ProDOS Controller", cpu, bus, machineState)
        {
            ArgumentNullException.ThrowIfNull(cpu, nameof(cpu));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            HasRom = true;
            Rom = new byte[MemoryPage.PageSize];

            const ushort Origin = 0x0800;
            const byte EntryPoint = 0xC0;

            // todo: this really needs to be merged and have the addresses fixed
            var assembler = new Assembler(rom1, Origin);
            assembler.Assemble();
            Array.Copy(assembler.ObjectCode, Rom, assembler.ObjectCode.Length);

            assembler = new Assembler(rom2, Origin + EntryPoint);
            assembler.Assemble();
            Array.Copy(assembler.ObjectCode, 0, Rom, EntryPoint, assembler.ObjectCode.Length);

            // 0xFC - 0xFD - number of blocks, will be handled by STATUS

            // capability
            // bit 7 Medium is removable.
            // bit 6 Device is interruptable.
            // bit 5-4 Number of volumes on the device (0-3).
            // bit 3 The device supports formatting.
            // bit 2 The device can be written to.
            // bit 1 The device can be read from (must be on).
            // bit 0 The device's status can be read (must be on).
            Rom[0xFE] = 0b00111111;

            // todo ^^^^ make bits 5-4 mirror the number of drives installed

            // drive entry point
            Rom[0xFF] = EntryPoint;

            bus.AddDevice(this);

            cpu.AddIntercept(0xC000 + EntryPoint + slot * 0x100, HandleIntercept);

            /*
            cpu.AddIntercept(0xC000 + EntryPoint + 0x03 + slot * 0x100, (cpu, bus) =>
            {
                // assume we're good
                cpu.Registers.A = Success;
                cpu.Registers.Carry = false;

                var callAddr = (ushort)(bus.Read(Cpu6502Core.StackBase + cpu.Registers.StackPointer + 2) << 8 | bus.Read(Cpu6502Core.StackBase + cpu.Registers.StackPointer + 1));

                var command = bus.Read(callAddr);
                var paramList = (ushort)(bus.Read(callAddr + 3) << 8 | bus.Read(callAddr + 2));

                var unit = bus.Read(paramList + 1);
                var bufferAddr = (ushort)(bus.Read(paramList + 3) << 8 | bus.Read(paramList + 2));

                switch (command)
                {
                    case 0:
                        cpu.Registers.Carry = true;
                        break;

                    case 1:
                        if (bus.Read(paramList) != 3)
                        {
                            cpu.Registers.Carry = true;
                        }
                        else
                        {
                            var block = bus.Read(paramList + 6) << 16 |
                                        bus.Read(paramList + 5) << 8 |
                                        bus.Read(paramList + 4);

                            // todo: read the block
                        }
                        break;

                    case 2:
                        cpu.Registers.Carry = true;
                        break;

                    default:
                        cpu.Registers.Carry = true;
                        break;
                }
            });
            */
        }

        private bool HandleIntercept(ICpu cpu, IBus bus)
        {
            // assume we're good
            cpu.Registers.A = Success;
            cpu.Registers.Carry = false;

            var command = (Command)bus.Read(0x42);
            var rawUnit = bus.Read(0x43);

            var slot = (rawUnit >> 4) & 0x07;
            var unit = (rawUnit & 0x80) >> 7;

            if (slot != Slot)
            {
                // we've been asked to access the upper two drives
                unit += 2;
            }

            var bufferAddr = (ushort)(bus.Read(0x45) << 8 | bus.Read(0x44));
            var blockStart = (ushort)(bus.Read(0x47) << 8 | bus.Read(0x46));

            var mountedDrives = 0;
            for (var d = 0; d < NumberOfDrives; d++)
            {
                mountedDrives += string.IsNullOrEmpty(drivePaths[d]) ? 0 : 1;
            }

            // SimDebugger.Info($"command={command} rawUnit={rawUnit:X2} unit={unit} slot={slot} blockStart={blockStart:X4} bufferAddr={bufferAddr:X4} mountedDrives={mountedDrives}\n");

            if (unit > mountedDrives - 1)
            {
                cpu.Registers.Carry = true;
                cpu.Registers.A = NoDevice;
                return true;
            }

            switch (command)
            {
                case Command.Status:
                    if (string.IsNullOrEmpty(drivePaths[unit]))
                    {
                        cpu.Registers.X = 0;
                        cpu.Registers.Y = 0;
                        cpu.Registers.Carry = true;
                        cpu.Registers.A = NoDevice;

                        break;
                    }

                    var numberOfBlocks = (ushort)(fileStreamLength[unit] / BlockSize);

                    cpu.Registers.X = (byte)(numberOfBlocks & 0xFF);
                    cpu.Registers.Y = (byte)(numberOfBlocks >> 8);

                    break;

                case Command.Read:
                    if (fileStream[unit] == null || fileStream[unit].CanRead == false || fileStream[unit].CanSeek == false)
                    {
                        cpu.Registers.Carry = true;
                        cpu.Registers.A = IOError;
                    }
                    else
                    {
                        if (blockStart + BlockSize > fileStreamLength[unit])
                        {
                            cpu.Registers.Carry = true;
                            cpu.Registers.A = IOError;
                        }
                        else
                        {
                            var buffer = new byte[BlockSize];
                            fileStream[unit].Seek(blockStart * BlockSize, SeekOrigin.Begin);
                            fileStream[unit].ReadExactly(buffer);

                            CopyBlockToMemory(bus, bufferAddr, buffer);
                        }
                    }

                    break;

                case Command.Write:
                    if (fileStream[unit] == null || fileStream[unit].CanWrite == false || fileStream[unit].CanSeek == false)
                    {
                        cpu.Registers.Carry = true;
                        cpu.Registers.A = WriteProtected;
                    }
                    else
                    {
                        if (blockStart + BlockSize > fileStreamLength[unit])
                        {
                            cpu.Registers.Carry = true;
                            cpu.Registers.A = IOError;
                        }
                        else
                        {
                            var buffer = new byte[BlockSize];
                            CopyBlockFromMemory(bus, bufferAddr, buffer);

                            fileStream[unit].Seek(blockStart * BlockSize, SeekOrigin.Begin);
                            fileStream[unit].Write(buffer);
                        }
                    }

                    break;

                case Command.Format:
                    if (string.IsNullOrEmpty(drivePaths[unit]))
                    {
                        cpu.Registers.Carry = true;
                        cpu.Registers.A = NoDevice;
                    }
                    else
                    {
                        FormatDisk(unit);
                    }

                    break;
            }

            return true;
        }

        public void InsertDisk(string path, int drive)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            fileStream[drive]?.Close();
            fileStream[drive] = null;
            fileStreamLength[drive] = 0;

            drivePaths[drive] = path;
            if (File.Exists(path) == false)
            {
                FormatDisk(drive);
                return;
            }

            // todo: allow for R/O disks
            fileStream[drive] = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
            fileStreamLength[drive] = fileStream[drive].Length;
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

            return 0xFF;
        }

        public override bool HandlesRead(ushort address) =>
            address >= IoBaseAddressLo && address <= IoBaseAddressHi;

        public override bool HandlesWrite(ushort address) =>
            address >= IoBaseAddressLo && address <= IoBaseAddressHi;

        protected override byte DoCx(CardIoType ioType, ushort address, byte value) { return 0x00; }

        protected override byte DoC8(CardIoType ioType, ushort address, byte value) { return 0x00; }

        public override void Tick(int cycles) {/* NO-OP */ }

        public override void Reset() { }

        private static void CopyBlockToMemory(IBus bus, ushort bufferAddr, Span<byte> buffer)
        {
            for (var b = 0; b < BlockSize; b++)
            {
                bus.Write(bufferAddr + b, buffer[b]);
            }
        }

        private static void CopyBlockFromMemory(IBus bus, ushort bufferAddr, Span<byte> buffer)
        {
            for (var b = 0; b < BlockSize; b++)
            {
                buffer[b] = bus.Read(bufferAddr + b);
            }
        }

        private void FormatDisk(int drive)
        {
            // close any open file we might have
            fileStream[drive]?.Close();
            fileStreamLength[drive] = 0;

            // create a new, empty disk, 64k blocks of 512 bytes == 32mb
            File.WriteAllBytes(drivePaths[drive], new byte[NumberOfBlocks * BlockSize]);

            fileStream[drive] = File.Open(drivePaths[drive], FileMode.Open, FileAccess.ReadWrite);

            var block2 = new byte[512];

            block2[0] = 0xF1;           // volume header, name length 1
            block2[1] = (byte)'H';      // volume name

            block2[0x17] = 0x27;        // directory entry length
            block2[0x18] = 0x0D;        // entries per block

            block2[0x1B] = 0x06;        // bitmap pointer low
            block2[0x1C] = 0x00;        // bitmap pointer high

            block2[0x1D] = (byte)(NumberOfBlocks & 0xFF);
            block2[0x1E] = (byte)(NumberOfBlocks >> 8);

            fileStream[drive].Seek(2 * BlockSize, SeekOrigin.Begin);
            fileStream[drive].Write(block2);

            var block6 = new byte[512];
            block6[0x00] = 0b11111110;

            fileStream[drive].Seek(6 * BlockSize, SeekOrigin.Begin);
            fileStream[drive].Write(block6);

            drivePaths[drive] = drivePaths[drive];
            fileStreamLength[drive] = fileStream[drive].Length;
        }
    }
}

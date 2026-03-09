using System;
using System.IO;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
    public sealed class ProfileSlotDevice : SlotRomDevice
    {
        private const int BlockSize = 512;
        private readonly byte[] diskImage;

        private const int Blocks5MB = 9728;
        private const int Blocks10MB = 19456;

        private enum ProfileState
        {
            Idle,
            ReceivingCommand,
            ReceivingParams,
            DataPhaseRead,
            DataPhaseWrite,
            SendingStatus
        }

        private ProfileState state = ProfileState.Idle;

        private byte command;
        private readonly byte[] param = new byte[3];
        private int paramIndex;

        private uint currentBlock;

        private readonly byte[] dataBuffer = new byte[BlockSize];
        private int dataIndex;

        private readonly bool tenMbDrive;
        private string path;

        private byte status;

        // todo: fix this so it can live on any page
        private static readonly byte[] cxRom = [
            /* 00 */  0x4C, 0x10, 0xC5,                 // JMP $C510    ; Init
            /* 03 */  0x4C, 0x20, 0xC5,                 // JMP $C520    ; Driver entry
            /* 06 */  0x01, 0x00, 0x00, 0x00, 0x00,     //              ; prodos signature
            /* 0b */  0x20,                             //              ; device id
            /* 0c */  0x00, 0x00, 0x00, 0x00,           //              ; padding
            /* 10 */  0xAD, 0xFF, 0xC5,                 // LDA $C5FF    ; enable $C800
            /* 13 */  0x4C, 0xD7, 0xC8,                 // JMP $C8D7    ; jump to firmware init
            /* 16 */  0x00, 0x00, 0x00, 0x00, 0x00,
            /* 1b */  0x00, 0x00, 0x00, 0x00, 0x00,
            /* 20 */  0xAD, 0xFF, 0xC5,                 // LDA $C5FF    ; enable $C800
            /* 23 */  0x4C, 0xD7, 0xC8,                 // JMP $C8D7    ; jump to firmware entry
        ];

        public ProfileSlotDevice(
            int slot,
            ICpu cpu,
            IBus bus,
            MachineState machineState,
            byte[] c8Rom,
            bool tenMb = false)
            : base(slot, "Profile Controller", cpu, bus, machineState, cxRom, c8Rom)
        {
            tenMbDrive = tenMb;

            int blocks = tenMb ? Blocks10MB : Blocks5MB;
            diskImage = new byte[blocks * BlockSize];

            ArgumentNullException.ThrowIfNull(bus, nameof(bus));
            bus.AddDevice(this);
        }

        public void InsertDisk(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            this.path = path;

            // todo: bullet-proof this, it's sloppy
            if (File.Exists(path) == false)
            {
                // create a new, empty disk
                File.WriteAllBytes(path, new byte[tenMbDrive ? Blocks10MB * BlockSize : Blocks5MB * BlockSize]);
            }

            var contents = File.ReadAllBytes(path);
            Array.Copy(contents, 0, diskImage, 0, tenMbDrive ? Blocks10MB * BlockSize : Blocks5MB * BlockSize);
        }

        protected override byte DoIo(CardIoType ioType, byte address, byte value)
        {
            switch (address)
            {
                case 0x0: // Data register
                    return HandleData(ioType, value);

                case 0x1: // Status register
                    return status;

                case 0x2: // Command register
                    if (ioType == CardIoType.Write)
                    {
                        BeginCommand(value);
                    }
                    return 0x00;

                case 0x3: // Reset
                    if (ioType == CardIoType.Write)
                    {
                        ResetState();
                    }
                    return 0x00;

                default:
                    return 0x00;
            }
        }

        public override bool HandlesRead(ushort address) =>
            (address >= IoBaseAddressLo && address <= IoBaseAddressHi) ||
            (address >= RomBaseAddressLo && address <= RomBaseAddressHi);

        public override bool HandlesWrite(ushort address) =>
            (address >= IoBaseAddressLo && address <= IoBaseAddressHi);

        private void BeginCommand(byte cmd)
        {
            command = cmd;
            paramIndex = 0;
            dataIndex = 0;
            status = 0x80; // busy
            state = ProfileState.ReceivingParams;
        }

        private byte HandleData(CardIoType ioType, byte value)
        {
            switch (state)
            {
                case ProfileState.ReceivingParams:
                    if (ioType == CardIoType.Write)
                    {
                        param[paramIndex++] = value;

                        if (paramIndex == 3)
                        {
                            currentBlock =
                                (uint)((param[0] << 16) |
                                       (param[1] << 8) |
                                        param[2]);

                            ExecuteCommand();
                        }
                    }
                    break;

                case ProfileState.DataPhaseRead:
                    if (dataIndex < BlockSize)
                        return dataBuffer[dataIndex++];
                    break;

                case ProfileState.DataPhaseWrite:
                    if (ioType == CardIoType.Write && dataIndex < BlockSize)
                    {
                        dataBuffer[dataIndex++] = value;

                        if (dataIndex == BlockSize)
                        {
                            CommitWrite();
                        }
                    }
                    break;
            }

            return 0x00;
        }

        private void ExecuteCommand()
        {
            switch (command)
            {
                case 0x00: // READ
                    PrepareRead();
                    break;

                case 0x01: // WRITE
                    state = ProfileState.DataPhaseWrite;
                    break;

                case 0x02: // STATUS
                    status = 0x00;
                    state = ProfileState.Idle;
                    break;

                case 0x03: // FORMAT (stub)
                    Array.Clear(diskImage, 0, diskImage.Length);
                    status = 0x00;
                    state = ProfileState.Idle;
                    break;

                default:
                    status = 0x40; // error
                    state = ProfileState.Idle;
                    break;
            }
        }

        private void PrepareRead()
        {
            if (!IsBlockValid())
            {
                status = 0x40;
                state = ProfileState.Idle;
                return;
            }

            int offset = (int)(currentBlock * BlockSize);
            Array.Copy(diskImage, offset, dataBuffer, 0, BlockSize);

            dataIndex = 0;
            state = ProfileState.DataPhaseRead;
        }

        private void CommitWrite()
        {
            if (!IsBlockValid())
            {
                status = 0x40;
                state = ProfileState.Idle;
                return;
            }

            int offset = (int)(currentBlock * BlockSize);
            Array.Copy(dataBuffer, 0, diskImage, offset, BlockSize);

            status = 0x00;
            state = ProfileState.Idle;

            // save the disk contents to disk :)
            File.WriteAllBytes(path, dataBuffer);
        }

        private bool IsBlockValid()
        {
            return currentBlock < (diskImage.Length / BlockSize);
        }

        private void ResetState()
        {
            state = ProfileState.Idle;
            status = 0x00;
            paramIndex = 0;
            dataIndex = 0;
        }

        protected override byte DoCx(CardIoType ioType, ushort address, byte value) { return 0x00; }

        protected override byte DoC8(CardIoType ioType, ushort address, byte value) { return 0x00; }

        public override void Tick(int cycles) {/* NO-OP */ }

        public override void Reset()
        {
            ResetState();
        }
    }
}

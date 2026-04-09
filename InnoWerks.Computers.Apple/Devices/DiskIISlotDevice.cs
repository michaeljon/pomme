using System;
using System.IO;

namespace InnoWerks.Computers.Apple
{
#pragma warning disable CA1051, IDE1006

    public sealed class DiskIISlotDevice : SlotRomDevice
    {
        private readonly DiskIIDrive drive1 = new(1);
        private readonly DiskIIDrive drive2 = new(2);

        private DiskIIDrive currentDrive;
        private bool motorEnabled;

        /// <summary>
        /// Optional callback invoked when drive state changes (motor on/off,
        /// disk insert/eject, drive select). Parameters are (slot, driveNumber).
        /// </summary>
        public Action<int, int> OnDriveStateChanged { get; set; }

        public DiskIISlotDevice(
            int slot,
            Computer computer)
            : base(slot, "Disk II Controller", computer)
        {
            currentDrive = drive1;

            HasRom = true;
            Rom = new byte[MemoryPage.PageSize];

            var diskIIRom = File.ReadAllBytes("roms/DiskII.rom");
            Array.Copy(diskIIRom, Rom, diskIIRom.Length);
        }

        protected override byte DoIo(MemoryAccessType ioType, ushort address, byte value)
        {
            switch ((byte)(address & 0x0F))
            {
                case 0x0:
                case 0x1:
                case 0x2:
                case 0x3:
                case 0x4:
                case 0x5:
                case 0x6:
                case 0x7:
                    // step the head
                    currentDrive.Step(address, motorEnabled);
                    break;

                case 0x8:
                    // motor off
                    if (motorEnabled == true)
                    {
                        currentDrive.MotorOff();
                        motorEnabled = false;
                        NotifyStateChanged();
                    }
                    break;

                case 0x9:
                    // motor on
                    motorEnabled = true;
                    NotifyStateChanged();
                    break;

                case 0xA:
                    // choose drive 1
                    currentDrive = drive1;
                    break;

                case 0xB:
                    // choose drive 2
                    currentDrive = drive2;
                    break;

                case 0xC:
                    // read/write latch
                    currentDrive.Write(motorEnabled);
                    return currentDrive.ReadLatch(motorEnabled);

                case 0xD:
                    // set latch
                    // SimDebugger.Info($"DoIo{ioType} DiskII Set(${address:X1}, {value:X2})\n");
                    if (ioType == MemoryAccessType.Write)
                    {
                        currentDrive.SetLatchValue(value);
                    }
                    return currentDrive.ReadLatch(motorEnabled);

                case 0xE:
                    // read mode
                    currentDrive.SetReadMode();
                    if (currentDrive.DiskPresent && currentDrive.IsWriteProtected)
                    {
                        return 0x80;
                    }
                    else
                    {
                        return 0x00;
                    }

                case 0xF:
                    // write mode
                    currentDrive.SetWriteMode();
                    if (ioType == MemoryAccessType.Write)
                    {
                        currentDrive.SetLatchValue(value);
                    }
                    return currentDrive.ReadLatch(motorEnabled);
            }

            return 0xFF;
        }

        public override void Tick() {/* NO-OP */ }

        public override void Reset()
        {
            drive1.Reset();
            drive2.Reset();

            motorEnabled = false;
            currentDrive = drive1;
        }

        public bool IsMotorOn(int drive)
        {
            var driveObj = drive == 0 ? drive1 : drive2;
            return motorEnabled == true && currentDrive == driveObj;
        }

        public DiskIIDrive GetDrive(int drive)
        {
            if (drive < 0 || drive > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(drive), "DiskII Controller support Drive 0 and Drive 1 only");
            }

            return drive == 0 ? drive1 : drive2;
        }

        public void InsertDisk(int drive, string path)
        {
            GetDrive(drive).InsertDisk(path);
            OnDriveStateChanged?.Invoke(Slot, drive);
        }

        public void EjectDisk(int drive)
        {
            GetDrive(drive).EjectDisk();
            OnDriveStateChanged?.Invoke(Slot, drive);
        }

        public void FlushAll()
        {
            drive1.Flush();
            drive2.Flush();
        }

        private void NotifyStateChanged()
        {
            var driveNumber = currentDrive == drive1 ? 0 : 1;
            OnDriveStateChanged?.Invoke(Slot, driveNumber);
        }
    }
}

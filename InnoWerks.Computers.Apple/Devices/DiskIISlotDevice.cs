using System;
using System.IO;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
#pragma warning disable CA1051, IDE1006

    public sealed class DiskIISlotDevice : SlotRomDevice
    {
        private readonly DiskIIDrive drive1 = new(1);
        private readonly DiskIIDrive drive2 = new(2);

        DiskIIDrive currentDrive;

        /// <summary>
        /// Optional callback invoked when drive state changes (motor on/off,
        /// disk insert/eject, drive select). Parameters are (slot, driveNumber).
        /// </summary>
        public Action<int, int> OnDriveStateChanged { get; set; }

        public DiskIISlotDevice(
            int slot,
            ICpu cpu,
            IAppleBus bus,
            MachineState machineState)
            : base(slot, "Disk II Controller", cpu, bus, machineState)
        {
            ArgumentNullException.ThrowIfNull(cpu, nameof(cpu));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            currentDrive = drive1;

            HasRom = true;
            Rom = new byte[MemoryPage.PageSize];

            var diskIIRom = File.ReadAllBytes("roms/DiskII.rom");
            Array.Copy(diskIIRom, Rom, diskIIRom.Length);

            bus.AddDevice(this);
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
                    // SimDebugger.Info($"DoIo{ioType} DiskII Step(${address:X1}, {value:X2})\n");
                    currentDrive.Step(address);
                    break;

                case 0x8:
                    // drive off
                    currentDrive.SetOn(false);
                    NotifyStateChanged();
                    break;

                case 0x9:
                    // drive on
                    currentDrive.SetOn(true);
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
                    currentDrive.Write();
                    return currentDrive.ReadLatch();

                case 0xD:
                    // set latch
                    // SimDebugger.Info($"DoIo{ioType} DiskII Set(${address:X1}, {value:X2})\n");
                    if (ioType == MemoryAccessType.Write)
                    {
                        currentDrive.SetLatchValue(value);
                    }
                    return currentDrive.ReadLatch();

                case 0xE:
                    // read mode
                    // SimDebugger.Info($"DoIo{ioType} DiskII Rmode(${address:X1}, {value:X2})\n");
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
                    // SimDebugger.Info($"DoIo{ioType} DiskII Wmode(${address:X1}, {value:X2})\n");
                    currentDrive.SetWriteMode();
                    // set latch
                    if (ioType == MemoryAccessType.Write)
                    {
                        currentDrive.SetLatchValue(value);
                    }
                    return currentDrive.ReadLatch();
            }

            return 0xFF;
        }

        public override void Tick() {/* NO-OP */ }

        public override void Reset()
        {
            drive1.Reset();
            drive2.Reset();

            currentDrive = drive1;
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

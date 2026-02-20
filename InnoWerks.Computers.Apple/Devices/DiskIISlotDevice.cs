using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using InnoWerks.Processors;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
#pragma warning disable CA1051, IDE1006

    public sealed class DiskIISlotDevice : SlotRomDevice
    {
        private readonly DiskIIDrive drive1 = new(1);
        private readonly DiskIIDrive drive2 = new(2);

        DiskIIDrive currentDrive;

        public DiskIISlotDevice(IBus bus, MachineState machineState, byte[] romImage)
            : base(6, "Disk II Controller", bus, machineState, romImage)
        {
            currentDrive = drive1;
        }

        protected override byte DoIo(CardIoType ioType, byte address, byte value)
        {
            // SimDebugger.Info($"DoIo{ioType} DiskII(${address:X1}, {value:X2})\n");

            switch (address)
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
                    // SimDebugger.Info($"DoIo{ioType} DiskII Off(${address:X1}, {value:X2})\n");
                    currentDrive.SetOn(false);
                    break;

                case 0x9:
                    // drive on
                    // SimDebugger.Info($"DoIo{ioType} DiskII On(${address:X1}, {value:X2})\n");
                    currentDrive.SetOn(true);
                    break;

                case 0xA:
                    // choose drive 1
                    // SimDebugger.Info($"DoIo{ioType} DiskII D1(${address:X1}, {value:X2})\n");
                    currentDrive = drive1;
                    break;

                case 0xB:
                    // choose drive 2
                    // SimDebugger.Info($"DoIo{ioType} DiskII D2(${address:X1}, {value:X2})\n");
                    currentDrive = drive2;
                    break;

                case 0xC:
                    // read/write latch
                    currentDrive.Write();
                    return currentDrive.ReadLatch();

                case 0xD:
                    // set latch
                    // SimDebugger.Info($"DoIo{ioType} DiskII Set(${address:X1}, {value:X2})\n");
                    if (ioType == CardIoType.Write)
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
                    if (ioType == CardIoType.Write)
                    {
                        currentDrive.SetLatchValue(value);
                    }
                    return currentDrive.ReadLatch();
            }

            return 0xFF;
        }

        protected override void DoCx(CardIoType ioType, byte address, byte value) { }

        protected override void DoC8(CardIoType ioType, byte address, byte value) { }

        public override void Tick(int cycles) {/* NO-OP */ }

        public override void Reset()
        {
            drive1.Reset();
            drive2.Reset();

            currentDrive = drive1;
        }

        public DiskIIDrive GetDrive(int drive)
        {
            if (drive < 1 || drive > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(drive), "DiskII Controller support Drive 1 and Drive 2 only");
            }

            return drive == 1 ? drive1 : drive2;
        }
    }
}

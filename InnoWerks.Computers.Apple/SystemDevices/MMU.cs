// #define DEBUG_C08X_HANDLER
// #define DEBUG_READ
// #define DEBUG_WRITE

using System;
using System.Collections.Generic;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
    public class MMU : ISoftSwitchDevice
    {
        private bool preWrite;

        private readonly IAppleBus bus;

        private readonly Memory128k memoryBlocks;

        private readonly MachineState machineState;

        private readonly HashSet<ushort> handlesRead =
        [
            //
            // LANGUAGE CARD
            //
            SoftSwitchAddress.RDLCBNK2,
            SoftSwitchAddress.RDLCRAM,

            SoftSwitchAddress.RDRAMRD,
            SoftSwitchAddress.RDRAMWRT,
            SoftSwitchAddress.RDALTSTKZP,
            SoftSwitchAddress.RD80STORE,
            SoftSwitchAddress.RDPAGE2,
            SoftSwitchAddress.RDHIRES,
            SoftSwitchAddress.RDDHIRES,

            //
            // I/O BANKING
            //
            SoftSwitchAddress.RDCXROM,
            SoftSwitchAddress.RDC3ROM,

            //           BSR2                              BSR1
            0xC080, 0xC081, 0xC082, 0xC083,    0xC088, 0xC089, 0xC08A, 0xC08B,
            0xC084, 0xC085, 0xC086, 0xC087,    0xC08C, 0xC08D, 0xC08E, 0xC08F,
        ];

        private readonly HashSet<ushort> handlesWrite =
        [
            //
            // LANGUAGE CARD
            //
            SoftSwitchAddress.CLR80STORE,
            SoftSwitchAddress.SET80STORE,
            SoftSwitchAddress.RDMAINRAM,
            SoftSwitchAddress.RDCARDRAM,
            SoftSwitchAddress.WRMAINRAM,
            SoftSwitchAddress.WRCARDRAM,
            SoftSwitchAddress.CLRALSTKZP,
            SoftSwitchAddress.SETALTSTKZP,

            //
            // I/O BANKING
            //
            SoftSwitchAddress.SETSLOTCXROM,
            SoftSwitchAddress.SETINTCXROM,
            SoftSwitchAddress.SETINTC3ROM,
            SoftSwitchAddress.SETSLOTC3ROM,

            //           BSR2                              BSR1
            0xC080, 0xC081, 0xC082, 0xC083,    0xC088, 0xC089, 0xC08A, 0xC08B,
            0xC084, 0xC085, 0xC086, 0xC087,    0xC08C, 0xC08D, 0xC08E, 0xC08F,
        ];

        private const ushort LANG_A3 = 0b00001000;

        private const ushort LANG_A0A1 = 0b00000011;

        private const ushort LANG_A0 = 0b00000001;

        public string Name => $"MMU";

        public MMU(Memory128k memoryBlocks, MachineState machineState, IAppleBus bus)
        {
            ArgumentNullException.ThrowIfNull(machineState, nameof(machineState));
            ArgumentNullException.ThrowIfNull(memoryBlocks, nameof(memoryBlocks));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            this.machineState = machineState;
            this.memoryBlocks = memoryBlocks;
            this.bus = bus;

            bus.AddDevice(this);
        }

        public bool HandlesRead(ushort address)
            => handlesRead.Contains(address);

        public bool HandlesWrite(ushort address)
            => handlesWrite.Contains(address);

        public byte Read(ushort address)
        {
#if DEBUG_READ
            if (address < 0xC090)
            {
                SimDebugger.Info($"Read MMU({address:X4}) [{SoftSwitchAddress.LookupAddress(address)}]\n");
            }
#endif

            switch (address)
            {
                //
                // LANGUAGE CARD
                //

                case SoftSwitchAddress.RDLCBNK2: return (byte)(machineState.State[SoftSwitch.LcBank2] ? 0x80 : 0x00);
                case SoftSwitchAddress.RDLCRAM: return (byte)(machineState.State[SoftSwitch.LcReadEnabled] ? 0x80 : 0x00);

                case SoftSwitchAddress.RDRAMRD: return (byte)(machineState.State[SoftSwitch.AuxRead] ? 0x80 : 0x00);
                case SoftSwitchAddress.RDRAMWRT: return (byte)(machineState.State[SoftSwitch.AuxWrite] ? 0x80 : 0x00);
                case SoftSwitchAddress.RDALTSTKZP: return (byte)(machineState.State[SoftSwitch.ZpAux] ? 0x80 : 0x00);
                case SoftSwitchAddress.RD80STORE: return (byte)(machineState.State[SoftSwitch.Store80] ? 0x80 : 0x00);
                case SoftSwitchAddress.RDPAGE2: return (byte)(machineState.State[SoftSwitch.Page2] ? 0x80 : 0x00);
                case SoftSwitchAddress.RDHIRES: return (byte)(machineState.State[SoftSwitch.HiRes] ? 0x80 : 0x00);
                case SoftSwitchAddress.RDDHIRES: return (byte)(machineState.State[SoftSwitch.DoubleHiRes] ? 0x80 : 0x00);

                case SoftSwitchAddress.RDCXROM: return (byte)(machineState.State[SoftSwitch.IntCxRomEnabled] ? 0x80 : 0x00);
                case SoftSwitchAddress.RDC3ROM: return (byte)(machineState.State[SoftSwitch.SlotC3RomEnabled] ? 0x80 : 0x00);
            }

            if (address >= 0xC080 && address <= 0xC08F)
            {
                HandleReadC08x(address);
            }

            return 0x00;
        }

        public void Write(ushort address, byte value)
        {
#if DEBUG_WRITE
            if (address < 0xC090)
            {
                SimDebugger.Info($"Write MMU({address:X4}, {value:X2}) [{SoftSwitchAddress.LookupAddress(address)}]\n");
            }
#endif

            switch (address)
            {
                //
                // LANGUAGE CARD
                //
                case SoftSwitchAddress.CLR80STORE:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.Store80, false);
                    return;
                case SoftSwitchAddress.SET80STORE:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.Store80, true);
                    return;

                case SoftSwitchAddress.RDMAINRAM:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.AuxRead, false);
                    return;
                case SoftSwitchAddress.RDCARDRAM:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.AuxRead, true);
                    return;

                case SoftSwitchAddress.WRMAINRAM:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.AuxWrite, false);
                    return;
                case SoftSwitchAddress.WRCARDRAM:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.AuxWrite, true);
                    return;

                case SoftSwitchAddress.CLRALSTKZP:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.ZpAux, false);
                    return;
                case SoftSwitchAddress.SETALTSTKZP:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.ZpAux, true);
                    return;

                //
                // I/O BANKING
                //
                case SoftSwitchAddress.SETSLOTCXROM:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.IntCxRomEnabled, false);
                    return;
                case SoftSwitchAddress.SETINTCXROM:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.IntCxRomEnabled, true);
                    return;

                case SoftSwitchAddress.SETINTC3ROM:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.SlotC3RomEnabled, false);
                    return;
                case SoftSwitchAddress.SETSLOTC3ROM:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.SlotC3RomEnabled, true);
                    return;
            }

            if (0xC080 <= address && address <= 0xC08F)
            {
                HandleWriteC08x(address, value);
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(address), $"Write {address:X4} is not supported in this device");
        }

        public void Tick() { /* NO-OP */ }

        public void Reset()
        {
            // bank 2 is the primary bank
            machineState.State[SoftSwitch.LcBank2] = true;
        }

        private void HandleReadC08x(ushort address)
        {
#if DEBUG_C08X_HANDLER
            var entryState = "";

            entryState += machineState.State[SoftSwitch.LcBank2] ? "b=2," : "b=1,";
            entryState += machineState.State[SoftSwitch.LcReadEnabled] ? "r=1," : "r=0,";
            entryState += machineState.State[SoftSwitch.LcWriteEnabled] ? "w=1," : "w=0,";
            entryState += $"p={(preWrite ? 1 : 0)}";
#endif

            var lcBank2 = machineState.State[SoftSwitch.LcBank2];
            var lcReadEnabled = machineState.State[SoftSwitch.LcReadEnabled];
            var lcWriteEnabled = machineState.State[SoftSwitch.LcWriteEnabled];

            // Bank select
            machineState.State[SoftSwitch.LcBank2] = (address & LANG_A3) == 0;

            // Read enable
            int low = address & LANG_A0A1;
            machineState.State[SoftSwitch.LcReadEnabled] = low == 0 || low == 3;

            // Write enable sequencing (critical)
            if ((address & LANG_A0) == 1)
            {
                if (preWrite == true)
                {
                    machineState.State[SoftSwitch.LcWriteEnabled] = true;
                }
                else
                {
                    preWrite = true;
                }
            }
            else
            {
                preWrite = false;
                machineState.State[SoftSwitch.LcWriteEnabled] = false;
            }

#if DEBUG_C08X_HANDLER
            var exitState = "";

            exitState += machineState.State[SoftSwitch.LcBank2] ? "b=2," : "b=1,";
            exitState += machineState.State[SoftSwitch.LcReadEnabled] ? "r=1," : "r=0,";
            exitState += machineState.State[SoftSwitch.LcWriteEnabled] ? "w=1," : "w=0,";
            exitState += $"p={(preWrite ? 1 : 0)}";

            SimDebugger.Info($"Read MMU({address:X4}) entry: {entryState} exit: {exitState}\n");
#endif

            if (lcBank2 != machineState.State[SoftSwitch.LcBank2] ||
                lcReadEnabled != machineState.State[SoftSwitch.LcReadEnabled] ||
                lcWriteEnabled != machineState.State[SoftSwitch.LcWriteEnabled])
            {
                memoryBlocks.Remap();
            }
        }

        private void HandleWriteC08x(ushort address, byte value)
        {
#if DEBUG_C08X_HANDLER
            var entryState = "";

            entryState += machineState.State[SoftSwitch.LcBank2] ? "b=2," : "b=1,";
            entryState += machineState.State[SoftSwitch.LcReadEnabled] ? "r=1," : "r=0,";
            entryState += machineState.State[SoftSwitch.LcWriteEnabled] ? "w=1," : "w=0,";
            entryState += $"p={(preWrite ? 1 : 0)}";
#endif

            var lcBank2 = machineState.State[SoftSwitch.LcBank2];
            var lcReadEnabled = machineState.State[SoftSwitch.LcReadEnabled];

            preWrite = false;

            // Bank select
            machineState.State[SoftSwitch.LcBank2] = (address & LANG_A3) == 0;

            // Read enable
            int low = address & LANG_A0A1;
            machineState.State[SoftSwitch.LcReadEnabled] = low == 0 || low == 3;

#if DEBUG_C08X_HANDLER
            var exitState = "";

            exitState += machineState.State[SoftSwitch.LcBank2] ? "b=2," : "b=1,";
            exitState += machineState.State[SoftSwitch.LcReadEnabled] ? "r=1," : "r=0,";
            exitState += machineState.State[SoftSwitch.LcWriteEnabled] ? "w=1," : "w=0,";
            exitState += $"p={(preWrite ? 1 : 0)}";

            SimDebugger.Info($"Write MMU({address:X4}) entry: {entryState} exit: {exitState}\n");
#endif

            if (lcBank2 != machineState.State[SoftSwitch.LcBank2] ||
                   lcReadEnabled != machineState.State[SoftSwitch.LcReadEnabled])
            {
                memoryBlocks.Remap();
            }
        }
    }
}

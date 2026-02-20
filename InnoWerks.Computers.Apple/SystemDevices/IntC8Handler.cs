// #define DEBUG_READ
// #define DEBUG_WRITE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using InnoWerks.Processors;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
    public class IntC8Handler : ISoftSwitchDevice
    {
        private readonly IBus bus;

        private readonly Memory128k memoryBlocks;

        private readonly MachineState machineState;

        public string Name => $"IntC8Handler";

        public IntC8Handler(Memory128k memoryBlocks, MachineState machineState, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(machineState, nameof(machineState));
            ArgumentNullException.ThrowIfNull(memoryBlocks, nameof(memoryBlocks));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            this.machineState = machineState;
            this.memoryBlocks = memoryBlocks;
            this.bus = bus;
        }

        public bool HandlesRead(ushort address) =>
            (address >= 0xC300 && address <= 0xC3FF) || address == 0xCFFF;

        public bool HandlesWrite(ushort address) =>
            (address >= 0xC300 && address <= 0xC3FF) || address == 0xCFFF;

        public byte Read(ushort address)
        {
#if DEBUG_READ
            SimDebugger.Info($"Read IntC8Handler({address:X4})\n");
#endif

            if (address >= 0xC300 && address <= 0xC3FF)
            {
                if (machineState.State[SoftSwitch.SlotC3RomEnabled] == false)
                {
                    var pre = machineState.State[SoftSwitch.IntC8RomEnabled];
                    machineState.State[SoftSwitch.IntC8RomEnabled] = true;

                    // only remap if we've changed the value
                    if (pre == false)
                    {
                        memoryBlocks.Remap();
                    }
                }
            }
            else if (address == 0xCFFF)
            {
                var pre = machineState.State[SoftSwitch.IntC8RomEnabled];
                machineState.State[SoftSwitch.IntC8RomEnabled] = false;

                // only remap if we've changed the value
                if (pre == true)
                {
                    memoryBlocks.Remap();
                }
            }

            return memoryBlocks.Read(address);
        }

        public void Write(ushort address, byte value)
        {
#if DEBUG_WRITE
            SimDebugger.Info($"Write IOU({address:X4}, {value:X2}) [{SoftSwitchAddress.LookupAddress(address)}]\n");
#endif

            if (address >= 0xC300 && address <= 0xC3FF)
            {
                if (machineState.State[SoftSwitch.SlotC3RomEnabled] == false)
                {
                    var pre = machineState.State[SoftSwitch.IntC8RomEnabled];
                    machineState.State[SoftSwitch.IntC8RomEnabled] = true;

                    // only remap if we've changed the value
                    if (pre == false)
                    {
                        memoryBlocks.Remap();
                    }
                }
            }
            else if (address == 0xCFFF)
            {
                var pre = machineState.State[SoftSwitch.IntC8RomEnabled];
                machineState.State[SoftSwitch.IntC8RomEnabled] = false;

                // only remap if we've changed the value
                if (pre == true)
                {
                    memoryBlocks.Remap();
                }
            }

            memoryBlocks.Write(address, value);
        }

        public void Tick(int cycles) { /* NO-OP */ }

        public void Reset()
        {
            machineState.State[SoftSwitch.IntC8RomEnabled] = true;
            machineState.State[SoftSwitch.IntCxRomEnabled] = true;

            machineState.CurrentSlot = 0;
            machineState.ExpansionRomType = ExpansionRomType.ExpRomInternal;
        }
    }
}

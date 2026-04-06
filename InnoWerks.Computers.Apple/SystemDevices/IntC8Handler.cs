// #define DEBUG_READ
// #define DEBUG_WRITE

using System;
using System.Collections.Generic;

namespace InnoWerks.Computers.Apple
{
    public class IntC8Handler : IAddressInterceptDevice
    {
        private readonly IAppleBus bus;

        private readonly Memory128k memoryBlocks;

        private readonly MachineState machineState;

        public string Name => $"IntC8Handler";

        public InterceptPriority InterceptPriority => InterceptPriority.SoftSwitch;

        public IntC8Handler(Memory128k memoryBlocks, MachineState machineState, IAppleBus bus)
        {
            ArgumentNullException.ThrowIfNull(machineState, nameof(machineState));
            ArgumentNullException.ThrowIfNull(memoryBlocks, nameof(memoryBlocks));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            this.machineState = machineState;
            this.memoryBlocks = memoryBlocks;
            this.bus = bus;

            AddressRanges =
            [
                new (0xC300, 0xC3FF, MemoryAccessType.Any),
                new (0xCFFF, MemoryAccessType.Any),
            ];
        }

        public bool DoRead(ushort address, out byte value)
        {
#if DEBUG_READ
            SimDebugger.Info($"Read IntC8Handler({address:X4})\n");
#endif

            value = 0;

            // observe the access for soft switch side effects,
            // but don't intercept — let the normal routing handle the read
            HandleAccess(address);

            return false;
        }

        public bool DoWrite(ushort address, byte value)
        {
#if DEBUG_WRITE
            SimDebugger.Info($"Write IOU({address:X4}, {value:X2}) [{SoftSwitchAddress.LookupAddress(address)}]\n");
#endif

            // observe the access for soft switch side effects,
            // but don't intercept — let the normal routing handle the write
            HandleAccess(address);

            return false;
        }

        public IReadOnlyList<AddressRange> AddressRanges { get; init; }

        public void Tick() { /* NO-OP */ }

        public void Reset()
        {
            machineState.State[SoftSwitch.IntC8RomEnabled] = false;
            machineState.State[SoftSwitch.IntCxRomEnabled] = false;

            machineState.CurrentSlot = 0;
            machineState.ExpansionRomType = ExpansionRomType.ExpRomInternal;
        }

        private void HandleAccess(ushort address)
        {
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
        }
    }
}

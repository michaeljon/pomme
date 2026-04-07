// #define DEBUG_READ
// #define DEBUG_WRITE

using System;
using System.Collections.Generic;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
#pragma warning disable CA1819 // Properties should not return arrays

    public class SlotHandler : IAddressInterceptDevice
    {
        private readonly IAppleBus bus;

        private readonly Memory128k memoryBlocks;

        private readonly MachineState machineState;

        private readonly ISlotDevice[] slotDevices;

        public string Name => $"SlotHandler";

        public InterceptPriority InterceptPriority => InterceptPriority.SlotDevice;

        public bool ReportKeyboardLatchAll { get; set; } = true;

        public SlotHandler(Memory128k memoryBlocks, MachineState machineState, IAppleBus bus, ISlotDevice[] slotDevices)
        {
            ArgumentNullException.ThrowIfNull(memoryBlocks, nameof(memoryBlocks));
            ArgumentNullException.ThrowIfNull(machineState, nameof(machineState));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));
            ArgumentNullException.ThrowIfNull(slotDevices, nameof(slotDevices));

            this.machineState = machineState;
            this.memoryBlocks = memoryBlocks;
            this.bus = bus;
            this.slotDevices = slotDevices;

            AddressRanges =
            [
                new (0xC090, 0xC0FF, MemoryAccessType.Any),
                new (0xC100, 0xC2FF, MemoryAccessType.Any),
                new (0xC400, 0xC7FF, MemoryAccessType.Any),
                new (0xC800, 0xCFFF, MemoryAccessType.Any),
            ];
        }

        public bool DoRead(ushort address, out byte value)
        {
            value = 0xFF;

            if (address >= 0xC090 && address <= 0xC0FF)
            {
                var slot = (address >> 4) & 7;
                var slotDevice = slotDevices[slot];

                if (slotDevice?.HandlesRead(address) == true)
                {
                    value = slotDevice.Read(address);
                    return true;
                }
            }
            else if ((address >= 0xC100 && address <= 0xC2FF) || (address >= 0xC400 && address <= 0xC7FF))
            {
                // Any access to $Cn00-$CnFF selects this slot for subsequent
                // $C800-$CFFF expansion ROM routing, regardless of soft switch state
                var slot = (address >> 8) & 7;
                if (machineState.CurrentSlot != slot)
                {
                    machineState.CurrentSlot = slot;
                    memoryBlocks.Remap();
                }

                if (machineState.State[SoftSwitch.IntCxRomEnabled] == false)
                {
                    var slotDevice = slotDevices[slot];

                    if (slotDevice?.HandlesRead(address) == true)
                    {
                        value = slotDevice.Read(address);
                        return true;
                    }
                }
            }
            else if (address >= 0xC800 && address <= 0xCFFF)
            {
                if (machineState.State[SoftSwitch.IntCxRomEnabled] == false && machineState.State[SoftSwitch.IntC8RomEnabled] == false)
                {
                    var slot = machineState.CurrentSlot;
                    if (slot != 0)
                    {
                        var slotDevice = slotDevices[slot];

                        if (slotDevice?.HandlesRead(address) == true)
                        {
                            value = slotDevice.Read(address);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool DoWrite(ushort address, byte value)
        {
            if (address >= 0xC090 && address <= 0xC0FF)
            {
                var slot = (address >> 4) & 7;
                var slotDevice = slotDevices[slot];

                if (slotDevice?.HandlesWrite(address) == true)
                {
                    // SimDebugger.Info("Write IO {0} {1:X4}\n", slot, address);
                    slotDevice.Write(address, value);
                    return true;
                }
            }
            else if ((address >= 0xC100 && address <= 0xC2FF) || (address >= 0xC400 && address <= 0xC7FF))
            {
                var slot = (address >> 8) & 7;
                if (machineState.CurrentSlot != slot)
                {
                    machineState.CurrentSlot = slot;
                    memoryBlocks.Remap();
                }

                if (machineState.State[SoftSwitch.IntCxRomEnabled] == false)
                {
                    var slotDevice = slotDevices[slot];

                    if (slotDevice?.HandlesWrite(address) == true)
                    {
                        slotDevice.Write(address, value);
                        return true;
                    }
                }
            }
            else if (address >= 0xC800 && address <= 0xCFFF)
            {
                if (machineState.State[SoftSwitch.IntCxRomEnabled] == false && machineState.State[SoftSwitch.IntC8RomEnabled] == false)
                {
                    var slot = machineState.CurrentSlot;
                    if (slot != 0)
                    {
                        var slotDevice = slotDevices[slot];

                        if (slotDevice?.HandlesWrite(address) == true)
                        {
                            slotDevice.Write(address, value);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public IReadOnlyList<AddressRange> AddressRanges { get; init; }

        public void Tick()
        {
            for (var slot = 0; slot < slotDevices.Length; slot++)
            {
                slotDevices[slot]?.Tick();
            }
        }

        public void Reset()
        {
            for (var slot = 0; slot < slotDevices.Length; slot++)
            {
                slotDevices[slot]?.Reset();
            }
        }
    }
}

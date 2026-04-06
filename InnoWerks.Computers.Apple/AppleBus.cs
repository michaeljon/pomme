using System;
using System.Collections.Generic;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
#pragma warning disable CA1819

    public class AppleBus : IAppleBus
    {
        private readonly AppleConfiguration configuration;

        private int transactionCycles;

        private readonly Memory128k memoryBlocks;

        public ISlotDevice[] SlotDevices { get; } = new SlotRomDevice[8];

        private readonly List<IAddressInterceptDevice> interceptDevices = [];

        // 64K dispatch tables — each entry is a list of devices interested in that address
        private readonly List<IAddressInterceptDevice>[] readDispatch =
            new List<IAddressInterceptDevice>[ushort.MaxValue + 1];
        private readonly List<IAddressInterceptDevice>[] writeDispatch =
            new List<IAddressInterceptDevice>[ushort.MaxValue + 1];

        private readonly MachineState machineState;

        private bool reportKeyboardLatchAll = true;

        public AppleBus(AppleConfiguration configuration, Memory128k memoryBlocks, MachineState machineState)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(memoryBlocks);
            ArgumentNullException.ThrowIfNull(machineState);

            this.configuration = configuration;
            this.memoryBlocks = memoryBlocks;
            this.machineState = machineState;

            AddDevice(new IntC8Handler(memoryBlocks, machineState, this));
        }

        public void AddDevice(ISlotDevice slotDevice)
        {
            ArgumentNullException.ThrowIfNull(slotDevice, nameof(slotDevice));

            if (SlotDevices[slotDevice.Slot] != null)
            {
                throw new ArgumentException($"There is already a device {SlotDevices[slotDevice.Slot].Name} in slot {slotDevice.Slot}");
            }

            slotDevice.Reset();

            if (slotDevice is not SlotRomDevice slotRomDevice)
            {
                throw new ArgumentNullException($"Device being added implements ISlotDevice but is not a SlotRomDevice");
            }

            SlotDevices[slotDevice.Slot] = slotDevice;

            if (slotRomDevice.HasRom)
            {
                memoryBlocks.LoadSlotCxRom(slotDevice.Slot, slotRomDevice.Rom);
            }

            if (slotRomDevice.ExpansionRom != null)
            {
                memoryBlocks.LoadSlotC8Rom(slotDevice.Slot, slotRomDevice.ExpansionRom);
            }

            if (slotDevice.Slot == 1)
            {
                reportKeyboardLatchAll = false;
            }
        }

        public void AddDevice(IAddressInterceptDevice interceptDevice)
        {
            ArgumentNullException.ThrowIfNull(interceptDevice, nameof(interceptDevice));

            interceptDevice.Reset();
            interceptDevices.Add(interceptDevice);

            // populate dispatch tables from the device's address ranges,
            // maintaining priority order within each address entry
            foreach (var range in interceptDevice.AddressRanges)
            {
                foreach (var addr in range.GetAddresses(MemoryAccessType.Read))
                {
                    readDispatch[addr] ??= [];
                    InsertByPriority(readDispatch[addr], interceptDevice);
                }

                foreach (var addr in range.GetAddresses(MemoryAccessType.Write))
                {
                    writeDispatch[addr] ??= [];
                    InsertByPriority(writeDispatch[addr], interceptDevice);
                }
            }
        }

        public void BeginTransaction()
        {
            transactionCycles = 0;
        }

        public int EndTransaction()
        {
            return transactionCycles;
        }

        public ulong CycleCount { get; private set; }

        public void SetCpu(ICpu cpu)
        {
            ArgumentNullException.ThrowIfNull(cpu, nameof(cpu));
        }

        public byte Read(ushort address)
        {
            Tick();

            // O(1) lookup into the dispatch table for registered intercept devices
            var readers = readDispatch[address];
            if (readers != null)
            {
                foreach (var device in readers)
                {
                    if (device.DoRead(address, out var interceptValue))
                    {
                        return interceptValue;
                    }
                }
            }

            if (address >= 0xC000 && address <= 0xC08F)
            {
                return 0x00;
            }
            else if (address >= 0xC090 && address <= 0xC0FF)
            {
                var slot = (address >> 4) & 7;
                var slotDevice = SlotDevices[slot];

                if (slotDevice?.HandlesRead(address) == true)
                {
                    return slotDevice.Read(address);
                }

                return 0xFF;
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
                    var slotDevice = SlotDevices[slot];

                    if (slotDevice?.HandlesRead(address) == true)
                    {
                        return slotDevice.Read(address);
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
                        var slotDevice = SlotDevices[slot];

                        if (slotDevice?.HandlesRead(address) == true)
                        {
                            return slotDevice.Read(address);
                        }
                    }
                }
            }

            return memoryBlocks.Read(address);
        }

        public void Write(ushort address, byte value)
        {
            Tick();

            // O(1) lookup into the dispatch table for registered intercept devices
            var writers = writeDispatch[address];
            if (writers != null)
            {
                foreach (var device in writers)
                {
                    if (device.DoWrite(address, value))
                    {
                        return;
                    }
                }
            }

            if (address >= 0xC000 && address <= 0xC08F)
            {
                CheckClearKeystrobe(address);

                return;
            }
            else if (address >= 0xC090 && address <= 0xC0FF)
            {
                var slot = (address >> 4) & 7;
                var slotDevice = SlotDevices[slot];

                if (slotDevice?.HandlesWrite(address) == true)
                {
                    // SimDebugger.Info("Write IO {0} {1:X4}\n", slot, address);
                    slotDevice.Write(address, value);
                }

                return;
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
                    var slotDevice = SlotDevices[slot];

                    if (slotDevice?.HandlesWrite(address) == true)
                    {
                        slotDevice.Write(address, value);
                        return;
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
                        var slotDevice = SlotDevices[slot];

                        if (slotDevice?.HandlesWrite(address) == true)
                        {
                            slotDevice.Write(address, value);
                            return;
                        }
                    }
                }
            }

            memoryBlocks.Write(address, value);
        }

        public byte Peek(ushort address)
        {
            return memoryBlocks.Read(address);
        }

        public void Poke(ushort address, byte value)
        {
            memoryBlocks.Write(address, value);
        }

        public void LoadProgramToRom(byte[] objectCode)
        {
            ArgumentNullException.ThrowIfNull(objectCode);
            memoryBlocks.LoadProgramToRom(objectCode);
        }

        public void LoadProgramToRam(byte[] objectCode, ushort origin)
        {
            ArgumentNullException.ThrowIfNull(objectCode);
            memoryBlocks.LoadProgramToRam(objectCode, origin);
        }

        public void Reset()
        {
            foreach (var (softSwitch, value) in machineState.State)
            {
                machineState.State[softSwitch] = false;
            }

            foreach (var interceptDevice in interceptDevices)
            {
                interceptDevice.Reset();
            }

            for (var slot = 0; slot < SlotDevices.Length; slot++)
            {
                SlotDevices[slot]?.Reset();
            }

            memoryBlocks.Remap();

            transactionCycles = 0;
            CycleCount = 0;
        }

        private void Tick()
        {
            CycleCount++;

            for (var slot = 0; slot < SlotDevices.Length; slot++)
            {
                SlotDevices[slot]?.Tick();
            }

            foreach (var interceptDevice in interceptDevices)
            {
                interceptDevice.Tick();
            }

            transactionCycles++;
        }

        private static void InsertByPriority(List<IAddressInterceptDevice> list, IAddressInterceptDevice device)
        {
            var priority = device.InterceptPriority;

            for (var i = 0; i < list.Count; i++)
            {
                if (priority < list[i].InterceptPriority)
                {
                    list.Insert(i, device);
                    return;
                }
            }

            list.Add(device);
        }

        private byte CheckKeyboardLatch(ushort address)
        {
            if (reportKeyboardLatchAll == false)
            {
                return 0x00;
            }

            // all these addresses return the KSTRB and ASCII value
            if (address >= 0xC001 && address <= 0xC00F)
            {
                return machineState.PeekKeyboard();
            }

            // 0xC010 is handled directly by the keyboard as the "owning" device

            // if the IOU is disabled then we only handle the MMU soft switch
            if (machineState.State[SoftSwitch.IOUDisabled] == true)
            {
                return 0x00;
            }

            if (address >= 0xC001 && address <= 0xC01F)
            {
                return machineState.KeyLatch;
            }

            return 0x00;
        }

        private void CheckClearKeystrobe(ushort address)
        {
            if (reportKeyboardLatchAll == false)
            {
                return;
            }

            if (address >= 0xC010 && address <= 0xC01F)
            {
                machineState.ClearKeyboardStrobe();
            }
        }
    }
}

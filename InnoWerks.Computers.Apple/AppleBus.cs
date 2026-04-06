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

        private readonly List<IAddressInterceptDevice> interceptDevices = [];

        // 64K dispatch tables — each entry is a list of devices interested in that address
        private readonly List<IAddressInterceptDevice>[] readDispatch =
            new List<IAddressInterceptDevice>[ushort.MaxValue + 1];
        private readonly List<IAddressInterceptDevice>[] writeDispatch =
            new List<IAddressInterceptDevice>[ushort.MaxValue + 1];

        private readonly IntC8Handler intC8Handler;
        private readonly KeylatchHandler keylatchHandler;
        private readonly SlotHandler slotHandler;

        private readonly MachineState machineState;

        public ISlotDevice[] SlotDevices => slotHandler.SlotDevices;

        public AppleBus(AppleConfiguration configuration, Memory128k memoryBlocks, MachineState machineState)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(memoryBlocks);
            ArgumentNullException.ThrowIfNull(machineState);

            this.configuration = configuration;
            this.memoryBlocks = memoryBlocks;
            this.machineState = machineState;

            intC8Handler = new IntC8Handler(memoryBlocks, machineState, this);
            keylatchHandler = new KeylatchHandler(memoryBlocks, machineState, this);
            slotHandler = new SlotHandler(memoryBlocks, machineState, this);

            AddDevice(intC8Handler);
            AddDevice(keylatchHandler);
            AddDevice(slotHandler);
        }

        public void FillEmptySlots(ICpu cpu)
        {
            ArgumentNullException.ThrowIfNull(cpu, nameof(cpu));
            slotHandler.FillEmptySlots(cpu);
        }

        public void AddDevice(ISlotDevice slotDevice)
        {
            ArgumentNullException.ThrowIfNull(slotDevice, nameof(slotDevice));

            if (slotHandler.SlotDevices[slotDevice.Slot] != null)
            {
                throw new ArgumentException($"There is already a device {slotHandler.SlotDevices[slotDevice.Slot].Name} in slot {slotDevice.Slot}");
            }

            slotDevice.Reset();

            if (slotDevice is SlotRomDevice slotRomDevice)
            {
                if (slotRomDevice.HasRom)
                {
                    memoryBlocks.LoadSlotCxRom(slotDevice.Slot, slotRomDevice.Rom);
                }

                if (slotRomDevice.ExpansionRom != null)
                {
                    memoryBlocks.LoadSlotC8Rom(slotDevice.Slot, slotRomDevice.ExpansionRom);
                }
            }

            slotHandler.SlotDevices[slotDevice.Slot] = slotDevice;
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

            // slotHandler.Reset();

            if (slotHandler.SlotDevices[1] != null)
            {
                keylatchHandler.ReportKeyboardLatchAll = false;
            }

            memoryBlocks.Remap();

            transactionCycles = 0;
            CycleCount = 0;
        }

        private void Tick()
        {
            CycleCount++;

            foreach (var interceptDevice in interceptDevices)
            {
                interceptDevice.Tick();
            }

            // slotHandler.Tick();

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
    }
}

using System;
using System.Collections.Generic;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1819

    public class AppleBus : IAppleBus
    {
        private int transactionCycles;

        private readonly Memory128k memoryBlocks;

        private readonly List<IAddressInterceptDevice> interceptDevices;

        // 64K dispatch tables — each entry is a list of devices interested in that address
        private readonly List<IAddressInterceptDevice>[] readDispatch =
            new List<IAddressInterceptDevice>[ushort.MaxValue + 1];
        private readonly List<IAddressInterceptDevice>[] writeDispatch =
            new List<IAddressInterceptDevice>[ushort.MaxValue + 1];

        public AppleBus(Memory128k memoryBlocks, List<IAddressInterceptDevice> interceptDevices)
        {
            ArgumentNullException.ThrowIfNull(memoryBlocks);
            ArgumentNullException.ThrowIfNull(interceptDevices);

            this.memoryBlocks = memoryBlocks;
            this.interceptDevices = interceptDevices;
        }

        public void AddDevice(ISlotDevice slotDevice)
        {
            throw new NotImplementedException();
        }

        public void AddDevice(IAddressInterceptDevice interceptDevice)
        {
            ArgumentNullException.ThrowIfNull(interceptDevice, nameof(interceptDevice));

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

        public void SetCpu(I6502Cpu cpu)
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

        public void Reset()
        {
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

            transactionCycles++;
        }

        public void LoadProgramToRom(byte[] objectCode) => throw new NotImplementedException();

        public void LoadProgramToRam(byte[] objectCode, ushort origin) => throw new NotImplementedException();

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

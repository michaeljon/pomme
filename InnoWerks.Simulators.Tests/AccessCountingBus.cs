using System;
using System.Collections.Generic;
using System.Linq;

namespace InnoWerks.Simulators.Tests
{
#pragma warning disable CA1002, CA1819, RCS1085, CA1851

    public class AccessCountingBus : IBus
    {
        private readonly byte[] memory = new byte[64 * 1024];

        private readonly int[] readCounts = new int[64 * 1024];

        private readonly int[] writeCounts = new int[64 * 1024];

        public AccessCountingBus()
        {
            BusAccesses = [];
        }

        private int transactionCycles;

        public void BeginTransaction()
        {
            transactionCycles = 0;
        }

        public int EndTransaction()
        {
            return transactionCycles;
        }

        public ulong CycleCount { get; private set; }

        public void SetCpu(ICpu cpu) { /* this is a no-op for the debug bus */ }

        public byte Peek(ushort address)
        {
            return memory[address];
        }

        public void Poke(ushort address, byte value)
        {
            memory[address] = value;
        }

        public byte Read(ushort address)
        {
            Tick(1);

            BusAccesses.Add(
                new JsonHarteTestBusAccess()
                {
                    Address = address,
                    Value = memory[address],
                    Type = CycleType.Read
                }
            );

            readCounts[address]++;

            return memory[address];
        }

        public void Write(ushort address, byte value)
        {
            Tick(1);

            BusAccesses.Add(
                new JsonHarteTestBusAccess()
                {
                    Address = address,
                    Value = value,
                    Type = CycleType.Write
                }
            );

            writeCounts[address]++;

            memory[address] = value;
        }

        public void Initialize(IEnumerable<JsonHarteRamEntry> mem)
        {
            ArgumentNullException.ThrowIfNull(mem);

            CycleCount = 0;
            transactionCycles = 0;

            foreach (var m in mem)
            {
                memory[m.Address] = m.Value;
            }
        }

        public void LoadProgramToRom(byte[] objectCode)
        {
            ArgumentNullException.ThrowIfNull(objectCode);

            Array.Copy(objectCode, 0, memory, 0, objectCode.Length);
        }

        public void LoadProgramToRam(byte[] objectCode, ushort origin)
        {
            ArgumentNullException.ThrowIfNull(objectCode);

            Array.Copy(objectCode, 0, memory, origin, objectCode.Length);
        }

        public void Reset() { /* no-op */ }

        public (bool matches, ushort differsAtAddr, byte expectedValue, byte actualValue) ValidateMemory(IEnumerable<JsonHarteRamEntry> mem)
        {
            ArgumentNullException.ThrowIfNull(mem);

            foreach (var m in mem)
            {
                if (m.Value != memory[m.Address])
                {
                    return (false, m.Address, m.Value, memory[m.Address]);
                }
            }

            return (true, 0, 0, 0);
        }

        public (bool matches, int differsAtIndex, JsonHarteTestBusAccess expectedValue, JsonHarteTestBusAccess actualValue) ValidateCycles(IEnumerable<JsonHarteTestBusAccess> expectedBusAccesses)
        {
            ArgumentNullException.ThrowIfNull(expectedBusAccesses);

            var expectedCount = expectedBusAccesses.Count();

            if (expectedCount != BusAccesses.Count)
            {
                return (false, 0, null, null);
            }

            for (var a = 0; a < expectedCount; a++)
            {
                var expected = expectedBusAccesses.ElementAt(a);
                var actual = BusAccesses[a];

                if (expected.Address != actual.Address || expected.Value != actual.Value || expected.Type != actual.Type)
                {
                    return (false, a, expected, actual);
                }
            }

            return (true, 0, null, null);
        }

        public List<JsonHarteTestBusAccess> BusAccesses { get; init; }

        private void Tick(int howMany)
        {
            CycleCount += (ulong)howMany;
            transactionCycles += howMany;
        }
    }
}

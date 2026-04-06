using System;

using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple.Tests
{
    /// <summary>
    /// A test double for <see cref="IBus"/> that tracks reads, writes, and cycle
    /// counts over a flat 64 KB memory buffer. Modelled after
    /// <c>AccessCountingBus</c> from InnoWerks.Simulators.Tests but without
    /// any dependency on the JSON Harte test infrastructure.
    /// </summary>
    public class AppleBusTestDouble : IAppleBus
    {
        private readonly byte[] memory = new byte[64 * 1024];

        private readonly int[] readCounts = new int[64 * 1024];

        private readonly int[] writeCounts = new int[64 * 1024];

        private int transactionCycles;

        // ------------------------------------------------------------------ //
        // IBus — device registration (no-ops for unit tests)
        // ------------------------------------------------------------------ //

        public void AddDevice(ISlotDevice slotDevice) { }

        public void AddDevice(IAddressInterceptDevice interceptDevice) { }

        public void SetCpu(ICpu cpu) { }

        // ------------------------------------------------------------------ //
        // IBus — cycle / transaction tracking
        // ------------------------------------------------------------------ //

        public ulong CycleCount { get; private set; }

        public void BeginTransaction() => transactionCycles = 0;

        public int EndTransaction() => transactionCycles;

        // ------------------------------------------------------------------ //
        // IBus — memory access
        // ------------------------------------------------------------------ //

        public byte Peek(ushort address) => memory[address];

        public void Poke(ushort address, byte value) => memory[address] = value;

        public byte Read(ushort address)
        {
            Tick(1);
            readCounts[address]++;
            return memory[address];
        }

        public void Write(ushort address, byte value)
        {
            Tick(1);
            writeCounts[address]++;
            memory[address] = value;
        }

        // ------------------------------------------------------------------ //
        // IBus — loading
        // ------------------------------------------------------------------ //

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

        public void Reset()
        {
            Array.Clear(memory, 0, memory.Length);
            Array.Clear(readCounts, 0, readCounts.Length);
            Array.Clear(writeCounts, 0, writeCounts.Length);
            CycleCount = 0;
            transactionCycles = 0;
        }

        // ------------------------------------------------------------------ //
        // Test helpers
        // ------------------------------------------------------------------ //

        /// <summary>Returns the number of cycle-affecting reads at <paramref name="address"/>.</summary>
        public int ReadCount(ushort address) => readCounts[address];

        /// <summary>Returns the number of cycle-affecting writes at <paramref name="address"/>.</summary>
        public int WriteCount(ushort address) => writeCounts[address];

        // ------------------------------------------------------------------ //
        // Private
        // ------------------------------------------------------------------ //

        private void Tick(int howMany)
        {
            CycleCount += (ulong)howMany;
            transactionCycles += howMany;
        }
    }
}

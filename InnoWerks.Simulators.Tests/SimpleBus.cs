using System;

namespace InnoWerks.Simulators.Tests
{
    public class SimpleBus : IBus
    {
        private readonly byte[] memory = new byte[64 * 1024];

        public void BeginTransaction() { }

        public int EndTransaction() => 0;

        public ulong CycleCount { get; private set; }

        public void SetCpu(I6502Cpu cpu) { }

        public byte Peek(ushort address) => memory[address];

        public void Poke(ushort address, byte value) => memory[address] = value;

        public byte Read(ushort address)
        {
            CycleCount++;

            return memory[address];
        }

        public void Write(ushort address, byte value)
        {
            CycleCount++;
            memory[address] = value;
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

        public void Reset() { }
    }
}

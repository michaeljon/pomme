using System;

namespace InnoWerks.Simulators
{
    public readonly struct CpuTraceEntry : IEquatable<CpuTraceEntry>
    {
        public readonly ushort ProgramCounter { get; init; }
        public readonly OpCodeDefinition OpCode { get; init; }
        public readonly DecodedOperation DecodedOperation { get; init; }
        public readonly ulong CycleCount { get; init; }
        public readonly string Mnemonic { get; init; }

        public CpuTraceEntry(
            ushort programCounter,
            IBus bus,
            ulong cycleCount,
            OpCodeDefinition opcode)
        {
            ArgumentNullException.ThrowIfNull(opcode);

            ProgramCounter = programCounter;
            OpCode = opcode;
            CycleCount = cycleCount;
            DecodedOperation = opcode.DecodeOperand(ProgramCounter, bus);

            Mnemonic = opcode.OpCode.ToString();
        }

        public override bool Equals(object obj)
        {
            return ((CpuTraceEntry)obj).ProgramCounter == ProgramCounter;
        }

        public override int GetHashCode()
        {
            return ProgramCounter.GetHashCode();
        }

        public static bool operator ==(CpuTraceEntry left, CpuTraceEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CpuTraceEntry left, CpuTraceEntry right)
        {
            return !(left == right);
        }

        public bool Equals(CpuTraceEntry other)
        {
            return other.ProgramCounter == ProgramCounter;
        }

        public override string ToString()
        {
            return $"{ProgramCounter:X4} {OpCode.OpCode}   {DecodedOperation.Display}";
        }
    }
}

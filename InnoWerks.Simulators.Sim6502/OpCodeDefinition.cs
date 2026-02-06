using System;
using System.Diagnostics;
using InnoWerks.Processors;

namespace InnoWerks.Simulators
{
    public record DecodedOperation
    {
        public string Display { get; set; }

        public int Length { get; set; } = 1;

        public byte OpCodeValue { get; set; }

        public byte Operand1 { get; set; }

        public byte Operand2 { get; set; }
    }

    public record OpCodeDefinition(
        byte OpCodeValue,
        OpCode OpCode,
        Action<Cpu6502Core, ushort, byte> Execute,
        Func<ushort, IBus, DecodedOperation> DecodeOperand,
        AddressingMode AddressingMode,
        int Bytes = 0,
        int Cycles = 0)
    {
        /// <summary>
        ///
        /// </summary>
        public byte OpCodeValue { get; init; } = OpCodeValue;

        /// <summary>
        ///
        /// </summary>
        public OpCode OpCode { get; init; } = OpCode;

        /// <summary>
        ///
        /// </summary>
        public Func<ushort, IBus, DecodedOperation> DecodeOperand { get; init; } = DecodeOperand;

        /// <summary>
        ///
        /// </summary>
        public Action<Cpu6502Core, ushort, byte> Execute { get; init; } = Execute;

        /// <summary>
        ///
        /// </summary>
        public AddressingMode AddressingMode { get; init; } = AddressingMode;

        /// <summary>
        ///
        /// </summary>
        public int Bytes { get; init; } = Bytes;

        /// <summary>
        ///
        /// </summary>
        public int Cycles { get; init; } = Cycles;

        public override string ToString()
        {
            return $"${OpCodeValue:X2} {OpCode} {AddressingMode}";
        }
    }
}

using System;
using InnoWerks.Processors;

namespace InnoWerks.Simulators
{
    public class UnhandledAddressingModeException : Exception
    {
        public UnhandledAddressingModeException() { }

        public UnhandledAddressingModeException(string message) : base(message) { }

        public UnhandledAddressingModeException(string message, Exception innerException) : base(message, innerException) { }

        public UnhandledAddressingModeException(ushort programCounter, byte operation, OpCode opCode, AddressingMode addressingMode)
            : base($"Unhandled addressing mode {opCode} ${operation:X2} {addressingMode} at ${programCounter:X4}") { }
    }
}

using System;

namespace InnoWerks.Simulators
{
    public class IllegalOpCodeException : Exception
    {
        public IllegalOpCodeException() { }

        public IllegalOpCodeException(string message) : base(message) { }

        public IllegalOpCodeException(string message, Exception innerException) : base(message, innerException) { }

        public IllegalOpCodeException(ushort programCounter, byte operation)
            : base($"Illegal instruction {operation:X2} at ${programCounter:X4}") { }
    }
}

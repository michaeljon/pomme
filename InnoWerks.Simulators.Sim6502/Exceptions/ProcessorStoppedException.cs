using System;

namespace InnoWerks.Simulators
{
    public class ProcessorStoppedException : Exception
    {
        public ProcessorStoppedException() { }

        public ProcessorStoppedException(string message) : base(message) { }

        public ProcessorStoppedException(string message, Exception innerException) : base(message, innerException) { }

        public ProcessorStoppedException(ushort programCounter)
            : base($"Processor is stopped at ${programCounter:X4}") { }
    }
}

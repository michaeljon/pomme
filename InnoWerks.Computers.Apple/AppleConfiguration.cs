using InnoWerks.Processors;

namespace InnoWerks.Computers.Apple
{
    public sealed class AppleConfiguration
    {
        public AppleModel Model { get; init; }

        public CpuClass CpuClass { get; init; }

        public bool HasLowercase { get; init; }     // IIe yes

        public bool Has80Column { get; init; }      // IIe

        public bool HasAuxMemory { get; init; }     // IIe

        public int RamSize { get; init; }            // 48K, 64K, 128K

        public AppleConfiguration(AppleModel appleModel)
        {
            Model = appleModel;
        }
    }
}

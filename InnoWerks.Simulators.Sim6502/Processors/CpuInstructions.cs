using System;
using InnoWerks.Processors;

namespace InnoWerks.Simulators
{
    public static class CpuInstructions
    {
        public static OpCodeDefinition[] GetInstructionSet(CpuClass cpuClass)
        {
            return cpuClass switch
            {
                CpuClass.Undefined => throw new ArgumentOutOfRangeException(nameof(cpuClass), cpuClass, "CpuClass.Undefined is obviously not supported."),
                CpuClass.WDC6502 => OpCode6502.Instructions,
                CpuClass.WDC65C02 => OpCode65C02.Instructions,
                CpuClass.Synertek65C02 => OpCode65SC02.Instructions,
                CpuClass.Rockwell65C02 => OpCodeR65C02.Instructions,
                CpuClass.Nes6502 => OpCodeNes6502.Instructions,

                _ => throw new ArgumentOutOfRangeException(nameof(cpuClass), cpuClass, "CpuClass is not currently supported"),
            };
        }
    }
}

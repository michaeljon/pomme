using System;
using InnoWerks.Processors;

namespace InnoWerks.Simulators
{
    public static class Cpu6502Factory
    {
        public static I6502Cpu Construct(
            CpuClass cpuClass,
            IBus bus,
            Action<I6502Cpu, ushort> preExecutionCallback = null,
            Action<I6502Cpu> postExecutionCallback = null)
        {
            return cpuClass switch
            {
                CpuClass.WDC6502 => new Cpu6502(bus, preExecutionCallback, postExecutionCallback),
                CpuClass.WDC65C02 => new Cpu65C02(bus, preExecutionCallback, postExecutionCallback),
                CpuClass.Synertek65C02 => new Cpu65SC02(bus, preExecutionCallback, postExecutionCallback),
                CpuClass.Rockwell65C02 => new CpuR65C02(bus, preExecutionCallback, postExecutionCallback),
                CpuClass.Nes6502 => new CpuNes6502(bus, preExecutionCallback, postExecutionCallback),

                _ => throw new ArgumentOutOfRangeException(nameof(cpuClass), "Constructing CpuClass not (yet) supported")
            };
        }

        public static T Construct<T>(
            CpuClass cpuClass,
            IBus bus,
            Action<I6502Cpu, ushort> preExecutionCallback = null,
            Action<I6502Cpu> postExecutionCallback = null) where T : Cpu6502Core
        {
            return cpuClass switch
            {
                CpuClass.WDC6502 => new Cpu6502(bus, preExecutionCallback, postExecutionCallback) as T,
                CpuClass.WDC65C02 => new Cpu65C02(bus, preExecutionCallback, postExecutionCallback) as T,
                CpuClass.Synertek65C02 => new Cpu65SC02(bus, preExecutionCallback, postExecutionCallback) as T,
                CpuClass.Rockwell65C02 => new CpuR65C02(bus, preExecutionCallback, postExecutionCallback) as T,
                CpuClass.Nes6502 => new CpuNes6502(bus, preExecutionCallback, postExecutionCallback) as T,

                _ => throw new ArgumentOutOfRangeException(nameof(cpuClass), "Constructing CpuClass not (yet) supported")
            };
        }
    }
}

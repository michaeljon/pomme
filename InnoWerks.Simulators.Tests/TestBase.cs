using System;
using System.Collections.Generic;
using InnoWerks.Assemblers;
using InnoWerks.Processors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CA1822

namespace InnoWerks.Simulators.Tests
{
    [TestClass]
    public class TestBase
    {
        public TestContext TestContext { get; set; }

        protected static void DummyLoggerCallback(I6502Cpu _1, IBus _2, int _3 = 0) { }

        protected static void FlagsLoggerCallback(I6502Cpu cpu, IBus memory, int lines = 0)
        {
            ArgumentNullException.ThrowIfNull(cpu);
            ArgumentNullException.ThrowIfNull(memory);

            Console.WriteLine($"\tPC:{cpu.Registers.ProgramCounter:X4} {cpu.Registers.GetRegisterDisplay} {cpu.Registers.InternalGetFlagsDisplay}");
            Console.WriteLine();

            PrintMemoryLines(memory, lines);
        }

        protected static void LoggerCallback(I6502Cpu cpu, IBus memory, int lines = 1)
        {
            ArgumentNullException.ThrowIfNull(cpu);
            ArgumentNullException.ThrowIfNull(memory);

            PrintMemoryLines(memory, lines);
        }

        protected static void DummyTraceCallback(I6502Cpu _1, ushort _2, IBus _3, Dictionary<ushort, LineInformation> _4 = null) { }

        protected static void FlagsTraceCallback(I6502Cpu cpu, ushort _1, IBus memory, Dictionary<ushort, LineInformation> _2 = null)
        {
            ArgumentNullException.ThrowIfNull(cpu);
            ArgumentNullException.ThrowIfNull(memory);

            Console.WriteLine($"\tPC:{cpu.Registers.ProgramCounter:X4} {cpu.Registers.GetRegisterDisplay} {cpu.Registers.InternalGetFlagsDisplay}");
        }

        protected static void TraceCallback(I6502Cpu cpu, ushort programCounter, IBus memory, Dictionary<ushort, LineInformation> code)
        {
            ArgumentNullException.ThrowIfNull(cpu);
            ArgumentNullException.ThrowIfNull(memory);

            if (code != null)
            {
                if (code.TryGetValue(programCounter, out var lineInformation))
                {
                    Console.WriteLine($"{lineInformation.EffectiveAddress:X4} | {lineInformation.MachineCodeAsString,-10}| {lineInformation.RawInstructionText}");
                }
                else
                {
                    Console.WriteLine($"{programCounter:X4} | {{no information found}}");
                }
            }
        }

        protected I6502Cpu RunTinyTest(IBus bus, Dictionary<ushort, LineInformation> code, CpuClass cpuClass, int lines = 1)
        {
            ArgumentNullException.ThrowIfNull(bus);

            // power up initialization
            bus.Poke(Cpu6502Core.RstVectorH, 0x00);
            bus.Poke(Cpu6502Core.RstVectorL, 0x00);

            var cpu = Cpu6502Factory.Construct(
                cpuClass,
                bus,
                (cpu, pc) => DummyTraceCallback(cpu, pc, bus, code),
                (cpu) => DummyLoggerCallback(cpu, bus, lines));

            cpu.Reset();
            if (code != null)
            {
                for (var s = 0; s < code.Count; s++)
                {
                    cpu.Step();
                }
            }
            else
            {
                var (instructionCount, cycleCount) = RunCpu(cpu, bus);

                TestContext.WriteLine($"INST: {instructionCount}");
                TestContext.WriteLine($"CYCLES: {cycleCount}");
            }

            return cpu;
        }

        protected (int intructionCount, ulong cycleCount) RunCpu(I6502Cpu cpu, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(cpu);
            ArgumentNullException.ThrowIfNull(bus);

            var instructionCount = 0;

            while (true)
            {
                instructionCount++;

                var operation = bus.Peek(cpu.Registers.ProgramCounter);
                if (operation == 0x00)
                {
                    break;
                }

                cpu.Step();
            }

            return (instructionCount, bus.CycleCount);
        }

        protected void PrintPage(IBus bus, byte page)
        {
            ArgumentNullException.ThrowIfNull(bus);

            for (var l = page * 0x100; l < (page + 1) * 0x100; l += 16)
            {
                Console.Write("{0:X4}:  ", l);

                for (var b = 0; b < 8; b++)
                {
                    Console.Write("{0:X2} ", bus.Peek(l + b));
                }

                Console.Write("  ");

                for (var b = 8; b < 16; b++)
                {
                    Console.Write("{0:X2} ", bus.Peek(l + b));
                }

                Console.WriteLine("");
            }

            Console.WriteLine("");
        }

        private static void PrintMemoryLines(IBus bus, int lines)
        {
            if (lines == 0)
            {
                return;
            }

            Console.Write($"\t      ");

            for (var b = 0; b < 8; b++)
            {
                Console.Write($"{b:X2} ");
            }

            Console.Write("\t  ");

            for (var b = 8; b < 16; b++)
            {
                Console.Write($"{b:X2} ");
            }

            Console.WriteLine("");

            for (var l = 0; l < lines; l++)
            {
                Console.Write($"\t{l:X4}  ");

                for (var b = 0; b < 8; b++)
                {
                    Console.Write($"{bus.Peek((l * 16) + b):X2} ");
                }

                Console.Write("\t  ");

                for (var b = 8; b < 16; b++)
                {
                    Console.Write($"{bus.Peek((l * 16) + b):X2} ");
                }

                Console.WriteLine("");
            }

            Console.WriteLine("");
        }
    }
}

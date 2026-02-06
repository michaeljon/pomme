using System;
using System.IO;
using InnoWerks.Assemblers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Simulators.Tests
{
    [TestClass]
    public class BruceClarkTests : TestBase
    {
        [TestMethod]
        public void BruceClark6502()
        {
            const string Filename = "Modules/BcdTest/BruceClark6502.S";
            const ushort Origin = 0x8000;
            const ushort InitializationVector = 0x8000;

            const ushort ERROR = 0x2F;

            var programLines = File.ReadAllLines(Filename);
            var assembler = new Assembler(
                programLines,
                Origin
            );
            assembler.Assemble();

            var bus = new AccessCountingBus();
            bus.LoadProgramToRam(assembler.ObjectCode, Origin);

            // power up initialization
            bus.Poke(Cpu6502Core.RstVectorH, (byte)((InitializationVector & 0xff00) >> 8));
            bus.Poke(Cpu6502Core.RstVectorL, (byte)(InitializationVector & 0xff));

            var cpu = new Cpu6502(
                bus,
                (cpu, pc) => DummyTraceCallback(cpu, pc, bus, assembler.ProgramByAddress),
                (cpu) => DummyLoggerCallback(cpu, bus, 2));

            cpu.Reset();

            // run
            Console.WriteLine();
            var (instructionCount, cycleCount) = cpu.Run(stopOnBreak: true, writeInstructions: false);

            TestContext.WriteLine($"INST: {instructionCount}");
            TestContext.WriteLine($"CYCLES: {cycleCount}");
            Assert.AreEqual(0x00, bus.Peek(ERROR));
        }

        [TestMethod]
        public void BruceClark65C02()
        {
            const string Filename = "Modules/BcdTest/BruceClark65C02.S";
            const ushort Origin = 0x8000;
            const ushort InitializationVector = 0x8000;

            const ushort ERROR = 0x2F;

            var programLines = File.ReadAllLines(Filename);
            var assembler = new Assembler(
                programLines,
                Origin
            );
            assembler.Assemble();

            var bus = new AccessCountingBus();
            bus.LoadProgramToRam(assembler.ObjectCode, Origin);

            // power up initialization
            bus.Poke(Cpu6502Core.RstVectorH, (byte)((InitializationVector & 0xff00) >> 8));
            bus.Poke(Cpu6502Core.RstVectorL, (byte)(InitializationVector & 0xff));

            var cpu = new Cpu65C02(
                bus,
                (cpu, pc) => DummyTraceCallback(cpu, pc, bus, assembler.ProgramByAddress),
                (cpu) => DummyLoggerCallback(cpu, bus, 2));

            cpu.Reset();

            // run
            Console.WriteLine();
            var (instructionCount, cycleCount) = cpu.Run(stopOnBreak: true, writeInstructions: false);

            TestContext.WriteLine($"INST: {instructionCount}");
            TestContext.WriteLine($"CYCLES: {cycleCount}");
            Assert.AreEqual(0x00, bus.Peek(ERROR));
        }

        [TestMethod]
        public void BruceClarkOverflowTest()
        {
            const string Filename = "Modules/BcdTest/OverflowTest.S";
            const ushort Origin = 0x8000;
            const ushort InitializationVector = 0x8000;

            const ushort ERROR = 0x0B;

            var programLines = File.ReadAllLines(Filename);
            var assembler = new Assembler(
                programLines,
                Origin
            );
            assembler.Assemble();

            var bus = new AccessCountingBus();
            bus.LoadProgramToRam(assembler.ObjectCode, Origin);

            // power up initialization
            bus.Poke(Cpu6502Core.RstVectorH, (byte)((InitializationVector & 0xff00) >> 8));
            bus.Poke(Cpu6502Core.RstVectorL, (byte)(InitializationVector & 0xff));

            var cpu = new Cpu65C02(
                bus,
                (cpu, pc) => DummyTraceCallback(cpu, pc, bus, assembler.ProgramByAddress),
                (cpu) => DummyLoggerCallback(cpu, bus, 0));

            cpu.Reset();

            // run
            Console.WriteLine();
            var (instructionCount, cycleCount) = cpu.Run(stopOnBreak: true, writeInstructions: false);

            TestContext.WriteLine($"INST: {instructionCount}");
            TestContext.WriteLine($"CYCLES: {cycleCount}");
            Assert.AreEqual(0x00, bus.Peek(ERROR));
        }
    }
}

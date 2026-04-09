using System;
using System.IO;
using InnoWerks.Assemblers;
using InnoWerks.Processors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Simulators.Tests
{
    [TestClass]
    public class AlgorithmTests : TestBase
    {
        [TestMethod]
        public void BinarySearchNegativeCase()
        {
            const string Filename = "Modules/BinarySearch/binarySearch.S";
            const ushort Origin = 0x6000;
            const ushort InitializationVector = 0x606f;

            var programLines = File.ReadAllLines(Filename);
            var assembler = new Assembler(
                programLines,
                Origin
            );
            assembler.Assemble();

            var bus = new SimpleBus();
            bus.LoadProgramToRam(assembler.ObjectCode, Origin);

            // power up initialization
            bus.Poke(Cpu6502Core.RstVectorH, (byte)((InitializationVector & 0xff00) >> 8));
            bus.Poke(Cpu6502Core.RstVectorL, (byte)(InitializationVector & 0xff));

            var cpu = Cpu6502Factory.Construct(
                CpuClass.WDC65C02,
                bus,
                (cpu, pc) => TraceCallback(cpu, pc, bus, null),
                (cpu) => LoggerCallback(cpu, bus, 0));

            cpu.Reset();

            // run
            Console.WriteLine();
            RunCpu(cpu, bus);

            Assert.IsTrue(cpu.Registers.Carry);
        }

        [TestMethod]
        public void BinarySearchPositiveCase()
        {
            const string Filename = "Modules/BinarySearch/binarySearch.S";
            const ushort Origin = 0x6000;
            const ushort InitializationVector = 0x605c;

            var programLines = File.ReadAllLines(Filename);
            var assembler = new Assembler(
                programLines,
                Origin
            );
            assembler.Assemble();

            var bus = new SimpleBus();
            bus.LoadProgramToRam(assembler.ObjectCode, Origin);

            // power up initialization
            bus.Poke(Cpu6502Core.RstVectorH, (byte)((InitializationVector & 0xff00) >> 8));
            bus.Poke(Cpu6502Core.RstVectorL, (byte)(InitializationVector & 0xff));

            var cpu = Cpu6502Factory.Construct(
                CpuClass.WDC6502,
                bus,
                (cpu, pc) => TraceCallback(cpu, pc, bus, null),
                (cpu) => LoggerCallback(cpu, bus, 0));

            cpu.Reset();

            // run
            Console.WriteLine();
            RunCpu(cpu, bus);

            Assert.AreEqual(0x04, cpu.Registers.A);
            Assert.IsFalse(cpu.Registers.Carry);
        }
    }
}

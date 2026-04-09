using InnoWerks.Assemblers;
using InnoWerks.Processors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Simulators.Tests
{
    /// <summary>
    /// These tests execute the snippits that are in
    /// http://www.6502.org/tutorials/decimal_mode.html
    /// </summary>
    [TestClass]
    public class BruceClarkTinyTests : TestBase
    {
        [TestMethod]
        public void BruceClarkExampleTestA()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED",
                    $"   LDA #$05",
                    $"   CLC",
                    $"   ADC #$05",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.AreEqual(0x10, cpu.Registers.A);
        }

        [TestMethod]
        public void BruceClarkExampleTestB()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED",
                    $"   LDA #$05",
                    $"   ASL",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.AreEqual(0x0A, cpu.Registers.A);
        }

        [TestMethod]
        public void BruceClarkExampleTestC()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED",
                    $"   LDA #$09",
                    $"   CLC",
                    $"   ADC #$01",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.AreEqual(0x10, cpu.Registers.A);
        }

        [TestMethod]
        public void BruceClarkExampleTestD()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED            ; Decimal mode (has no effect on this sequence)",
                    $"   LDA #$09",
                    $"   STA $E0",
                    $"   INC $E0        ; NUM (assuming it is an ordinary RAM location) will contain $0A."
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.AreEqual(0x0a, bus.Peek(0xe0));
        }

        [TestMethod]
        public void BruceClarkExample1()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   CLD            ; Binary mode (binary addition: 88 + 70 + 1 = 159)",
                    $"   SEC            ; Note: carry is set, not clear!",
                    $"   LDA #$58       ; 88",
                    $"   ADC #$46       ; 70 (after this instruction, C = 0, A = $9F = 159)",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.IsFalse(cpu.Registers.Carry);
            Assert.AreEqual(0x9f, cpu.Registers.A);
        }

        [TestMethod]
        public void BruceClarkExample2()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED            ; Decimal mode (BCD addition: 58 + 46 + 1 = 105)",
                    $"   SEC            ; Note: carry is set, not clear!",
                    $"   LDA #$58",
                    $"   ADC #$46       ; After this instruction, C = 1, A = $05",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.IsTrue(cpu.Registers.Carry);
            Assert.AreEqual(0x05, cpu.Registers.A);
        }

        [TestMethod]
        public void BruceClarkExample3()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED            ; Decimal mode (BCD addition: 12 + 34 = 46)",
                    $"   CLC            ; Note: carry is set, not clear!",
                    $"   LDA #$12",
                    $"   ADC #$34       ; After this instruction, C = 0, A = $46",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.IsFalse(cpu.Registers.Carry);
            Assert.AreEqual(0x46, cpu.Registers.A);
        }

        [TestMethod]
        public void BruceClarkExample4()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED            ; Decimal mode (BCD addition: 15 + 26 = 41)",
                    $"   CLC            ; Note: carry is set, not clear!",
                    $"   LDA #$15",
                    $"   ADC #$26       ; After this instruction, C = 0, A = $41",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.IsFalse(cpu.Registers.Carry);
            Assert.AreEqual(0x41, cpu.Registers.A);
        }

        [TestMethod]
        public void BruceClarkExample5()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED            ; Decimal mode (BCD addition: 81 + 92 = 173)",
                    $"   CLC            ; Note: carry is set, not clear!",
                    $"   LDA #$81",
                    $"   ADC #$92       ; After this instruction, C = 1, A = $73",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.IsTrue(cpu.Registers.Carry);
            Assert.AreEqual(0x73, cpu.Registers.A);
        }

        [TestMethod]
        public void BruceClarkExample6()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED            ; Decimal mode (BCD subtraction: 46 - 12 = 34)",
                    $"   SEC            ; Note: carry is set, not clear!",
                    $"   LDA #$46",
                    $"   SBC #$12       ; After this instruction, C = 1, A = $34)",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.IsTrue(cpu.Registers.Carry);
            Assert.AreEqual(0x34, cpu.Registers.A);
        }

        [TestMethod]
        public void BruceClarkExample7()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED            ; Decimal mode (BCD subtraction: 40 - 13 = 27)",
                    $"   SEC            ; Note: carry is set, not clear!",
                    $"   LDA #$40",
                    $"   SBC #$13       ; After this instruction, C = 1, A = $27)",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.IsTrue(cpu.Registers.Carry);
            Assert.AreEqual(0x27, cpu.Registers.A);
        }

        [TestMethod]
        public void BruceClarkExample8()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED            ; Decimal mode (BCD subtraction: 32 - 2 - 1 = 29)",
                    $"   CLC            ; Note: carry is set, not clear!",
                    $"   LDA #$32",
                    $"   SBC #$02       ; After this instruction, C = 1, A = $29)",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.IsTrue(cpu.Registers.Carry);
            Assert.AreEqual(0x29, cpu.Registers.A);
        }

        [TestMethod]
        public void BruceClarkExample9()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED            ; Decimal mode (BCD subtraction: 12 - 21)",
                    $"   SEC            ; Note: carry is set, not clear!",
                    $"   LDA #$12",
                    $"   SBC #$21       ; After this instruction, C = 0, A = $91)",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC65C02);
            Assert.IsFalse(cpu.Registers.Carry);
            Assert.AreEqual(0x91, cpu.Registers.A);
        }

        [TestMethod]
        public void BruceClarkExample10()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED            ; Decimal mode (BCD subtraction: 21 - 34)",
                    $"   SEC            ; Note: carry is set, not clear!",
                    $"   LDA #$21",
                    $"   SBC #$34       ; After this instruction, C = 0, A = $87)",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC65C02);
            Assert.IsFalse(cpu.Registers.Carry);
            Assert.AreEqual(0x87, cpu.Registers.A);
        }

        [TestMethod]
        public void BruceClarkExample13()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED",
                    $"   CLC",
                    $"   LDA #$90",
                    $"   ADC #$90",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.IsTrue(cpu.Registers.Overflow);
        }

        [TestMethod]
        public void BruceClarkExample14()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED",
                    $"   SEC",
                    $"   LDA #$01",
                    $"   SBC #$01",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.IsTrue(cpu.Registers.Zero);
            Assert.AreEqual(0x00, cpu.Registers.A);
        }

        [TestMethod]
        public void AppendixA1()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   CLD",
                    $"   CLC",
                    $"   LDA #$99",
                    $"   ADC #$01",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.AreEqual(0x9a, cpu.Registers.A);
            Assert.IsFalse(cpu.Registers.Zero);
        }

        [TestMethod]
        public void AppendixA2()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED            ; decimal mode: 99 + 1",
                    $"   CLC",
                    $"   LDA #$99",
                    $"   ADC #$01",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC65C02);
            Assert.AreEqual(0x00, cpu.Registers.A);
            Assert.IsTrue(cpu.Registers.Zero);
        }

        [TestMethod]
        public void BruceClarkExample99()
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   SED",
                    $"   SEC",
                    $"   LDA #$00",
                    $"   SBC #$00",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.IsTrue(cpu.Registers.Zero);
            Assert.AreEqual(0x00, cpu.Registers.A);
        }

        [TestMethod]
        [DataRow((byte)0x01, (byte)0x01, (byte)0x02, false, false, false)]
        [DataRow((byte)0x01, (byte)0xFF, (byte)0x00, true, true, false)]
        [DataRow((byte)0x7f, (byte)0x01, (byte)0x80, false, false, true)]
        [DataRow((byte)0x80, (byte)0xff, (byte)0x7f, true, false, true)]
        public void ClcAdcOverflowFlagTests(byte xx, byte yy, byte accum, bool c, bool z, bool v)
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   CLD",
                    $"   CLC",
                    $"   LDA #${xx:x2}",
                    $"   ADC #${yy:x2}",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.AreEqual(c, cpu.Registers.Carry);
            Assert.AreEqual(z, cpu.Registers.Zero);
            Assert.AreEqual(v, cpu.Registers.Overflow);

            Assert.AreEqual(accum, cpu.Registers.A);
        }

        [TestMethod]
        [DataRow((byte)0x00, (byte)0x01, (byte)0xff, false, false, false)]
        [DataRow((byte)0x80, (byte)0x01, (byte)0x7f, true, false, true)]
        [DataRow((byte)0x7f, (byte)0xff, (byte)0x80, false, false, true)]
        public void SecSbcOverflowFlagTests(byte xx, byte yy, byte accum, bool c, bool z, bool v)
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   CLD",
                    $"   SEC",
                    $"   LDA #${xx:x2}",
                    $"   SBC #${yy:x2}",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC65C02);
            Assert.AreEqual(c, cpu.Registers.Carry);
            Assert.AreEqual(z, cpu.Registers.Zero);
            Assert.AreEqual(v, cpu.Registers.Overflow);

            Assert.AreEqual(accum, cpu.Registers.A);
        }

        [TestMethod]
        [DataRow((byte)0x3f, (byte)0x40, (byte)0x80, false, false, true)]
        public void SecAdcOverflowFlagTests(byte xx, byte yy, byte accum, bool c, bool z, bool v)
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   CLD",
                    $"   SEC",
                    $"   LDA #${xx:x2}",
                    $"   ADC #${yy:x2}",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.AreEqual(c, cpu.Registers.Carry);
            Assert.AreEqual(z, cpu.Registers.Zero);
            Assert.AreEqual(v, cpu.Registers.Overflow);

            Assert.AreEqual(accum, cpu.Registers.A);
        }

        [TestMethod]
        [DataRow((byte)0xc0, (byte)0x40, (byte)0x7f, true, false, true)]
        public void ClcSbcOverflowFlagTests(byte xx, byte yy, byte accum, bool c, bool z, bool v)
        {
            var bus = new SimpleBus();
            var assembler = new Assembler(
                [
                    $"   CLD",
                    $"   CLC",
                    $"   LDA #${xx:x2}",
                    $"   SBC #${yy:x2}",
                ],
                0x0000
            );
            assembler.Assemble();
            bus.LoadProgramToRam(assembler.ObjectCode, 0);

            var cpu = RunTinyTest(bus, assembler.ProgramByAddress, CpuClass.WDC6502);
            Assert.AreEqual(c, cpu.Registers.Carry);
            Assert.AreEqual(z, cpu.Registers.Zero);
            Assert.AreEqual(v, cpu.Registers.Overflow);

            Assert.AreEqual(accum, cpu.Registers.A);
        }
    }
}

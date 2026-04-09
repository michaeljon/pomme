using InnoWerks.Processors;

namespace InnoWerks.Assemblers.Tests
{
    [TestClass]
    public class AssemblerAddressingModeTests
    {
        //
        // Immediate — #$xx
        //

        [TestMethod]
        public void ImmediateModeParsesHexArgument()
        {
            var assembler = new Assembler(["LABEL LDA #$42"], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(OpCode.LDA, line.OpCode);
            Assert.AreEqual(AddressingMode.Immediate, line.AddressingMode);
        }

        [TestMethod]
        public void ImmediateModeProducesCorrectObjectCode()
        {
            var assembler = new Assembler(["LABEL LDA #$42"], 0x0000);
            assembler.Assemble();

            // LDA immediate = $A9, operand = $42
            CollectionAssert.AreEqual(new byte[] { 0xA9, 0x42 }, assembler.ObjectCode);
        }

        [TestMethod]
        public void ImmediateModeWithDecimalArgument()
        {
            var assembler = new Assembler(["LABEL LDA #66"], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(AddressingMode.Immediate, line.AddressingMode);
        }

        //
        // Zero Page — $xx (two hex digits)
        //

        [TestMethod]
        public void ZeroPageModeParsesAddressArgument()
        {
            var assembler = new Assembler(["LABEL LDA $42"], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(OpCode.LDA, line.OpCode);
            Assert.AreEqual(AddressingMode.ZeroPage, line.AddressingMode);
        }

        [TestMethod]
        public void ZeroPageModeProducesCorrectObjectCode()
        {
            var assembler = new Assembler(["LABEL LDA $42"], 0x0000);
            assembler.Assemble();

            // LDA ZeroPage = $A5, operand = $42
            CollectionAssert.AreEqual(new byte[] { 0xA5, 0x42 }, assembler.ObjectCode);
        }

        //
        // Zero Page,X — $xx,X
        //

        [TestMethod]
        public void ZeroPageXModeParsesArgument()
        {
            var assembler = new Assembler(["LABEL LDA $42,X"], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(OpCode.LDA, line.OpCode);
            Assert.AreEqual(AddressingMode.ZeroPageXIndexed, line.AddressingMode);
        }

        [TestMethod]
        public void ZeroPageXModeProducesCorrectObjectCode()
        {
            var assembler = new Assembler(["LABEL LDA $42,X"], 0x0000);
            assembler.Assemble();

            // LDA ZeroPage,X = $B5, operand = $42
            CollectionAssert.AreEqual(new byte[] { 0xB5, 0x42 }, assembler.ObjectCode);
        }

        //
        // Zero Page,Y — $xx,Y
        //

        [TestMethod]
        public void ZeroPageYModeParsesArgument()
        {
            // LDX is one of the few instructions that supports ZeroPage,Y
            var assembler = new Assembler(["LABEL LDX $42,Y"], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(OpCode.LDX, line.OpCode);
            Assert.AreEqual(AddressingMode.ZeroPageYIndexed, line.AddressingMode);
        }

        [TestMethod]
        public void ZeroPageYModeProducesCorrectObjectCode()
        {
            var assembler = new Assembler(["LABEL LDX $42,Y"], 0x0000);
            assembler.Assemble();

            // LDX ZeroPage,Y = $B6, operand = $42
            CollectionAssert.AreEqual(new byte[] { 0xB6, 0x42 }, assembler.ObjectCode);
        }

        //
        // Absolute — $xxxx (four hex digits)
        //

        [TestMethod]
        public void AbsoluteModeParsesAddress()
        {
            var assembler = new Assembler(["LABEL LDA $1234"], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(OpCode.LDA, line.OpCode);
            Assert.AreEqual(AddressingMode.Absolute, line.AddressingMode);
        }

        [TestMethod]
        public void AbsoluteModeProducesCorrectObjectCodeLittleEndian()
        {
            var assembler = new Assembler(["LABEL LDA $1234"], 0x0000);
            assembler.Assemble();

            // LDA Absolute = $AD, address = $1234 in little-endian
            CollectionAssert.AreEqual(new byte[] { 0xAD, 0x34, 0x12 }, assembler.ObjectCode);
        }

        //
        // Absolute,X — $xxxx,X
        //

        [TestMethod]
        public void AbsoluteXModeParsesArgument()
        {
            var assembler = new Assembler(["LABEL LDA $1234,X"], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(OpCode.LDA, line.OpCode);
            Assert.AreEqual(AddressingMode.AbsoluteXIndexed, line.AddressingMode);
        }

        [TestMethod]
        public void AbsoluteXModeProducesCorrectObjectCode()
        {
            var assembler = new Assembler(["LABEL LDA $1234,X"], 0x0000);
            assembler.Assemble();

            // LDA Absolute,X = $BD
            CollectionAssert.AreEqual(new byte[] { 0xBD, 0x34, 0x12 }, assembler.ObjectCode);
        }

        //
        // Absolute,Y — $xxxx,Y
        //

        [TestMethod]
        public void AbsoluteYModeParsesArgument()
        {
            var assembler = new Assembler(["LABEL LDA $1234,Y"], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(OpCode.LDA, line.OpCode);
            Assert.AreEqual(AddressingMode.AbsoluteYIndexed, line.AddressingMode);
        }

        [TestMethod]
        public void AbsoluteYModeProducesCorrectObjectCode()
        {
            var assembler = new Assembler(["LABEL LDA $1234,Y"], 0x0000);
            assembler.Assemble();

            // LDA Absolute,Y = $B9
            CollectionAssert.AreEqual(new byte[] { 0xB9, 0x34, 0x12 }, assembler.ObjectCode);
        }

        //
        // (Indirect,X) — X-indexed indirect
        //

        [TestMethod]
        public void XIndexedIndirectModeParsesArgument()
        {
            var assembler = new Assembler(["LABEL LDA ($42,X)"], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(OpCode.LDA, line.OpCode);
            Assert.AreEqual(AddressingMode.XIndexedIndirect, line.AddressingMode);
        }

        [TestMethod]
        public void XIndexedIndirectModeProducesCorrectObjectCode()
        {
            var assembler = new Assembler(["LABEL LDA ($42,X)"], 0x0000);
            assembler.Assemble();

            // LDA (ZP,X) = $A1
            CollectionAssert.AreEqual(new byte[] { 0xA1, 0x42 }, assembler.ObjectCode);
        }

        //
        // (Indirect),Y — indirect Y-indexed
        //

        [TestMethod]
        public void IndirectYIndexedModeParsesArgument()
        {
            var assembler = new Assembler(["LABEL LDA ($42),Y"], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(OpCode.LDA, line.OpCode);
            Assert.AreEqual(AddressingMode.IndirectYIndexed, line.AddressingMode);
        }

        [TestMethod]
        public void IndirectYIndexedModeProducesCorrectObjectCode()
        {
            var assembler = new Assembler(["LABEL LDA ($42),Y"], 0x0000);
            assembler.Assemble();

            // LDA (ZP),Y = $B1
            CollectionAssert.AreEqual(new byte[] { 0xB1, 0x42 }, assembler.ObjectCode);
        }

        //
        // Zero Page Indirect — ($xx) [65C02]
        //

        [TestMethod]
        public void ZeroPageIndirectModeParsesArgument()
        {
            var assembler = new Assembler(["LABEL LDA ($42)"], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(OpCode.LDA, line.OpCode);
            Assert.AreEqual(AddressingMode.ZeroPageIndirect, line.AddressingMode);
        }

        [TestMethod]
        public void ZeroPageIndirectModeProducesCorrectObjectCode()
        {
            var assembler = new Assembler(["LABEL LDA ($42)"], 0x0000);
            assembler.Assemble();

            // LDA (ZP) = $B2
            CollectionAssert.AreEqual(new byte[] { 0xB2, 0x42 }, assembler.ObjectCode);
        }

        //
        // Relative — branch instructions
        //

        [TestMethod]
        public void RelativeModeParsesBranchTarget()
        {
            var assembler = new Assembler(
            [
                "LOOP BNE LOOP"
            ], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(OpCode.BNE, line.OpCode);
            Assert.AreEqual(AddressingMode.Relative, line.AddressingMode);
        }

        [TestMethod]
        public void RelativeModeBackwardBranchProducesNegativeOffset()
        {
            // LOOP is at $0000; BNE is 2 bytes; PC after fetch = $0002;
            // branch target = $0000 → offset = 0x0000 - 0x0002 = -2 = 0xFE
            var assembler = new Assembler(
            [
                "LOOP BNE LOOP"
            ], 0x0000);
            assembler.Assemble();

            CollectionAssert.AreEqual(new byte[] { 0xD0, 0xFE }, assembler.ObjectCode);
        }

        [TestMethod]
        public void RelativeModeForwardBranchProducesPositiveOffset()
        {
            // BNE is at $0000 (2 bytes), target NOP is at $0002;
            // PC after fetch = $0002; offset = $0002 - $0002 = 0
            var assembler = new Assembler(
            [
                "LABEL BNE TARGET",
                "TARGET NOP"
            ], 0x0000);
            assembler.Assemble();

            // BNE $D0, offset=0x00, NOP=$EA
            CollectionAssert.AreEqual(new byte[] { 0xD0, 0x00, 0xEA }, assembler.ObjectCode);
        }

        //
        // Indirect — ($xxxx) for JMP
        //

        [TestMethod]
        public void IndirectModeParsesJmpArgument()
        {
            var assembler = new Assembler(["LABEL JMP ($1234)"], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(OpCode.JMP, line.OpCode);
            Assert.AreEqual(AddressingMode.AbsoluteIndirect, line.AddressingMode);
        }

        [TestMethod]
        public void IndirectModeProducesCorrectObjectCode()
        {
            var assembler = new Assembler(["LABEL JMP ($1234)"], 0x0000);
            assembler.Assemble();

            // JMP ($xxxx) = $6C, address little-endian
            CollectionAssert.AreEqual(new byte[] { 0x6C, 0x34, 0x12 }, assembler.ObjectCode);
        }

        //
        // Absolute Indexed Indirect — ($xxxx,X) for JMP [65C02]
        //

        [TestMethod]
        public void AbsoluteIndexedIndirectModeParsesArgument()
        {
            var assembler = new Assembler(["LABEL JMP ($1234,X)"], 0x0000);
            assembler.Assemble();

            var line = assembler.Program[0];
            Assert.AreEqual(OpCode.JMP, line.OpCode);
            Assert.AreEqual(AddressingMode.AbsoluteIndexedIndirect, line.AddressingMode);
        }

        [TestMethod]
        public void AbsoluteIndexedIndirectModeProducesCorrectObjectCode()
        {
            var assembler = new Assembler(["LABEL JMP ($1234,X)"], 0x0000);
            assembler.Assemble();

            // JMP ($xxxx,X) = $7C, address little-endian
            CollectionAssert.AreEqual(new byte[] { 0x7C, 0x34, 0x12 }, assembler.ObjectCode);
        }
    }
}

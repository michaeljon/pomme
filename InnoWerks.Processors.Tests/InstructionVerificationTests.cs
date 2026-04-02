using InnoWerks.Simulators;

namespace InnoWerks.Processors.Tests
{
    [TestClass]
    public class InstructionVerificationTests
    {
        [Ignore]
        [TestMethod]
        public void Verify6502MatchedAddressingModes()
        {
            var instructions = new (OpCode opCode, AddressingMode addressingMode)?[256];
            foreach (var (k, v) in InstructionInformation.Instructions)
            {
                instructions[v] = k;
            }

            for (var o = 0; o <= 255; o++)
            {
                var ocd = CpuInstructions.GetInstructionSet(CpuClass.WDC6502)[(byte)o];

                if (instructions[o] == null)
                {
                    Assert.AreEqual(OpCode.Unknown, ocd.OpCode, $"unassigned has non-Unknown definition: {o:X2}");
                }

                if (instructions[o] != null && ocd.OpCode != OpCode.Unknown)
                {
                    Assert.AreEqual(instructions[o].Value.opCode, ocd.OpCode, $"assigned has mismatched opcodes");
                    Assert.AreEqual(instructions[o].Value.addressingMode, ocd.AddressingMode, $"assigned has mismatched addressing modes");
                }
            }
        }

        [TestMethod]
        public void Verify65C02MatchedAddressingModes()
        {
            var instructions = new (OpCode opCode, AddressingMode addressingMode)?[256];
            foreach (var (k, v) in InstructionInformation.Instructions)
            {
                instructions[v] = k;
            }

            for (var o = 0; o <= 255; o++)
            {
                var ocd = CpuInstructions.GetInstructionSet(CpuClass.WDC65C02)[(byte)o];

                if (instructions[o] == null)
                {
                    Assert.AreEqual(OpCode.Unknown, ocd.OpCode, $"unassigned has non-Unknown definition: {o:X2}");
                }

                if (instructions[o] != null)
                {
                    Assert.AreEqual(instructions[o].Value.opCode, ocd.OpCode, $"assigned has mismatched opcodes: {o:X2}");
                    Assert.AreEqual(instructions[o].Value.addressingMode, ocd.AddressingMode, $"assigned has mismatched addressing modes: {o:X2}");
                }
            }
        }

        [TestMethod]
        public void VerifyR65C02MatchedAddressingModes()
        {
            var instructions = new (OpCode opCode, AddressingMode addressingMode)?[256];
            foreach (var (k, v) in InstructionInformation.Instructions)
            {
                instructions[v] = k;
            }

            for (var o = 0; o <= 255; o++)
            {
                var ocd = CpuInstructions.GetInstructionSet(CpuClass.Rockwell65C02)[(byte)o];

                if (instructions[o] == null)
                {
                    Assert.AreEqual(OpCode.Unknown, ocd.OpCode, $"unassigned has non-Unknown definition: {o:X2}");
                }

                if (instructions[o] != null)
                {
                    Assert.AreEqual(instructions[o].Value.opCode, ocd.OpCode, $"assigned has mismatched opcodes: {o:X2}");
                    Assert.AreEqual(instructions[o].Value.addressingMode, ocd.AddressingMode, $"assigned has mismatched addressing modes: {o:X2}");
                }
            }
        }

        [Ignore]
        [TestMethod]
        public void Verify65SC02MatchedAddressingModes()
        {
            var instructions = new (OpCode opCode, AddressingMode addressingMode)?[256];
            foreach (var (k, v) in InstructionInformation.Instructions)
            {
                instructions[v] = k;
            }

            for (var o = 0; o <= 255; o++)
            {
                var ocd = CpuInstructions.GetInstructionSet(CpuClass.Synertek65C02)[(byte)o];

                if (instructions[o] == null)
                {
                    Assert.AreEqual(OpCode.Unknown, ocd.OpCode, $"unassigned has non-Unknown definition: {o:X2}");
                }

                if (instructions[o] != null)
                {
                    Assert.AreEqual(instructions[o].Value.opCode, ocd.OpCode, $"assigned has mismatched opcodes: {o:X2}");
                    Assert.AreEqual(instructions[o].Value.addressingMode, ocd.AddressingMode, $"assigned has mismatched addressing modes: {o:X2}");
                }
            }
        }

        [TestMethod]
        public void VerifyByteUniqueness()
        {
            var instructions = new (OpCode opCode, AddressingMode addressingMode)?[256];
            foreach (var (k, v) in InstructionInformation.Instructions)
            {
                Assert.IsNull(instructions[v], $"instruction {v:X2} duplicated");
                instructions[v] = k;
            }
        }

        [TestMethod]
        public void Verify6502InstructionCount()
        {
            Assert.AreEqual(256, CpuInstructions.GetInstructionSet(CpuClass.WDC6502).Length);
        }

        [TestMethod]
        public void Verify65C02InstructionCount()
        {
            Assert.AreEqual(256, CpuInstructions.GetInstructionSet(CpuClass.WDC65C02).Length);
        }

        [TestMethod]
        public void Verify65SC02InstructionCount()
        {
            Assert.AreEqual(256, CpuInstructions.GetInstructionSet(CpuClass.Rockwell65C02).Length);
        }

        [TestMethod]
        public void VerifyR65C02InstructionCount()
        {
            Assert.AreEqual(256, CpuInstructions.GetInstructionSet(CpuClass.Synertek65C02).Length);
        }

        [Ignore]
        [TestMethod]
        public void AddressModesAreConsistent()
        {
            Assert.AreEqual(-1, CompareAddressModes(OpCode6502.Instructions, OpCode65C02.Instructions), "6502 v 65C02");
            Assert.AreEqual(-1, CompareAddressModes(OpCode6502.Instructions, OpCode65SC02.Instructions), "6502 v 65SC02");
            Assert.AreEqual(-1, CompareAddressModes(OpCode6502.Instructions, OpCodeR65C02.Instructions), "6502 v R65C02");

            Assert.AreEqual(-1, CompareAddressModes(OpCode65C02.Instructions, OpCode65SC02.Instructions), "65C02 v 65SC02");
            Assert.AreEqual(-1, CompareAddressModes(OpCode65C02.Instructions, OpCodeR65C02.Instructions), "65C02 v R65C02");

            Assert.AreEqual(-1, CompareAddressModes(OpCode65SC02.Instructions, OpCodeR65C02.Instructions), "65SC02 v R65C02");
        }

        private static int CompareAddressModes(OpCodeDefinition[] left, OpCodeDefinition[] right)
        {
            for (var i = 0; i < 256; i++)
            {
                if (left[i].AddressingMode != right[i].AddressingMode)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

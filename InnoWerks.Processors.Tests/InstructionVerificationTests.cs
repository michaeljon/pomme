using InnoWerks.Simulators;

namespace InnoWerks.Processors.Tests
{
    [TestClass]
    public class InstructionVerificationTests
    {
        [TestMethod]
        public void Verify6502MatchedAddressingModes()
        {
            var instructions = new (OpCode opCode, AddressingMode addressingMode)?[256];
            foreach (var (k, v) in InstructionInformation.Instructions)
            {
                if (v.minCpuClass <= CpuClass.WDC6502)
                {
                    instructions[v.code] = k;
                }
            }

            for (var o = 0; o <= 255; o++)
            {
                var ocd = CpuInstructions.OpCode6502[(byte)o];

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
                if (v.minCpuClass <= CpuClass.WDC65C02)
                {
                    instructions[v.code] = k;
                }
            }

            for (var o = 0; o <= 255; o++)
            {
                var ocd = CpuInstructions.OpCode65C02[(byte)o];

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
                Assert.IsNull(instructions[v.code], $"instruction {v:X2} duplicated");
                instructions[v.code] = k;
            }
        }

        [TestMethod]
        public void Verify6502InstructionCount()
        {
            Assert.AreEqual(256, CpuInstructions.OpCode6502.Length);
        }

        [TestMethod]
        public void Verify65C02InstructionCount()
        {
            Assert.AreEqual(256, CpuInstructions.OpCode65C02.Length);
        }
    }
}

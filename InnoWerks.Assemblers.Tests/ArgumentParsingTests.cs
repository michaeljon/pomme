using InnoWerks.Processors;

namespace InnoWerks.Assemblers.Tests
{
    [TestClass]
    public class ArgumentParsingTests
    {
        [TestMethod]
        public void CanReadImplied()
        {
            foreach (var opCode in InstructionInformation.ImpliedOperations)
            {
                var assembler = new Assembler(
                    [
                        $"LABEL {opCode}        ; implied operation, no value"
                    ],
                    0x0000
                );
                assembler.Assemble();

                var lineInformation = assembler.Program[0];

                Assert.AreEqual(opCode, lineInformation.OpCode);
                Assert.AreEqual(AddressingMode.Implicit, lineInformation.AddressingMode);
                Assert.IsNull(lineInformation.Value);

                var expectedInstructionCode = InstructionInformation.Instructions[(opCode, lineInformation.AddressingMode)];
                CollectionAssert.AreEqual(new byte[] { expectedInstructionCode }, assembler.ObjectCode);
            }
        }

        [TestMethod]
        public void CanReadImplicitAccumulator()
        {
            foreach (var opCode in InstructionInformation.AccumulatorOperations)
            {
                var assembler = new Assembler(
                    [
                        $"LABEL {opCode}        ; accumulator operation, no value"
                    ],
                    0x0000
                );
                assembler.Assemble();

                var lineInformation = assembler.Program[0];
                var program = assembler.ObjectCode;

                var expectedOpCode = opCode switch
                {
                    OpCode.ASL => OpCode.ASL_A,
                    OpCode.DEA => OpCode.DEA,
                    OpCode.INA => OpCode.INA,
                    OpCode.LSR => OpCode.LSR_A,
                    OpCode.ROL => OpCode.ROL_A,
                    OpCode.ROR => OpCode.ROR_A,

                    _ => throw new InvalidInstructionFormatException("Accumulator mode without ASL/DEC/INC/LSR/ROL/ROR ")
                };

                Assert.AreEqual(expectedOpCode, lineInformation.OpCode);
                Assert.AreEqual(AddressingMode.Accumulator, lineInformation.AddressingMode);
                Assert.IsNull(lineInformation.Value);

                var expectedOperation = InstructionInformation.Instructions[(expectedOpCode, AddressingMode.Accumulator)];
                CollectionAssert.AreEqual(new byte[] { expectedOperation }, assembler.ObjectCode);
            }
        }

        [TestMethod]
        public void CanReadExplicitAccumulator()
        {
            foreach (var opCode in InstructionInformation.AccumulatorOperations)
            {
                var assembler = new Assembler(
                    [
                        $"LABEL {opCode} A      ; accumulator operation, no value"
                    ],
                    0x0000
                );
                assembler.Assemble();

                var lineInformation = assembler.Program[0];

                var expectedOpCode = opCode switch
                {
                    OpCode.ASL => OpCode.ASL_A,
                    OpCode.DEA => OpCode.DEA,
                    OpCode.INA => OpCode.INA,
                    OpCode.LSR => OpCode.LSR_A,
                    OpCode.ROL => OpCode.ROL_A,
                    OpCode.ROR => OpCode.ROR_A,

                    _ => throw new InvalidInstructionFormatException("Accumulator mode without ASL/DEC/INC/LSR/ROL/ROR ")
                };

                Assert.AreEqual(expectedOpCode, lineInformation.OpCode);
                Assert.AreEqual(AddressingMode.Accumulator, lineInformation.AddressingMode);
                Assert.IsNull(lineInformation.Value);

                var expectedOperation = InstructionInformation.Instructions[(expectedOpCode, AddressingMode.Accumulator)];
                CollectionAssert.AreEqual(new byte[] { expectedOperation }, assembler.ObjectCode);
            }
        }

        [TestMethod]
        public void CanReadStack()
        {
            foreach (var opCode in InstructionInformation.StackOperations)
            {
                var assembler = new Assembler(
                    [
                        $"LABEL {opCode}        ; stack operation, no value"
                    ],
                    0x0000
                );
                assembler.Assemble();

                var lineInformation = assembler.Program[0];

                Assert.AreEqual(opCode, lineInformation.OpCode);
                Assert.AreEqual(AddressingMode.Stack, lineInformation.AddressingMode);
                Assert.IsNull(lineInformation.Value);

                var expectedInstructionCode = InstructionInformation.Instructions[(opCode, lineInformation.AddressingMode)];
                CollectionAssert.AreEqual(new byte[] { expectedInstructionCode }, assembler.ObjectCode);
            }
        }
    }
}

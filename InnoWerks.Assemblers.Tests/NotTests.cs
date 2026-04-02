using InnoWerks.Processors;

namespace InnoWerks.Assemblers.Tests
{
    [TestClass]
    public class NotTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void GenerateOpTable()
        {
            var instructions = new (OpCode opCode, AddressingMode addressingMode)[256];
            foreach (var (k, v) in InstructionInformation.Instructions)
            {
                instructions[v] = k;
            }

            TestContext.WriteLine("\r");

            GenerateHeaderFooter();

            for (var row = 0; row <= 0x0f; row++)
            {
                TestContext.Write($"|  {row:x1}  |");
                for (var col = 0; col <= 0x0f; col++)
                {
                    var index = (byte)(row << 4 | col);
                    var (opCode, _) = instructions[index];
                    var disp = opCode != OpCode.Unknown ? opCode.ToString() : "   ";
                    disp = disp.Substring(0, 3);

                    TestContext.Write(disp.Length == 3 ? $"   {disp}   " : $"   {disp}  ");
                    TestContext.Write("|");
                }

                TestContext.WriteLine("\r");

                TestContext.Write($"|     |");
                for (var col = 0; col <= 0x0f; col++)
                {
                    var index = (byte)(row << 4 | col);
                    var (_, addressingMode) = instructions[index];

                    TestContext.Write($"{AddressModeLookup.GetDisplay(addressingMode)}");
                    TestContext.Write("|");
                }

                TestContext.WriteLine("\r");
                GenerateSeparator();
            }

            GenerateHeaderFooter(true);
        }

        private void GenerateHeaderFooter(bool last = false)
        {
            if (last == false)
            {
                GenerateSeparator();
            }

            TestContext.Write("|     |");
            for (var col = 0; col <= 0x0f; col++)
            {
                TestContext.Write($"    {col:x1}    ");
                TestContext.Write("|");
            }
            TestContext.WriteLine("\r");

            GenerateSeparator();
        }

        private void GenerateSeparator()
        {
            TestContext.Write($"|-----|");
            for (var col = 0; col <= 0x0f; col++)
            {
                TestContext.Write($"---------");
                TestContext.Write("|");
            }
            TestContext.WriteLine("\r");
        }
    }
}

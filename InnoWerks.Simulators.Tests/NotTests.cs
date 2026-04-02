using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using InnoWerks.Assemblers;
using InnoWerks.Processors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CA1823, RCS1213

namespace InnoWerks.Simulators.Tests
{
    [TestClass]
    public class NotTests
    {
        public TestContext TestContext { get; set; }

        private static readonly Dictionary<int, ushort> lineToBaseAddress = new()
        {
            {0, 0x400}, {1, 0x480}, {2, 0x500}, {3, 0x580}, {4, 0x600}, {5, 0x680}, {6, 0x700}, {7, 0x780},
            {8, 0x428}, {9, 0x4a8}, {10, 0x528}, {11, 0x5a8}, {12, 0x628}, {13, 0x6a8}, {14, 0x728}, {15, 0x7a8},
            {16, 0x450}, {17, 0x4d0}, {18, 0x550}, {19, 0x5d0}, {20, 0x650}, {21, 0x6d0}, {22, 0x750}, {23, 0x7d0},
        };

        private static readonly (ushort lo, ushort hi, int line)[] baseAddressToLine =
        [
            (0x400, 0x400 + 0x28, 0), (0x480, 0x480 + 0x28, 1), (0x500, 0x500 + 0x28, 2), (0x580, 0x580 + 0x28, 3),
            (0x600, 0x600 + 0x28, 4), (0x680, 0x680 + 0x28, 5), (0x700, 0x700 + 0x28, 6), (0x780, 0x780 + 0x28, 7),

            (0x428, 0x428 + 0x28, 8), (0x4a8, 0x4a8 + 0x28, 9), (0x528, 0x528 + 0x28, 10), (0x5a8, 0x5a8 + 0x28, 11),
            (0x628, 0x628 + 0x28, 12), (0x6a8, 0x6a8 + 0x28, 13), (0x728, 0x728 + 0x28, 14), (0x7a8, 0x7a8 + 0x28, 15),

            (0x450, 0x450 + 0x28, 16), (0x4d0, 0x4d0 + 0x28, 17), (0x550, 0x550 + 0x28, 18), (0x5d0, 0x5d0 + 0x28, 19),
            (0x650, 0x650 + 0x28, 20), (0x6d0, 0x6d0 + 0x28, 21), (0x750, 0x750 + 0x28, 22), (0x7d0, 0x7d0 + 0x28, 23),
        ];

        [TestMethod]
        public void WhereAmI()
        {
            Console.WriteLine(TestContext.TestResultsDirectory);
        }

        [TestMethod]
        public void Generate6502OpCodeTable()
        {
            GenerateOpTable(CpuInstructions.GetInstructionSet(CpuClass.WDC6502));
        }

        [TestMethod]
        public void Generate65C02OpCodeTable()
        {
            GenerateOpTable(CpuInstructions.GetInstructionSet(CpuClass.WDC65C02));
        }

        [TestMethod]
        public void Generate65SC02OpCodeTable()
        {
            GenerateOpTable(CpuInstructions.GetInstructionSet(CpuClass.Synertek65C02));
        }

        [TestMethod]
        public void GenerateR65C02OpCodeTable()
        {
            GenerateOpTable(CpuInstructions.GetInstructionSet(CpuClass.Rockwell65C02));
        }

        [TestMethod]
        public void LineGeneratorWorks()
        {
            // top 1/3
            Assert.AreEqual(0x0400, lineToBaseAddress[0]);
            Assert.AreEqual(0x0480, lineToBaseAddress[1]);
            Assert.AreEqual(0x0500, lineToBaseAddress[2]);
            Assert.AreEqual(0x0580, lineToBaseAddress[3]);
            Assert.AreEqual(0x0600, lineToBaseAddress[4]);
            Assert.AreEqual(0x0680, lineToBaseAddress[5]);
            Assert.AreEqual(0x0700, lineToBaseAddress[6]);
            Assert.AreEqual(0x0780, lineToBaseAddress[7]);

            // middle 1/3
            Assert.AreEqual(0x0428, lineToBaseAddress[8]);
            Assert.AreEqual(0x04a8, lineToBaseAddress[9]);
            Assert.AreEqual(0x0528, lineToBaseAddress[10]);
            Assert.AreEqual(0x05a8, lineToBaseAddress[11]);
            Assert.AreEqual(0x0628, lineToBaseAddress[12]);
            Assert.AreEqual(0x06a8, lineToBaseAddress[13]);
            Assert.AreEqual(0x0728, lineToBaseAddress[14]);
            Assert.AreEqual(0x07a8, lineToBaseAddress[15]);

            // bottom 1/3
            Assert.AreEqual(0x0450, lineToBaseAddress[16]);
            Assert.AreEqual(0x04d0, lineToBaseAddress[17]);
            Assert.AreEqual(0x0550, lineToBaseAddress[18]);
            Assert.AreEqual(0x05d0, lineToBaseAddress[19]);
            Assert.AreEqual(0x0650, lineToBaseAddress[20]);
            Assert.AreEqual(0x06d0, lineToBaseAddress[21]);
            Assert.AreEqual(0x0750, lineToBaseAddress[22]);
            Assert.AreEqual(0x07d0, lineToBaseAddress[23]);
        }

        [TestMethod]
        public void FastLineGeneratorWorks()
        {
            // top 1/3
            Assert.AreEqual(0x0400, AddressFromPageRowCol(false, 0, 0));
            Assert.AreEqual(0x0481, AddressFromPageRowCol(false, 1, 1));
            Assert.AreEqual(0x0502, AddressFromPageRowCol(false, 2, 2));
            Assert.AreEqual(0x0583, AddressFromPageRowCol(false, 3, 3));
            Assert.AreEqual(0x0604, AddressFromPageRowCol(false, 4, 4));
            Assert.AreEqual(0x0685, AddressFromPageRowCol(false, 5, 5));
            Assert.AreEqual(0x0706, AddressFromPageRowCol(false, 6, 6));
            Assert.AreEqual(0x0787, AddressFromPageRowCol(false, 7, 7));

            // middle 1/3
            Assert.AreEqual(0x0428, AddressFromPageRowCol(false, 8, 0));
            Assert.AreEqual(0x04a8, AddressFromPageRowCol(false, 9, 0));
            Assert.AreEqual(0x0528, AddressFromPageRowCol(false, 10, 0));
            Assert.AreEqual(0x05a8, AddressFromPageRowCol(false, 11, 0));
            Assert.AreEqual(0x0628, AddressFromPageRowCol(false, 12, 0));
            Assert.AreEqual(0x06a8, AddressFromPageRowCol(false, 13, 0));
            Assert.AreEqual(0x0728, AddressFromPageRowCol(false, 14, 0));
            Assert.AreEqual(0x07a8, AddressFromPageRowCol(false, 15, 0));

            // bottom 1/3
            Assert.AreEqual(0x0450, AddressFromPageRowCol(false, 16, 0));
            Assert.AreEqual(0x04d0, AddressFromPageRowCol(false, 17, 0));
            Assert.AreEqual(0x0550, AddressFromPageRowCol(false, 18, 0));
            Assert.AreEqual(0x05d0, AddressFromPageRowCol(false, 19, 0));
            Assert.AreEqual(0x0650, AddressFromPageRowCol(false, 20, 0));
            Assert.AreEqual(0x06d0, AddressFromPageRowCol(false, 21, 0));
            Assert.AreEqual(0x0750, AddressFromPageRowCol(false, 22, 0));
            Assert.AreEqual(0x07d0, AddressFromPageRowCol(false, 23, 0));
        }

        [TestMethod]
        public void RowColFromAddress()
        {
            // top 1/3
            Assert.AreEqual((0, 0), GenerateRowColFromAddress(0x0400));
            Assert.AreEqual((0, 20), GenerateRowColFromAddress(0x0400 + 20));

            // middle 1/3
            Assert.AreEqual((11, 0), GenerateRowColFromAddress(0x05a8));
            Assert.AreEqual((11, 20), GenerateRowColFromAddress(0x05a8 + 20));

            // bottom 1/3
            Assert.AreEqual((20, 0), GenerateRowColFromAddress(0x0650));
            Assert.AreEqual((20, 20), GenerateRowColFromAddress(0x0650 + 20));
        }

        /*
            F847: 48        141  GBASCALC PHA
            F848: 4A        142           LSR
            F849: 29 03     143           AND   #$03
            F84B: 09 04     144           ORA   #$04
            F84D: 85 27     145           STA   GBASH
            F84F: 68        146           PLA
            F850: 29 18     147           AND   #$18
            F852: 90 02     148           BCC   GBCALC
            F854: 69 7F     149           ADC   #$7F
            F856: 85 26     150  GBCALC   STA   GBASL
            F858: 0A        151           ASL
            F859: 0A        152           ASL
            F85A: 05 26     153           ORA   GBASL
            F85C: 85 26     154           STA   GBASL
            F85E: 60        155           RTS
        */
        private static ushort GenerateLineFromAddress(int lineNumber)
        {
            int gbash = ((lineNumber >> 1) & 0x03) | 0x04; // | 0x05 for p.2
            int gbasl = lineNumber & 0x18;
            if ((lineNumber & 0x01) == 0x01)
            {
                gbasl |= 0x80;
            }
            gbasl = ((gbasl << 2) | gbasl) & 0xff;

            return (ushort)((gbash << 8) | gbasl);
        }

        private static ushort GenerateLineFromAddress2(int lineNumber)
        {
            return lineToBaseAddress[lineNumber];
        }

        private static (int row, int col) GenerateRowColFromAddress(ushort address)
        {
            foreach (var (lo, hi, line) in baseAddressToLine)
            {
                if (lo <= address && address < hi)
                {
                    return (line, address - lo);
                }
            }

            return (0, 0);
        }
        private static ushort AddressFromPageRowCol(bool page2, int row, int col)
        {
            int pageOffset = page2 ? 0x800 : 0x400;

            int[] textRowBase =
            [
                0x000, 0x080, 0x100, 0x180,
                0x200, 0x280, 0x300, 0x380
            ];

            return (ushort)(
                pageOffset +
                textRowBase[row & 0x07] +
                (row >> 3) * 40 +
                col
            );
        }

        private void GenerateOpTable(OpCodeDefinition[] opCodeTable)
        {
            TestContext.WriteLine("\r");
            GenerateHeaderFooter();

            for (var row = 0; row <= 0x0f; row++)
            {
                TestContext.Write($"|  {row:x1}  |");
                for (var col = 0; col <= 0x0f; col++)
                {
                    var index = (byte)(row << 4 | col);
                    var ocd = opCodeTable[index];
                    var disp = ocd.OpCode != OpCode.Unknown ? ocd.OpCode.ToString() : "   ";
                    disp = disp.Substring(0, 3);

                    TestContext.Write(disp.Length == 3 ? $"   {disp}   " : $"   {disp}  ");

                    TestContext.Write("|");
                }

                TestContext.WriteLine("\r");

                TestContext.Write($"|     |");
                for (var col = 0; col <= 0x0f; col++)
                {
                    var opcode = (byte)(row << 4 | col);
                    var ocd = opCodeTable[opcode];

                    TestContext.Write($"{AddressModeLookup.GetDisplay(ocd.AddressingMode)}");
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

        [TestMethod]
        public void RegexSwitch()
        {
            var commandRegex = new Regex(
                "^((?<command>t) (?<steps>[0-9]+))$?|" +
                "^((?<command>pc) (?<addr>[a-f0-9]{4}))$?|" +
                "^((?<command>jsr) (?<addr>[a-f0-9]{4}))$?|" +
                "^(?<command>sb (?<addr>[a-f0-9a-f]{4}))$?|" +
                "^(?<command>cb (?<addr>[a-f0-9]{4}))$?|" +
                "^(?<command>ca)$?|" +
                "^(?<command>lb)$?|" +
                "^(?<command>df)$?|" +
                "^(?<command>sf (?<flag>[cnvz]))$?|" +
                "^(?<command>cf (?<flag>[cnvz]))$?|" +
                "^(?<command>dr)$?|" +
                "^(?<command>sr (?<register>[axys]) (?<value>[a-f0-9]{1,2}))$?|" +
                "^(?<command>zr (?<register>[axys]))$?|" +
                "^(?<command>w (?<addr>[a-f0-9]{1,4}) (?<values>[a-f0-9]{1,2}( [a-f0-9]{1,2})*))$?|" +
                "^(?<command>r (?<addr>[a-f0-9]{1,4}) (?<len>[0-9]*))$?|" +
                "^(?<command>d (?<page>[a-f0-9]{1,2}))$?|" +
                "^(?<command>o ts (?<speed>[0-9]+))?|$" +
                "^(?<command>o tv (?<flag>(true|false)))$?|" +
                "^(?<command>(q|quit))$?|" +
                "^(?<command>g)$?|" +
                "^(?<command>s)$?|" +
                "^(?<command>(\\?|h))$?"
            );

            commandRegex.IsMatch("t 10");
            commandRegex.MatchNamedCaptures("t 10");
        }
    }
}

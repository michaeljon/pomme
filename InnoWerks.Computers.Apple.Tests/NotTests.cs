using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CA1823, RCS1213

namespace InnoWerks.Computers.Apple
{
    [TestClass]
    public class NotTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void WriteSoftSwitchListByAddress()
        {
            using (var f = File.CreateText("SoftSwitchListByAddress.tsv"))
            {
                foreach (var (key, value) in SoftSwitchAddress.Lookup.OrderBy(p => p.Key))
                {
                    f.WriteLine($"{value,-22} ${key:X4}");
                }
            }
        }

        [TestMethod]
        public void WriteSoftSwitchListByName()
        {
            using (var f = File.CreateText("SoftSwitchListByName.tsv"))
            {
                foreach (var (key, value) in SoftSwitchAddress.Lookup.OrderBy(p => p.Value))
                {
                    f.WriteLine($"{value,-22} ${key:X4}");
                }
            }
        }

        [TestMethod]
        public void GenerateReverseEncodingTable()
        {
            byte[] ENCODING_TABLE =
            [
                0x96, 0x97, 0x9A, 0x9B, 0x9D, 0x9E, 0x9F, 0xA6,
                0xA7, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF, 0xB2, 0xB3,
                0xB4, 0xB5, 0xB6, 0xB7, 0xB9, 0xBA, 0xBB, 0xBC,
                0xBD, 0xBE, 0xBF, 0xCB, 0xCD, 0xCE, 0xCF, 0xD3,
                0xD6, 0xD7, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE,
                0xDF, 0xE5, 0xE6, 0xE7, 0xE9, 0xEA, 0xEB, 0xEC,
                0xED, 0xEE, 0xEF, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6,
                0xF7, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF
            ];

            var ENCODING_TABLE_REVERSED = new byte[256];
            for (int i = 0; i < ENCODING_TABLE.Length; i++)
            {
                ENCODING_TABLE_REVERSED[ENCODING_TABLE[i] & 0xff] = (byte)(0xff & i);
            }

            TestContext.WriteLine("public static readonly byte[] DENCODING_TABLE =");
            TestContext.WriteLine("[");

            for (var r = 0; r < 16; r++)
            {
                for (var c = 0; c < 26; c++)
                {
                    TestContext.Write($"0x{r * c:X2}, ");
                }
                TestContext.WriteLine("");
            }

            TestContext.WriteLine("];");
        }
    }
}

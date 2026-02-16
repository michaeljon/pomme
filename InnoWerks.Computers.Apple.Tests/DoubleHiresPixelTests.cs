using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple
{
    [TestClass]
    public class DoubleHiresPixelTests
    {
        private static readonly byte[] flipped =
        [
            0b0000, // 0000 black
            0b1000, // 0001 magenta
            0b0100, // 0010 brown
            0b1100, // 0011 orange
            0b0010, // 0100 dark green
            0b1010, // 0101 grey1
            0b0110, // 0110 green
            0b1110, // 0111 yellow

            0b0001, // 1000 dark blue
            0b1001, // 1001 violet
            0b0101, // 1010 grey2
            0b1101, // 1011 pink
            0b0011, // 1100 medium blue
            0b1011, // 1101 light blue
            0b0111, // 1110 aqua
            0b1111, // 1111 white
        ];

        private static readonly string[] colors =
        [
            "black", // 0000
            "magenta", // 0001
            "brown", // 0010
            "orange", // 0011
            "dark green", // 0100
            "grey1", // 0101
            "green", // 0110
            "yellow", // 0111

            "dark blue", // 1000
            "violet", // 1001
            "grey2", // 1010
            "pink", // 1011
            "medium blue", // 1100
            "light blue", // 1101
            "aqua", // 1110
            "white", // 1111
        ];

        private static byte[] GetPixels(byte b0, byte b1, byte b2, byte b3)
        {
            uint word = (uint)(b3 << 21 |
                              (b2 & 0x7f) << 14 |
                              (b1 & 0x7f) << 7 |
                              (b0 & 0x7f));

            var pixels = new byte[7];
            for (var p = 0; p < 7; p++)
            {
                pixels[p] = flipped[(byte)(word >> (p * 4) & 0x0F)];
            }

            return pixels;
        }

        private static byte[] GetPixels(byte[] bytes)
        {
            uint word = (uint)(bytes[3] << 21 |
                              (bytes[2] & 0x7f) << 14 |
                              (bytes[1] & 0x7f) << 7 |
                              (bytes[0] & 0x7f));

            var pixels = new byte[7];
            for (var p = 0; p < 7; p++)
            {
                pixels[p] = flipped[(byte)(word >> (p * 4) & 0x0F)];
            }

            return pixels;
        }

        [DataTestMethod]
        [DataRow("Black", (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0b0000)]
        [DataRow("Magenta", (byte)0x08, (byte)0x11, (byte)0x22, (byte)0x44, (byte)0b0001)]
        [DataRow("Brown", (byte)0x44, (byte)0x08, (byte)0x11, (byte)0x22, (byte)0b0010)]
        [DataRow("Orange", (byte)0x4C, (byte)0x19, (byte)0x33, (byte)0x66, (byte)0b0011)]
        [DataRow("Dark Green", (byte)0x22, (byte)0x44, (byte)0x08, (byte)0x11, (byte)0b0100)]
        [DataRow("Grey1", (byte)0x2A, (byte)0x55, (byte)0x2A, (byte)0x55, (byte)0b0101)]
        [DataRow("Green", (byte)0x66, (byte)0x4C, (byte)0x19, (byte)0x33, (byte)0b0110)]
        [DataRow("Yellow", (byte)0x6E, (byte)0x5D, (byte)0x3B, (byte)0x77, (byte)0b0111)]
        [DataRow("Dark Blue", (byte)0x11, (byte)0x22, (byte)0x44, (byte)0x08, (byte)0b1000)]
        [DataRow("Violet", (byte)0x19, (byte)0x33, (byte)0x66, (byte)0x4C, (byte)0b1001)]
        [DataRow("Grey2", (byte)0x55, (byte)0x2A, (byte)0x55, (byte)0x2A, (byte)0b1010)]
        [DataRow("Pink", (byte)0x5D, (byte)0x3B, (byte)0x77, (byte)0x6E, (byte)0b1011)]
        [DataRow("Medium Blue", (byte)0x33, (byte)0x66, (byte)0x4C, (byte)0x19, (byte)0b1100)]
        [DataRow("Light Blue", (byte)0x3B, (byte)0x77, (byte)0x6E, (byte)0x5D, (byte)0b1101)]
        [DataRow("Aqua", (byte)0x77, (byte)0x6E, (byte)0x5D, (byte)0x3B, (byte)0b1110)]
        [DataRow("White", (byte)0x7F, (byte)0x7F, (byte)0x7F, (byte)0x7F, (byte)0b1111)]
        public void TestIndividual(string color, byte aux1, byte main1, byte aux2, byte main2, byte pattern)
        {
            ArgumentException.ThrowIfNullOrEmpty(color);

            var pixels = GetPixels(aux1, main1, aux2, main2);
            Assert.AreEqual(7, pixels.Length);

            for (var p = 0; p < pixels.Length; p++)
            {
                Assert.AreEqual(pattern, pixels[p]);
                Assert.AreEqual(color.ToLowerInvariant(), colors[pixels[p]].ToLowerInvariant());
            }
        }

        [DataTestMethod]
        [DataRow("Black", (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0b0000)]
        [DataRow("Magenta", (byte)0x08, (byte)0x11, (byte)0x22, (byte)0x44, (byte)0b0001)]
        [DataRow("Brown", (byte)0x44, (byte)0x08, (byte)0x11, (byte)0x22, (byte)0b0010)]
        [DataRow("Orange", (byte)0x4C, (byte)0x19, (byte)0x33, (byte)0x66, (byte)0b0011)]
        [DataRow("Dark Green", (byte)0x22, (byte)0x44, (byte)0x08, (byte)0x11, (byte)0b0100)]
        [DataRow("Grey1", (byte)0x2A, (byte)0x55, (byte)0x2A, (byte)0x55, (byte)0b0101)]
        [DataRow("Green", (byte)0x66, (byte)0x4C, (byte)0x19, (byte)0x33, (byte)0b0110)]
        [DataRow("Yellow", (byte)0x6E, (byte)0x5D, (byte)0x3B, (byte)0x77, (byte)0b0111)]
        [DataRow("Dark Blue", (byte)0x11, (byte)0x22, (byte)0x44, (byte)0x08, (byte)0b1000)]
        [DataRow("Violet", (byte)0x19, (byte)0x33, (byte)0x66, (byte)0x4C, (byte)0b1001)]
        [DataRow("Grey2", (byte)0x55, (byte)0x2A, (byte)0x55, (byte)0x2A, (byte)0b1010)]
        [DataRow("Pink", (byte)0x5D, (byte)0x3B, (byte)0x77, (byte)0x6E, (byte)0b1011)]
        [DataRow("Medium Blue", (byte)0x33, (byte)0x66, (byte)0x4C, (byte)0x19, (byte)0b1100)]
        [DataRow("Light Blue", (byte)0x3B, (byte)0x77, (byte)0x6E, (byte)0x5D, (byte)0b1101)]
        [DataRow("Aqua", (byte)0x77, (byte)0x6E, (byte)0x5D, (byte)0x3B, (byte)0b1110)]
        [DataRow("White", (byte)0x7F, (byte)0x7F, (byte)0x7F, (byte)0x7F, (byte)0b1111)]
        public void TestBatch(string color, byte aux1, byte main1, byte aux2, byte main2, byte pattern)
        {
            ArgumentException.ThrowIfNullOrEmpty(color);

            var pixels = GetPixels([aux1, main1, aux2, main2]);
            Assert.AreEqual(7, pixels.Length);

            for (var p = 0; p < pixels.Length; p++)
            {
                Assert.AreEqual(pattern, pixels[p]);
                Assert.AreEqual(color.ToLowerInvariant(), colors[pixels[p]].ToLowerInvariant());
            }
        }
    }
}

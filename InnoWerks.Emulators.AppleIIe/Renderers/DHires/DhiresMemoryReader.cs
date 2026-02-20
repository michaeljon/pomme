using System;
using InnoWerks.Computers.Apple;

#pragma warning disable CA1819 // Properties should not return arrays

namespace InnoWerks.Emulators.AppleIIe
{
    public sealed class DhiresMemoryReader
    {
        private readonly Memory128k ram;
        private readonly MachineState machineState;

        private readonly int page;

        public DhiresMemoryReader(Memory128k ram, MachineState machineState, int page)
        {
            this.ram = ram;
            this.machineState = machineState;
            this.page = page;
        }

        public static ushort[] RowOffsets
        {
            get
            {
                if (field == null)
                {
                    field = new ushort[192];

                    for (var y = 0; y < 192; y++)
                    {
                        field[y] = (ushort)(((y & 0x07) << 10) +
                                           (((y >> 3) & 0x07) << 7) +
                                           ((y >> 6) * 40));
                    }
                }

                return field;
            }
        }

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

        public void ReadDhiresPage(DhiresBuffer buffer, int rows = 192)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            var main = ram.GetMain((byte)(page == 2 ? 0x40 : 0x20), 32);
            var aux = ram.GetAux((byte)(page == 2 ? 0x40 : 0x20), 32);

            for (int y = 0; y < rows; y++)
            {
                var rawBytes = new byte[80];
                for (var b = 0; b < 40; b++)
                {
                    rawBytes[(b * 2) + 0] = aux[RowOffsets[y] + b];
                    rawBytes[(b * 2) + 1] = main[RowOffsets[y] + b];
                }

                var x = 0;
                for (var b = 0; b < 80; b += 4)
                {
                    var pixels = GetPixels(rawBytes[b + 0], rawBytes[b + 1], rawBytes[b + 2], rawBytes[b + 3]);

                    buffer.SetPixel(y, x++, pixels[0]);
                    buffer.SetPixel(y, x++, pixels[1]);
                    buffer.SetPixel(y, x++, pixels[2]);
                    buffer.SetPixel(y, x++, pixels[3]);
                    buffer.SetPixel(y, x++, pixels[4]);
                    buffer.SetPixel(y, x++, pixels[5]);
                    buffer.SetPixel(y, x++, pixels[6]);
                }
            }
        }
    }
}

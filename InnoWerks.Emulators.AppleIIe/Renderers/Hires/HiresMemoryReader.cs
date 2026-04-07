using System;
using InnoWerks.Computers.Apple;

#pragma warning disable CA1819 // Properties should not return arrays

namespace InnoWerks.Emulators.AppleIIe
{
    public sealed class HiresMemoryReader
    {
        private readonly Computer computer;

        private readonly int page;

        public HiresMemoryReader(Computer computer, int page)
        {
            this.computer = computer;
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

        public void ReadHiresPage(HiresBuffer buffer, int rows = 192)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            var memory = computer.Memory.Read((byte)(page == 2 ? 0x40 : 0x20), 32);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < 40; x++)
                {
                    byte b = memory[RowOffsets[y] + x];
                    buffer.SetByte(y, x, b);
                }
            }
        }
    }
}

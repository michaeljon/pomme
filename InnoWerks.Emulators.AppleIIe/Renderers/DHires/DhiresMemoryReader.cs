using System;
using InnoWerks.Computers.Apple;

namespace InnoWerks.Emulators.AppleIIe
{
    public sealed class DhiresMemoryReader
    {
        private readonly Memory128k ram;
        private readonly MachineState machineState;

        private ushort[] rowOffsets;

        public DhiresMemoryReader(Memory128k ram, MachineState machineState)
        {
            this.ram = ram;
            this.machineState = machineState;
        }

        private ushort[] RowOffsets
        {
            get
            {
                if (rowOffsets == null)
                {
                    rowOffsets = new ushort[192];

                    for (var y = 0; y < 192; y++)
                    {
                        rowOffsets[y] = (ushort)(((y & 0x07) << 10) +
                                                 (((y >> 3) & 0x07) << 7) +
                                                 ((y >> 6) * 40));
                    }
                }

                return rowOffsets;
            }
        }

        public void ReadDhiresPage(DhiresBuffer buffer)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            var main = ram.GetMain((byte)(machineState.State[SoftSwitch.Page2] ? 0x40 : 0x20), 32);
            var aux = ram.GetAux((byte)(machineState.State[SoftSwitch.Page2] ? 0x40 : 0x20), 32);

            for (int y = 0; y < 192; y++)
            {
                for (int byteCol = 0; byteCol < 40; byteCol++)
                {
                    // AUX = "even" pixel, MAIN = "odd" pixel
                    byte mainByte = main[RowOffsets[y] + byteCol];
                    byte auxByte = aux[RowOffsets[y] + byteCol];

                    for (int bit = 0; bit < 7; bit++)
                    {
                        bool auxBit = ((auxByte >> bit) & 1) != 0;
                        bool mainBit = ((mainByte >> bit) & 1) != 0;

                        int x = byteCol * 14 + bit * 2;
                        bool msb = (auxByte & 0x80) != 0 || (mainByte & 0x80) != 0;

                        buffer.SetPixel(y, x, auxBit, mainBit, msb);     // left
                        buffer.SetPixel(y, x + 1, auxBit, mainBit, msb); // right
                    }
                }
            }
        }
    }
}

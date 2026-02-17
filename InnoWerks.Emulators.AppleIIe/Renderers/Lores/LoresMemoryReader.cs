using System;
using System.Runtime.InteropServices;
using InnoWerks.Computers.Apple;

#pragma warning disable CA1819 // Properties should not return arrays

namespace InnoWerks.Emulators.AppleIIe
{
    public sealed class LoresMemoryReader
    {
        private readonly Memory128k ram;
        private readonly MachineState machineState;

        private readonly bool eightyColumnMode;
        private readonly int page;

        public LoresMemoryReader(Memory128k ram, MachineState machineState, bool eightyColumnMode, int page)
        {
            this.ram = ram;
            this.machineState = machineState;
            this.eightyColumnMode = eightyColumnMode;
            this.page = page;
        }

        public static ushort[] RowOffsets
        {
            get
            {
                if (field == null)
                {
                    // initialize
                    int[] blockRowBase = [0x000, 0x080, 0x100, 0x180, 0x200, 0x280, 0x300, 0x380];

                    field = new ushort[24];
                    for (var y = 0; y < 24; y++)
                    {
                        field[y] = (ushort)(blockRowBase[y & 0x07] + (y >> 3) * 40);
                    }
                }

                return field;
            }
        }

        public void ReadLoresPage(LoresBuffer loresBuffer)
        {
            ArgumentNullException.ThrowIfNull(loresBuffer);

            if (eightyColumnMode == false)
            {
                ReadLores40(loresBuffer, page);
            }
            else
            {
                ReadLores80(loresBuffer, page);
            }
        }

        private void ReadLores40(LoresBuffer loresBuffer, int page)
        {
            var memory = ram.Read((byte)(page == 2 ? 0x08 : 0x04), 4);

            for (var row = 0; row < 24; row++)
            {
                for (var col = 0; col < 40; col++)
                {
                    var addr = RowOffsets[row] + col;

                    loresBuffer.Put(row, col, ConstructLoresCell(memory[addr]));
                }
            }
        }

        private void ReadLores80(LoresBuffer loresBuffer, int page)
        {
            var main = ram.GetMain(0x04, 4);
            var aux = ram.GetAux(0x04, 4);

            for (var row = 0; row < 24; row++)
            {
                for (var col = 0; col < 40; col++)
                {
                    var addr = RowOffsets[row] + col;

                    loresBuffer.Put(row, col * 2, ConstructLoresCell(aux[addr]));
                    loresBuffer.Put(row, (col * 2) + 1, ConstructLoresCell(main[addr]));
                }
            }
        }

        private static LoresCell ConstructLoresCell(byte value)
        {
            return new LoresCell(value);
        }
    }
}

using System;
using System.Runtime.InteropServices;
using InnoWerks.Computers.Apple;

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnoWerks.Emulators.AppleIIe
{
    public sealed class LoresMemoryReader
    {
        private readonly Memory128k ram;
        private readonly MachineState machineState;

        public LoresMemoryReader(Memory128k ram, MachineState machineState)
        {
            this.ram = ram;
            this.machineState = machineState;
        }

        public void ReadLoresPage(LoresBuffer loresBuffer)
        {
            ArgumentNullException.ThrowIfNull(loresBuffer);

            if (machineState.State[SoftSwitch.EightyColumnMode] == false)
            {
                ReadLores40(loresBuffer);
            }
            else
            {
                ReadLores80(loresBuffer);
            }
        }

        private void ReadLores40(LoresBuffer loresBuffer)
        {
            var memory = ram.Read((byte)(machineState.State[SoftSwitch.Page2] ? 0x08 : 0x04), 4);

            for (int row = 0; row < 24; row++)
            {
                for (int col = 0; col < 40; col++)
                {
                    ushort addr = (ushort)(blockRowBase[row & 0x07] + (row >> 3) * 40 + col);
                    byte value = memory[addr];

                    loresBuffer.Put(row, col, ConstructLoresCell(value));
                }
            }
        }

        private void ReadLores80(LoresBuffer loresBuffer)
        {
            var main = ram.GetMain(0x04, 4);
            var aux = ram.GetAux(0x04, 4);

            for (int row = 0; row < 24; row++)
            {
                for (int col = 0; col < 40; col++)
                {
                    ushort addr = (ushort)(blockRowBase[row & 0x07] + (row >> 3) * 40 + col);

                    loresBuffer.Put(row, col * 2, ConstructLoresCell(aux[addr]));
                    loresBuffer.Put(row, (col * 2) + 1, ConstructLoresCell(main[addr]));
                }
            }
        }

        private static readonly int[] blockRowBase =
        [
            0x000, 0x080, 0x100, 0x180,
            0x200, 0x280, 0x300, 0x380
        ];

        private static LoresCell ConstructLoresCell(byte value)
        {
            return new LoresCell(value);
        }
    }
}

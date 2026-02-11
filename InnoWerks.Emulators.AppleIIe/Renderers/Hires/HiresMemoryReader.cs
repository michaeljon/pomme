using System;
using InnoWerks.Computers.Apple;

namespace InnoWerks.Emulators.AppleIIe
{
    public sealed class HiresMemoryReader
    {
        private readonly Memory128k ram;
        private readonly MachineState machineState;

        private ushort[] rowOffsets;

        public HiresMemoryReader(Memory128k ram, MachineState machineState)
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

        public void ReadHiresPage(HiresBuffer buffer)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            var memory = ram.Read((byte)(machineState.State[SoftSwitch.Page2] ? 0x40 : 0x20), 32);

            for (int y = 0; y < 192; y++)
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

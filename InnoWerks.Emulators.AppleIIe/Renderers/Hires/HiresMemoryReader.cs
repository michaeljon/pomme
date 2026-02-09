using System;
using InnoWerks.Computers.Apple;

namespace InnoWerks.Emulators.AppleIIe
{
    public sealed class HiresMemoryReader
    {
        private readonly Memory128k ram;
        private readonly MachineState machineState;

        public HiresMemoryReader(Memory128k ram, MachineState machineState)
        {
            this.ram = ram;
            this.machineState = machineState;
        }

        public void ReadHiresPage(HiresBuffer buffer)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            var memory = ram.Read((byte)(machineState.State[SoftSwitch.Page2] ? 0x40 : 0x20), 32);

            for (int y = 0; y < 192; y++)
            {
                int rowAddr = ((y & 0x07) << 10) +       // (y % 8) * 0x400
                               (((y >> 3) & 0x07) << 7) + // ((y / 8) % 8) * 0x80
                               ((y >> 6) * 40);           // (y / 64) * 40

                for (int byteCol = 0; byteCol < 40; byteCol++)
                {
                    byte b = memory[rowAddr + byteCol];

                    for (int bit = 0; bit < 7; bit++)
                    {
                        int x = byteCol * 7 + bit;
                        bool on = ((b >> bit) & 1) != 0;
                        buffer.SetPixel(y, x, on, b);
                    }
                }
            }
        }
    }
}

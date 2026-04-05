using System;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
    internal static class ProDOSClockPatcher
    {
        private static readonly byte[] driverPattern = [
            0x00, 0x01f, 0x03b, 0x05a, 0x078, 0x097, 0x0b5, 0x0d3, 0x0f2
        ];

        private const int DRIVER_OFFSET = -26;

        private static int patchLoc = -1;

        public const bool PerformProDOSPatch = true;

        public static void PatchClockForProDOS(IBus bus)
        {
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            const byte LDA = 0xA9;
            const byte NOP = 0xEA;

            if (patchLoc > 0)
            {
                // We've already patched, just validate
                if (bus.Peek(patchLoc) == LDA)
                {
                    return;
                }
            }

            int match = 0;
            int matchStart = 0;

            for (int addr = 0x8000; addr < 0x10000; addr++)
            {
                if (bus.Peek(addr) == driverPattern[match])
                {
                    match++;
                    if (match == driverPattern.Length)
                    {
                        break;
                    }
                }
                else
                {
                    match = 0;
                    matchStart = addr;
                }
            }

            if (match != driverPattern.Length)
            {
                return;
            }

            patchLoc = matchStart + DRIVER_OFFSET;
            bus.Poke(patchLoc, LDA);

            int year = DateTime.Now.Year % 100;
            bus.Poke(patchLoc + 1, (byte)year);
            bus.Poke(patchLoc + 2, NOP);
            bus.Poke(patchLoc + 3, NOP);
        }

    }
}

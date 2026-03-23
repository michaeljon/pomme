using System;
using Microsoft.Xna.Framework;

namespace InnoWerks.Emulators.AppleIIe
{
    public sealed class HostLayout
    {
        // the main display
        public Rectangle AppleDisplay { get; set; }

        // the registers
        public Rectangle Registers { get; set; }

        // textmode, mixedmode, etc.
        public Rectangle Block1 { get; set; }

        // aux, main mem, etc.
        public Rectangle Block2 { get; set; }

        // slot selection, language card
        public Rectangle Block3 { get; set; }


        public Rectangle CpuTrace { get; set; }

        public static HostLayout ComputeLayout(int windowWidth, int windowHeight)
        {
            const int CpuTraceWidth = 288;
            const int Padding = 8;

            int availableWidth = windowWidth - CpuTraceWidth - Padding * 3;
            int availableHeight = windowHeight - Padding * 2;

            int scale = Math.Max(
                1,
                Math.Min(availableWidth / DisplayCharacteristics.AppleDisplayWidth, availableHeight / DisplayCharacteristics.AppleDisplayHeight)
            );

            int appleRenderWidth = DisplayCharacteristics.AppleDisplayWidth * scale;
            int appleRenderHeight = DisplayCharacteristics.AppleDisplayHeight * scale;

            int blockWidth = (appleRenderWidth - (3 * Padding)) / 4;
            int blockHeight = windowHeight - appleRenderHeight - (3 * Padding);

            return new HostLayout
            {
                AppleDisplay = new Rectangle(
                    Padding,
                    Padding,
                    appleRenderWidth,
                    appleRenderHeight
                ),

                CpuTrace = new Rectangle(
                    appleRenderWidth + 2 * Padding,
                    Padding,
                    CpuTraceWidth - Padding, // leaves some empty space on the right of the window
                    windowHeight - (2 * Padding)
                ),

                Registers = new Rectangle(
                    Padding * 1 + blockWidth * 0,
                    appleRenderHeight + (2 * Padding),
                    blockWidth,
                    blockHeight
                ),

                Block1 = new Rectangle(
                    Padding * 2 + blockWidth * 1,
                    appleRenderHeight + (2 * Padding),
                    blockWidth,
                    blockHeight
                ),

                Block2 = new Rectangle(
                    Padding * 3 + blockWidth * 2,
                    appleRenderHeight + (2 * Padding),
                    blockWidth,
                    blockHeight
                ),

                Block3 = new Rectangle(
                    Padding * 4 + blockWidth * 3,
                    appleRenderHeight + (2 * Padding),
                    blockWidth,
                    blockHeight
                ),
            };
        }
    }
}

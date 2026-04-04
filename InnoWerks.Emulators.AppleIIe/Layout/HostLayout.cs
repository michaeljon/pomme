using System;
using Microsoft.Xna.Framework;

namespace InnoWerks.Emulators.AppleIIe
{
    public sealed class HostLayout
    {
        public const int ToolbarHeight = 40;
        public const int CpuTraceWidth = 288;
        public const int Padding = 8;

        // toolbar across the top
        public Rectangle Toolbar { get; set; }

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
            int toolbarBottom = Padding + ToolbarHeight + Padding;

            int availableWidth = windowWidth - CpuTraceWidth - Padding * 3;
            int availableHeight = windowHeight - toolbarBottom - Padding;

            int scale = Math.Max(
                1,
                Math.Min(availableWidth / DisplayCharacteristics.AppleDisplayWidth, availableHeight / DisplayCharacteristics.AppleDisplayHeight)
            );

            int appleRenderWidth = DisplayCharacteristics.AppleDisplayWidth * scale;
            int appleRenderHeight = DisplayCharacteristics.AppleDisplayHeight * scale;

            int blockWidth = (appleRenderWidth - (3 * Padding)) / 4;
            int blockHeight = windowHeight - toolbarBottom - appleRenderHeight - (2 * Padding);

            return new HostLayout
            {
                Toolbar = new Rectangle(
                    Padding,
                    Padding,
                    windowWidth - 2 * Padding,
                    ToolbarHeight
                ),

                AppleDisplay = new Rectangle(
                    Padding,
                    toolbarBottom,
                    appleRenderWidth,
                    appleRenderHeight
                ),

                CpuTrace = new Rectangle(
                    appleRenderWidth + 2 * Padding,
                    toolbarBottom,
                    CpuTraceWidth - Padding,
                    windowHeight - toolbarBottom - Padding
                ),

                Registers = new Rectangle(
                    Padding * 1 + blockWidth * 0,
                    toolbarBottom + appleRenderHeight + Padding,
                    blockWidth,
                    blockHeight
                ),

                Block1 = new Rectangle(
                    Padding * 2 + blockWidth * 1,
                    toolbarBottom + appleRenderHeight + Padding,
                    blockWidth,
                    blockHeight
                ),

                Block2 = new Rectangle(
                    Padding * 3 + blockWidth * 2,
                    toolbarBottom + appleRenderHeight + Padding,
                    blockWidth,
                    blockHeight
                ),

                Block3 = new Rectangle(
                    Padding * 4 + blockWidth * 3,
                    toolbarBottom + appleRenderHeight + Padding,
                    blockWidth,
                    blockHeight
                ),
            };
        }
    }
}

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

        public static HostLayout ComputeLayout(bool showInternals, int windowWidth, int windowHeight)
        {
            if (showInternals == true)
            {
                return ComputeLayoutWithDebug(windowWidth, windowHeight);
            }
            else
            {
                return ComputeLayoutWithoutDebug(windowWidth, windowHeight);
            }
        }

        private static HostLayout ComputeLayoutWithDebug(int windowWidth, int windowHeight)
        {
            const int ToolbarBottom = Padding + ToolbarHeight + Padding;

            var availableWidth = windowWidth - CpuTraceWidth - Padding * 3;
            var availableHeight = windowHeight - ToolbarBottom - Padding;

            var scale = Math.Min(
                (float)availableWidth / DisplayCharacteristics.AppleDisplayWidth,
                (float)availableHeight / DisplayCharacteristics.AppleDisplayHeight
            );

            var appleRenderWidth = (int)(DisplayCharacteristics.AppleDisplayWidth * scale);
            var appleRenderHeight = (int)(DisplayCharacteristics.AppleDisplayHeight * scale);

            int blockWidth = (appleRenderWidth - (3 * Padding)) / 4;
            int blockHeight = windowHeight - ToolbarBottom - appleRenderHeight - (2 * Padding);

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
                    ToolbarBottom,
                    appleRenderWidth,
                    appleRenderHeight
                ),

                CpuTrace = new Rectangle(
                    appleRenderWidth + 2 * Padding,
                    ToolbarBottom,
                    CpuTraceWidth - Padding,
                    windowHeight - ToolbarBottom - Padding
                ),

                Registers = new Rectangle(
                    Padding * 1 + blockWidth * 0,
                    ToolbarBottom + appleRenderHeight + Padding,
                    blockWidth,
                    blockHeight
                ),

                Block1 = new Rectangle(
                    Padding * 2 + blockWidth * 1,
                    ToolbarBottom + appleRenderHeight + Padding,
                    blockWidth,
                    blockHeight
                ),

                Block2 = new Rectangle(
                    Padding * 3 + blockWidth * 2,
                    ToolbarBottom + appleRenderHeight + Padding,
                    blockWidth,
                    blockHeight
                ),

                Block3 = new Rectangle(
                    Padding * 4 + blockWidth * 3,
                    ToolbarBottom + appleRenderHeight + Padding,
                    blockWidth,
                    blockHeight
                ),
            };
        }

        private static HostLayout ComputeLayoutWithoutDebug(int windowWidth, int windowHeight)
        {
            const int ToolbarBottom = Padding + ToolbarHeight + Padding;

            var availableWidth = windowWidth - Padding * 2;
            var availableHeight = windowHeight - ToolbarBottom - Padding;

            var scale = Math.Min(
                (float)availableWidth / DisplayCharacteristics.AppleDisplayWidth,
                (float)availableHeight / DisplayCharacteristics.AppleDisplayHeight
            );

            var appleRenderWidth = (int)(DisplayCharacteristics.AppleDisplayWidth * scale);
            var appleRenderHeight = (int)(DisplayCharacteristics.AppleDisplayHeight * scale);

            return new HostLayout
            {
                Toolbar = new Rectangle(
                    Padding,
                    Padding,
                    windowWidth - 2 * Padding,
                    ToolbarHeight
                ),

                AppleDisplay = new Rectangle(
                    (availableWidth - appleRenderWidth) / 2,
                    ToolbarBottom + (availableHeight - appleRenderHeight) / 2,
                    appleRenderWidth,
                    appleRenderHeight
                ),
            };
        }
    }
}

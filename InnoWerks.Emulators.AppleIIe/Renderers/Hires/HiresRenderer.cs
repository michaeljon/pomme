using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using InnoWerks.Computers.Apple;
using InnoWerks.Simulators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

#pragma warning disable CA2213 // Disposable fields should be disposed
#pragma warning disable CA1823 // Avoid unused private fields

namespace InnoWerks.Emulators.AppleIIe
{
    public class HiresRenderer : Renderer
    {
        private readonly HiresMemoryReader hiresMemoryReader;
        private readonly HiresBuffer hiresBuffer;

        // Define this at the class level
        private readonly Texture2D screenTexture;
        private readonly Color[] screenPixels = new Color[560 * 192];


        private readonly int page;
        private readonly bool monochrome;

        public HiresRenderer(
            GraphicsDevice graphicsDevice,
            Cpu6502Core cpu,
            IBus bus,
            Memory128k memoryBlocks,
            MachineState machineState,

            int page,
            bool monochrome)
            : base(graphicsDevice, cpu, bus, memoryBlocks, machineState)
        {
            this.page = page;
            this.monochrome = monochrome;

            hiresBuffer = new HiresBuffer();
            hiresMemoryReader = new(memoryBlocks, machineState, page);

            screenTexture = new Texture2D(graphicsDevice, 560, 192);
        }

        public override ushort GetYOffsetAddress(int y)
        {
            return HiresMemoryReader.RowOffsets[y];
        }

        public override void RenderByte(SpriteBatch spriteBatch, int x, int y) => throw new NotImplementedException();

        public override string WhoAmiI => $"{nameof(HiresRenderer)} page={page}";

        /*
            // --- Step 3: Horizontal Blur (The "CRT" Look) ---
            for (int x = 0; x < 560; x++)
            {
                // Simple 3-tap box filter: 25% Left, 50% Center, 25% Right
                Color leftC = (x > 0) ? rowColors[x - 1] : DisplayCharacteristics.HiresBlack1;
                Color centerC = rowColors[x];
                Color rightC = (x < 559) ? rowColors[x + 1] : DisplayCharacteristics.HiresBlack1;

                float r = (leftC.R * 0.25f) + (centerC.R * 0.5f) + (rightC.R * 0.25f);
                float g = (leftC.G * 0.25f) + (centerC.G * 0.5f) + (rightC.G * 0.25f);
                float b = (leftC.B * 0.25f) + (centerC.B * 0.5f) + (rightC.B * 0.25f);

                screenPixels[y * 560 + x] = new Color((int)r, (int)g, (int)b);
            }
        */

        public override void Draw(SpriteBatch spriteBatch, Rectangle rectangle, int start, int count)
        {
            ArgumentNullException.ThrowIfNull(spriteBatch);

            hiresMemoryReader.ReadHiresPage(hiresBuffer);

            // Temp buffers for the current scanline
            // bitStarts: 1 = A dot starts here. 0 = No dot starts here.
            byte[] bitStarts = new byte[560];
            bool[] msbFlags = new bool[560];

            for (int y = start; y < start + count; y++)
            {
                Array.Clear(bitStarts, 0, 560);
                Array.Clear(msbFlags, 0, 560);

                // --- Step 1: Map the "Dots" (Sparse Population) ---
                for (var x = 0; x < 40; x++)
                {
                    var p = x * 14;
                    var b = hiresBuffer.GetByte(y, x);
                    bool hasShift = (b & 0x80) != 0;
                    int offset = hasShift ? 1 : 0;

                    // Mark the Palette for this byte range
                    int endP = Math.Min(p + 14, 560);
                    for (int k = p; k < endP; k++) msbFlags[k] = hasShift;

                    for (var bit = 0; bit < 7; bit++)
                    {
                        if (((b >> bit) & 0x01) == 0x01)
                        {
                            // Mark the START of the dot only
                            int idx = p + (bit * 2) + offset;
                            if (idx < 559)
                            {
                                bitStarts[idx] = 1;
                            }
                        }
                    }
                }

                // --- Step 2: Render Colors (Per Dot, not Per Pixel) ---
                int rowOffset = y * 560;

                // Initialize row to Black first
                for (int i = 0; i < 560; i++) screenPixels[rowOffset + i] = DisplayCharacteristics.HiresBlack1;

                for (var x = 0; x < 560; x++)
                {
                    bool isDot = bitStarts[x] == 1;
                    bool isGap = false;

                    // --- Gap Detection ---
                    // A gap is a missing dot sandwiched between two existing dots.
                    // Check logical neighbors (Distance 2)
                    if (!isDot)
                    {
                        bool prevDot = (x >= 2) && (bitStarts[x - 2] == 1);
                        bool nextDot = (x < 558) && (bitStarts[x + 2] == 1);

                        // Only fill gap if neighbors are on the SAME palette (avoids clashing colors filling gaps)
                        if (prevDot && nextDot)
                        {
                            if (msbFlags[x] == msbFlags[x - 2]) // Simple check to ensure continuity
                            {
                                isGap = true;
                            }
                        }
                    }

                    if (!isDot && !isGap) continue;

                    // --- Determine Color ---
                    Color drawColor;
                    bool isWhite = false;

                    // Check for White Collision
                    // If this dot touches a neighbor dot (Distance 2), it's white.
                    // Note: Gaps effectively "connect" dots, but pure white usually comes from
                    // raw adjacent bits. Let's strictly check raw bits for White.
                    if (isDot)
                    {
                        bool prevRaw = (x >= 2) && (bitStarts[x - 2] == 1);
                        bool nextRaw = (x < 558) && (bitStarts[x + 2] == 1);
                        if (prevRaw || nextRaw) isWhite = true;
                    }

                    if (isWhite)
                    {
                        drawColor = DisplayCharacteristics.HiresWhite1;

                        // Back-propagate White to the previous dot if it touched us
                        if (x >= 2 && bitStarts[x - 2] == 1)
                        {
                            screenPixels[rowOffset + x - 2] = DisplayCharacteristics.HiresWhite1;
                            screenPixels[rowOffset + x - 1] = DisplayCharacteristics.HiresWhite1;
                        }
                    }
                    else
                    {
                        // [FIX] Determine the "Effective X" for phase calculation
                        // If this is a Gap, we are effectively extending the previous dot (x-2).
                        // If we use 'x' for a gap, we will pick the OPPOSITE color (the stripe color).
                        int effectiveX = isGap ? (x - 2) : x;

                        // Now calculate phase using the corrected position
                        bool isEvenPhase = ((effectiveX / 2) % 2) == 0;
                        bool isHighPalette = msbFlags[x];

                        if (!isHighPalette) // Group A
                        {
                            drawColor = isEvenPhase ?
                                DisplayCharacteristics.HiresViolet : // Violet
                                DisplayCharacteristics.HiresGreen;   // Green
                        }
                        else // Group B
                        {
                            drawColor = isEvenPhase ?
                                DisplayCharacteristics.HiresBlue : // Blue
                                DisplayCharacteristics.HiresOrange;  // Orange
                        }
                    }

                    // --- Paint the Dot (2 Pixels) ---
                    screenPixels[rowOffset + x] = drawColor;
                    if (x + 1 < 560)
                    {
                        screenPixels[rowOffset + x + 1] = drawColor;
                    }
                }
            }

            // --- Step 3: Upload ---
            if (count > 0)
            {
                screenTexture.SetData(screenPixels);
                spriteBatch.Draw(screenTexture, rectangle, DisplayCharacteristics.HiresWhite1);
            }
        }

        protected override void DoDispose(bool disposing)
        {
            if (disposing)
            {
                screenTexture?.Dispose();
            }
        }
    }
}

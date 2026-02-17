using System;
using System.Diagnostics;
using System.IO;
using InnoWerks.Computers.Apple;
using InnoWerks.Simulators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

#pragma warning disable CA2213 // Disposable fields should be disposed

namespace InnoWerks.Emulators.AppleIIe
{
    public class DhiresRenderer : Renderer
    {
        private readonly DhiresMemoryReader dhiresMemoryReader;
        private readonly DhiresBuffer dhiresBuffer;

        private readonly Texture2D screenTexture;
        private readonly Color[] screenPixels = new Color[DisplayCharacteristics.HiresAppleWidth * DisplayCharacteristics.AppleDisplayHeight];

        private readonly int page;

        public DhiresRenderer(
            GraphicsDevice graphicsDevice,
            Cpu6502Core cpu,
            IBus bus,
            Memory128k memoryBlocks,
            MachineState machineState,
            int page)
            : base(graphicsDevice, cpu, bus, memoryBlocks, machineState)
        {
            this.page = page;

            dhiresBuffer = new DhiresBuffer();
            dhiresMemoryReader = new(memoryBlocks, machineState, page);

            screenTexture = new Texture2D(graphicsDevice, DisplayCharacteristics.HiresAppleWidth, DisplayCharacteristics.AppleDisplayHeight);
        }

        public override ushort GetYOffsetAddress(int y)
        {
            return DhiresMemoryReader.RowOffsets[y];
        }

        public override void RenderByte(SpriteBatch spriteBatch, int x, int y) => throw new NotImplementedException();

        public override string WhoAmiI => $"{nameof(DhiresRenderer)} page={page}";

        public override void Draw(SpriteBatch spriteBatch, Rectangle rectangle, int start, int count)
        {
            ArgumentNullException.ThrowIfNull(spriteBatch);

            dhiresMemoryReader.ReadDhiresPage(dhiresBuffer);

            for (var y = start; y < start + count; y++)
            {
                int rowOffset = y * DisplayCharacteristics.HiresAppleWidth;

                for (var x = 0; x < DhiresBuffer.PixelCount / 4; x++)
                {
                    var p = dhiresBuffer.GetPixel(y, x);
                    var drawColor = DisplayCharacteristics.DHiresPalette[p.Color];

                    int baseIndex = rowOffset + (x * 4);

                    screenPixels[baseIndex] = drawColor;
                    screenPixels[baseIndex + 1] = drawColor;
                    screenPixels[baseIndex + 2] = drawColor;
                    screenPixels[baseIndex + 3] = drawColor;
                }
            }

            screenTexture.SetData(screenPixels);
            spriteBatch.Draw(screenTexture, rectangle, Color.White);
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

using System;
using System.Diagnostics;
using InnoWerks.Computers.Apple;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#pragma warning disable CA2213 // Disposable fields should be disposed

namespace InnoWerks.Emulators.AppleIIe
{
    [DebuggerDisplay("{WhoAmI}")]
    public class DhiresRenderer : Renderer
    {
        private readonly DhiresMemoryReader dhiresMemoryReader;
        private readonly DhiresBuffer dhiresBuffer;

        private readonly Texture2D screenTexture;
        private readonly Color[] screenPixels = new Color[DisplayCharacteristics.HiresAppleWidth * DisplayCharacteristics.AppleDisplayHeight];
        private readonly bool[][] monochromeBits;

        private readonly int page;
        private readonly Color? monochromeColor;

        public DhiresRenderer(
            GraphicsDevice graphicsDevice,
            Computer computer,
            int page,
            Color? monochromeColor = null)
            : base(graphicsDevice, computer)
        {
            this.page = page;
            this.monochromeColor = monochromeColor;

            dhiresBuffer = new DhiresBuffer();
            dhiresMemoryReader = new(computer, page);

            monochromeBits = new bool[DisplayCharacteristics.AppleDisplayHeight][];
            for (int i = 0; i < DisplayCharacteristics.AppleDisplayHeight; i++)
                monochromeBits[i] = new bool[DisplayCharacteristics.HiresAppleWidth];

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

            if (monochromeColor.HasValue)
                DrawMonochrome(start, count);
            else
                DrawColor(start, count);

            screenTexture.SetData(screenPixels);
            spriteBatch.Draw(
                screenTexture,
                rectangle,
                new Rectangle(0, 0, DisplayCharacteristics.HiresAppleWidth, count),
                DisplayCharacteristics.HiresWhite1);
        }

        private void DrawMonochrome(int start, int count)
        {
            dhiresMemoryReader.ReadDhiresMonochromePage(monochromeBits, count - start);

            for (var y = start; y < start + count; y++)
            {
                int rowOffset = y * DisplayCharacteristics.HiresAppleWidth;

                for (var x = 0; x < DisplayCharacteristics.HiresAppleWidth; x++)
                    screenPixels[rowOffset + x] = monochromeBits[y][x] ? monochromeColor.Value : Color.Black;
            }
        }

        private void DrawColor(int start, int count)
        {
            dhiresMemoryReader.ReadDhiresPage(dhiresBuffer, count - start);

            for (var y = start; y < start + count; y++)
            {
                int rowOffset = y * DisplayCharacteristics.HiresAppleWidth;

                for (var x = 0; x < DhiresBuffer.PixelCount / 4; x++)
                {
                    var p = dhiresBuffer.GetPixel(y, x);

                    int baseIndex = rowOffset + (x * 4);

                    var drawColor = DisplayCharacteristics.DHiresPalette[p.Color];

                    screenPixels[baseIndex] = drawColor;
                    screenPixels[baseIndex + 1] = drawColor;
                    screenPixels[baseIndex + 2] = drawColor;
                    screenPixels[baseIndex + 3] = drawColor;
                }
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

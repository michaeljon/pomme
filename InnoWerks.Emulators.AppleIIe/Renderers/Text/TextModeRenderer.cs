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
    [DebuggerDisplay("{WhoAmI}")]
    public class TextModeRenderer : Renderer
    {
        private const int GlyphWidth = 8;
        private const int GlyphHeight = 8;
        private const int GlyphsPerRow = 16;
        private const int GlyphCount = 256;

        private const int TexWidth = GlyphsPerRow * GlyphWidth;                  // 128
        private const int TexHeight = (GlyphCount / GlyphsPerRow) * GlyphHeight; // 256

        //
        // MonoGame stuff
        //
        private Texture2D charTexture;

        private Color textColor;

        private readonly TextMemoryReader textMemoryReader;

        private readonly bool eightyColumnMode;
        private readonly int page;

        public TextModeRenderer(
            GraphicsDevice graphicsDevice,
            Cpu6502Core cpu,
            IBus bus,
            Memory128k memoryBlocks,
            MachineState machineState,
            bool eightyColumnMode,
            int page,
            Color textColor)
            : base(graphicsDevice, cpu, bus, memoryBlocks, machineState)
        {
            this.eightyColumnMode = eightyColumnMode;
            this.page = page;
            this.textColor = textColor;

            LoadCharacterRom(graphicsDevice);
            textMemoryReader = new(memoryBlocks, machineState, eightyColumnMode, page);
        }

        public override ushort GetYOffsetAddress(int y)
        {
            return (ushort)(TextMemoryReader.RowOffsets[y & 0x07] + (y >> 3) * 40);
        }

        public override void RenderByte(SpriteBatch spriteBatch, int x, int y) => throw new NotImplementedException();

        public override string WhoAmiI => $"{nameof(TextModeRenderer)} page={page} eightyColumnMode={eightyColumnMode}";

        public override void Draw(SpriteBatch spriteBatch, Rectangle rectangle, int start, int count)
        {
            ArgumentNullException.ThrowIfNull(spriteBatch);

            var cols = eightyColumnMode ? 80 : 40;

            // todo: convert this back to scanlines instead of cells
            start /= DisplayCharacteristics.AppleCellHeight;
            count /= DisplayCharacteristics.AppleCellHeight;

            var textBuffer = new TextBuffer(cols);
            textMemoryReader.ReadTextPage(textBuffer, count - start);

            for (var row = start; row < start + count; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    var cell = textBuffer.Get(row, col);

                    DrawChar(spriteBatch, cell, col, row);
                }
            }
        }

        private void LoadCharacterRom(GraphicsDevice graphicsDevice)
        {
            var charRom = File.ReadAllBytes("roms/342-0265-A.bin");
            Debug.Assert(charRom.Length == 4096);

            charTexture = new Texture2D(graphicsDevice, TexWidth, TexHeight);

            var pixels = new Color[TexWidth * TexHeight];

            for (int ch = 0; ch < GlyphCount; ch++)
            {
                int gx = ch % GlyphsPerRow * GlyphWidth;
                int gy = ch / GlyphsPerRow * GlyphHeight;

                for (int row = 0; row < 8; row++)
                {
                    byte bits = charRom[ch * 8 + row];

                    for (int col = 0; col < 7; col++)
                    {
                        bool on = (bits & (1 << col)) != 0;

                        pixels[(gy + row) * TexWidth + (gx + col)] =
                            on ? Color.White : Color.Transparent;
                    }
                }
            }

            charTexture.SetData(pixels);
        }

        private void DrawChar(SpriteBatch spriteBatch, TextCell cell, int col, int row)
        {
            var glyph = cell.Ascii;

            var fg = textColor;
            var bg = Color.Black;

            if (cell.Attr.HasFlag(TextAttributes.Inverse) || (glyph & 0x80) == 0x80)
            {
                fg = Color.Black;
                bg = textColor;

                if (cell.Attr.HasFlag(TextAttributes.Flash))
                {
                    fg = textColor;
                    bg = Color.Black;
                }
            }
            else
            {
                if (cell.Attr.HasFlag(TextAttributes.Flash))
                {
                    fg = Color.Black;
                    bg = textColor;
                }
            }

            var srcX = glyph % 16 * 8;
            var srcY = glyph / 16 * 8;

            var src = new Rectangle(srcX, srcY, DisplayCharacteristics.AppleCellWidth, DisplayCharacteristics.AppleCellHeight);
            var dst = new Rectangle(
                col * (eightyColumnMode ? DisplayCharacteristics.AppleCellWidth : DisplayCharacteristics.AppleCellWidth * 2),
                row * DisplayCharacteristics.AppleCellHeight,
                eightyColumnMode ? DisplayCharacteristics.AppleCellWidth : DisplayCharacteristics.AppleCellWidth * 2,
                DisplayCharacteristics.AppleCellHeight);

            // Background
            if (fg != textColor)
            {
                spriteBatch.Draw(WhitePixel, dst, bg);
            }

            spriteBatch.Draw(
                charTexture,
                dst,
                src,
                fg);
        }

        protected override void DoDispose(bool disposing)
        {
            if (disposing)
            {
                charTexture?.Dispose();
            }
        }
    }
}

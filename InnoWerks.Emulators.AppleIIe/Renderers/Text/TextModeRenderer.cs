using System;
using System.Diagnostics;
using System.IO;
using InnoWerks.Computers.Apple;
using InnoWerks.Simulators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace InnoWerks.Emulators.AppleIIe
{
    public class TextModeRenderer : IDisposable
    {
        private const int GlyphWidth = 8;
        private const int GlyphHeight = 8;
        private const int GlyphsPerRow = 16;
        private const int GlyphCount = 512;

        private const int TexWidth = GlyphsPerRow * GlyphWidth;   // 128
        private const int TexHeight = (GlyphCount / GlyphsPerRow) * GlyphHeight; // 256

        //
        // MonoGame stuff
        //
        private readonly Texture2D whitePixel;
        private Texture2D charTexture;

        private Color textColor;

        private readonly Cpu6502Core cpu;
        private readonly IBus bus;
        private readonly MachineState machineState;

        private bool disposed;

        private readonly TextMemoryReader textMemoryReader;

        public TextModeRenderer(
            GraphicsDevice graphicsDevice,
            Cpu6502Core cpu,
            IBus bus,
            Memory128k memoryBlocks,
            MachineState machineState,

            ContentManager contentManager,
            Color textColor
            )
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            ArgumentNullException.ThrowIfNull(cpu);
            ArgumentNullException.ThrowIfNull(bus);
            ArgumentNullException.ThrowIfNull(memoryBlocks);
            ArgumentNullException.ThrowIfNull(machineState);

            ArgumentNullException.ThrowIfNull(contentManager);

            this.machineState = machineState;
            this.cpu = cpu;
            this.bus = bus;

            this.textColor = textColor;

            whitePixel = new Texture2D(graphicsDevice, 1, 1);
            whitePixel.SetData([Color.White]);

            LoadCharacterRom(graphicsDevice);

            textMemoryReader = new(memoryBlocks, machineState);
        }

        public void Draw(SpriteBatch spriteBatch, int start, int count, bool flashOn)
        {
            ArgumentNullException.ThrowIfNull(spriteBatch);

            var cols = machineState.State[SoftSwitch.EightyColumnMode] ? 80 : 40;

            var textBuffer = new TextBuffer(cols);
            textMemoryReader.ReadTextPage(textBuffer);

            for (var row = start; row < start + count; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    var cell = textBuffer.Get(row, col);

                    DrawChar(spriteBatch, cell, col, row, flashOn);
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

        private void DrawChar(SpriteBatch spriteBatch, TextCell cell, int col, int row, bool flashOn)
        {
            var glyph = cell.Ascii;

            var fg = textColor;
            var bg = Color.Black;

            if (cell.Attr.HasFlag(TextAttributes.Inverse) || (glyph & 0x80) == 0x80)
            {
                fg = Color.Black;
                bg = textColor;

                if (cell.Attr.HasFlag(TextAttributes.Flash) && flashOn)
                {
                    fg = textColor;
                    bg = Color.Black;
                }
            }
            else
            {
                if (cell.Attr.HasFlag(TextAttributes.Flash) && flashOn)
                {
                    fg = Color.Black;
                    bg = textColor;
                }
            }

            var srcX = glyph % 16 * 8;
            var srcY = glyph / 16 * 8;

            var src = new Rectangle(srcX, srcY, DisplayCharacteristics.AppleCellWidth, DisplayCharacteristics.AppleCellHeight);
            var dst = new Rectangle(col * DisplayCharacteristics.AppleCellWidth, row * DisplayCharacteristics.AppleCellHeight, DisplayCharacteristics.AppleCellWidth, DisplayCharacteristics.AppleCellHeight);

            // Background
            if (fg != textColor)
            {
                spriteBatch.Draw(whitePixel, dst, bg);
            }

            spriteBatch.Draw(
                charTexture,
                dst,
                src,
                fg);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed == true)
            {
                return;
            }

            if (disposing)
            {
                whitePixel?.Dispose();
                charTexture?.Dispose();
            }

            disposed = true;
        }
    }
}

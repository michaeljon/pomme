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
    public class LoresRenderer : Renderer
    {
        private readonly LoresMemoryReader loresMemoryReader;
        private readonly LoresBuffer loresBuffer;

        private readonly bool eightyColumnMode;
        private readonly int page;

        public LoresRenderer(
            GraphicsDevice graphicsDevice,
            Cpu6502Core cpu,
            IBus bus,
            Memory128k memoryBlocks,
            MachineState machineState,

            bool eightyColumnMode,
            int page)
            : base(graphicsDevice, cpu, bus, memoryBlocks, machineState)
        {
            this.eightyColumnMode = eightyColumnMode;
            this.page = page;

            loresBuffer = new LoresBuffer(eightyColumnMode ? 80 : 40);
            loresMemoryReader = new(memoryBlocks, machineState, eightyColumnMode, page);
        }

        public override ushort GetYOffsetAddress(int y)
        {
            return (ushort)(LoresMemoryReader.RowOffsets[y & 0x07] + (y >> 3) * 40);
        }

        public override void RenderByte(SpriteBatch spriteBatch, int x, int y) => throw new NotImplementedException();

        public override string WhoAmiI => $"{nameof(LoresRenderer)} page={page} eightyColumnMode={eightyColumnMode}";

        public override void Draw(SpriteBatch spriteBatch, Rectangle rectangle, int start, int count)
        {
            ArgumentNullException.ThrowIfNull(spriteBatch);

            // todo: convert this back to scanlines instead of cells
            start /= DisplayCharacteristics.AppleCellHeight;
            count /= DisplayCharacteristics.AppleCellHeight;

            var cols = eightyColumnMode ? 80 : 40;
            loresMemoryReader.ReadLoresPage(loresBuffer, count - start);

            for (var row = start; row < start + count; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    var cell = loresBuffer.Get(row, col);

                    DrawBlocks(
                        spriteBatch,
                        cell,
                        col,
                        row
                    );
                }
            }
        }

        private void DrawBlocks(SpriteBatch spriteBatch, LoresCell cell, int col, int row)
        {
            Color top = cell.Top(col, eightyColumnMode);
            Color bottom = cell.Bottom(col, eightyColumnMode);

            var topRect = new Rectangle(
                col * (eightyColumnMode ? DisplayCharacteristics.AppleCellWidth : DisplayCharacteristics.AppleCellWidth * 2),
                row * 2 * DisplayCharacteristics.AppleBlockHeight,
                eightyColumnMode ? DisplayCharacteristics.AppleCellWidth : DisplayCharacteristics.AppleCellWidth * 2,
                DisplayCharacteristics.AppleBlockHeight);
            spriteBatch.Draw(WhitePixel, topRect, top);

            var bottomRect = new Rectangle(
                col * (eightyColumnMode ? DisplayCharacteristics.AppleCellWidth : DisplayCharacteristics.AppleCellWidth * 2),
                ((row * 2) + 1) * DisplayCharacteristics.AppleBlockHeight,
                eightyColumnMode ? DisplayCharacteristics.AppleCellWidth : DisplayCharacteristics.AppleCellWidth * 2,
                DisplayCharacteristics.AppleBlockHeight);
            spriteBatch.Draw(WhitePixel, bottomRect, bottom);
        }

#if false
        private void DrawBlocks(SpriteBatch spriteBatch, LoresCell cell, int col, int row, bool eightyColMode)
        {
            Color top = cell.Top(col, eightyColMode);
            Color bottom = cell.Bottom(col, eightyColMode);

            var basex = col * DisplayCharacteristics.AppleBlockWidth;
            var basey = row * 2 * DisplayCharacteristics.AppleBlockHeight;

            var pos = new Vector2();
            for (var y = 0; y < 4; y++)
            {
                pos.X = basex;
                pos.Y = basey + y;

                spriteBatch.Draw(whitePixel, pos, top); pos.X++;
                spriteBatch.Draw(whitePixel, pos, top); pos.X++;
                spriteBatch.Draw(whitePixel, pos, top); pos.X++;
                spriteBatch.Draw(whitePixel, pos, top); pos.X++;
                spriteBatch.Draw(whitePixel, pos, top); pos.X++;
                spriteBatch.Draw(whitePixel, pos, top); pos.X++;
                spriteBatch.Draw(whitePixel, pos, top); pos.X++;
            }

            for (var y = 4; y < 8; y++)
            {
                pos.X = basex;
                pos.Y = basey + y;

                spriteBatch.Draw(whitePixel, pos, bottom); pos.X++;
                spriteBatch.Draw(whitePixel, pos, bottom); pos.X++;
                spriteBatch.Draw(whitePixel, pos, bottom); pos.X++;
                spriteBatch.Draw(whitePixel, pos, bottom); pos.X++;
                spriteBatch.Draw(whitePixel, pos, bottom); pos.X++;
                spriteBatch.Draw(whitePixel, pos, bottom); pos.X++;
                spriteBatch.Draw(whitePixel, pos, bottom); pos.X++;
            }
        }
#endif

        protected override void DoDispose(bool disposing)
        {
            if (disposing)
            {
            }
        }
    }
}

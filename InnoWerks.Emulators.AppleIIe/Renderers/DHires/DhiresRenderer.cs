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
        }

        public override ushort GetYOffsetAddress(int y)
        {
            return DhiresMemoryReader.RowOffsets[y];
        }

        public override void RenderByte(SpriteBatch spriteBatch, int x, int y) => throw new NotImplementedException();

        public override void Draw(SpriteBatch spriteBatch, int start, int count)
        {
            ArgumentNullException.ThrowIfNull(spriteBatch);

            dhiresMemoryReader.ReadDhiresPage(dhiresBuffer);

            for (var y = start; y < start + count; y++)
            {
                for (var x = 0; x < 140; x++)
                {
                    var p = dhiresBuffer.GetPixel(y, x);

                    spriteBatch.Draw(WhitePixel, new Rectangle((x * 4) + 0, y, 1, 1), DisplayCharacteristics.DHiresPalette[p.Color]);
                    spriteBatch.Draw(WhitePixel, new Rectangle((x * 4) + 1, y, 1, 1), DisplayCharacteristics.DHiresPalette[p.Color]);
                    spriteBatch.Draw(WhitePixel, new Rectangle((x * 4) + 2, y, 1, 1), DisplayCharacteristics.DHiresPalette[p.Color]);
                    spriteBatch.Draw(WhitePixel, new Rectangle((x * 4) + 3, y, 1, 1), DisplayCharacteristics.DHiresPalette[p.Color]);
                }
            }
        }

        protected override void DoDispose(bool disposing)
        {
        }
    }
}

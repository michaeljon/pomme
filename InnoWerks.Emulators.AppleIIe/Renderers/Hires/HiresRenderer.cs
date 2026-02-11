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
    public class HiresRenderer : Renderer
    {
        private static readonly Color HiresBlack = new(0, 0, 0);
        // private static readonly Color HiresPurple = new(128, 0, 255);
        // private static readonly Color HiresGreen = new(0, 192, 0);

        private static readonly Color HiresWhite = new(255, 255, 255);
        // private static readonly Color HiresOrange = new(255, 128, 0);
        // private static readonly Color HiresBlue = new(0, 0, 255);

        //
        // MonoGame stuff
        //
        private readonly Texture2D whitePixel;

        private readonly Cpu6502Core cpu;
        private readonly IBus bus;
        private readonly MachineState machineState;

        private readonly HiresMemoryReader hiresMemoryReader;

        public HiresRenderer(
            GraphicsDevice graphicsDevice,
            Cpu6502Core cpu,
            IBus bus,
            Memory128k memoryBlocks,
            MachineState machineState,

            ContentManager contentManager
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

            whitePixel = new Texture2D(graphicsDevice, 1, 1);
            whitePixel.SetData([Color.White]);

            hiresMemoryReader = new(memoryBlocks, machineState);
        }

        public override ushort GetYOffset(int y) => throw new NotImplementedException();

        public override void RenderByte(SpriteBatch spriteBatch, int x, int y) => throw new NotImplementedException();

        // HGR mono is easy: To generate pixels from left to right, you look at the pixels
        // from the LSB to bit 6, and generate 560 pixels on the screen (so 14 pixels per byte).
        //
        // If the MSB is 0, each set bit is white for two 560-resolution pixels, and each
        // clear bit is black for two pixels.
        //
        // If the MSB is 1, the first pixel is the previous pixel to the left, then you you do the
        // next 12 pixels from LSB to MSB.  Bit 6 of the byte is a single pixel (but if the next
        // byte MSB is set, then it is repeated one more time).
        public void Draw(SpriteBatch spriteBatch, int start, int count)
        {
            ArgumentNullException.ThrowIfNull(spriteBatch);

            var hiresBuffer = new HiresBuffer();
            hiresMemoryReader.ReadHiresPage(hiresBuffer);

            for (int y = start; y < start + count; y++)
            {
                Color[] pixels = new Color[280];

                // build the pixels in the line
                for (int x = 0; x < 40; x++)
                {
                    for (var bit = 0; bit < 7; bit++)
                    {
                        if ((hiresBuffer.GetByte(y, x) & (1 << bit)) != 0)
                        {
                            pixels[(x * 7) + bit] = HiresWhite;
                        }
                        else
                        {
                            pixels[(x * 7) + bit] = HiresBlack;
                        }
                    }
                }

                // render the pixels on the row
                for (int x = 0; x < 280; x += 2)
                {
                    spriteBatch.Draw(whitePixel, new Rectangle(x, y, 1, 1), pixels[x]);
                    spriteBatch.Draw(whitePixel, new Rectangle(x + 1, y, 1, 1), pixels[x + 1]);
                }
            }
        }

        protected override void DoDispose(bool disposing)
        {
            if (disposing)
            {
                whitePixel?.Dispose();
            }
        }
    }
}

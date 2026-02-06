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
    public class HiresRenderer : IDisposable
    {
        // private static readonly Color HiresBlack = new(0, 0, 0);
        private static readonly Color HiresPurple = new(128, 0, 255);
        private static readonly Color HiresGreen = new(0, 192, 0);

        // private static readonly Color HiresWhite = new(255, 255, 255);
        // private static readonly Color HiresOrange = new(255, 128, 0);
        // private static readonly Color HiresBlue = new(0, 0, 255);

        //
        // MonoGame stuff
        //
        private readonly Texture2D whitePixel;

        private readonly Cpu6502Core cpu;
        private readonly IBus bus;
        private readonly MachineState machineState;

        private bool disposed;

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

        public void Draw(SpriteBatch spriteBatch, int start, int count)
        {
            ArgumentNullException.ThrowIfNull(spriteBatch);

            var hiresBuffer = new HiresBuffer();
            hiresMemoryReader.ReadHiresPage(hiresBuffer);

            int pixelWidth = DisplayCharacteristics.AppleBlockWidth / 2;
            int pixelHeight = DisplayCharacteristics.AppleBlockHeight;

            for (int y = start; y < start + count; y++)
            {
                for (int x = 0; x < 280; x++)
                {
                    if (!hiresBuffer.GetPixel(y, x))
                        continue; // Off pixel → nothing to draw

                    byte sourceByte = hiresBuffer.GetSourceByte(y, x);

                    // Phase calculation: bit7 of byte + horizontal position
                    bool phaseBit = (sourceByte & 0x80) != 0;
                    bool phase = ((x & 1) == 1) ^ phaseBit;

                    Color color = phase ? HiresGreen : HiresPurple;

                    var rect = new Rectangle(
                        x * pixelWidth,
                        y * pixelHeight,
                        pixelWidth,
                        pixelHeight);

                    spriteBatch.Draw(whitePixel, rect, color);
                }
            }
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
            }

            disposed = true;
        }
    }
}

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
    public class LoresRenderer : IDisposable
    {
        //
        // MonoGame stuff
        //
        private readonly Texture2D whitePixel;

        private readonly Cpu6502Core cpu;
        private readonly IBus bus;
        private readonly MachineState machineState;

        private bool disposed;

        private readonly LoresMemoryReader loresMemoryReader;

        public LoresRenderer(
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

            loresMemoryReader = new(memoryBlocks, machineState);
        }

        public void Draw(SpriteBatch spriteBatch, int start, int count)
        {
            ArgumentNullException.ThrowIfNull(spriteBatch);

            var cols = machineState.State[SoftSwitch.EightyColumnMode] ? 80 : 40;
            var dhires = machineState.State[SoftSwitch.DoubleHiRes];

            var loresBuffer = new LoresBuffer(cols);
            loresMemoryReader.ReadLoresPage(loresBuffer);

            for (var row = start; row < start + count; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    var cell = loresBuffer.Get(row, col);

                    DrawBlocks(
                        spriteBatch,
                        cell,
                        col,
                        row,
                        dhires
                    );
                }
            }
        }

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

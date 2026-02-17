using System;
using InnoWerks.Computers.Apple;
using InnoWerks.Simulators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InnoWerks.Emulators.AppleIIe
{
    public abstract class Renderer : IDisposable
    {
        private bool disposed;

        //
        // MonoGame stuff
        //
        protected Texture2D WhitePixel { get; init; }

        protected Cpu6502Core Cpu { get; init; }
        protected IBus Bus { get; init; }
        protected MachineState MachineState { get; init; }

        protected abstract void DoDispose(bool disposing);

        public abstract ushort GetYOffsetAddress(int y);

        public abstract void RenderByte(SpriteBatch spriteBatch, int x, int y);

        public abstract void Draw(SpriteBatch spriteBatch, Rectangle rectangle, int start, int count);

        public virtual bool IsMixedMode => false;

        public abstract string WhoAmiI { get; }

        protected Renderer(
            GraphicsDevice graphicsDevice,
            Cpu6502Core cpu,
            IBus bus,
            Memory128k memoryBlocks,
            MachineState machineState
        )
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            ArgumentNullException.ThrowIfNull(cpu);
            ArgumentNullException.ThrowIfNull(bus);
            ArgumentNullException.ThrowIfNull(memoryBlocks);
            ArgumentNullException.ThrowIfNull(machineState);

            MachineState = machineState;
            Cpu = cpu;
            Bus = bus;

            WhitePixel = new Texture2D(graphicsDevice, 1, 1);
            WhitePixel.SetData([Color.White]);
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

            if (disposing == true)
            {
                WhitePixel?.Dispose();

                DoDispose(disposing);
            }

            disposed = true;
        }
    }
}

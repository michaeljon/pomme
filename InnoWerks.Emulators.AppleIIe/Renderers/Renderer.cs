using System;
using InnoWerks.Computers.Apple;
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

        protected Computer Computer { get; init; }

        protected abstract void DoDispose(bool disposing);

        public abstract ushort GetYOffsetAddress(int y);

        public abstract void RenderByte(SpriteBatch spriteBatch, int x, int y);

        public abstract void Draw(SpriteBatch spriteBatch, Rectangle rectangle, int start, int count);

        public virtual bool IsMixedMode => false;

        public abstract string WhoAmiI { get; }

        protected Renderer(
            GraphicsDevice graphicsDevice,
            Computer computer)
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            ArgumentNullException.ThrowIfNull(computer);

            Computer = computer;

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

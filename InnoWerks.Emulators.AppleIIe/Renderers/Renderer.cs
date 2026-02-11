using System;
using Microsoft.Xna.Framework.Graphics;

namespace InnoWerks.Emulators.AppleIIe
{
    public abstract class Renderer : IDisposable
    {
        private bool disposed;

        protected abstract void DoDispose(bool disposing);

        public abstract ushort GetYOffset(int y);

        public abstract void RenderByte(SpriteBatch spriteBatch, int x, int y);

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
                DoDispose(disposing);
            }

            disposed = true;
        }
    }
}

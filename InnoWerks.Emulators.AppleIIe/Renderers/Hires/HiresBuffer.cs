#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

using Microsoft.Xna.Framework;

namespace InnoWerks.Emulators.AppleIIe
{
    public sealed class HiresBuffer
    {
        private readonly byte[,] bytes = new byte[192, 40];

        public byte GetByte(int y, int x) => bytes[y, x];

        public void SetByte(int y, int x, byte sourceByte)
        {
            bytes[y, x] = sourceByte;
        }
    }
}

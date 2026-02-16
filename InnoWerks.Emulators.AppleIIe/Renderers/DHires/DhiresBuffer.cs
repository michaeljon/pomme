#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

using System;

namespace InnoWerks.Emulators.AppleIIe
{
    public readonly struct DhiresPixel : IEquatable<DhiresPixel>
    {
        public readonly byte Color { get; init; }

        public DhiresPixel(byte color)
        {
            Color = color;
        }

        public override readonly bool Equals(object obj)
        {
            return ((DhiresPixel)obj).Color == Color;
        }

        public override readonly int GetHashCode()
        {
            return Color.GetHashCode();
        }

        public readonly bool Equals(DhiresPixel other)
        {
            return other.Color == Color;
        }

        public static bool operator ==(DhiresPixel left, DhiresPixel right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DhiresPixel left, DhiresPixel right)
        {
            return !(left == right);
        }
    }

    public sealed class DhiresBuffer
    {
        private readonly DhiresPixel[,] pixels = new DhiresPixel[192, PixelCount];

        public static int PixelCount => 140;

        public void SetPixel(int y, int x, byte color) => pixels[y, x] = new DhiresPixel(color);

        public DhiresPixel GetPixel(int y, int x) => pixels[y, x];
    }
}

using System;
using System.Linq;
using Microsoft.Xna.Framework;

namespace InnoWerks.Emulators.AppleIIe
{
    public readonly struct LoresCell : IEquatable<LoresCell>
    {

        public readonly byte TopIndex => (byte)(value & 0x0F);
        public readonly byte BottomIndex => (byte)((value & 0xF0) >> 4);

        // REVIEW - determine if the 'hires' parameter is still necessary
        public readonly Color Top(int col, bool hires, Color? monochromeColor = null)
        {
            var color = (col & 1) == 1 && hires
                ? DisplayCharacteristics.LoresPaletteOdd[TopIndex]
                : DisplayCharacteristics.LoresPaletteEven[TopIndex];
            return monochromeColor.HasValue
                ? DisplayCharacteristics.ToMonochrome(color, monochromeColor.Value)
                : color;
        }

        // REVIEW - determine if the 'hires' parameter is still necessary
        public readonly Color Bottom(int col, bool hires, Color? monochromeColor = null)
        {
            var color = (col & 1) == 1 && hires
                ? DisplayCharacteristics.LoresPaletteOdd[BottomIndex]
                : DisplayCharacteristics.LoresPaletteEven[BottomIndex];
            return monochromeColor.HasValue
                ? DisplayCharacteristics.ToMonochrome(color, monochromeColor.Value)
                : color;
        }

        private readonly byte value;

        public LoresCell(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj) => ((LoresCell)obj).value == value;

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public bool Equals(LoresCell other)
        {
            return other.value == value;
        }

        public static bool operator ==(LoresCell left, LoresCell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoresCell left, LoresCell right)
        {
            return !(left == right);
        }
    }
}

using System;

namespace InnoWerks.Emulators.AppleIIe
{
    [Flags]
#pragma warning disable CA1028, CS0019
    public enum TextAttributes : byte
    {
        None = 0,

        Inverse = 1, // bit 0 of attribute, separate from ASCII high bit

        Flash = 2, // bit 1 of attribute
    }

    public readonly struct TextCell : IEquatable<TextCell>
    {
        public readonly byte Ascii { get; init; }
        public readonly TextAttributes Attr { get; init; }  // inverse / flash

        public TextCell(byte ascii, TextAttributes attr = TextAttributes.None)
        {
            Ascii = ascii;
            Attr = attr;
        }

        public override bool Equals(object obj) => ((TextCell)obj).Ascii == Ascii && ((TextCell)obj).Attr == Attr;

        public override int GetHashCode()
        {
            return Ascii.GetHashCode() ^ 31 + Attr.GetHashCode();
        }

        public bool Equals(TextCell other)
        {
            return other.Ascii == Ascii && other.Attr == Attr;
        }

        public static bool operator ==(TextCell left, TextCell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextCell left, TextCell right)
        {
            return !(left == right);
        }
    }
}

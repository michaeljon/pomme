using System.Linq;
using Microsoft.Xna.Framework;

namespace InnoWerks.Emulators.AppleIIe
{
    public static class DisplayCharacteristics
    {
        public const int AppleCellWidth = 7;

        public const int AppleCellHeight = 8;

        public const int AppleBlockWidth = 7;

        public const int AppleBlockHeight = 4;

        public const int AppleDisplayWidth = 280;

        public const int AppleDisplayHeight = 192;

        public const int HiresAppleWidth = 560;

        public static readonly Color[] TextPalette =
        [
            new Color(0x39, 0xFF, 0x14),    // "#39FF14" text green
            new Color(0xFF, 0xA5, 0x00),    // "#FFA500" text amber
            new Color(0xF0, 0xF0, 0xF0),    // "#F0F0F0" text white-ish
        ];

        public static readonly Color GreenText = TextPalette[0];
        public static readonly Color AmberText = TextPalette[1];
        public static readonly Color WhiteText = TextPalette[2];

        public static readonly Color[] LoresPaletteEven =
        [
           new Color(0x00, 0x00, 0x00),    //  "#000000", black
           new Color(0xDD, 0x00, 0x33),    //  "#DD0033", red
           new Color(0x00, 0x00, 0x99),    //  "#000099", dk blue
           new Color(0xDD, 0x22, 0xDD),    //  "#DD22DD", purple
           new Color(0x00, 0x77, 0x22),    //  "#007722", dk green
           new Color(0xAA, 0xAA, 0xAA),    //  "#AAAAAA", gray
           new Color(0x22, 0x22, 0xFF),    //  "#2222FF", med blue
           new Color(0x66, 0xAA, 0xFF),    //  "#66AAFF", lt blue
           new Color(0x88, 0x55, 0x00),    //  "#885500", brown
           new Color(0xFF, 0x66, 0x00),    //  "#FF6600", orange
           new Color(0xAA, 0xAA, 0xAA),    //  "#AAAAAA", grey
           new Color(0xFF, 0x99, 0x88),    //  "#FF9988", pink
           new Color(0x11, 0xDD, 0x00),    //  "#11DD00", lt green
           new Color(0xFF, 0xFF, 0x00),    //  "#FFFF00", yellow
           new Color(0x4A, 0xFD, 0xC5),    //  "#4AFDC5", aqua
           new Color(0xFF, 0xFF, 0xFF),    //  "#FFFFFF"  white
        ];

        public static readonly Color[] LoresPaletteOdd =
        [
           new Color(0x00, 0x00, 0x00),    //  "#000000", black
           new Color(0xDD, 0x00, 0x33),    //  "#DD0033", red
           new Color(0x88, 0x55, 0x00),    //  "#885500", brown
           new Color(0xFF, 0x66, 0x00),    //  "#FF6600", orange
           new Color(0x00, 0x77, 0x22),    //  "#007722", dk green
           new Color(0xAA, 0xAA, 0xAA),    //  "#AAAAAA", gray
           new Color(0x11, 0xDD, 0x00),    //  "#11DD00", lt green
           new Color(0xFF, 0xFF, 0x00),    //  "#FFFF00", yellow
           new Color(0x00, 0x00, 0x99),    //  "#000099", dk blue
           new Color(0xDD, 0x22, 0xDD),    //  "#DD22DD", purple
           new Color(0xAA, 0xAA, 0xAA),    //  "#AAAAAA", grey
           new Color(0xFF, 0x99, 0x88),    //  "#FF9988", pink
           new Color(0x22, 0x22, 0xFF),    //  "#2222FF", med blue
           new Color(0x66, 0xAA, 0xFF),    //  "#66AAFF", lt blue
           new Color(0x4A, 0xFD, 0xC5),    //  "#4AFDC5", aqua
           new Color(0xFF, 0xFF, 0xFF),    //  "#FFFFFF"  white
        ];

        public static readonly Color HiresBlack1 = new(0x00, 0x00, 0x00);
        public static readonly Color HiresViolet = new(0xDD, 0x22, 0xDD);
        public static readonly Color HiresGreen = new(0x11, 0xDD, 0x00);
        public static readonly Color HiresWhite1 = new(0xFF, 0xFF, 0xFF);
        public static readonly Color HiresBlack2 = new(0x00, 0x00, 0x00);
        public static readonly Color HiresBlue = new(0x22, 0x22, 0xFF);
        public static readonly Color HiresOrange = new(0xFF, 0x66, 0x00);
        public static readonly Color HiresWhite2 = new(0xFF, 0xFF, 0xFF);

        public static readonly Color[] DHiresPalette =
        [
           new Color(0x00, 0x00, 0x00),    //  "#000000", black
           new Color(0xDD, 0x00, 0x33),    //  "#DD0033", red
           new Color(0x88, 0x55, 0x00),    //  "#885500", brown
           new Color(0xFF, 0x66, 0x00),    //  "#FF6600", orange
           new Color(0x00, 0x77, 0x22),    //  "#007722", dk green
           new Color(0xAA, 0xAA, 0xAA),    //  "#AAAAAA", gray
           new Color(0x11, 0xDD, 0x00),    //  "#11DD00", lt green
           new Color(0xFF, 0xFF, 0x00),    //  "#FFFF00", yellow
           new Color(0x00, 0x00, 0x99),    //  "#000099", dk blue
           new Color(0xDD, 0x22, 0xDD),    //  "#DD22DD", purple
           new Color(0xAA, 0xAA, 0xAA),    //  "#AAAAAA", grey
           new Color(0xFF, 0x99, 0x88),    //  "#FF9988", pink
           new Color(0x22, 0x22, 0xFF),    //  "#2222FF", med blue
           new Color(0x66, 0xAA, 0xFF),    //  "#66AAFF", lt blue
           new Color(0x4A, 0xFD, 0xC5),    //  "#4AFDC5", aqua
           new Color(0xFF, 0xFF, 0xFF),    //  "#FFFFFF"  white
        ];
    }
}

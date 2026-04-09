#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnoWerks.Emulators.AppleIIe
{
    public class TextBuffer
    {
        private readonly TextCell[,] textBuffer;

        public TextBuffer()
        {
            textBuffer = new TextCell[24, 80];
        }

        public TextCell Get(int row, int col) => textBuffer[row, col];

        public void Put(int row, int col, TextCell cell) => textBuffer[row, col] = cell;
    }
}

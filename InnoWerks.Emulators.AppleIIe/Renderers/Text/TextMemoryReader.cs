using System;
using InnoWerks.Computers.Apple;

#pragma warning disable CA1819 // Properties should not return arrays

namespace InnoWerks.Emulators.AppleIIe
{
    public sealed class TextMemoryReader
    {
        private readonly Computer computer;

        private readonly bool eightyColumnMode;
        private readonly int page;

        public TextMemoryReader(Computer computer, bool eightyColumnMode, int page)
        {
            this.computer = computer;
            this.eightyColumnMode = eightyColumnMode;
            this.page = page;
        }

        public static ushort[] RowOffsets
        {
            get
            {
                if (field == null)
                {
                    // initialize
                    int[] textRowBase = [0x000, 0x080, 0x100, 0x180, 0x200, 0x280, 0x300, 0x380];

                    field = new ushort[24];
                    for (var y = 0; y < 24; y++)
                    {
                        field[y] = (ushort)(textRowBase[y & 0x07] + (y >> 3) * 40);
                    }
                }

                return field;
            }
        }

        public void ReadTextPage(TextBuffer textBuffer, int start = 0, int count = 24)
        {
            ArgumentNullException.ThrowIfNull(textBuffer);

            if (eightyColumnMode == false)
            {
                Read40Column(textBuffer, start, count);
            }
            else
            {
                Read80Column(textBuffer, start, count);
            }
        }

        private void Read40Column(TextBuffer textBuffer, int start = 0, int count = 24)
        {
            var memory = computer.Memory.Read((byte)(page == 2 ? 0x08 : 0x04), 4);

            for (var row = start; row < start + count; row++)
            {
                for (var col = 0; col < 40; col++)
                {
                    var addr = RowOffsets[row] + col;

                    textBuffer.Put(row, col, ConstructTextCell(memory[addr]));
                }
            }
        }

        private void Read80Column(TextBuffer textBuffer, int start = 0, int count = 24)
        {
            var main = computer.Memory.GetMain(0x04, 4);
            var aux = computer.Memory.GetAux(0x04, 4);

            for (var row = start; row < start + count; row++)
            {
                for (var col = 0; col < 40; col++)
                {
                    var addr = RowOffsets[row] + col;

                    textBuffer.Put(row, col * 2, ConstructTextCell(aux[addr]));
                    textBuffer.Put(row, (col * 2) + 1, ConstructTextCell(main[addr]));
                }
            }
        }

        // normal character set
        // | screen code | mode     | characters        | Pos in Char Rom |
        // | ----------- | -------- | ----------------- | --------------- |
        // | $00 - $1F   | Inverse  | Uppercase Letters | 00 - 1F         |
        // | $20 - $3F   | Inverse  | Symbols/Numbers   | 20 - 3F         |
        // | $40 - $5F   | Flashing | Uppercase Letters | 00 - 1F         |
        // | $60 - $7F   | Flashing | Symbols/Numbers   | 20 - 3F         |
        // | $80 - $9F   | Normal   | Uppercase Letters | 80 - 9F         |
        // | $A0 - $BF   | Normal   | Symbols/Numbers   | A0 - BF         |
        // | $C0 - $DF   | Normal   | Uppercase Letters | C0 - DF         |
        // | $E0 - $FF   | Normal   | Symbols/Numbers   | E0 - FF         |

        // alternate character set
        // | screen code | mode    | characters                       | Pos in Char Rom |
        // | ----------- | ------- | -------------------------------- | --------------- |
        // | $00 - $1F   | Inverse | Uppercase Letters                | 00 - 1F         |
        // | $20 - $3F   | Inverse | Symbols/Numbers                  | 20 - 3F         |
        // | $40 - $5F   | Inverse | Uppercase Letters (tb mousetext) | 40 - 5F         |
        // | $60 - $7F   | Inverse | Lowercase letters                | 60 - 7F         |
        // | $80 - $9F   | Normal  | Uppercase Letters                | 80 - 9F         |
        // | $A0 - $BF   | Normal  | Symbols/Numbers                  | A0 - BF         |
        // | $C0 - $DF   | Normal  | Uppercase Letters                | C0 - DF         |
        // | $E0 - $FF   | Normal  | Symbols/Numbers                  | E0 - FF         |

        private TextCell ConstructTextCell(byte value)
        {
            var attr = TextAttributes.None;

            if (value <= 0x3F)
            {
                // $00-$3F: Inverse
                attr |= TextAttributes.Inverse;
            }
            else if (value <= 0x7F)
            {
                if (computer.MachineState.State[SoftSwitch.AltCharSet] == false)
                {
                    // $40-$7F normal charset: Flashing, same glyphs as $00-$3F
                    value &= 0x3F;
                    attr |= TextAttributes.Flash;
                }
                else
                {
                    // $40-$7F alternate charset: Inverse (mousetext / lowercase)
                    attr |= TextAttributes.Inverse;
                }
            }

            return new TextCell(value, attr);
        }
    }
}

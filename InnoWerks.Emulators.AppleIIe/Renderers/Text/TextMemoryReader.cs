using System;
using System.Runtime.InteropServices;
using System.Text;
using InnoWerks.Computers.Apple;

#pragma warning disable CA1819 // Properties should not return arrays

namespace InnoWerks.Emulators.AppleIIe
{
    public sealed class TextMemoryReader
    {
        private readonly Memory128k ram;
        private readonly MachineState machineState;

        private readonly bool eightyColumnMode;
        private readonly int page;

        public TextMemoryReader(Memory128k ram, MachineState machineState, bool eightyColumnMode, int page)
        {
            this.ram = ram;
            this.machineState = machineState;
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

        public void ReadTextPage(TextBuffer textBuffer, int rows = 24)
        {
            ArgumentNullException.ThrowIfNull(textBuffer);

            if (eightyColumnMode == false)
            {
                Read40Column(textBuffer, rows);
            }
            else
            {
                Read80Column(textBuffer, rows);
            }
        }

        private void Read40Column(TextBuffer textBuffer, int rows = 24)
        {
            var memory = ram.Read((byte)(page == 2 ? 0x08 : 0x04), 4);

            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < 40; col++)
                {
                    var addr = RowOffsets[row] + col;

                    textBuffer.Put(row, col, ConstructTextCell(memory[addr]));
                }
            }
        }

        private void Read80Column(TextBuffer textBuffer, int rows = 24)
        {
            var main = ram.GetMain(0x04, 4);
            var aux = ram.GetAux(0x04, 4);

            for (var row = 0; row < rows; row++)
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

            // if ((value & 0xC0) == 0x00)
            // {
            //     attr |= TextAttributes.Inverse;
            // }

            // if ((value & 0x40) == 0x00)
            // {
            //     attr |= TextAttributes.Flash;
            // }

            if (value <= 0x3F)
            {
                // inverse
                attr |= TextAttributes.Inverse;
            }
            else if (value >= 0x40 && value <= 0x5F)
            {
                // mouse text, remap to upper case
                if (machineState.State[SoftSwitch.AltCharSet] == false)
                {
                    value &= 0xBF;
                    attr |= TextAttributes.Flash;
                }
            }
            else if (value <= 0x7F)
            {
                // inverse
                attr |= TextAttributes.Inverse;
            }

            return new TextCell(value, attr);
        }
    }
}

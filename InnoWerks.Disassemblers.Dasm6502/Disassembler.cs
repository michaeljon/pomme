using System.Collections.Generic;
using System.IO;

#pragma warning disable CA1002

namespace InnoWerks.Disassemblers
{
    public class Disassembler
    {
        private readonly string filename;

        private readonly ushort origin;

        private static readonly Instructions instructionDecoding = new();

        public Disassembler(string filename, ushort origin = 0x0000)
        {
            this.filename = filename;
            this.origin = origin;
        }

        public void Disassemble()
        {
            var bytes = File.ReadAllBytes(filename);

            Disassembly.Add($"    \tORG {origin:X4}\n");

            ushort pc = 0;

            while (pc < bytes.Length)
            {
                var op = bytes[pc];
                var decoding = instructionDecoding[op];
                var raw = GetRawBytes(bytes, pc, op, decoding);

                switch (decoding.AddressingMode)
                {
                    case Processors.AddressingMode.Unknown:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t???");
                        pc++;
                        break;

                    case Processors.AddressingMode.Implicit:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction}");
                        pc++;
                        break;

                    case Processors.AddressingMode.Accumulator:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} A");
                        pc++;
                        break;

                    case Processors.AddressingMode.Stack:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction}");
                        pc++;
                        break;

                    case Processors.AddressingMode.Immediate:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} #${bytes[pc + 1]:X2}");
                        pc += 2;
                        break;

                    case Processors.AddressingMode.ZeroPage:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} ${bytes[pc + 1]:X2}");
                        pc += 2;
                        break;

                    case Processors.AddressingMode.ZeroPageXIndexed:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} (${bytes[pc + 1]:X2}),X");
                        pc += 2;
                        break;

                    case Processors.AddressingMode.ZeroPageYIndexed:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} (${bytes[pc + 1]:X2}),Y");
                        pc += 2;
                        break;

                    case Processors.AddressingMode.Relative:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} ${bytes[pc + 1]:X2}");
                        pc += 2;
                        break;

                    case Processors.AddressingMode.ZeroPageIndirect:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} (${bytes[pc + 1]:X2})");
                        pc += 2;
                        break;

                    case Processors.AddressingMode.XIndexedIndirect:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} ({bytes[pc + 1]:X2},X)");
                        pc += 2;
                        break;

                    case Processors.AddressingMode.IndirectYIndexed:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} ({bytes[pc + 1]:X2}),Y");
                        pc += 2;
                        break;

                    case Processors.AddressingMode.Absolute:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} ${bytes[pc + 2]:X2}{bytes[pc + 1]:X2}");
                        pc += 3;
                        break;

                    case Processors.AddressingMode.AbsoluteXIndexed:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} ${bytes[pc + 2]:X2}{bytes[pc + 1]:X2},X");
                        pc += 3;
                        break;

                    case Processors.AddressingMode.AbsoluteYIndexed:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} ${bytes[pc + 2]:X2}{bytes[pc + 1]:X2},Y");
                        pc += 3;
                        break;

                    case Processors.AddressingMode.AbsoluteIndexedIndirect:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} (${bytes[pc + 2]:X2}{bytes[pc + 1]:X2}),X");
                        pc += 3;
                        break;

                    case Processors.AddressingMode.AbsoluteIndirect:
                        Disassembly.Add($"{pc + origin:X4}    {raw,-12}\t{decoding.Instruction} (${bytes[pc + 2]:X2}{bytes[pc + 1]:X2})");
                        pc += 3;
                        break;
                }
            }
        }

        private static string GetRawBytes(byte[] bytes, ushort pc, byte op, InstructionDecoding decoding)
        {
            switch (decoding.AddressingMode)
            {
                case Processors.AddressingMode.Unknown:
                case Processors.AddressingMode.Implicit:
                case Processors.AddressingMode.Accumulator:
                case Processors.AddressingMode.Stack:
                    return $"{op:X2}";

                case Processors.AddressingMode.Immediate:
                case Processors.AddressingMode.ZeroPage:
                case Processors.AddressingMode.ZeroPageXIndexed:
                case Processors.AddressingMode.ZeroPageYIndexed:
                case Processors.AddressingMode.Relative:
                case Processors.AddressingMode.ZeroPageIndirect:
                case Processors.AddressingMode.XIndexedIndirect:
                case Processors.AddressingMode.IndirectYIndexed:
                    if (pc + 1 >= bytes.Length)
                    {
                        return "out of data";
                    }

                    return $"{op:X2}{bytes[pc + 1]:X2}";

                case Processors.AddressingMode.Absolute:
                case Processors.AddressingMode.AbsoluteXIndexed:
                case Processors.AddressingMode.AbsoluteYIndexed:
                case Processors.AddressingMode.AbsoluteIndexedIndirect:
                case Processors.AddressingMode.AbsoluteIndirect:
                    if (pc + 1 >= bytes.Length || pc + 2 >= bytes.Length)
                    {
                        return "out of data";
                    }

                    return $"{op:X2}{bytes[pc + 1]:X2}{bytes[pc + 2]:X2}";
            }

            return "***";
        }

        public List<string> Disassembly { get; } = [];
    }
}

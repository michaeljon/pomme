using InnoWerks.Processors;

namespace InnoWerks.Simulators
{
    public static class OpCode65C02
    {
        //
        // From: http://6502.org/tutorials/65c02opcodes.html#9
        //
        // The following table lists the undocumented NOPs of the 65C02.
        // You'll see they don't necessarily match the two-byte, two-cycle
        // characteristics of the standard NOP (opcode $EA). For each entry
        // in the table, the first number is the size in bytes and the second
        // number is the number of cycles taken. After the second number, a
        // lower-case letter may be present, indicating a footnote.
        //
        //       x2:     x3:     x4:     x7:     xB:     xC:     xF:
        //      -----   -----   -----   -----   -----   -----   -----
        // 0x:  2 2 .   1 1 .   . . .   1 1 a   1 1 .   . . .   1 1 c
        // 1x:  . . .   1 1 .   . . .   1 1 a   1 1 .   . . .   1 1 c
        // 2x:  2 2 .   1 1 .   . . .   1 1 a   1 1 .   . . .   1 1 c
        // 3x:  . . .   1 1 .   . . .   1 1 a   1 1 .   . . .   1 1 c
        // 4x:  2 2 .   1 1 .   2 3 g   1 1 a   1 1 .   . . .   1 1 c
        // 5x:  . . .   1 1 .   2 4 h   1 1 a   1 1 .   3 8 j   1 1 c
        // 6x:  2 2 .   1 1 .   . . .   1 1 a   1 1 .   . . .   1 1 c
        // 7x:  . . .   1 1 .   . . .   1 1 a   1 1 .   . . .   1 1 c
        // 8x:  2 2 .   1 1 .   . . .   1 1 b   1 1 .   . . .   1 1 d
        // 9x:  . . .   1 1 .   . . .   1 1 b   1 1 .   . . .   1 1 d
        // Ax:  . . .   1 1 .   . . .   1 1 b   1 1 .   . . .   1 1 d
        // Bx:  . . .   1 1 .   . . .   1 1 b   1 1 .   . . .   1 1 d
        // Cx:  2 2 .   1 1 .   . . .   1 1 b   1 1 e   . . .   1 1 d
        // Dx:  . . .   1 1 .   2 4 h   1 1 b   1 1 f   3 4 i   1 1 d
        // Ex:  2 2 .   1 1 .   . . .   1 1 b   1 1 .   . . .   1 1 d
        // Fx:  . . .   1 1 .   2 4 h   1 1 b   1 1 .   3 4 i   1 1 d

        // a) 1-cycle NOP on some older 65C02s; RMB instruction on Rockwell and on modern WDC 65C02s
        // b) 1-cycle NOP on some older 65C02s; SMB instruction on Rockwell and on modern WDC 65C02s
        // c) 1-cycle NOP on some older 65C02s; BBR instruction on Rockwell and on modern WDC 65C02s
        // d) 1-cycle NOP on some older 65C02s; BBS instruction on Rockwell and on modern WDC 65C02s
        // e) $CB is the WAI instruction on WDC 65C02
        // f) $DB is the STP instruction on WDC 65C02
        // g) $44 uses zp address mode to read memory
        // h) $54, $D4, and $F4 use zp,X address mode to read memory
        // i) $DC and $FC use absolute address mode to read memory
        // j) $5C reads from somewhere in the 64K range, using no known address mode

        public static readonly OpCodeDefinition[] Instructions =
        {
            new(0x00, OpCode.BRK, (cpu, addr, value) => cpu.BRK(addr, value), InstructionDecoders.DecodeStack, AddressingMode.Stack),
            new(0x01, OpCode.ORA, (cpu, addr, value) => cpu.ORA(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect),
            new(0x02, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 2, Cycles: 2),
            new(0x03, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect, Bytes: 1, Cycles: 1),
            new(0x04, OpCode.TSB, (cpu, addr, value) => cpu.TSB(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x05, OpCode.ORA, (cpu, addr, value) => cpu.ORA(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x06, OpCode.ASL, (cpu, addr, value) => cpu.ASL(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x07, OpCode.RMB0, (cpu, addr, value) => ((Cpu65C02)cpu).RMB(addr, value, bit: 0), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x08, OpCode.PHP, (cpu, addr, value) => cpu.PHP(addr, value), InstructionDecoders.DecodeStack, AddressingMode.Stack),
            new(0x09, OpCode.ORA, (cpu, addr, value) => cpu.ORA(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate),
            new(0x0a, OpCode.ASL_A, (cpu, addr, value) => cpu.ASL_A(addr, value), InstructionDecoders.DecodeAccumulator, AddressingMode.Accumulator),
            new(0x0b, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 1, Cycles: 1),
            new(0x0c, OpCode.TSB, (cpu, addr, value) => cpu.TSB(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x0d, OpCode.ORA, (cpu, addr, value) => cpu.ORA(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x0e, OpCode.ASL, (cpu, addr, value) => cpu.ASL(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x0f, OpCode.BBR0, (cpu, addr, value) => ((Cpu65C02)cpu).BBR(addr, value, bit: 0), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0x10, OpCode.BPL, (cpu, addr, value) => cpu.BPL(addr, value), InstructionDecoders.DecodeRelative, AddressingMode.Relative),
            new(0x11, OpCode.ORA, (cpu, addr, value) => cpu.ORA(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed),
            new(0x12, OpCode.ORA, (cpu, addr, value) => cpu.ORA(addr, value), InstructionDecoders.DecodeZeroPageIndirect, AddressingMode.ZeroPageIndirect),
            new(0x13, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed, Bytes: 1, Cycles: 1),
            new(0x14, OpCode.TRB, (cpu, addr, value) => cpu.TRB(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x15, OpCode.ORA, (cpu, addr, value) => cpu.ORA(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0x16, OpCode.ASL, (cpu, addr, value) => cpu.ASL(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0x17, OpCode.RMB1, (cpu, addr, value) => ((Cpu65C02)cpu).RMB(addr, value, bit: 1), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x18, OpCode.CLC, (cpu, addr, value) => cpu.CLC(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0x19, OpCode.ORA, (cpu, addr, value) => cpu.ORA(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed),
            new(0x1a, OpCode.INA, (cpu, addr, value) => cpu.INA(addr, value), InstructionDecoders.DecodeAccumulator, AddressingMode.Accumulator),
            new(0x1b, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed, Bytes: 1, Cycles: 1),
            new(0x1c, OpCode.TRB, (cpu, addr, value) => cpu.TRB(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x1d, OpCode.ORA, (cpu, addr, value) => cpu.ORA(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0x1e, OpCode.ASL, (cpu, addr, value) => cpu.ASL(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0x1f, OpCode.BBR1, (cpu, addr, value) => ((Cpu65C02)cpu).BBR(addr, value, bit: 1), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0x20, OpCode.JSR, (cpu, addr, value) => cpu.JSR(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x21, OpCode.AND, (cpu, addr, value) => cpu.AND(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect),
            new(0x22, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 2, Cycles: 2),
            new(0x23, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect, Bytes: 1, Cycles: 1),
            new(0x24, OpCode.BIT, (cpu, addr, value) => cpu.BIT(addr, value, immediateMode: false), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x25, OpCode.AND, (cpu, addr, value) => cpu.AND(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x26, OpCode.ROL, (cpu, addr, value) => cpu.ROL(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x27, OpCode.RMB2, (cpu, addr, value) => ((Cpu65C02)cpu).RMB(addr, value, bit: 2), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x28, OpCode.PLP, (cpu, addr, value) => cpu.PLP(addr, value), InstructionDecoders.DecodeStack, AddressingMode.Stack),
            new(0x29, OpCode.AND, (cpu, addr, value) => cpu.AND(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate),
            new(0x2a, OpCode.ROL_A, (cpu, addr, value) => cpu.ROL_A(addr, value), InstructionDecoders.DecodeAccumulator, AddressingMode.Accumulator),
            new(0x2b, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 1, Cycles: 1),
            new(0x2c, OpCode.BIT, (cpu, addr, value) => cpu.BIT(addr, value, immediateMode: false), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x2d, OpCode.AND, (cpu, addr, value) => cpu.AND(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x2e, OpCode.ROL, (cpu, addr, value) => cpu.ROL(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x2f, OpCode.BBR2, (cpu, addr, value) => ((Cpu65C02)cpu).BBR(addr, value, bit: 2), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0x30, OpCode.BMI, (cpu, addr, value) => cpu.BMI(addr, value), InstructionDecoders.DecodeRelative, AddressingMode.Relative),
            new(0x31, OpCode.AND, (cpu, addr, value) => cpu.AND(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed),
            new(0x32, OpCode.AND, (cpu, addr, value) => cpu.AND(addr, value), InstructionDecoders.DecodeZeroPageIndirect, AddressingMode.ZeroPageIndirect),
            new(0x33, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed, Bytes: 1, Cycles: 1),
            new(0x34, OpCode.BIT, (cpu, addr, value) => cpu.BIT(addr, value, immediateMode: false), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0x35, OpCode.AND, (cpu, addr, value) => cpu.AND(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0x36, OpCode.ROL, (cpu, addr, value) => cpu.ROL(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0x37, OpCode.RMB3, (cpu, addr, value) => ((Cpu65C02)cpu).RMB(addr, value, bit: 3), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x38, OpCode.SEC, (cpu, addr, value) => cpu.SEC(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0x39, OpCode.AND, (cpu, addr, value) => cpu.AND(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed),
            new(0x3a, OpCode.DEA, (cpu, addr, value) => cpu.DEA(addr, value), InstructionDecoders.DecodeAccumulator, AddressingMode.Accumulator),
            new(0x3b, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed, Bytes: 1, Cycles: 1),
            new(0x3c, OpCode.BIT, (cpu, addr, value) => cpu.BIT(addr, value, immediateMode: false), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0x3d, OpCode.AND, (cpu, addr, value) => cpu.AND(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0x3e, OpCode.ROL, (cpu, addr, value) => cpu.ROL(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0x3f, OpCode.BBR3, (cpu, addr, value) => ((Cpu65C02)cpu).BBR(addr, value, bit: 3), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0x40, OpCode.RTI, (cpu, addr, value) => cpu.RTI(addr, value), InstructionDecoders.DecodeStack, AddressingMode.Stack),
            new(0x41, OpCode.EOR, (cpu, addr, value) => cpu.EOR(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect),
            new(0x42, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 2, Cycles: 2),
            new(0x43, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect, Bytes: 1, Cycles: 1),
            new(0x44, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage, Bytes: 2, Cycles: 3),
            new(0x45, OpCode.EOR, (cpu, addr, value) => cpu.EOR(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x46, OpCode.LSR, (cpu, addr, value) => cpu.LSR(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x47, OpCode.RMB4, (cpu, addr, value) => ((Cpu65C02)cpu).RMB(addr, value, bit: 4), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x48, OpCode.PHA, (cpu, addr, value) => cpu.PHA(addr, value), InstructionDecoders.DecodeStack, AddressingMode.Stack),
            new(0x49, OpCode.EOR, (cpu, addr, value) => cpu.EOR(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate),
            new(0x4a, OpCode.LSR_A, (cpu, addr, value) => cpu.LSR_A(addr, value), InstructionDecoders.DecodeAccumulator, AddressingMode.Accumulator),
            new(0x4b, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 1, Cycles: 1),
            new(0x4c, OpCode.JMP, (cpu, addr, value) => cpu.JMP(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x4d, OpCode.EOR, (cpu, addr, value) => cpu.EOR(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x4e, OpCode.LSR, (cpu, addr, value) => cpu.LSR(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x4f, OpCode.BBR4, (cpu, addr, value) => ((Cpu65C02)cpu).BBR(addr, value, bit: 4), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0x50, OpCode.BVC, (cpu, addr, value) => cpu.BVC(addr, value), InstructionDecoders.DecodeRelative, AddressingMode.Relative),
            new(0x51, OpCode.EOR, (cpu, addr, value) => cpu.EOR(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed),
            new(0x52, OpCode.EOR, (cpu, addr, value) => cpu.EOR(addr, value), InstructionDecoders.DecodeZeroPageIndirect, AddressingMode.ZeroPageIndirect),
            new(0x53, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed, Bytes: 1, Cycles: 1),
            new(0x54, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed, Bytes: 2, Cycles: 4),
            new(0x55, OpCode.EOR, (cpu, addr, value) => cpu.EOR(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0x56, OpCode.LSR, (cpu, addr, value) => cpu.LSR(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0x57, OpCode.RMB5, (cpu, addr, value) => ((Cpu65C02)cpu).RMB(addr, value, bit: 5), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x58, OpCode.CLI, (cpu, addr, value) => cpu.CLI(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0x59, OpCode.EOR, (cpu, addr, value) => cpu.EOR(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed),
            new(0x5a, OpCode.PHY, (cpu, addr, value) => cpu.PHY(addr, value), InstructionDecoders.DecodeStack, AddressingMode.Stack),
            new(0x5b, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed, Bytes: 1, Cycles: 1),
            new(0x5c, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed, Bytes: 3, Cycles: 8),
            new(0x5d, OpCode.EOR, (cpu, addr, value) => cpu.EOR(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0x5e, OpCode.LSR, (cpu, addr, value) => cpu.LSR(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0x5f, OpCode.BBR5, (cpu, addr, value) => ((Cpu65C02)cpu).BBR(addr, value, bit: 5), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0x60, OpCode.RTS, (cpu, addr, value) => cpu.RTS(addr, value), InstructionDecoders.DecodeStack, AddressingMode.Stack),
            new(0x61, OpCode.ADC, (cpu, addr, value) => ((Cpu65C02)cpu).ADC(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect),
            new(0x62, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 2, Cycles: 2),
            new(0x63, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect, Bytes: 1, Cycles: 1),
            new(0x64, OpCode.STZ, (cpu, addr, value) => cpu.STZ(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x65, OpCode.ADC, (cpu, addr, value) => ((Cpu65C02)cpu).ADC(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x66, OpCode.ROR, (cpu, addr, value) => cpu.ROR(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x67, OpCode.RMB6, (cpu, addr, value) => ((Cpu65C02)cpu).RMB(addr, value, bit: 6), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x68, OpCode.PLA, (cpu, addr, value) => cpu.PLA(addr, value), InstructionDecoders.DecodeStack, AddressingMode.Stack),
            new(0x69, OpCode.ADC, (cpu, addr, value) => ((Cpu65C02)cpu).ADC(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate),
            new(0x6a, OpCode.ROR_A, (cpu, addr, value) => cpu.ROR_A(addr, value), InstructionDecoders.DecodeAccumulator, AddressingMode.Accumulator),
            new(0x6b, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 1, Cycles: 1),
            new(0x6c, OpCode.JMP, (cpu, addr, value) => cpu.JMP(addr, value), InstructionDecoders.DecodeAbsoluteIndirect, AddressingMode.AbsoluteIndirect),
            new(0x6d, OpCode.ADC, (cpu, addr, value) => ((Cpu65C02)cpu).ADC(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x6e, OpCode.ROR, (cpu, addr, value) => cpu.ROR(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x6f, OpCode.BBR6, (cpu, addr, value) => ((Cpu65C02)cpu).BBR(addr, value, bit: 6), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0x70, OpCode.BVS, (cpu, addr, value) => cpu.BVS(addr, value), InstructionDecoders.DecodeRelative, AddressingMode.Relative),
            new(0x71, OpCode.ADC, (cpu, addr, value) => ((Cpu65C02)cpu).ADC(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed),
            new(0x72, OpCode.ADC, (cpu, addr, value) => ((Cpu65C02)cpu).ADC(addr, value), InstructionDecoders.DecodeZeroPageIndirect, AddressingMode.ZeroPageIndirect),
            new(0x73, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed, Bytes: 1, Cycles: 1),
            new(0x74, OpCode.STZ, (cpu, addr, value) => cpu.STZ(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0x75, OpCode.ADC, (cpu, addr, value) => ((Cpu65C02)cpu).ADC(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0x76, OpCode.ROR, (cpu, addr, value) => cpu.ROR(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0x77, OpCode.RMB7, (cpu, addr, value) => ((Cpu65C02)cpu).RMB(addr, value, bit: 7), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x78, OpCode.SEI, (cpu, addr, value) => cpu.SEI(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0x79, OpCode.ADC, (cpu, addr, value) => ((Cpu65C02)cpu).ADC(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed),
            new(0x7a, OpCode.PLY, (cpu, addr, value) => cpu.PLY(addr, value), InstructionDecoders.DecodeStack, AddressingMode.Stack),
            new(0x7b, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed, Bytes: 1, Cycles: 1),
            new(0x7c, OpCode.JMP, (cpu, addr, value) => cpu.JMP(addr, value), InstructionDecoders.DecodeAbsoluteIndexedIndirect, AddressingMode.AbsoluteIndexedIndirect),
            new(0x7d, OpCode.ADC, (cpu, addr, value) => ((Cpu65C02)cpu).ADC(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0x7e, OpCode.ROR, (cpu, addr, value) => cpu.ROR(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0x7f, OpCode.BBR7, (cpu, addr, value) => ((Cpu65C02)cpu).BBR(addr, value, bit: 7), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0x80, OpCode.BRA, (cpu, addr, value) => cpu.BRA(addr, value), InstructionDecoders.DecodeRelative, AddressingMode.Relative),
            new(0x81, OpCode.STA, (cpu, addr, value) => cpu.STA(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect),
            new(0x82, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 2, Cycles: 2),
            new(0x83, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect, Bytes: 1, Cycles: 1),
            new(0x84, OpCode.STY, (cpu, addr, value) => cpu.STY(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x85, OpCode.STA, (cpu, addr, value) => cpu.STA(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x86, OpCode.STX, (cpu, addr, value) => cpu.STX(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x87, OpCode.SMB0, (cpu, addr, value) => ((Cpu65C02)cpu).SMB(addr, value, bit: 0), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x88, OpCode.DEY, (cpu, addr, value) => cpu.DEY(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0x89, OpCode.BIT, (cpu, addr, value) => cpu.BIT(addr, value, immediateMode: true), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate),
            new(0x8a, OpCode.TXA, (cpu, addr, value) => cpu.TXA(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0x8b, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 1, Cycles: 1),
            new(0x8c, OpCode.STY, (cpu, addr, value) => cpu.STY(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x8d, OpCode.STA, (cpu, addr, value) => cpu.STA(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x8e, OpCode.STX, (cpu, addr, value) => cpu.STX(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x8f, OpCode.BBS0, (cpu, addr, value) => ((Cpu65C02)cpu).BBS(addr, value, bit: 0), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0x90, OpCode.BCC, (cpu, addr, value) => cpu.BCC(addr, value), InstructionDecoders.DecodeRelative, AddressingMode.Relative),
            new(0x91, OpCode.STA, (cpu, addr, value) => cpu.STA(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed),
            new(0x92, OpCode.STA, (cpu, addr, value) => cpu.STA(addr, value), InstructionDecoders.DecodeZeroPageIndirect, AddressingMode.ZeroPageIndirect),
            new(0x93, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed, Bytes: 1, Cycles: 1),
            new(0x94, OpCode.STY, (cpu, addr, value) => cpu.STY(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0x95, OpCode.STA, (cpu, addr, value) => cpu.STA(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0x96, OpCode.STX, (cpu, addr, value) => cpu.STX(addr, value), InstructionDecoders.DecodeZeroPageYIndexed, AddressingMode.ZeroPageYIndexed),
            new(0x97, OpCode.SMB1, (cpu, addr, value) => ((Cpu65C02)cpu).SMB(addr, value, bit: 1), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0x98, OpCode.TYA, (cpu, addr, value) => cpu.TYA(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0x99, OpCode.STA, (cpu, addr, value) => cpu.STA(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed),
            new(0x9a, OpCode.TXS, (cpu, addr, value) => cpu.TXS(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0x9b, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed, Bytes: 1, Cycles: 1),
            new(0x9c, OpCode.STZ, (cpu, addr, value) => cpu.STZ(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0x9d, OpCode.STA, (cpu, addr, value) => cpu.STA(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0x9e, OpCode.STZ, (cpu, addr, value) => cpu.STZ(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0x9f, OpCode.BBS1, (cpu, addr, value) => ((Cpu65C02)cpu).BBS(addr, value, bit: 1), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0xa0, OpCode.LDY, (cpu, addr, value) => cpu.LDY(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate),
            new(0xa1, OpCode.LDA, (cpu, addr, value) => cpu.LDA(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect),
            new(0xa2, OpCode.LDX, (cpu, addr, value) => cpu.LDX(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate),
            new(0xa3, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect, Bytes: 1, Cycles: 1),
            new(0xa4, OpCode.LDY, (cpu, addr, value) => cpu.LDY(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xa5, OpCode.LDA, (cpu, addr, value) => cpu.LDA(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xa6, OpCode.LDX, (cpu, addr, value) => cpu.LDX(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xa7, OpCode.SMB2, (cpu, addr, value) => ((Cpu65C02)cpu).SMB(addr, value, bit: 2), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xa8, OpCode.TAY, (cpu, addr, value) => cpu.TAY(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0xa9, OpCode.LDA, (cpu, addr, value) => cpu.LDA(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate),
            new(0xaa, OpCode.TAX, (cpu, addr, value) => cpu.TAX(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0xab, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 1, Cycles: 1),
            new(0xac, OpCode.LDY, (cpu, addr, value) => cpu.LDY(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0xad, OpCode.LDA, (cpu, addr, value) => cpu.LDA(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0xae, OpCode.LDX, (cpu, addr, value) => cpu.LDX(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0xaf, OpCode.BBS2, (cpu, addr, value) => ((Cpu65C02)cpu).BBS(addr, value, bit: 2), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0xb0, OpCode.BCS, (cpu, addr, value) => cpu.BCS(addr, value), InstructionDecoders.DecodeRelative, AddressingMode.Relative),
            new(0xb1, OpCode.LDA, (cpu, addr, value) => cpu.LDA(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed),
            new(0xb2, OpCode.LDA, (cpu, addr, value) => cpu.LDA(addr, value), InstructionDecoders.DecodeZeroPageIndirect, AddressingMode.ZeroPageIndirect),
            new(0xb3, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed, Bytes: 1, Cycles: 1),
            new(0xb4, OpCode.LDY, (cpu, addr, value) => cpu.LDY(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0xb5, OpCode.LDA, (cpu, addr, value) => cpu.LDA(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0xb6, OpCode.LDX, (cpu, addr, value) => cpu.LDX(addr, value), InstructionDecoders.DecodeZeroPageYIndexed, AddressingMode.ZeroPageYIndexed),
            new(0xb7, OpCode.SMB3, (cpu, addr, value) => ((Cpu65C02)cpu).SMB(addr, value, bit: 3), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xb8, OpCode.CLV, (cpu, addr, value) => cpu.CLV(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0xb9, OpCode.LDA, (cpu, addr, value) => cpu.LDA(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed),
            new(0xba, OpCode.TSX, (cpu, addr, value) => cpu.TSX(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0xbb, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed, Bytes: 1, Cycles: 1),
            new(0xbc, OpCode.LDY, (cpu, addr, value) => cpu.LDY(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0xbd, OpCode.LDA, (cpu, addr, value) => cpu.LDA(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0xbe, OpCode.LDX, (cpu, addr, value) => cpu.LDX(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed),
            new(0xbf, OpCode.BBS3, (cpu, addr, value) => ((Cpu65C02)cpu).BBS(addr, value, bit: 3), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0xc0, OpCode.CPY, (cpu, addr, value) => cpu.CPY(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate),
            new(0xc1, OpCode.CMP, (cpu, addr, value) => cpu.CMP(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect),
            new(0xc2, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 2, Cycles: 2),
            new(0xc3, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect, Bytes: 1, Cycles: 1),
            new(0xc4, OpCode.CPY, (cpu, addr, value) => cpu.CPY(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xc5, OpCode.CMP, (cpu, addr, value) => cpu.CMP(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xc6, OpCode.DEC, (cpu, addr, value) => cpu.DEC(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xc7, OpCode.SMB4, (cpu, addr, value) => ((Cpu65C02)cpu).SMB(addr, value, bit: 4), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xc8, OpCode.INY, (cpu, addr, value) => cpu.INY(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0xc9, OpCode.CMP, (cpu, addr, value) => cpu.CMP(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate),
            new(0xca, OpCode.DEX, (cpu, addr, value) => cpu.DEX(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0xcb, OpCode.WAI, (cpu, addr, value) => cpu.WAI(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0xcc, OpCode.CPY, (cpu, addr, value) => cpu.CPY(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0xcd, OpCode.CMP, (cpu, addr, value) => cpu.CMP(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0xce, OpCode.DEC, (cpu, addr, value) => cpu.DEC(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0xcf, OpCode.BBS4, (cpu, addr, value) => ((Cpu65C02)cpu).BBS(addr, value, bit: 4), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0xd0, OpCode.BNE, (cpu, addr, value) => cpu.BNE(addr, value), InstructionDecoders.DecodeRelative, AddressingMode.Relative),
            new(0xd1, OpCode.CMP, (cpu, addr, value) => cpu.CMP(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed),
            new(0xd2, OpCode.CMP, (cpu, addr, value) => cpu.CMP(addr, value), InstructionDecoders.DecodeZeroPageIndirect, AddressingMode.ZeroPageIndirect),
            new(0xd3, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed, Bytes: 1, Cycles: 1),
            new(0xd4, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed, Bytes: 2, Cycles: 4),
            new(0xd5, OpCode.CMP, (cpu, addr, value) => cpu.CMP(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0xd6, OpCode.DEC, (cpu, addr, value) => cpu.DEC(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0xd7, OpCode.SMB5, (cpu, addr, value) => ((Cpu65C02)cpu).SMB(addr, value, bit: 5), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xd8, OpCode.CLD, (cpu, addr, value) => cpu.CLD(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0xd9, OpCode.CMP, (cpu, addr, value) => cpu.CMP(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed),
            new(0xda, OpCode.PHX, (cpu, addr, value) => cpu.PHX(addr, value), InstructionDecoders.DecodeStack, AddressingMode.Stack),
            new(0xdb, OpCode.STP, (cpu, addr, value) => cpu.STP(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0xdc, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute, Bytes: 3, Cycles: 4),
            new(0xdd, OpCode.CMP, (cpu, addr, value) => cpu.CMP(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0xde, OpCode.DEC, (cpu, addr, value) => cpu.DEC(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0xdf, OpCode.BBS5, (cpu, addr, value) => ((Cpu65C02)cpu).BBS(addr, value, bit: 5), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0xe0, OpCode.CPX, (cpu, addr, value) => cpu.CPX(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate),
            new(0xe1, OpCode.SBC, (cpu, addr, value) => ((Cpu65C02)cpu).SBC(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect),
            new(0xe2, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 2, Cycles: 2),
            new(0xe3, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeXIndexedIndirect, AddressingMode.XIndexedIndirect, Bytes: 1, Cycles: 1),
            new(0xe4, OpCode.CPX, (cpu, addr, value) => cpu.CPX(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xe5, OpCode.SBC, (cpu, addr, value) => ((Cpu65C02)cpu).SBC(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xe6, OpCode.INC, (cpu, addr, value) => cpu.INC(addr, value), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xe7, OpCode.SMB6, (cpu, addr, value) => ((Cpu65C02)cpu).SMB(addr, value, bit: 6), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xe8, OpCode.INX, (cpu, addr, value) => cpu.INX(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0xe9, OpCode.SBC, (cpu, addr, value) => ((Cpu65C02)cpu).SBC(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate),
            new(0xea, OpCode.NOP, (cpu, addr, value) => cpu.NOP(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0xeb, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeImmediate, AddressingMode.Immediate, Bytes: 1, Cycles: 1),
            new(0xec, OpCode.CPX, (cpu, addr, value) => cpu.CPX(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0xed, OpCode.SBC, (cpu, addr, value) => ((Cpu65C02)cpu).SBC(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0xee, OpCode.INC, (cpu, addr, value) => cpu.INC(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute),
            new(0xef, OpCode.BBS6, (cpu, addr, value) => ((Cpu65C02)cpu).BBS(addr, value, bit: 6), InstructionDecoders.DecodeRelative, AddressingMode.Relative),

            new(0xf0, OpCode.BEQ, (cpu, addr, value) => cpu.BEQ(addr, value), InstructionDecoders.DecodeRelative, AddressingMode.Relative),
            new(0xf1, OpCode.SBC, (cpu, addr, value) => ((Cpu65C02)cpu).SBC(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed),
            new(0xf2, OpCode.SBC, (cpu, addr, value) => ((Cpu65C02)cpu).SBC(addr, value), InstructionDecoders.DecodeZeroPageIndirect, AddressingMode.ZeroPageIndirect),
            new(0xf3, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeIndirectYIndexed, AddressingMode.IndirectYIndexed, Bytes: 1, Cycles: 1),
            new(0xf4, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed, Bytes: 2, Cycles: 4),
            new(0xf5, OpCode.SBC, (cpu, addr, value) => ((Cpu65C02)cpu).SBC(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0xf6, OpCode.INC, (cpu, addr, value) => cpu.INC(addr, value), InstructionDecoders.DecodeZeroPageXIndexed, AddressingMode.ZeroPageXIndexed),
            new(0xf7, OpCode.SMB7, (cpu, addr, value) => ((Cpu65C02)cpu).SMB(addr, value, bit: 7), InstructionDecoders.DecodeZeroPage, AddressingMode.ZeroPage),
            new(0xf8, OpCode.SED, (cpu, addr, value) => cpu.SED(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Implicit),
            new(0xf9, OpCode.SBC, (cpu, addr, value) => ((Cpu65C02)cpu).SBC(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed),
            new(0xfa, OpCode.PLX, (cpu, addr, value) => cpu.PLX(addr, value), InstructionDecoders.DecodeImplicit, AddressingMode.Stack),
            new(0xfb, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeAbsoluteYIndexed, AddressingMode.AbsoluteYIndexed, Bytes: 1, Cycles: 1),
            new(0xfc, OpCode.Unknown, (cpu, addr, value) => cpu.IllegalInstruction(addr, value), InstructionDecoders.DecodeAbsolute, AddressingMode.Absolute, Bytes: 3, Cycles: 4),
            new(0xfd, OpCode.SBC, (cpu, addr, value) => ((Cpu65C02)cpu).SBC(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0xfe, OpCode.INC, (cpu, addr, value) => cpu.INC(addr, value), InstructionDecoders.DecodeAbsoluteXIndexed, AddressingMode.AbsoluteXIndexed),
            new(0xff, OpCode.BBS7, (cpu, addr, value) => ((Cpu65C02)cpu).BBS(addr, value, bit: 7), InstructionDecoders.DecodeRelative, AddressingMode.Relative),
        };
    }
}

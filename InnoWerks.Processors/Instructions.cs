using System.Collections.Generic;

namespace InnoWerks.Processors
{
    public static class InstructionInformation
    {
        public static readonly ISet<AddressingMode> SingleByteAddressModes = new HashSet<AddressingMode>
        {
            AddressingMode.Immediate,
            AddressingMode.Relative,
            AddressingMode.ZeroPage,
            AddressingMode.ZeroPageXIndexed,
            AddressingMode.ZeroPageYIndexed,
            AddressingMode.ZeroPageIndirect,
            AddressingMode.XIndexedIndirect,
            AddressingMode.IndirectYIndexed
        };

        public static readonly ISet<AddressingMode> TwoByteAddressModes = new HashSet<AddressingMode>
        {
            AddressingMode.Absolute,
            AddressingMode.AbsoluteXIndexed,
            AddressingMode.AbsoluteYIndexed,
            AddressingMode.AbsoluteIndirect,
            AddressingMode.AbsoluteIndexedIndirect
        };

        public static readonly ISet<OpCode> BranchingOperations = new HashSet<OpCode>
        {
            OpCode.BBR0,
            OpCode.BBR1,
            OpCode.BBR2,
            OpCode.BBR3,
            OpCode.BBR4,
            OpCode.BBR5,
            OpCode.BBR6,
            OpCode.BBR7,
            OpCode.BBS0,
            OpCode.BBS1,
            OpCode.BBS2,
            OpCode.BBS3,
            OpCode.BBS4,
            OpCode.BBS5,
            OpCode.BBS6,
            OpCode.BBS7,
            OpCode.BCC,
            OpCode.BCS,
            OpCode.BEQ,
            OpCode.BMI,
            OpCode.BPL,
            OpCode.BNE,
            OpCode.BRA,
            OpCode.BVC,
            OpCode.BVS,
        };

        public static readonly ISet<OpCode> ImpliedOperations = new HashSet<OpCode>
        {
            OpCode.CLC,
            OpCode.CLD,
            OpCode.CLI,
            OpCode.CLV,
            OpCode.DEX,
            OpCode.DEY,
            OpCode.INX,
            OpCode.INY,
            OpCode.NOP,
            OpCode.SEC,
            OpCode.SED,
            OpCode.SEI,
            OpCode.STP,
            OpCode.TAX,
            OpCode.TAY,
            OpCode.TSX,
            OpCode.TXA,
            OpCode.TXS,
            OpCode.TYA,
            OpCode.WAI,
        };

        public static readonly ISet<OpCode> StackOperations = new HashSet<OpCode>
        {
            OpCode.BRK,
            OpCode.PHA,
            OpCode.PHP,
            OpCode.PHX,
            OpCode.PHY,
            OpCode.PLA,
            OpCode.PLP,
            OpCode.PLX,
            OpCode.PLY,
            OpCode.RTI,
            OpCode.RTS,
        };

        public static readonly ISet<OpCode> AccumulatorOperations = new HashSet<OpCode>
        {
            OpCode.ASL,
            OpCode.DEA,
            OpCode.INA,
            OpCode.LSR,
            OpCode.ROL,
            OpCode.ROR,
        };

        public static readonly ISet<byte> KillInstructions6502 = new HashSet<byte>
        {
            0x02, 0x12, 0x22, 0x32, 0x42, 0x52, 0x62, 0x72, 0x92, 0xB2, 0xD2, 0xF2
        };

        public static readonly IDictionary<(OpCode opCode, AddressingMode addressingMode), byte> Instructions =
            new Dictionary<(OpCode opCode, AddressingMode addressingMode), byte>
            {
                {(OpCode.BRK, AddressingMode.Stack), 0x00 },
                {(OpCode.ORA, AddressingMode.XIndexedIndirect), 0x01 },
                // unassigned
                // unassigned
                {(OpCode.TSB, AddressingMode.ZeroPage), 0x04 },
                {(OpCode.ORA, AddressingMode.ZeroPage), 0x05 },
                {(OpCode.ASL, AddressingMode.ZeroPage), 0x06 },
                {(OpCode.RMB0, AddressingMode.ZeroPage), 0x07 },
                {(OpCode.PHP, AddressingMode.Stack), 0x08 },
                {(OpCode.ORA, AddressingMode.Immediate), 0x09 },
                {(OpCode.ASL_A, AddressingMode.Accumulator), 0x0a },
                // unassigned
                {(OpCode.TSB, AddressingMode.Absolute), 0x0c },
                {(OpCode.ORA, AddressingMode.Absolute), 0x0d },
                {(OpCode.ASL, AddressingMode.Absolute), 0x0e },
                {(OpCode.BBR0, AddressingMode.Relative), 0x0f },

                {(OpCode.BPL, AddressingMode.Relative), 0x10 },
                {(OpCode.ORA, AddressingMode.IndirectYIndexed), 0x11 },
                {(OpCode.ORA, AddressingMode.ZeroPageIndirect), 0x12 },
                // unassigned
                {(OpCode.TRB, AddressingMode.ZeroPage), 0x14 },
                {(OpCode.ORA, AddressingMode.ZeroPageXIndexed), 0x15 },
                {(OpCode.ASL, AddressingMode.ZeroPageXIndexed), 0x16 },
                {(OpCode.RMB1, AddressingMode.ZeroPage), 0x17 },
                {(OpCode.CLC, AddressingMode.Implicit), 0x18 },
                {(OpCode.ORA, AddressingMode.AbsoluteYIndexed), 0x19 },
                {(OpCode.INA, AddressingMode.Accumulator), 0x1a },
                // unassigned
                {(OpCode.TRB, AddressingMode.Absolute), 0x1c },
                {(OpCode.ORA, AddressingMode.AbsoluteXIndexed), 0x1d },
                {(OpCode.ASL, AddressingMode.AbsoluteXIndexed), 0x1e },
                {(OpCode.BBR1, AddressingMode.Relative), 0x1f },

                {(OpCode.JSR, AddressingMode.Absolute), 0x20 },
                {(OpCode.AND, AddressingMode.XIndexedIndirect), 0x21 },
                // unassigned
                // unassigned
                {(OpCode.BIT, AddressingMode.ZeroPage), 0x24 },
                {(OpCode.AND, AddressingMode.ZeroPage), 0x25 },
                {(OpCode.ROL, AddressingMode.ZeroPage), 0x26 },
                {(OpCode.RMB2, AddressingMode.ZeroPage), 0x27 },
                {(OpCode.PLP, AddressingMode.Stack), 0x28 },
                {(OpCode.AND, AddressingMode.Immediate), 0x29 },
                {(OpCode.ROL_A, AddressingMode.Accumulator), 0x2a },
                // unassigned
                {(OpCode.BIT, AddressingMode.Absolute), 0x2c },
                {(OpCode.AND, AddressingMode.Absolute), 0x2d },
                {(OpCode.ROL, AddressingMode.Absolute), 0x2e },
                {(OpCode.BBR2, AddressingMode.Relative), 0x2f },

                {(OpCode.BMI, AddressingMode.Relative), 0x30 },
                {(OpCode.AND, AddressingMode.IndirectYIndexed), 0x31 },
                {(OpCode.AND, AddressingMode.ZeroPageIndirect), 0x32 },
                // unassigned
                {(OpCode.BIT, AddressingMode.ZeroPageXIndexed), 0x34 },
                {(OpCode.AND, AddressingMode.ZeroPageXIndexed), 0x35 },
                {(OpCode.ROL, AddressingMode.ZeroPageXIndexed), 0x36 },
                {(OpCode.RMB3, AddressingMode.ZeroPage), 0x37 },
                {(OpCode.SEC, AddressingMode.Implicit), 0x38 },
                {(OpCode.AND, AddressingMode.AbsoluteYIndexed), 0x39 },
                {(OpCode.DEA, AddressingMode.Accumulator), 0x3a },
                // unassigned
                {(OpCode.BIT, AddressingMode.AbsoluteXIndexed), 0x3c },
                {(OpCode.AND, AddressingMode.AbsoluteXIndexed), 0x3d },
                {(OpCode.ROL, AddressingMode.AbsoluteXIndexed), 0x3e },
                {(OpCode.BBR3, AddressingMode.Relative), 0x3f },

                {(OpCode.RTI, AddressingMode.Stack), 0x40 },
                {(OpCode.EOR, AddressingMode.XIndexedIndirect), 0x41 },
                // unassigned
                // unassigned
                // unassigned
                {(OpCode.EOR, AddressingMode.ZeroPage), 0x45 },
                {(OpCode.LSR, AddressingMode.ZeroPage), 0x46 },
                {(OpCode.RMB4, AddressingMode.ZeroPage), 0x47 },
                {(OpCode.PHA, AddressingMode.Stack), 0x48 },
                {(OpCode.EOR, AddressingMode.Immediate), 0x49 },
                {(OpCode.LSR_A, AddressingMode.Accumulator), 0x4a },
                // unassigned
                {(OpCode.JMP, AddressingMode.Absolute), 0x4c },
                {(OpCode.EOR, AddressingMode.Absolute), 0x4d },
                {(OpCode.LSR, AddressingMode.Absolute), 0x4e },
                {(OpCode.BBR4, AddressingMode.Relative), 0x4f },

                {(OpCode.BVC, AddressingMode.Relative), 0x50 },
                {(OpCode.EOR, AddressingMode.IndirectYIndexed), 0x51 },
                {(OpCode.EOR, AddressingMode.ZeroPageIndirect), 0x52 },
                // unassigned
                // unassigned
                {(OpCode.EOR, AddressingMode.ZeroPageXIndexed), 0x55 },
                {(OpCode.LSR, AddressingMode.ZeroPageXIndexed), 0x56 },
                {(OpCode.RMB5, AddressingMode.ZeroPage), 0x57 },
                {(OpCode.CLI, AddressingMode.Implicit), 0x58 },
                {(OpCode.EOR, AddressingMode.AbsoluteYIndexed), 0x59 },
                {(OpCode.PHY, AddressingMode.Stack), 0x5a },
                // unassigned
                // unassigned
                {(OpCode.EOR, AddressingMode.AbsoluteXIndexed), 0x5d },
                {(OpCode.LSR, AddressingMode.AbsoluteXIndexed), 0x5e },
                {(OpCode.BBR5, AddressingMode.Relative), 0x5f },

                {(OpCode.RTS, AddressingMode.Stack), 0x60 },
                {(OpCode.ADC, AddressingMode.XIndexedIndirect), 0x61 },
                // unassigned
                // unassigned
                {(OpCode.STZ, AddressingMode.ZeroPage), 0x64 },
                {(OpCode.ADC, AddressingMode.ZeroPage), 0x65 },
                {(OpCode.ROR, AddressingMode.ZeroPage), 0x66 },
                {(OpCode.RMB6, AddressingMode.ZeroPage), 0x67 },
                {(OpCode.PLA, AddressingMode.Stack), 0x68 },
                {(OpCode.ADC, AddressingMode.Immediate), 0x69 },
                {(OpCode.ROR_A, AddressingMode.Accumulator), 0x6a },
                // unassigned
                {(OpCode.JMP, AddressingMode.AbsoluteIndirect), 0x6c },
                {(OpCode.ADC, AddressingMode.Absolute), 0x6d },
                {(OpCode.ROR, AddressingMode.Absolute), 0x6e },
                {(OpCode.BBR6, AddressingMode.Relative), 0x6f },

                {(OpCode.BVS, AddressingMode.Relative), 0x70 },
                {(OpCode.ADC, AddressingMode.IndirectYIndexed), 0x71 },
                {(OpCode.ADC, AddressingMode.ZeroPageIndirect), 0x72 },
                // unassigned
                {(OpCode.STZ, AddressingMode.ZeroPageXIndexed), 0x74 },
                {(OpCode.ADC, AddressingMode.ZeroPageXIndexed), 0x75 },
                {(OpCode.ROR, AddressingMode.ZeroPageXIndexed), 0x76 },
                {(OpCode.RMB7, AddressingMode.ZeroPage), 0x77 },
                {(OpCode.SEI, AddressingMode.Implicit), 0x78 },
                {(OpCode.ADC, AddressingMode.AbsoluteYIndexed), 0x79 },
                {(OpCode.PLY, AddressingMode.Stack), 0x7a },
                // unassigned
                {(OpCode.JMP, AddressingMode.AbsoluteIndexedIndirect), 0x7c },
                {(OpCode.ADC, AddressingMode.AbsoluteXIndexed), 0x7d },
                {(OpCode.ROR, AddressingMode.AbsoluteXIndexed), 0x7e },
                {(OpCode.BBR7, AddressingMode.Relative), 0x7f },

                {(OpCode.BRA, AddressingMode.Relative), 0x80 },
                {(OpCode.STA, AddressingMode.XIndexedIndirect), 0x81 },
                // unassigned
                // unassigned
                {(OpCode.STY, AddressingMode.ZeroPage), 0x84 },
                {(OpCode.STA, AddressingMode.ZeroPage), 0x85 },
                {(OpCode.STX, AddressingMode.ZeroPage), 0x86 },
                {(OpCode.SMB0, AddressingMode.ZeroPage), 0x87 },
                {(OpCode.DEY, AddressingMode.Implicit), 0x88 },
                {(OpCode.BIT, AddressingMode.Immediate), 0x89 },
                {(OpCode.TXA, AddressingMode.Implicit), 0x8a },
                // unassigned
                {(OpCode.STY, AddressingMode.Absolute), 0x8c },
                {(OpCode.STA, AddressingMode.Absolute), 0x8d },
                {(OpCode.STX, AddressingMode.Absolute), 0x8e },
                {(OpCode.BBS0, AddressingMode.Relative), 0x8f },

                {(OpCode.BCC, AddressingMode.Relative), 0x90 },
                {(OpCode.STA, AddressingMode.IndirectYIndexed), 0x91 },
                {(OpCode.STA, AddressingMode.ZeroPageIndirect), 0x92 },
                // unassigned
                {(OpCode.STY, AddressingMode.ZeroPageXIndexed), 0x94 },
                {(OpCode.STA, AddressingMode.ZeroPageXIndexed), 0x95 },
                {(OpCode.STX, AddressingMode.ZeroPageYIndexed), 0x96 },
                {(OpCode.SMB1, AddressingMode.ZeroPage), 0x97 },
                {(OpCode.TYA, AddressingMode.Implicit), 0x98 },
                {(OpCode.STA, AddressingMode.AbsoluteYIndexed), 0x99 },
                {(OpCode.TXS, AddressingMode.Implicit), 0x9a },
                // unassigned
                {(OpCode.STZ, AddressingMode.Absolute), 0x9c },
                {(OpCode.STA, AddressingMode.AbsoluteXIndexed), 0x9d },
                {(OpCode.STZ, AddressingMode.AbsoluteXIndexed), 0x9e },
                {(OpCode.BBS1, AddressingMode.Relative), 0x9f },

                {(OpCode.LDY, AddressingMode.Immediate), 0xa0 },
                {(OpCode.LDA, AddressingMode.XIndexedIndirect), 0xa1 },
                {(OpCode.LDX, AddressingMode.Immediate), 0xa2 },
                // unassigned
                {(OpCode.LDY, AddressingMode.ZeroPage), 0xa4 },
                {(OpCode.LDA, AddressingMode.ZeroPage), 0xa5 },
                {(OpCode.LDX, AddressingMode.ZeroPage), 0xa6 },
                {(OpCode.SMB2, AddressingMode.ZeroPage), 0xa7 },
                {(OpCode.TAY, AddressingMode.Implicit), 0xa8 },
                {(OpCode.LDA, AddressingMode.Immediate), 0xa9 },
                {(OpCode.TAX, AddressingMode.Implicit), 0xaa },
                // unassigned
                {(OpCode.LDY, AddressingMode.Absolute), 0xac },
                {(OpCode.LDA, AddressingMode.Absolute), 0xad },
                {(OpCode.LDX, AddressingMode.Absolute), 0xae },
                {(OpCode.BBS2, AddressingMode.Relative), 0xaf },

                {(OpCode.BCS, AddressingMode.Relative), 0xb0 },
                {(OpCode.LDA, AddressingMode.IndirectYIndexed), 0xb1 },
                {(OpCode.LDA, AddressingMode.ZeroPageIndirect), 0xb2 },
                // unassigned
                {(OpCode.LDY, AddressingMode.ZeroPageXIndexed), 0xb4 },
                {(OpCode.LDA, AddressingMode.ZeroPageXIndexed), 0xb5 },
                {(OpCode.LDX, AddressingMode.ZeroPageYIndexed), 0xb6 },
                {(OpCode.SMB3, AddressingMode.ZeroPage), 0xb7 },
                {(OpCode.CLV, AddressingMode.Implicit), 0xb8 },
                {(OpCode.LDA, AddressingMode.AbsoluteYIndexed), 0xb9 },
                {(OpCode.TSX, AddressingMode.Implicit), 0xba },
                // unassigned
                {(OpCode.LDY, AddressingMode.AbsoluteXIndexed), 0xbc },
                {(OpCode.LDA, AddressingMode.AbsoluteXIndexed), 0xbd },
                {(OpCode.LDX, AddressingMode.AbsoluteYIndexed), 0xbe },
                {(OpCode.BBS3, AddressingMode.Relative), 0xbf },

                {(OpCode.CPY, AddressingMode.Immediate), 0xc0 },
                {(OpCode.CMP, AddressingMode.XIndexedIndirect), 0xc1 },
                // unassigned
                // unassigned
                {(OpCode.CPY, AddressingMode.ZeroPage), 0xc4 },
                {(OpCode.CMP, AddressingMode.ZeroPage), 0xc5 },
                {(OpCode.DEC, AddressingMode.ZeroPage), 0xc6 },
                {(OpCode.SMB4, AddressingMode.ZeroPage), 0xc7 },
                {(OpCode.INY, AddressingMode.Implicit), 0xc8 },
                {(OpCode.CMP, AddressingMode.Immediate), 0xc9 },
                {(OpCode.DEX, AddressingMode.Implicit), 0xca },
                {(OpCode.WAI, AddressingMode.Implicit), 0xcb },
                {(OpCode.CPY, AddressingMode.Absolute), 0xcc },
                {(OpCode.CMP, AddressingMode.Absolute), 0xcd },
                {(OpCode.DEC, AddressingMode.Absolute), 0xce },
                {(OpCode.BBS4, AddressingMode.Relative), 0xcf },

                {(OpCode.BNE, AddressingMode.Relative), 0xd0 },
                {(OpCode.CMP, AddressingMode.IndirectYIndexed), 0xd1 },
                {(OpCode.CMP, AddressingMode.ZeroPageIndirect), 0xd2 },
                // unassigned
                // unassigned
                {(OpCode.CMP, AddressingMode.ZeroPageXIndexed), 0xd5 },
                {(OpCode.DEC, AddressingMode.ZeroPageXIndexed), 0xd6 },
                {(OpCode.SMB5, AddressingMode.ZeroPage), 0xd7 },
                {(OpCode.CLD, AddressingMode.Implicit), 0xd8 },
                {(OpCode.CMP, AddressingMode.AbsoluteYIndexed), 0xd9 },
                {(OpCode.PHX, AddressingMode.Stack), 0xda },
                {(OpCode.STP, AddressingMode.Implicit), 0xdb },
                // unassigned
                {(OpCode.CMP, AddressingMode.AbsoluteXIndexed), 0xdd },
                {(OpCode.DEC, AddressingMode.AbsoluteXIndexed), 0xde },
                {(OpCode.BBS5, AddressingMode.Relative), 0xdf },

                {(OpCode.CPX, AddressingMode.Immediate), 0xe0 },
                {(OpCode.SBC, AddressingMode.XIndexedIndirect), 0xe1 },
                // unassigned
                // unassigned
                {(OpCode.CPX, AddressingMode.ZeroPage), 0xe4 },
                {(OpCode.SBC, AddressingMode.ZeroPage), 0xe5 },
                {(OpCode.INC, AddressingMode.ZeroPage), 0xe6 },
                {(OpCode.SMB6, AddressingMode.ZeroPage), 0xe7 },
                {(OpCode.INX, AddressingMode.Implicit), 0xe8 },
                {(OpCode.SBC, AddressingMode.Immediate), 0xe9 },
                {(OpCode.NOP, AddressingMode.Implicit), 0xea },
                // unassigned
                {(OpCode.CPX, AddressingMode.Absolute), 0xec },
                {(OpCode.SBC, AddressingMode.Absolute), 0xed },
                {(OpCode.INC, AddressingMode.Absolute), 0xee },
                {(OpCode.BBS6, AddressingMode.Relative), 0xef },

                {(OpCode.BEQ, AddressingMode.Relative), 0xf0 },
                {(OpCode.SBC, AddressingMode.IndirectYIndexed), 0xf1 },
                {(OpCode.SBC, AddressingMode.ZeroPageIndirect), 0xf2 },
                // unassigned
                // unassigned
                {(OpCode.SBC, AddressingMode.ZeroPageXIndexed), 0xf5 },
                {(OpCode.INC, AddressingMode.ZeroPageXIndexed), 0xf6 },
                {(OpCode.SMB7, AddressingMode.ZeroPage), 0xf7 },
                {(OpCode.SED, AddressingMode.Implicit), 0xf8 },
                {(OpCode.SBC, AddressingMode.AbsoluteYIndexed), 0xf9 },
                {(OpCode.PLX, AddressingMode.Stack), 0xfa },
                // unassigned
                // unassigned
                {(OpCode.SBC, AddressingMode.AbsoluteXIndexed), 0xfd },
                {(OpCode.INC, AddressingMode.AbsoluteXIndexed), 0xfe },
                {(OpCode.BBS7, AddressingMode.Relative), 0xff },
        };
    }
}

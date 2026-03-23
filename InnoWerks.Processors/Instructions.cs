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

        public static readonly IDictionary<(OpCode opCode, AddressingMode addressingMode), (byte code, CpuClass minCpuClass)> Instructions =
            new Dictionary<(OpCode opCode, AddressingMode addressingMode), (byte code, CpuClass minCpuClass)>
            {
                {(OpCode.BRK, AddressingMode.Stack), (0x00, CpuClass.WDC6502) },
                {(OpCode.ORA, AddressingMode.XIndexedIndirect), (0x01, CpuClass.WDC6502) },
                // unassigned
                // unassigned
                {(OpCode.TSB, AddressingMode.ZeroPage), (0x04, CpuClass.WDC65C02) },
                {(OpCode.ORA, AddressingMode.ZeroPage), (0x05, CpuClass.WDC6502) },
                {(OpCode.ASL, AddressingMode.ZeroPage), (0x06, CpuClass.WDC6502) },
                {(OpCode.RMB0, AddressingMode.ZeroPage), (0x07, CpuClass.WDC65C02) },
                {(OpCode.PHP, AddressingMode.Stack), (0x08, CpuClass.WDC6502) },
                {(OpCode.ORA, AddressingMode.Immediate), (0x09, CpuClass.WDC6502) },
                {(OpCode.ASL_A, AddressingMode.Accumulator), (0x0a, CpuClass.WDC6502) },
                // unassigned
                {(OpCode.TSB, AddressingMode.Absolute), (0x0c, CpuClass.WDC65C02) },
                {(OpCode.ORA, AddressingMode.Absolute), (0x0d, CpuClass.WDC6502) },
                {(OpCode.ASL, AddressingMode.Absolute), (0x0e, CpuClass.WDC6502) },
                {(OpCode.BBR0, AddressingMode.Relative), (0x0f, CpuClass.WDC65C02) },

                {(OpCode.BPL, AddressingMode.Relative), (0x10, CpuClass.WDC6502) },
                {(OpCode.ORA, AddressingMode.IndirectYIndexed), (0x11, CpuClass.WDC6502) },
                {(OpCode.ORA, AddressingMode.ZeroPageIndirect), (0x12, CpuClass.WDC65C02) },
                // unassigned
                {(OpCode.TRB, AddressingMode.ZeroPage), (0x14, CpuClass.WDC65C02) },
                {(OpCode.ORA, AddressingMode.ZeroPageXIndexed), (0x15, CpuClass.WDC6502) },
                {(OpCode.ASL, AddressingMode.ZeroPageXIndexed), (0x16, CpuClass.WDC6502) },
                {(OpCode.RMB1, AddressingMode.ZeroPage), (0x17, CpuClass.WDC65C02) },
                {(OpCode.CLC, AddressingMode.Implicit), (0x18, CpuClass.WDC6502) },
                {(OpCode.ORA, AddressingMode.AbsoluteYIndexed), (0x19, CpuClass.WDC6502) },
                {(OpCode.INA, AddressingMode.Accumulator), (0x1a, CpuClass.WDC65C02) },
                // unassigned
                {(OpCode.TRB, AddressingMode.Absolute), (0x1c, CpuClass.WDC65C02) },
                {(OpCode.ORA, AddressingMode.AbsoluteXIndexed), (0x1d, CpuClass.WDC6502) },
                {(OpCode.ASL, AddressingMode.AbsoluteXIndexed), (0x1e, CpuClass.WDC6502) },
                {(OpCode.BBR1, AddressingMode.Relative), (0x1f, CpuClass.WDC65C02) },

                {(OpCode.JSR, AddressingMode.Absolute), (0x20, CpuClass.WDC6502) },
                {(OpCode.AND, AddressingMode.XIndexedIndirect), (0x21, CpuClass.WDC6502) },
                // unassigned
                // unassigned
                {(OpCode.BIT, AddressingMode.ZeroPage), (0x24, CpuClass.WDC6502) },
                {(OpCode.AND, AddressingMode.ZeroPage), (0x25, CpuClass.WDC6502) },
                {(OpCode.ROL, AddressingMode.ZeroPage), (0x26, CpuClass.WDC6502) },
                {(OpCode.RMB2, AddressingMode.ZeroPage), (0x27, CpuClass.WDC65C02) },
                {(OpCode.PLP, AddressingMode.Stack), (0x28, CpuClass.WDC6502) },
                {(OpCode.AND, AddressingMode.Immediate), (0x29, CpuClass.WDC6502) },
                {(OpCode.ROL_A, AddressingMode.Accumulator), (0x2a, CpuClass.WDC6502) },
                // unassigned
                {(OpCode.BIT, AddressingMode.Absolute), (0x2c, CpuClass.WDC6502) },
                {(OpCode.AND, AddressingMode.Absolute), (0x2d, CpuClass.WDC6502) },
                {(OpCode.ROL, AddressingMode.Absolute), (0x2e, CpuClass.WDC6502) },
                {(OpCode.BBR2, AddressingMode.Relative), (0x2f, CpuClass.WDC65C02) },

                {(OpCode.BMI, AddressingMode.Relative), (0x30, CpuClass.WDC6502) },
                {(OpCode.AND, AddressingMode.IndirectYIndexed), (0x31, CpuClass.WDC6502) },
                {(OpCode.AND, AddressingMode.ZeroPageIndirect), (0x32, CpuClass.WDC65C02) },
                // unassigned
                {(OpCode.BIT, AddressingMode.ZeroPageXIndexed), (0x34, CpuClass.WDC65C02) },
                {(OpCode.AND, AddressingMode.ZeroPageXIndexed), (0x35, CpuClass.WDC6502) },
                {(OpCode.ROL, AddressingMode.ZeroPageXIndexed), (0x36, CpuClass.WDC6502) },
                {(OpCode.RMB3, AddressingMode.ZeroPage), (0x37, CpuClass.WDC65C02) },
                {(OpCode.SEC, AddressingMode.Implicit), (0x38, CpuClass.WDC6502) },
                {(OpCode.AND, AddressingMode.AbsoluteYIndexed), (0x39, CpuClass.WDC6502) },
                {(OpCode.DEA, AddressingMode.Accumulator), (0x3a, CpuClass.WDC65C02) },
                // unassigned
                {(OpCode.BIT, AddressingMode.AbsoluteXIndexed), (0x3c, CpuClass.WDC65C02) },
                {(OpCode.AND, AddressingMode.AbsoluteXIndexed), (0x3d, CpuClass.WDC6502) },
                {(OpCode.ROL, AddressingMode.AbsoluteXIndexed), (0x3e, CpuClass.WDC6502) },
                {(OpCode.BBR3, AddressingMode.Relative), (0x3f, CpuClass.WDC65C02) },

                {(OpCode.RTI, AddressingMode.Stack), (0x40, CpuClass.WDC6502) },
                {(OpCode.EOR, AddressingMode.XIndexedIndirect), (0x41, CpuClass.WDC6502) },
                // unassigned
                // unassigned
                // unassigned
                {(OpCode.EOR, AddressingMode.ZeroPage), (0x45, CpuClass.WDC6502) },
                {(OpCode.LSR, AddressingMode.ZeroPage), (0x46, CpuClass.WDC6502) },
                {(OpCode.RMB4, AddressingMode.ZeroPage), (0x47, CpuClass.WDC65C02) },
                {(OpCode.PHA, AddressingMode.Stack), (0x48, CpuClass.WDC6502) },
                {(OpCode.EOR, AddressingMode.Immediate), (0x49, CpuClass.WDC6502) },
                {(OpCode.LSR_A, AddressingMode.Accumulator), (0x4a, CpuClass.WDC6502) },
                // unassigned
                {(OpCode.JMP, AddressingMode.Absolute), (0x4c, CpuClass.WDC6502) },
                {(OpCode.EOR, AddressingMode.Absolute), (0x4d, CpuClass.WDC6502) },
                {(OpCode.LSR, AddressingMode.Absolute), (0x4e, CpuClass.WDC6502) },
                {(OpCode.BBR4, AddressingMode.Relative), (0x4f, CpuClass.WDC65C02) },

                {(OpCode.BVC, AddressingMode.Relative), (0x50, CpuClass.WDC6502) },
                {(OpCode.EOR, AddressingMode.IndirectYIndexed), (0x51, CpuClass.WDC6502) },
                {(OpCode.EOR, AddressingMode.ZeroPageIndirect), (0x52, CpuClass.WDC65C02) },
                // unassigned
                // unassigned
                {(OpCode.EOR, AddressingMode.ZeroPageXIndexed), (0x55, CpuClass.WDC6502) },
                {(OpCode.LSR, AddressingMode.ZeroPageXIndexed), (0x56, CpuClass.WDC6502) },
                {(OpCode.RMB5, AddressingMode.ZeroPage), (0x57, CpuClass.WDC65C02) },
                {(OpCode.CLI, AddressingMode.Implicit), (0x58, CpuClass.WDC6502) },
                {(OpCode.EOR, AddressingMode.AbsoluteYIndexed), (0x59, CpuClass.WDC6502) },
                {(OpCode.PHY, AddressingMode.Stack), (0x5a, CpuClass.WDC65C02) },
                // unassigned
                // unassigned
                {(OpCode.EOR, AddressingMode.AbsoluteXIndexed), (0x5d, CpuClass.WDC6502) },
                {(OpCode.LSR, AddressingMode.AbsoluteXIndexed), (0x5e, CpuClass.WDC6502) },
                {(OpCode.BBR5, AddressingMode.Relative), (0x5f, CpuClass.WDC65C02) },

                {(OpCode.RTS, AddressingMode.Stack), (0x60, CpuClass.WDC6502) },
                {(OpCode.ADC, AddressingMode.XIndexedIndirect), (0x61, CpuClass.WDC6502) },
                // unassigned
                // unassigned
                {(OpCode.STZ, AddressingMode.ZeroPage), (0x64, CpuClass.WDC65C02) },
                {(OpCode.ADC, AddressingMode.ZeroPage), (0x65, CpuClass.WDC6502) },
                {(OpCode.ROR, AddressingMode.ZeroPage), (0x66, CpuClass.WDC6502) },
                {(OpCode.RMB6, AddressingMode.ZeroPage), (0x67, CpuClass.WDC65C02) },
                {(OpCode.PLA, AddressingMode.Stack), (0x68, CpuClass.WDC6502) },
                {(OpCode.ADC, AddressingMode.Immediate), (0x69, CpuClass.WDC6502) },
                {(OpCode.ROR_A, AddressingMode.Accumulator), (0x6a, CpuClass.WDC6502) },
                // unassigned
                {(OpCode.JMP, AddressingMode.AbsoluteIndirect), (0x6c, CpuClass.WDC6502) },
                {(OpCode.ADC, AddressingMode.Absolute), (0x6d, CpuClass.WDC6502) },
                {(OpCode.ROR, AddressingMode.Absolute), (0x6e, CpuClass.WDC6502) },
                {(OpCode.BBR6, AddressingMode.Relative), (0x6f, CpuClass.WDC65C02) },

                {(OpCode.BVS, AddressingMode.Relative), (0x70, CpuClass.WDC6502) },
                {(OpCode.ADC, AddressingMode.IndirectYIndexed), (0x71, CpuClass.WDC6502) },
                {(OpCode.ADC, AddressingMode.ZeroPageIndirect), (0x72, CpuClass.WDC65C02) },
                // unassigned
                {(OpCode.STZ, AddressingMode.ZeroPageXIndexed), (0x74, CpuClass.WDC65C02) },
                {(OpCode.ADC, AddressingMode.ZeroPageXIndexed), (0x75, CpuClass.WDC6502) },
                {(OpCode.ROR, AddressingMode.ZeroPageXIndexed), (0x76, CpuClass.WDC6502) },
                {(OpCode.RMB7, AddressingMode.ZeroPage), (0x77, CpuClass.WDC65C02) },
                {(OpCode.SEI, AddressingMode.Implicit), (0x78, CpuClass.WDC6502) },
                {(OpCode.ADC, AddressingMode.AbsoluteYIndexed), (0x79, CpuClass.WDC6502) },
                {(OpCode.PLY, AddressingMode.Stack), (0x7a, CpuClass.WDC65C02) },
                // unassigned
                {(OpCode.JMP, AddressingMode.AbsoluteIndexedIndirect), (0x7c, CpuClass.WDC65C02) },
                {(OpCode.ADC, AddressingMode.AbsoluteXIndexed), (0x7d, CpuClass.WDC6502) },
                {(OpCode.ROR, AddressingMode.AbsoluteXIndexed), (0x7e, CpuClass.WDC6502) },
                {(OpCode.BBR7, AddressingMode.Relative), (0x7f, CpuClass.WDC65C02) },

                {(OpCode.BRA, AddressingMode.Relative), (0x80, CpuClass.WDC65C02) },
                {(OpCode.STA, AddressingMode.XIndexedIndirect), (0x81, CpuClass.WDC6502) },
                // unassigned
                // unassigned
                {(OpCode.STY, AddressingMode.ZeroPage), (0x84, CpuClass.WDC6502) },
                {(OpCode.STA, AddressingMode.ZeroPage), (0x85, CpuClass.WDC6502) },
                {(OpCode.STX, AddressingMode.ZeroPage), (0x86, CpuClass.WDC6502) },
                {(OpCode.SMB0, AddressingMode.ZeroPage), (0x87, CpuClass.WDC65C02) },
                {(OpCode.DEY, AddressingMode.Implicit), (0x88, CpuClass.WDC6502) },
                {(OpCode.BIT, AddressingMode.Immediate), (0x89, CpuClass.WDC65C02) },
                {(OpCode.TXA, AddressingMode.Implicit), (0x8a, CpuClass.WDC6502) },
                // unassigned
                {(OpCode.STY, AddressingMode.Absolute), (0x8c, CpuClass.WDC6502) },
                {(OpCode.STA, AddressingMode.Absolute), (0x8d, CpuClass.WDC6502) },
                {(OpCode.STX, AddressingMode.Absolute), (0x8e, CpuClass.WDC6502) },
                {(OpCode.BBS0, AddressingMode.Relative), (0x8f, CpuClass.WDC65C02) },

                {(OpCode.BCC, AddressingMode.Relative), (0x90, CpuClass.WDC6502) },
                {(OpCode.STA, AddressingMode.IndirectYIndexed), (0x91, CpuClass.WDC6502) },
                {(OpCode.STA, AddressingMode.ZeroPageIndirect), (0x92, CpuClass.WDC65C02) },
                // unassigned
                {(OpCode.STY, AddressingMode.ZeroPageXIndexed), (0x94, CpuClass.WDC6502) },
                {(OpCode.STA, AddressingMode.ZeroPageXIndexed), (0x95, CpuClass.WDC6502) },
                {(OpCode.STX, AddressingMode.ZeroPageYIndexed), (0x96, CpuClass.WDC6502) },
                {(OpCode.SMB1, AddressingMode.ZeroPage), (0x97, CpuClass.WDC65C02) },
                {(OpCode.TYA, AddressingMode.Implicit), (0x98, CpuClass.WDC6502) },
                {(OpCode.STA, AddressingMode.AbsoluteYIndexed), (0x99, CpuClass.WDC6502) },
                {(OpCode.TXS, AddressingMode.Implicit), (0x9a, CpuClass.WDC6502) },
                // unassigned
                {(OpCode.STZ, AddressingMode.Absolute), (0x9c, CpuClass.WDC65C02) },
                {(OpCode.STA, AddressingMode.AbsoluteXIndexed), (0x9d, CpuClass.WDC6502) },
                {(OpCode.STZ, AddressingMode.AbsoluteXIndexed), (0x9e, CpuClass.WDC65C02) },
                {(OpCode.BBS1, AddressingMode.Relative), (0x9f, CpuClass.WDC65C02) },

                {(OpCode.LDY, AddressingMode.Immediate), (0xa0, CpuClass.WDC6502) },
                {(OpCode.LDA, AddressingMode.XIndexedIndirect), (0xa1, CpuClass.WDC6502) },
                {(OpCode.LDX, AddressingMode.Immediate), (0xa2, CpuClass.WDC6502) },
                // unassigned
                {(OpCode.LDY, AddressingMode.ZeroPage), (0xa4, CpuClass.WDC6502) },
                {(OpCode.LDA, AddressingMode.ZeroPage), (0xa5, CpuClass.WDC6502) },
                {(OpCode.LDX, AddressingMode.ZeroPage), (0xa6, CpuClass.WDC6502) },
                {(OpCode.SMB2, AddressingMode.ZeroPage), (0xa7, CpuClass.WDC65C02) },
                {(OpCode.TAY, AddressingMode.Implicit), (0xa8, CpuClass.WDC6502) },
                {(OpCode.LDA, AddressingMode.Immediate), (0xa9, CpuClass.WDC6502) },
                {(OpCode.TAX, AddressingMode.Implicit), (0xaa, CpuClass.WDC6502) },
                // unassigned
                {(OpCode.LDY, AddressingMode.Absolute), (0xac, CpuClass.WDC6502) },
                {(OpCode.LDA, AddressingMode.Absolute), (0xad, CpuClass.WDC6502) },
                {(OpCode.LDX, AddressingMode.Absolute), (0xae, CpuClass.WDC6502) },
                {(OpCode.BBS2, AddressingMode.Relative), (0xaf, CpuClass.WDC65C02) },

                {(OpCode.BCS, AddressingMode.Relative), (0xb0, CpuClass.WDC6502) },
                {(OpCode.LDA, AddressingMode.IndirectYIndexed), (0xb1, CpuClass.WDC6502) },
                {(OpCode.LDA, AddressingMode.ZeroPageIndirect), (0xb2, CpuClass.WDC65C02) },
                // unassigned
                {(OpCode.LDY, AddressingMode.ZeroPageXIndexed), (0xb4, CpuClass.WDC6502) },
                {(OpCode.LDA, AddressingMode.ZeroPageXIndexed), (0xb5, CpuClass.WDC6502) },
                {(OpCode.LDX, AddressingMode.ZeroPageYIndexed), (0xb6, CpuClass.WDC6502) },
                {(OpCode.SMB3, AddressingMode.ZeroPage), (0xb7, CpuClass.WDC65C02) },
                {(OpCode.CLV, AddressingMode.Implicit), (0xb8, CpuClass.WDC6502) },
                {(OpCode.LDA, AddressingMode.AbsoluteYIndexed), (0xb9, CpuClass.WDC6502) },
                {(OpCode.TSX, AddressingMode.Implicit), (0xba, CpuClass.WDC6502) },
                // unassigned
                {(OpCode.LDY, AddressingMode.AbsoluteXIndexed), (0xbc, CpuClass.WDC6502) },
                {(OpCode.LDA, AddressingMode.AbsoluteXIndexed), (0xbd, CpuClass.WDC6502) },
                {(OpCode.LDX, AddressingMode.AbsoluteYIndexed), (0xbe, CpuClass.WDC6502) },
                {(OpCode.BBS3, AddressingMode.Relative), (0xbf, CpuClass.WDC65C02) },

                {(OpCode.CPY, AddressingMode.Immediate), (0xc0, CpuClass.WDC6502) },
                {(OpCode.CMP, AddressingMode.XIndexedIndirect), (0xc1, CpuClass.WDC6502) },
                // unassigned
                // unassigned
                {(OpCode.CPY, AddressingMode.ZeroPage), (0xc4, CpuClass.WDC6502) },
                {(OpCode.CMP, AddressingMode.ZeroPage), (0xc5, CpuClass.WDC6502) },
                {(OpCode.DEC, AddressingMode.ZeroPage), (0xc6, CpuClass.WDC6502) },
                {(OpCode.SMB4, AddressingMode.ZeroPage), (0xc7, CpuClass.WDC65C02) },
                {(OpCode.INY, AddressingMode.Implicit), (0xc8, CpuClass.WDC6502) },
                {(OpCode.CMP, AddressingMode.Immediate), (0xc9, CpuClass.WDC6502) },
                {(OpCode.DEX, AddressingMode.Implicit), (0xca, CpuClass.WDC6502) },
                {(OpCode.WAI, AddressingMode.Implicit), (0xcb, CpuClass.WDC65C02) },
                {(OpCode.CPY, AddressingMode.Absolute), (0xcc, CpuClass.WDC6502) },
                {(OpCode.CMP, AddressingMode.Absolute), (0xcd, CpuClass.WDC6502) },
                {(OpCode.DEC, AddressingMode.Absolute), (0xce, CpuClass.WDC6502) },
                {(OpCode.BBS4, AddressingMode.Relative), (0xcf, CpuClass.WDC65C02) },

                {(OpCode.BNE, AddressingMode.Relative), (0xd0, CpuClass.WDC6502) },
                {(OpCode.CMP, AddressingMode.IndirectYIndexed), (0xd1, CpuClass.WDC6502) },
                {(OpCode.CMP, AddressingMode.ZeroPageIndirect), (0xd2, CpuClass.WDC65C02) },
                // unassigned
                // unassigned
                {(OpCode.CMP, AddressingMode.ZeroPageXIndexed), (0xd5, CpuClass.WDC6502) },
                {(OpCode.DEC, AddressingMode.ZeroPageXIndexed), (0xd6, CpuClass.WDC6502) },
                {(OpCode.SMB5, AddressingMode.ZeroPage), (0xd7, CpuClass.WDC65C02) },
                {(OpCode.CLD, AddressingMode.Implicit), (0xd8, CpuClass.WDC6502) },
                {(OpCode.CMP, AddressingMode.AbsoluteYIndexed), (0xd9, CpuClass.WDC6502) },
                {(OpCode.PHX, AddressingMode.Stack), (0xda, CpuClass.WDC65C02) },
                {(OpCode.STP, AddressingMode.Implicit), (0xdb, CpuClass.WDC65C02) },
                // unassigned
                {(OpCode.CMP, AddressingMode.AbsoluteXIndexed), (0xdd, CpuClass.WDC6502) },
                {(OpCode.DEC, AddressingMode.AbsoluteXIndexed), (0xde, CpuClass.WDC6502) },
                {(OpCode.BBS5, AddressingMode.Relative), (0xdf, CpuClass.WDC65C02) },

                {(OpCode.CPX, AddressingMode.Immediate), (0xe0, CpuClass.WDC6502) },
                {(OpCode.SBC, AddressingMode.XIndexedIndirect), (0xe1, CpuClass.WDC6502) },
                // unassigned
                // unassigned
                {(OpCode.CPX, AddressingMode.ZeroPage), (0xe4, CpuClass.WDC6502) },
                {(OpCode.SBC, AddressingMode.ZeroPage), (0xe5, CpuClass.WDC6502) },
                {(OpCode.INC, AddressingMode.ZeroPage), (0xe6, CpuClass.WDC6502) },
                {(OpCode.SMB6, AddressingMode.ZeroPage), (0xe7, CpuClass.WDC65C02) },
                {(OpCode.INX, AddressingMode.Implicit), (0xe8, CpuClass.WDC6502) },
                {(OpCode.SBC, AddressingMode.Immediate), (0xe9, CpuClass.WDC6502) },
                {(OpCode.NOP, AddressingMode.Implicit), (0xea, CpuClass.WDC6502) },
                // unassigned
                {(OpCode.CPX, AddressingMode.Absolute), (0xec, CpuClass.WDC6502) },
                {(OpCode.SBC, AddressingMode.Absolute), (0xed, CpuClass.WDC6502) },
                {(OpCode.INC, AddressingMode.Absolute), (0xee, CpuClass.WDC6502) },
                {(OpCode.BBS6, AddressingMode.Relative), (0xef, CpuClass.WDC6502) },

                {(OpCode.BEQ, AddressingMode.Relative), (0xf0, CpuClass.WDC6502) },
                {(OpCode.SBC, AddressingMode.IndirectYIndexed), (0xf1, CpuClass.WDC6502) },
                {(OpCode.SBC, AddressingMode.ZeroPageIndirect), (0xf2, CpuClass.WDC65C02) },
                // unassigned
                // unassigned
                {(OpCode.SBC, AddressingMode.ZeroPageXIndexed), (0xf5, CpuClass.WDC6502) },
                {(OpCode.INC, AddressingMode.ZeroPageXIndexed), (0xf6, CpuClass.WDC6502) },
                {(OpCode.SMB7, AddressingMode.ZeroPage), (0xf7, CpuClass.WDC65C02) },
                {(OpCode.SED, AddressingMode.Implicit), (0xf8, CpuClass.WDC6502) },
                {(OpCode.SBC, AddressingMode.AbsoluteYIndexed), (0xf9, CpuClass.WDC6502) },
                {(OpCode.PLX, AddressingMode.Stack), (0xfa, CpuClass.WDC65C02) },
                // unassigned
                // unassigned
                {(OpCode.SBC, AddressingMode.AbsoluteXIndexed), (0xfd, CpuClass.WDC6502) },
                {(OpCode.INC, AddressingMode.AbsoluteXIndexed), (0xfe, CpuClass.WDC6502) },
                {(OpCode.BBS7, AddressingMode.Relative), (0xff, CpuClass.WDC65C02) },
        };
    }
}

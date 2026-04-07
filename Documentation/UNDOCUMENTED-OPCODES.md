# Undocumented 6502 Opcodes

## Overview

All 256 NMOS 6502 opcode slots are implemented and validated against the Harte cycle-accurate test suite. The undocumented opcodes are implemented in `Cpu6502.cs` (not the base class) since they are specific to the NMOS 6502.

## Reference

- [Masswerk: 6502 Illegal Opcodes](https://www.masswerk.at/nowgobang/2021/6502-illegal-opcodes)
- `Documentation/c74-6502-undocumented-opcodes.pdf`

## Instruction Categories

### RMW Combos (42 opcodes)

These combine a read-modify-write operation with an ALU operation on the accumulator:

| Mnemonic | Operation | Opcodes |
|---|---|---|
| SLO | ASL mem, ORA A | 03, 07, 0F, 13, 17, 1B, 1F |
| RLA | ROL mem, AND A | 23, 27, 2F, 33, 37, 3B, 3F |
| SRE | LSR mem, EOR A | 43, 47, 4F, 53, 57, 5B, 5F |
| RRA | ROR mem, ADC A | 63, 67, 6F, 73, 77, 7B, 7F |
| DCP | DEC mem, CMP A | C3, C7, CF, D3, D7, DB, DF |
| ISC | INC mem, SBC A | E3, E7, EF, F3, F7, FB, FF |

These required adding AbsoluteYIndexed, XIndexedIndirect, and IndirectYIndexed cases to the RMW dispatch block in `Cpu6502.Dispatch()`.

### Store/Load Combos (10 opcodes)

| Mnemonic | Operation | Opcodes |
|---|---|---|
| SAX | Store A AND X | 83, 87, 8F, 97 |
| LAX | Load A and X | A3, A7, AF, B3, B7, BF |

### Immediate ALU (6 opcodes)

| Mnemonic | Operation | Opcodes |
|---|---|---|
| ANC | AND #imm, bit 7→C | 0B, 2B |
| ALR | AND #imm, LSR A | 4B |
| ARR | AND #imm, ROR A (special C/V) | 6B |
| AXS | (A AND X) - imm → X | CB |
| USBC | Same as SBC #imm | EB |

### Unstable (8 opcodes)

| Mnemonic | Operation | Opcodes |
|---|---|---|
| ANE | (A OR CONST) AND X AND imm → A | 8B |
| LXA | (A OR CONST) AND imm → A, X | AB |
| SHA | A AND X AND (H+1) → M | 93, 9F |
| SHX | X AND (H+1) → M | 9E |
| SHY | Y AND (H+1) → M | 9C |
| TAS | A AND X → SP; A AND X AND (H+1) → M | 9B |
| LAS | M AND SP → A, X, SP | BB |

The magic constant for ANE/LXA is `0xEE` (matches Harte test expectations, consistent with VIC20 behavior).

SHA/SHX/SHY/TAS have a page-crossing address fixup where the high byte of the stored value AND the high byte of the target address interact.

### Multi-byte NOPs — DOP (27 opcodes)

Various cycle counts and byte widths depending on addressing mode. Separated from `OpCode.NOP` (which is the legal 1-byte implied NOP) to avoid dispatch conflicts.

### Processor Halt — KIL (12 opcodes)

`$02, $12, $22, $32, $42, $52, $62, $72, $92, $B2, $D2, $F2`

11-cycle bus pattern: reads PC+1, then oscillates reading `$FFFE`/`$FFFF`. Sets the `stopped` flag.

## 65C02 Unknown Opcode Dispatch

All 65C02 variants (WDC, Synertek, Rockwell) share a consistent pattern for unimplemented opcode slots:

1. `$5C` — special case, unique 8-cycle timing
2. `Bytes == 1` — 1-byte, 1-cycle NOP (x3/xB slots)
3. Addressing mode switch — standard bus cycle pattern
4. Synertek adds Relative mode with `b & 4` check for BBS extra cycle

The B=5 (SMB slots) on Synertek use ZeroPageXIndexed addressing from the C=1 pattern. The C=1 addressing mode pattern does NOT apply to B=3 (BBR) or B=0,2,4,6 (1-byte NOPs).

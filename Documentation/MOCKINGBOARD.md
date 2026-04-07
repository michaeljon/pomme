# Mockingboard Sound Card

## Overview

The emulator implements the Mockingboard A/B (Sweet Micro Systems), a sound card containing two 6522 VIAs and two AY-3-8910 Programmable Sound Generators (PSGs), providing 6 voices of synthesized audio.

## Hardware Architecture

```
              ┌────────────────┐
              │  Mockingboard  │
    $Cn00─┬───┤                │
          │   │  ┌──────────┐  │
  VIA1 ───┼───┤  │ AY-3-8910│  │──── Audio L
  (bit 7=0)   │  │  PSG 1   │  │
              │  └──────────┘  │
    $Cn80─┬───┤                │
          │   │  ┌──────────┐  │
  VIA2 ───┼───┤  │ AY-3-8910│  │──── Audio R
  (bit 7=1)   │  │  PSG 2   │  │
              │  └──────────┘  │
              └────────────────┘
```

## Address Mapping

The Mockingboard A/B has **no ROM**. All 256 bytes of the `$Cn00-$CnFF` slot ROM space are used for VIA register access:

- **VIA1:** `$Cn00-$Cn0F` (address bit 7 = 0)
- **VIA2:** `$Cn80-$Cn8F` (address bit 7 = 1)
- Bits 0-3 select the VIA register (16 registers each)

The `$C0n0-$C0nF` I/O space is not used.

## VIA-to-PSG Connection

Each VIA connects to its PSG via:

- **Port A:** 8-bit bidirectional data bus to PSG
- **Port B bits 0-2:** PSG bus control signals

| Port B Bit | Signal | Function         |
| ---------- | ------ | ---------------- |
| 0          | BC1    | Bus Control 1    |
| 1          | BDIR   | Bus Direction    |
| 2          | ~RESET | Active-low reset |

### Bus Control Truth Table

| BDIR | BC1 | Operation              |
| ---- | --- | ---------------------- |
| 0    | 0   | Inactive               |
| 0    | 1   | Read PSG register      |
| 1    | 0   | Write to PSG register  |
| 1    | 1   | Latch register address |

The ~RESET line (bit 2) must be high (inactive) for normal operation. Setting it low resets all PSG registers.

## Detection

The Mockingboard is detected via **VIA Timer 1**. Detection software (CardCat, A2Desktop) reads Timer 1 Counter Low (register `$04`) repeatedly and checks that it decrements. On a real 6522, Timer 1 always free-runs — it counts down continuously even after reset, without needing to be explicitly started.

## Audio Integration

`MockingboardSlotDevice.GenerateSample()` clocks both PSGs and mixes their output. The `AudioRenderer` calls this at 44.1 kHz, using a fractional accumulator to clock the PSGs at the correct rate (~23.14 system clocks per audio sample). The Mockingboard audio is mixed with the built-in speaker signal.

## AY-3-8910 Register Map

| Register | Function                                            |
| -------- | --------------------------------------------------- |
| R0-R1    | Channel A tone period (12-bit)                      |
| R2-R3    | Channel B tone period (12-bit)                      |
| R4-R5    | Channel C tone period (12-bit)                      |
| R6       | Noise period (5-bit)                                |
| R7       | Mixer control                                       |
| R8-R10   | Channel A/B/C volume (5-bit, bit 4 = envelope mode) |
| R11-R12  | Envelope period (16-bit)                            |
| R13      | Envelope shape (4-bit)                              |
| R14-R15  | I/O ports (unused)                                  |

## Limitations

- Speech synthesis (SC-01 Votrax / SSI 263) is not implemented
- The emulated model is the Mockingboard A/B; the Mockingboard D (IIc-only) is not supported

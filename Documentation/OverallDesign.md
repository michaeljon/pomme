# Apple IIe Emulator — Design Document

## Project Overview

This is a complete Apple IIe emulator written in C#, built on MonoGame for rendering and audio. The project targets the enhanced Apple IIe with a WDC 65C02 CPU, 128KB RAM, and standard peripheral cards.

---

## Solution Structure

| Project | Purpose |
|---|---|
| `InnoWerks.Processors` | Opcode and instruction set definitions |
| `InnoWerks.Simulators.Sim6502` | 6502/65C02 CPU emulation core |
| `InnoWerks.Computers.Apple` | Apple II hardware (memory, bus, devices) |
| `InnoWerks.Emulators.AppleIIe` | Main emulator app (MonoGame UI, renderers, audio) |
| `InnoWerks.Assemblers.Asm6502` | 6502 assembler (used to build device ROMs at runtime) |
| `InnoWerks.Disassemblers.Dasm6502` | 6502 disassembler (debug tooling) |
| Test projects | Unit tests and Harte 65x02 test suite integration |

The `ConsoleTools` projects are developer utilities not part of the emulator itself.

---

## Architecture Overview

```
┌─────────────────────────── Emulator (MonoGame Game) ───────────────────────────┐
│                                                                                  │
│  ┌──────────────┐   ┌─────────────────────┐   ┌───────────────────────────┐   │
│  │   Display    │   │     Cpu65C02        │   │      AudioRenderer        │   │
│  │  (renderer   │   │  (instruction       │   │  (PCM synthesis from      │   │
│  │  selection)  │   │   dispatch)         │   │   speaker toggles)        │   │
│  └──────┬───────┘   └──────────┬──────────┘   └───────────────────────────┘   │
│         │                      │                                                │
│         └──────────────────────▼──────────────────────────────────────────┐   │
│                            AppleBus (IBus)                                 │   │
│         ┌────────────────────┬──────────────┬───────────────────────────┐ │   │
│         │                    │              │                           │ │   │
│       IOU                  MMU        SlotDevices               Memory128k│   │
│  (kbd, speaker,     (LC banking,    [Slot 5: ProDOS]          (64K main + │   │
│   paddles, VBL)      ROM control,   [Slot 6: Disk II]          64K aux)   │   │
│                       aux memory)                                          │   │
└────────────────────────────────────────────────────────────────────────────────┘
```

---

## CPU Subsystem

**Location:** `InnoWerks.Simulators.Sim6502/`

The CPU is modeled in two layers:

- **`Cpu6502Core`** (abstract) — registers, interrupt handling, stack operations, reset vectors, memory access via `IBus`
- **`Cpu65C02`** (concrete) — instruction dispatch table, all 11 addressing modes, cycle-accurate execution, decimal mode

Every opcode is stored as an `OpCodeDefinition` containing the opcode byte, addressing mode, cycle count, and an executor function delegate. The `CpuInstructions` static class builds this dictionary for the full 65C02 instruction set.

**CPU registers:** A, X, Y, SP, PC, P (NV-BDIZC flags)

**Interrupt vectors:**
- Reset: `$FFFC/$FFFD`
- NMI: `$FFFA/$FFFB`
- IRQ/BRK: `$FFFE/$FFFF`

---

## Bus Architecture

**File:** `InnoWerks.Computers.Apple/AppleBus.cs`

`AppleBus` implements `IBus` and is the single routing point for all CPU memory transactions. Every read or write flows through it:

1. Soft-switch addresses (`$C000–$C0FF`) are dispatched to `IOU` or `MMU`
2. Slot device I/O ranges are dispatched to the matching `ISlotDevice`
3. All other addresses go to `Memory128k`
4. Each transaction increments the global cycle counter

---

## Memory Subsystem

**Location:** `InnoWerks.Computers.Apple/Memory/`

The Apple IIe's 128KB is organized as 64KB main + 64KB auxiliary RAM. The logical address space is 64KB divided into 256 pages of 256 bytes. Two independent maps — `activeRead[]` and `activeWrite[]` — point each page to the appropriate physical backing store. `Remap()` recalculates both maps whenever any soft switch changes.

**Address space layout:**

| Range | Description |
|---|---|
| `$0000–$BFFF` | Main/aux RAM (switchable) |
| `$C000–$C07F` | On-board I/O (IOU: keyboard, speaker, paddles) |
| `$C080–$C08F` | Language card / MMU control |
| `$C090–$C0FF` | Slot device I/O registers |
| `$C100–$C7FF` | Per-slot ROM (256 bytes each, slots 1–7) |
| `$C800–$CFFF` | Shared slot expansion ROM (2KB) |
| `$D000–$DFFF` | Language card (bank 1 or bank 2, ROM or RAM) |
| `$E000–$FFFF` | System ROM |

**Banking soft switches:** `LcBank2`, `LcReadEnabled`, `LcWriteEnabled`, `AuxRead`, `AuxWrite`, `ZpAux`, `Store80`, `IntCxRomEnabled`, `IntC8RomEnabled`, `SlotC3RomEnabled`

---

## I/O and Soft Switches

**Location:** `InnoWerks.Computers.Apple/SystemDevices/`

All 56 soft-switch flags live in `MachineState` as a `Dictionary<SoftSwitch, bool>`.

**`IOU`** handles:
- Keyboard latch, strobe, and queue
- Speaker toggle (`$C030`)
- Paddle/joystick inputs and game strobe
- Annunciators (4 outputs)
- VBL status

**`MMU`** handles:
- Language card read/write/bank control
- Display mode flags (text, mixed, page 2, hi-res, double hi-res, 80-col, alt charset)
- Cx/C8 ROM banking
- Auxiliary memory routing

---

## Video Subsystem

**Location:** `InnoWerks.Emulators.AppleIIe/Renderers/`

`Display` reads the active soft switches each frame and delegates to the appropriate renderer. Each renderer is a subclass of the abstract `Renderer` base, which provides address calculation helpers and a `Draw()` method.

| Mode | Renderer | Resolution | Notes |
|---|---|---|---|
| Text 40-col | `TextModeRenderer` | 40×24 characters | 8×8 font from character ROM |
| Text 80-col | `TextModeRenderer` | 80×24 characters | Interleaved main/aux RAM |
| Lo-res | `LoresRenderer` | 40×48 blocks | 16 colors, 7×4 pixels/block |
| Hi-res | `HiresRenderer` | 280×192 | Color artifacts; rendered at 560px wide |
| Double hi-res | `DhiresRenderer` | 560×192 | 16-color; requires aux RAM |

**Timing constants:**
- Frame: 17,030 CPU cycles (~59.94 FPS)
- VBL start: cycle 12,480

Text flashing is driven by a 100ms timer independent of the frame loop.

---

## Audio Subsystem

**Location:** `InnoWerks.Emulators.AppleIIe/Audio/`

Audio comes from a 1-bit speaker toggled by writes to `$C030`. `AppleIIAudioSource` queues each toggle with a CPU cycle timestamp. `AudioRenderer` converts this to 44.1kHz PCM:

1. Calculate target sample count from elapsed CPU cycles
2. For each sample, integrate speaker level over the sample period (cycle-accurate)
3. Apply DSP chain: low-pass filter → high-pass (DC blocker) → silence snap → gain (0.25×) → clip
4. Convert to PCM16LE and submit to MonoGame's `DynamicSoundEffectInstance`

**Constants:** clock = 1,020,484 Hz, sample rate = 44,100 Hz, buffer = 2,048 samples

---

## Peripheral Slot Architecture

**Location:** `InnoWerks.Computers.Apple/Devices/`

Each of the 7 slots can hold one device implementing `ISlotDevice` (base: `SlotRomDevice`). The bus dispatches to a device's three address windows:
- **I/O registers:** `$C0X0–$C0XF` (X = slot number)
- **Cx ROM:** `$CX00–$CXFF`
- **C8 ROM:** `$C800–$CFFF` (shared, activated by slot selection)

Devices can also register **CPU intercept handlers** — callbacks invoked when the PC reaches a specific address. This is used by the ProDOS driver to implement SmartPort commands in managed code rather than ROM.

### Disk II Controller (Slot 6)

**File:** `InnoWerks.Computers.Apple/Devices/DiskIISlotDevice.cs`

Emulates the Apple Disk II controller with two drives. Each `DiskIIDrive` contains:

- **Head stepper** — 4-phase magnet simulation with phase-delta lookup tables, half-track positioning (163 half-tracks), mechanical inertia
- **Spin simulation** — Data reads timed to disk rotation; nibble-stream auto-advance
- **Latch I/O** — Read/write latch at `$C0xC–$C0xD`

`FloppyDisk` stores 35 tracks × 6,384 nibbles in standard DSK format (140KB), encoded using 6-and-2 nibble encoding.

### ProDOS Hard Disk Controller (Slot 5)

**File:** `InnoWerks.Computers.Apple/Devices/ProDOSSlotDevice.cs`

Implements the ProDOS SmartPort protocol for up to 4 drives. The ROM is assembled at runtime from two fragments using the built-in assembler. Driver entry is intercepted by a CPU hook that handles:

- **Command `$01`** — Read 512-byte block
- **Command `$02`** — Write 512-byte block
- **Command `$00`** — Status
- **Command `$03`** — Format

Block number and buffer address come from zero-page parameters (`$42–$47`). The actual data is read from/written to host `.hd`/`.2mg` image files via `FileStream`.

---

## Emulator Loop

**File:** `InnoWerks.Emulators.AppleIIe/Emulator.cs`

```
MonoGame Update():
  1. Poll host keyboard/mouse → inject into IOU
  2. RunCpuForFrame():
       while (cycles < frameTarget):
           check breakpoints
           cpu.Step()  →  fetch → decode → execute → tick bus
  3. Advance audio renderer to current cycle count

MonoGame Draw():
  4. Display.Render() → select renderer → draw to RenderTarget2D → blit
  5. 100ms timer → toggle text flash state
```

Fixed-timestep mode is enabled (`IsFixedTimeStep = true`, `TargetElapsedTime = 1/59.94s`) with vsync.

---

## Key Design Patterns

| Pattern | Where used |
|---|---|
| **Template Method** | `Cpu6502Core` (abstract) / `Cpu65C02` (concrete) |
| **Strategy** | Video renderers (one per display mode) |
| **Observer** | Soft switch writes trigger `Remap()` on memory |
| **Dispatch Table** | CPU opcode dictionary (byte → `OpCodeDefinition`) |
| **Intercept Hook** | `ProDOSSlotDevice` SmartPort driver in managed code |
| **DSP Chain** | Audio filter pipeline (LP → HP → gate → gain → clip) |

---

## Test Infrastructure

The project includes a Harte 65x02 test suite integration (git submodule) that runs JSON-based cycle-accurate tests against the CPU implementation, validating register state and memory contents after every instruction.

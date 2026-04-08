# 6502/65C02 Interrupt Handling — Implementation Design

## Overview

The 6502 has two hardware interrupt lines — **IRQ** (maskable) and **NMI** (non-maskable) — plus
the software **BRK** instruction which piggybacks the IRQ vector.  This document describes how
each is modeled in the emulator, the precise stack frame laid down on entry, and how RTI unwinds it.

---

## Source Files

| File | Purpose |
|------|---------|
| `InnoWerks.Simulators.Sim6502/I6502Cpu.cs` | `InjectInterrupt(bool nmi)` public API |
| `InnoWerks.Simulators.Sim6502/Cpu6502Core.cs` | `HandleNMI()`, `HandleIRQ()`, pending flags, `Step()` dispatch |
| `InnoWerks.Simulators.Tests/InterruptTests.cs` | Unit tests for all interrupt scenarios |

---

## Hardware Background

### Interrupt Vectors

| Vector | Address | Purpose |
|--------|---------|---------|
| NMI    | `$FFFA` / `$FFFB` | Non-maskable interrupt (PCL / PCH) |
| RESET  | `$FFFC` / `$FFFD` | Power-on / reset vector |
| IRQ/BRK | `$FFFE` / `$FFFF` | Maskable interrupt and BRK (PCL / PCH) |

These are defined as public constants on `Cpu6502Core`:

```csharp
public const ushort IrqVectorH = 0xFFFF;
public const ushort IrqVectorL = 0xFFFE;
public const ushort RstVectorH = 0xFFFD;
public const ushort RstVectorL = 0xFFFC;
public const ushort NmiVectorH = 0xFFFB;
public const ushort NmiVectorL = 0xFFFA;
```

### The I (Interrupt Disable) Flag

- When **I = 1**, IRQ is **masked** — the CPU ignores the IRQ line.
- When **I = 0**, IRQ is **acknowledged** and the ISR is entered.
- **NMI always fires** regardless of the I flag.
- Both IRQ and NMI set I = 1 on entry so the ISR is not re-entered by a new IRQ while running.
- The I flag is restored by RTI (it was saved on the stack).

---

## Injection API

External hardware (keyboard, VBL, disk controller, etc.) signals an interrupt by calling:

```csharp
cpu.InjectInterrupt(bool nmi);
```

- `nmi: true`  — sets `nmiPending = true`
- `nmi: false` — sets `irqPending = true`

Both flags are private fields on `Cpu6502Core`.  Callers do not need to know whether the CPU
is currently mid-instruction; the pending flag is checked atomically at the top of the next
call to `Step()`.

---

## Dispatch in Step()

At the very beginning of `Step()`, before instruction fetch, the pending flags are evaluated:

```csharp
if (nmiPending == true)
{
    HandleNMI();
}
else if (irqPending == true && Registers.Interrupt == false)
{
    HandleIRQ();
}
```

Key points:

1. **NMI has absolute priority** — if both are pending, NMI is taken and IRQ remains pending
   for the next `Step()`.
2. **IRQ checks I flag** — if the flag is set the IRQ stays pending (the flag is not cleared)
   until a CLI or RTI lowers it.
3. After handling, execution continues immediately: `HandleNMI()` / `HandleIRQ()` set PC to
   the ISR entry point, so the normal instruction fetch that follows naturally executes the
   first ISR instruction.  There is no branch; the interrupt setup and first ISR instruction
   happen in the same `Step()` call.

---

## Stack Frame

Both NMI and IRQ lay down an identical 3-byte stack frame (MSB first):

```
SP before: $FD (post-reset default)

$01FD  ← PCH  (high byte of PC at interrupt time)
$01FC  ← PCL  (low byte of PC at interrupt time)
$01FB  ← PS   (processor status with B flag forced clear, Unused forced set)

SP after:  $FA
```

The B (Break) flag distinction:

| Source  | B in stacked PS |
|---------|-----------------|
| IRQ     | 0 (clear)       |
| NMI     | 0 (clear)       |
| BRK     | 1 (set)         |

This lets ISR code distinguish a software BRK from hardware IRQ/NMI by inspecting bit 4 of the
restored PS after pulling it from the stack.

The implementation:

```csharp
StackPushWord(Registers.ProgramCounter);          // PCH then PCL
StackPush((byte)((Registers.ProcessorStatus & 0xef)   // clear B (bit 4)
               | (byte)ProcessorStatusBit.Unused));   // ensure Unused (bit 5) set
```

### 65C02 — Decimal Flag

On the WDC 65C02, both NMI and IRQ **clear the Decimal flag** on entry.  The NMOS 6502 leaves
the Decimal flag in an undefined state (this emulator leaves it unchanged, matching common
emulator practice for 6502 mode).

---

## Handlers

Both handlers follow the same structure; they differ only in which vector they load and which
pending flag they clear.

```csharp
private void HandleNMI()
{
    StackPushWord(Registers.ProgramCounter);
    StackPush((byte)((Registers.ProcessorStatus & 0xef) | (byte)ProcessorStatusBit.Unused));

    if (CpuClass == CpuClass.WDC65C02) Registers.Decimal = false;

    Registers.Interrupt = true;

    byte pcl = bus.Read(NmiVectorL);
    byte pch = bus.Read(NmiVectorH);
    Registers.ProgramCounter = RegisterMath.MakeShort(pch, pcl);

    nmiPending = false;
}
```

`HandleIRQ()` is identical except it reads `IrqVectorL` / `IrqVectorH` and clears `irqPending`.

---

## Return from Interrupt (RTI)

RTI is implemented in `Cpu6502Core.RTI()`:

```csharp
Registers.ProcessorStatus = StackPop();   // PS (clears B, ensures Unused)
Registers.Break  = false;
Registers.Unused = true;

Registers.ProgramCounter = StackPopWord();  // PCL then PCH → full address
```

`StackPopWord` is the reverse of `StackPushWord`: it pops PCL first, then PCH, and combines
them into the 16-bit PC.  After RTI, execution resumes at the instruction that was interrupted.

---

## Reset

`Reset()` clears both pending flags in addition to initialising registers and loading PC from
the reset vector:

```csharp
nmiPending = false;
irqPending = false;
```

---

## Testing

`InnoWerks.Simulators.Tests/InterruptTests.cs` covers:

| Test | What it exercises |
|------|-------------------|
| `NmiDispatchesToNmiVector` | PC, SP, I flag after NMI dispatch |
| `IrqDispatchesToIrqVector` | PC, SP, I flag after IRQ dispatch |
| `NmiStackFrameIsCorrect` | Exact bytes on stack: PCH, PCL, PS with B=0 |
| `IrqStackFrameIsCorrect` | Same for IRQ |
| `IrqMaskedWhenInterruptFlagSet` | IRQ suppressed when I=1 |
| `IrqRemainsDeferredThenFiresAfterCli` | IRQ deferred through masked steps, fires after CLI |
| `NmiFiresEvenWhenInterruptFlagSet` | NMI ignores I flag |
| `NmiTakesPriorityOverIrq` | NMI wins when both pending |
| `RtiRestoresPcAndPsAfterNmi` | RTI unwinds PC and PS exactly |
| `RtiRestoresPcAndPsAfterIrq` | Same for IRQ |
| `NmiClearsDecimalFlagOn65C02` | 65C02-specific D flag clearing |
| `IrqClearsDecimalFlagOn65C02` | 65C02-specific D flag clearing |
| `NmiDoesNotClearDecimalFlagOnNmos6502` | NMOS 6502 leaves D unchanged |

### Test Technique — BRK Sentinel

Most dispatch tests place a `$00` (BRK) byte at the ISR address and call
`cpu.Step(returnPriorToBreak: true)`.  `Step()` peeks at the next opcode after interrupt
setup; if it is `$00` and `returnPriorToBreak` is true it returns immediately.  This
freezes execution at the ISR entry point so register and stack state can be inspected without
executing any ISR code.

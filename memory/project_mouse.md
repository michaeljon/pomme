---
name: Mouse Interface Card Implementation
description: Status and design decisions for the Apple Mouse Interface Card emulation
type: project
---

Apple Mouse Interface Card emulation is complete and working with A2DeskTop.

**Why:** User wanted mouse support for A2DeskTop running on the emulated Apple IIe.

**How to apply:** When revisiting mouse code, refer to Documentation/MOUSE.md for the authoritative design. Key implementation facts below.

## Architecture: Two-Level Dispatch

The ROM uses a vector table (offsets $12–$1A) pointing to intercept handler offsets ($92–$9A, computed as InterceptBase=$80 + vector offset). Applications read the vector to find the handler address, then JSR there. CPU intercepts fire at the handler addresses.

## Intercept Handler Convention

Handlers are `Func<ICpu, IBus, bool>`. Returning `true` causes the CPU to automatically perform RTS (pop return address, resume caller). Returning `false` falls through. This is the same model used by ProDOS block device. No handler should call `((Cpu6502Core)cpu).RTS(0, 0)` — that was the old model.

## Correct Screen Holes (per Apple Mouse User's Manual)

For slot n:
- $0478+n = MOUX1 (X lo)
- $04F8+n = MOUY1 (Y lo)
- $0578+n = MOUX2 (X hi)
- $05F8+n = MOUY2 (Y hi)
- $0778+n = MOUSTAT
- $07F8+n = MOUMODE

CLAMPMOUSE reads from FIXED addresses $0478/$0578 (min) and $04F8/$05F8 (max), not slot-offset.

## Key Lessons from Debugging

- A2DeskTop reads the vector table entry at $Cn12–$Cn1A to find firmware; it does NOT call $Cn05, $Cn0B etc. directly (those old offsets were wrong)
- Screen hole addresses for STAT/MODE are at $0778/$07F8, NOT $0678/$06F8 (the old values were wrong)
- The ROM must have the vector table populated correctly or A2DeskTop silently calls whatever random byte is there
- CardCat will find the card correctly even if screen holes or vectors are wrong — it only checks ID bytes

## Slot Assignment

- Mouse: slot 4 (--mouse CLI flag)
- ProDOS HD: slot 5 (--harddisk1/2/3/4)
- Disk II: slot 6 (--disk1/2)

## Remaining Limitations

- No IRQ/VBL interrupt delivery (MOUMODE bits 1–3 stored but never trigger 6502 IRQ)
- POSMOUSE is a no-op (can't warp host mouse)
- Single button only (left mouse button)
- No relative movement mode

## Next Planned Component

User mentioned joystick support for a Bluetooth Xbox controller acting as paddles.

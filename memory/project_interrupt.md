---
name: Interrupt Support (IRQ/NMI) — In Progress
description: External IRQ and NMI delivery being implemented as a prerequisite for interrupt-driven mouse support
type: project
---

As of 2026-03-19, external IRQ and NMI processing is being implemented. This is the next step toward interrupt-driven mouse support.

**Why:** The mouse card (MouseSlotDevice) tracks MOUMODE interrupt enable bits (move, button, VBL) but currently never delivers a 6502 IRQ. Interrupt-driven mouse applications will not work until the CPU can receive external interrupts.

**How to apply:** When working on mouse or slot device interrupt code, this is the prerequisite — check whether IRQ/NMI delivery is complete before assuming interrupt-driven paths work.

See `Documentation/MOUSE.md` Limitations section: "No IRQ/VBL interrupt delivery."

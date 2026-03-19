---
name: Use authoritative hardware documentation, not cross-references
description: When implementing Apple II hardware, rely on the original manual, not other emulators or secondary sources
type: feedback
---

Use the original Apple hardware documentation (e.g. Apple Mouse User's Manual, Apple IIe Technical Reference) as the authoritative source for register layouts, screen holes, entry point offsets, and calling conventions.

**Why:** During mouse card implementation, the screen hole addresses, firmware entry point offsets, and CLAMPMOUSE parameter convention were all wrong when derived by cross-referencing AppleWin source and TechNotes. The Apple Mouse User's Manual had the correct values. Significant debugging time was lost chasing the wrong addresses.

**How to apply:** If the user references a specific Apple manual for a hardware component, defer to that document rather than proposing values from other emulators or secondary sources. If uncertain, ask the user to verify against their copy of the manual before writing code.

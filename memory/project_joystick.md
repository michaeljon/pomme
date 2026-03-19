---
name: Joystick / Paddle Implementation
description: Xbox controller mapped to Apple II joystick via IOU paddle timing; current status and design decisions
type: project
---

Joystick support is implemented and working. An Xbox (XInput) controller is mapped to the Apple II joystick inputs via `IOU.UpdateJoystick()` called from `Emulator.UpdateGamepad()` each frame.

**Why:** Needed for testing games that require analog joystick input (e.g. Apple Cider Spider).

**Key design decisions:**
- Paddle timing uses the 558-timer model: PTRIG records `bus.CycleCount`, PADDLEx reads check elapsed cycles against `paddleValues[index] * 11`
- PTRIG (`$C070`–`$C07D`) must be handled as both read AND write — many games use `STA $C070` not `LDA $C070`. `$C07E`/`$C07F` are IOUDISON/IOUDISOFF, not PTRIG.
- Both read and write paths fall through their switch statements to a shared range-check `if` block rather than per-address `case` labels
- Y axes are negated by default (`JoystickInverted` config flag suppresses this)
- Joystick buttons A/B are OR'd with keyboard Open/Solid Apple in `UpdateKeyboard()` so either input source works

**How to apply:** If touching paddle/PTRIG code, remember the read/write symmetry and that $C07E/$C07F are excluded from the PTRIG range.

See `Documentation/JOYSTICK.md` for full design details.

# Apple Mouse Interface Card — Implementation Design

## Overview

The Apple Mouse Interface Card (Apple part number A2B0047) is emulated as a virtual slot
card using the same ROM + CPU intercept pattern established by the ProDOS block device.  No
real 6502 ROM binary is needed: a minimal ROM image provides the hardware identification
bytes and a two-level firmware dispatch table, and all firmware behaviour is implemented in
C# intercept handlers.

The card is instantiated in **slot 4** (the traditional Apple IIe mouse slot) when the
emulator is launched with `--mouse`.

---

## Source Files

| File | Purpose |
|------|---------|
| `InnoWerks.Computers.Apple/Devices/MouseSlotDevice.cs` | Card device, firmware intercepts, state |
| `InnoWerks.Emulators.AppleIIe/CliOptions.cs` | `--mouse` CLI flag |
| `InnoWerks.Emulators.AppleIIe/Emulator.cs` | Instantiation, host input polling, capture UI |

---

## ROM Layout

The slot ROM occupies `$C400–$C4FF` for slot 4 (generally `$Cn00–$CnFF` for slot n).  The
256-byte image is filled with `$00` (BRK) as a safe fallback, then the identification
bytes, Pascal signature bytes, and firmware dispatch vectors are written at their required
offsets.

### Identification Bytes

These bytes are scanned by ProDOS and applications such as A2DeskTop to locate a mouse card.

| Offset | Value | Meaning |
|--------|-------|---------|
| `$05`  | `$38` | Required ID byte |
| `$07`  | `$18` | Required ID byte |
| `$08`  | `$01` | Pascal signature byte |
| `$0B`  | `$01` | Required ID byte |
| `$0C`  | `$20` | Device type = mouse |
| `$11`  | `$00` | Pascal signature byte |
| `$FB`  | `$D6` | Required ID byte (Mouse TechNote #5) |

### Firmware Dispatch Vector Table

The vector table occupies offsets `$12–$1A`.  Each entry holds a single byte — the offset
within the slot ROM page of the corresponding C# intercept handler.  Applications read the
appropriate vector entry to find the handler address, then JSR to `$Cn00 + vector_byte`.

| Offset | Routine    | Vector value | Handler offset | Handler address (slot 4) |
|--------|------------|--------------|----------------|--------------------------|
| `$12`  | SETMOUSE   | `$92`        | `$92`          | `$C492` |
| `$13`  | SERVEMOUSE | `$93`        | `$93`          | `$C493` |
| `$14`  | READMOUSE  | `$94`        | `$94`          | `$C494` |
| `$15`  | CLEARMOUSE | `$95`        | `$95`          | `$C495` |
| `$16`  | POSMOUSE   | `$96`        | `$96`          | `$C496` |
| `$17`  | CLAMPMOUSE | `$97`        | `$97`          | `$C497` |
| `$18`  | HOMEMOUSE  | `$98`        | `$98`          | `$C498` |
| `$19`  | INITMOUSE  | `$99`        | `$99`          | `$C499` |
| `$1A`  | GETCLAMP   | `$9A`        | `$9A`          | `$C49A` |

Each handler offset is computed as `InterceptBase ($80) + vector_offset`, guaranteeing
uniqueness with no manual assignment required.

---

## Firmware Dispatch Architecture

Calling a mouse firmware routine involves two indirections:

1. **Vector lookup** — the caller reads the vector byte at `$Cn12`–`$Cn1A` (e.g. `$Cn14`
   for READMOUSE) to obtain the handler offset within the slot ROM page.
2. **Handler call** — the caller JSRs to `$Cn00 + handler_offset` (e.g. `$C494`).
3. **CPU intercept** — a `Func<I6502Cpu, IBus, bool>` handler is registered at that address.
   When the CPU's PC reaches it, the handler fires before any ROM byte is executed.
   Returning `true` causes the CPU to perform an automatic RTS (pop the return address and
   resume the caller); returning `false` falls through to whatever ROM byte follows.

All current handlers return `true`.  This model is also used by the ProDOS block device.

---

## Screen Hole Variable Layout

All state communicated between the firmware and the calling application lives in main
memory pages 4–7.  For slot n, the addresses are:

| Address       | Name    | Contents |
|---------------|---------|----------|
| `$0478 + n`   | MOUX1   | X position, low byte |
| `$04F8 + n`   | MOUY1   | Y position, low byte |
| `$0578 + n`   | MOUX2   | X position, high byte |
| `$05F8 + n`   | MOUY2   | Y position, high byte |
| `$0778 + n`   | MOUSTAT | Status byte (see below) |
| `$07F8 + n`   | MOUMODE | Mode byte (see below) |

These pages overlap with text screen pages 1 and 2 (`$0400–$07FF`).  Mouse-driven
applications (e.g. A2DeskTop) typically run in hi-res graphics mode, so there is no
conflict in practice.

> **Note:** CLAMPMOUSE reads its min/max parameters from **fixed** addresses `$0478`,
> `$0578`, `$04F8`, `$05F8` (the slot-0 screen hole bases), not from slot-offset addresses.
> This matches the Apple Mouse User's Manual calling convention.

### MOUSTAT Bit Definitions

Source: Apple Mouse User's Manual.

| Bit | Mask   | Meaning |
|-----|--------|---------|
| 0   | `$01`  | Movement interrupt pending |
| 1   | `$02`  | Button-press interrupt pending |
| 2   | `$04`  | Screen-refresh interrupt pending |
| 4   | `$10`  | (reserved) |
| 5   | `$20`  | Mouse moved since last READMOUSE |
| 6   | `$40`  | Button 0 was down on prior READMOUSE call |
| 7   | `$80`  | Button 0 is currently down |

Bits 0–2 are interrupt-pending flags; they are tracked but not yet wired to IRQ delivery.
Polling software (A2DeskTop) uses bit 5 for movement detection and bits 6–7 for button
state.

### MOUMODE Bit Definitions

Source: Apple Mouse User's Manual.

| Bit | Mask   | Meaning |
|-----|--------|---------|
| 0   | `$01`  | Mouse enabled (1 = enabled) |
| 1   | `$02`  | Interrupt on mouse movement |
| 2   | `$04`  | Interrupt on button change |
| 3   | `$08`  | Interrupt on VBL |
| 4–7 | —      | (reserved) |

The full mode byte is written by SETMOUSE and reflected in the screen hole by every
READMOUSE call.  `UpdateFromHost` only tracks host mouse position when both the mouse is
captured **and** MOUMODE bit 0 (Enabled) is set.

---

## Firmware Routine Descriptions

### INITMOUSE (vector `$19`, handler `$C499` for slot 4)
Resets all internal state to power-on defaults: position (0, 0), mode `$00` (disabled),
clamp region [0, 1023] × [0, 1023].  Writes screen holes.  Returns carry clear.

### SETMOUSE (vector `$12`, handler `$C492` for slot 4)
Stores the A register as the new mode byte.  Returns carry clear on success.  Returns
carry set (without updating mode) if A > `$0F` (invalid mode value).

### READMOUSE (vector `$14`, handler `$C494` for slot 4)
Writes the current X position, Y position, status byte, and mode byte to the screen holes.
Clears the movement flag (MOUSTAT bit 5) and advances `buttonPreviouslyDown` to the current
button state so that the next call correctly reflects delta.  Returns carry clear.

This is the routine called most frequently by applications (typically once per VBL).

### CLEARMOUSE (vector `$15`, handler `$C495` for slot 4)
Sets position to (0, 0), clears button state, and clears the movement flag.  Writes screen
holes.  Does not change the clamp region or mode.  Returns carry clear.

### HOMEMOUSE (vector `$18`, handler `$C498` for slot 4)
Sets position to (0, 0).  Clears the movement flag.  Returns carry clear.

### POSMOUSE (vector `$16`, handler `$C496` for slot 4)
No-op in this implementation (host mouse position cannot be repositioned programmatically).
Returns carry clear.

### CLAMPMOUSE (vector `$17`, handler `$C497` for slot 4)
Sets the minimum and maximum coordinate for one axis.  The A register selects the axis
(0 = X, 1 = Y).  Parameters are read from fixed screen hole addresses (not slot-offset):

| Parameter | Lo byte   | Hi byte   |
|-----------|-----------|-----------|
| Minimum   | `$0478`   | `$0578`   |
| Maximum   | `$04F8`   | `$05F8`   |

Values are treated as signed 16-bit integers; values ≥ 32768 are sign-extended to negative.
Returns carry clear.

### GETCLAMP (vector `$1A`, handler `$C49A` for slot 4)
Returns one clamp value byte in the screen hole at `$0578`.  The byte at `$0478` selects
which value to return.  Returns carry clear.

### SERVEMOUSE (vector `$13`, handler `$C493` for slot 4)
Writes interrupt-cause flags to the status screen hole: bit 0 = mouse moved, bit 1 = button
changed.  Intended for use inside an IRQ handler.  Does not check the X/Y calling
convention registers.  Returns carry clear.

---

## Coordinate Mapping

The Apple Mouse Interface Card coordinate space is 10-bit (0–1023 on each axis by
default).  Applications call CLAMPMOUSE to restrict the usable range to their screen
dimensions (A2DeskTop typically sets X to [0, 279] and Y to [0, 191] or similar).

Each emulator frame, `MouseSlotDevice.UpdateFromHost` maps the host mouse pixel position
to the current clamp range:

```
appleX = clampMinX + (hostX - displayLeft) * clampWidth  / displayWidth
appleY = clampMinY + (hostY - displayTop)  * clampHeight / displayHeight
```

`displayLeft/Top/Width/Height` are taken from `hostLayout.AppleDisplay` — the rectangle
within the MonoGame window that contains the rendered Apple II screen.  The result is
clamped to `[clampMin, clampMax]` on each axis.

This calculation lives in `Emulator.cs`, which passes the unwrapped integers to
`UpdateFromHost`.  The `InnoWerks.Computers.Apple` library has no MonoGame dependency.

---

## Mouse Capture

The host cursor must be captured before host mouse events are forwarded to the emulated
card.  Two actions capture the mouse:

| Action | Effect |
|--------|--------|
| Left-click inside the Apple display area | Captures mouse, hides cursor |
| **F12** | Toggles capture on/off |

When captured, `IsMouseVisible = false` is set on the MonoGame `Game` instance and
`mouseDevice.Capture()` is called.  Releasing (F12 or any future release path) calls
`mouseDevice.Release()` and restores cursor visibility.

F12 was chosen as the toggle key because Escape is a valid Apple II application key
(notably used within ProDOS applications).

When the mouse is not captured, or when MOUMODE bit 0 is clear (mouse disabled by
software), `UpdateFromHost` is a no-op and the emulated position is not updated.

---

## Limitations and Known Issues

1. **No IRQ/VBL interrupt delivery.** MOUMODE bits 1–3 enable interrupt-on-move,
   interrupt-on-button, and interrupt-on-VBL.  The flags are stored but never cause a 6502
   IRQ.  Applications that rely on interrupt-driven mouse input (rather than polling
   READMOUSE) will not work correctly until interrupt delivery is implemented.

2. **POSMOUSE not implemented.** The routine returns carry clear but does not reposition
   the emulated cursor, because the host mouse cannot be warped programmatically.

3. **Single button only.** The original card supported one button.  This implementation
   maps the left mouse button only.

4. **No relative-movement mode.** Some applications prefer relative (delta) coordinates.
   Only absolute positioning within the clamp region is currently implemented.

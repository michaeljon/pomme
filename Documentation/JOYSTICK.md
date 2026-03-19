# Apple II Joystick / Paddle Interface — Implementation Design

## Overview

The Apple II joystick interface is built into the IOU (Input/Output Unit) on the main board
— it is not a slot card.  Up to two joysticks (four analog paddles and two fire buttons each)
are supported.  Analog position is communicated to software via a timing mechanism driven by
a 558 dual timer IC: the software fires a trigger, then measures how long each paddle signal
takes to fall, with the duration proportional to the joystick axis position.

This emulator maps a connected host Xbox (XInput-compatible) controller to the Apple II
joystick inputs.  The left thumbstick drives PDL(0)/PDL(1), the right thumbstick drives
PDL(2)/PDL(3), and the A/B buttons map to PB0/PB1.

---

## Source Files

| File | Purpose |
|------|---------|
| `InnoWerks.Computers.Apple/SystemDevices/IOU.cs` | Paddle timing, soft switch read/write, `UpdateJoystick()` |
| `InnoWerks.Emulators.AppleIIe/Emulator.cs` | Host gamepad polling, axis scaling, `UpdateGamepad()` |

---

## Hardware Background

### The 558 Timer Mechanism

The Apple II does not expose an analog-to-digital converter directly.  Instead, each paddle
input is wired to one channel of a 558 dual timer through an RC network.  The capacitor in
each RC pair charges at a rate determined by the paddle's variable resistance (0–150 kΩ).
Software interacts with the paddles as follows:

1. **Trigger** — a read or write to any address in `$C070`–`$C07D` fires PTRIG (Paddle
   TRIGger), simultaneously resetting and starting all four timer channels.
2. **Poll** — software reads `$C064`–`$C067` (PADDLE0–PADDLE3) in a tight loop.  Bit 7 of
   each byte is `1` while the corresponding timer is still running, `0` once it has expired.
3. **Measure** — the number of iterations (or clock cycles) that elapsed between PTRIG and
   the falling edge is proportional to the paddle position.  A value of 0 falls almost
   immediately; a value of 255 holds bit 7 high for approximately 2800 µs.

The Apple II Technical Reference Manual specifies approximately 11 µs per unit of paddle
value, giving a maximum timer duration of ~2816 µs (256 × 11 µs).

### PTRIG Address Range

Any access — read **or** write — to addresses `$C070`–`$C07D` triggers the paddle timer.
Addresses `$C07E` and `$C07F` are reserved for `IOUDISON` and `IOUDISOFF` and do **not**
trigger PTRIG.  Many games use a `STA $C070` (write) rather than `LDA $C070` (read), so
the trigger must be registered in both `HandlesRead` and `HandlesWrite`.

### Buttons

The Apple II exposes three pushbutton inputs:

| Button | Soft switch address | Alias |
|--------|---------------------|-------|
| PB0    | `$C061`             | Open Apple key |
| PB1    | `$C062`             | Solid Apple key |
| PB2    | `$C063`             | Shift key |

PB0 and PB1 are shared with the Open Apple and Solid Apple modifier keys respectively.
Either the keyboard key or the joystick button being held will assert the corresponding bit.

---

## Soft Switch Address Map

| Address | Name    | Direction | Effect |
|---------|---------|-----------|--------|
| `$C061` | OPENAPPLE / PB0 | Read | Bit 7 = 1 if Open Apple or joystick button 0 is held |
| `$C062` | SOLIDAPPLE / PB1 | Read | Bit 7 = 1 if Solid Apple or joystick button 1 is held |
| `$C063` | SHIFT / PB2 | Read | Bit 7 = 1 if Shift is held |
| `$C064` | PADDLE0 | Read | Bit 7 = 1 while PDL(0) timer running |
| `$C065` | PADDLE1 | Read | Bit 7 = 1 while PDL(1) timer running |
| `$C066` | PADDLE2 | Read | Bit 7 = 1 while PDL(2) timer running |
| `$C067` | PADDLE3 | Read | Bit 7 = 1 while PDL(3) timer running |
| `$C070`–`$C07D` | PTRIG | Read/Write | Resets and starts all four paddle timers |

---

## Emulation Model

### Timer State

`IOU` maintains two fields for paddle timing:

```
private readonly int[] paddleValues = new int[4];   // 0–255, set each frame from host
private long paddleTimerStartCycle = -1;            // bus cycle when PTRIG last fired
```

`paddleTimerStartCycle` is initialised to `-1`.  A read of any PADDLE address before PTRIG
has ever been triggered returns `0x00` (timer not running).

### PTRIG

Any read or write to `$C070`–`$C07D` sets:

```
paddleTimerStartCycle = (long)bus.CycleCount;
```

Both the read and write paths in `IOU` fall through their switch statements to a shared
range check rather than using individual `case` labels, since the address range is
contiguous and both directions have identical behaviour.

### PADDLE Read

```
elapsed   = (long)bus.CycleCount − paddleTimerStartCycle
threshold = paddleValues[index] × CyclesPerPaddleUnit   // CyclesPerPaddleUnit = 11
return elapsed < threshold ? 0x80 : 0x00
```

The 11-cycle-per-unit constant approximates the real hardware's ~11 µs per unit at the
Apple IIe's 1.02 MHz clock.

---

## Host Controller Integration

### Polling

Each MonoGame `Update()` frame, `Emulator.UpdateGamepad()` calls
`GamePad.GetState(PlayerIndex.One)`.  If no controller is connected the method returns
immediately and the paddle values remain at their previous state.

### Axis Scaling

MonoGame reports thumbstick axes in the range `[−1.0, +1.0]`.  These are mapped to the
Apple II paddle range `[0, 255]`:

```
pdl = (int)((axis + 1.0f) / 2.0f × 255)
```

The Y axes are negated before scaling because MonoGame's thumbstick convention is
+Y = up, while the Apple II paddle convention treats higher values as further down
(consistent with screen coordinate direction):

```
pdl1 = (int)((-leftY  + 1.0f) / 2.0f × 255)
pdl3 = (int)((-rightY + 1.0f) / 2.0f × 255)
```

This behaviour can be overridden per-launch with the `JoystickInverted` configuration
flag, which suppresses the negation for software that uses the opposite convention.

### Button Mapping

| Xbox button | Apple II input | Soft switch |
|-------------|----------------|-------------|
| A           | PB0 / Button 0 | `$C061`     |
| B           | PB1 / Button 1 | `$C062`     |

Button state is stored in `joystickButton0` / `joystickButton1` fields in `Emulator`.
When `UpdateKeyboard()` sets the Open Apple and Solid Apple lines it ORs these fields with
the physical key state, so either input source asserts the button:

```csharp
iou.OpenApple(currentState.IsKeyDown(Keys.LeftAlt)  || joystickButton0);
iou.SolidApple(currentState.IsKeyDown(Keys.RightAlt) || joystickButton1);
```

### Controller Mapping Summary

| Xbox input        | PDL / button | Soft switch |
|-------------------|--------------|-------------|
| Left stick X      | PDL(0)       | `$C064`     |
| Left stick Y      | PDL(1)       | `$C065`     |
| Right stick X     | PDL(2)       | `$C066`     |
| Right stick Y     | PDL(3)       | `$C067`     |
| A button          | PB0          | `$C061`     |
| B button          | PB1          | `$C062`     |

---

## Configuration

`EmulatorConfiguration` exposes one joystick-related property:

| Property | Type | Effect |
|----------|------|--------|
| `JoystickInverted` | `bool` | When `true`, Y axes are **not** negated (non-standard convention) |

---

## Limitations and Known Issues

1. **Single controller only.** Only `PlayerIndex.One` is polled.  A second physical
   controller driving PDL(2)/PDL(3) independently is not supported.

2. **No keyboard-driven paddle emulation.** There is no fallback for systems without a
   connected gamepad.  Software that requires analog input will see the thumbstick at its
   resting centre position (PDL ≈ 127) until a controller is connected.

3. **Fixed timing constant.** `CyclesPerPaddleUnit = 11` is an integer approximation of
   the real 11.25-cycle value.  Games with very tight polling loops may see minor
   positional inaccuracy at the high end of the paddle range.

4. **No dead-zone processing.** Physical thumbsticks have mechanical dead zones near
   centre.  No dead-zone filtering is applied, so software may see small non-zero values
   when the stick is at rest.

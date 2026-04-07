# No-Slot-Clock (DS1215)

## Overview

The Dallas Semiconductor DS1215 No-Slot-Clock (NSC) sits between a ROM chip and its socket, monitoring address lines for a specific 64-bit unlock sequence. Once unlocked, subsequent reads return BCD-encoded clock data instead of ROM data.

## Configuration

Add `"noSlotClock": true` to the emulator JSON configuration. The NSC does not occupy a slot.

## Address Range

The NSC monitors `$C100-$CFFF` but only intercepts when the internal ROM is being served:

- `IntCxRomEnabled` is true (all `$C100-$CFFF` is internal ROM), OR
- Address is `$C300-$C3FF` AND `SlotC3RomEnabled` is false, OR
- Address is `$C800-$CFFF` AND `IntC8RomEnabled` is true

## Unlock Protocol

The unlock sequence is 64 reads where bit 0 of the address matches the comparison pattern `$5CA33AC55CA33AC5`, LSB first:

1. Read 64 addresses with bit 0 matching the pattern — this unlocks the device
2. Read 64 more addresses — each returns one bit of clock data in bit 0
3. After 64 bits read, the device locks again

If any bit in the sequence mismatches, the progress resets to bit 0.

Writes to the address range also feed the comparison sequence via address bit 0.

## Clock Data Format

8 bytes, BCD-encoded, LSB of each byte first:

| Byte | Field | Range |
|---|---|---|
| 0 | Hundredths of seconds | 00-99 |
| 1 | Seconds | 00-59 |
| 2 | Minutes | 00-59 |
| 3 | Hours (24h) | 00-23 |
| 4 | Day of week | 01-07 (1=Sunday) |
| 5 | Day of month | 01-31 |
| 6 | Month | 01-12 |
| 7 | Year | 00-99 |

## Implementation

`NoSlotClockDevice` implements `IAddressInterceptDevice` at `AddressIntercept` priority (highest). It registers at `$C100-$CFFF` and checks soft switch state via `ShouldIntercept()` before processing. The clock data is loaded from `DateTime.Now` when the device is unlocked.

## ProDOS Integration

ProDOS automatically detects and uses the NSC for its clock driver. The time appears in applications like A2Desktop's menu bar.

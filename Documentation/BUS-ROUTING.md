# Bus Routing Architecture

## Overview

The Apple IIe bus routes CPU memory accesses through a priority-ordered dispatch system. Every read and write flows through `AppleBus`, which uses two 64K dispatch tables for O(1) device lookup.

## Dispatch Tables

```
readDispatch[65536]  — List<IAddressInterceptDevice> per address
writeDispatch[65536] — List<IAddressInterceptDevice> per address
```

When a device registers via `AddDevice(IAddressInterceptDevice)`, its `AddressRanges` are enumerated. For each address the device covers, it is inserted into the corresponding dispatch table list, sorted by `InterceptPriority` (lower value = higher priority).

At runtime, a bus access is:
```
1. Tick (increment cycle counter, tick all devices)
2. Look up dispatch table entry for address
3. Iterate the list, calling DoRead/DoWrite on each device
4. If any device returns true, use its value
5. If no device handles it, fall through to Memory128k
```

## Priority Order

| Priority | Value | Device | Purpose |
|---|---|---|---|
| AddressIntercept | 0 | NoSlotClockDevice | Observe/intercept before anything else |
| IntC8 | 1 | IntC8Handler | Expansion ROM state management |
| SoftSwitch | 2 | IOU, MMU, KeylatchHandler | Soft switch handling |
| SlotDevice | 3 | SlotHandler | Slot I/O and ROM routing |
| Default | 4 | DefaultSoftSwitchHandler | Catch-all for $C000-$C08F |

## Slot Device Routing

Slot devices are NOT in the dispatch tables. They are routed by `SlotHandler`, which IS in the dispatch tables at `SlotDevice` priority. `SlotHandler` handles three address ranges:

### $C090-$C0FF (Slot I/O)

Slot number extracted from address: `slot = (address >> 4) & 7`. The device's `HandlesRead`/`HandlesWrite` is checked, then `Read`/`Write` is called.

### $Cn00-$CnFF (Slot ROM)

Slot number: `slot = (address >> 8) & 7`. On every access:
1. `CurrentSlot` is updated if changed
2. `Memory.Remap()` is called if `CurrentSlot` changed
3. If `IntCxRomEnabled` is false AND the device handles the address, the device is called
4. Otherwise falls through to `Memory128k` (internal ROM)

### $C800-$CFFF (Expansion ROM)

Only routed to a device if ALL of:
- `IntCxRomEnabled` is false
- `IntC8RomEnabled` is false
- `CurrentSlot` matches the device's slot
- The device handles the address

Otherwise falls through to `Memory128k`.

## CurrentSlot and Remap

`CurrentSlot` tracks which slot's expansion ROM should be visible at `$C800-$CFFF`. It is set whenever `$Cn00-$CnFF` is accessed (by any slot, including empty ones). Changing `CurrentSlot` triggers `Memory128k.Remap()` which updates the memory map to point `$C800-$CFFF` at the correct slot's expansion ROM.

## Keyboard Strobe

`CheckClearKeystrobe` runs on EVERY bus Read and Write for the `$C010-$C01F` range. This is a bus-level side effect that cannot be handled by a device alone — multiple devices (IOU, MMU) register at overlapping addresses in this range, and the strobe clearing must happen regardless of which device handles the access.

## AddressRange

The `AddressRange` class supports three modes:

```csharp
// Contiguous range
new AddressRange(0xC000, 0xC08F, MemoryAccessType.Any)

// Discrete set of addresses
new AddressRange(new HashSet<ushort> { 0xC000, 0xC010 }, MemoryAccessType.Read)

// Single address
new AddressRange(0xCFFF, MemoryAccessType.Any)
```

`MemoryAccessType` is a flags enum: `Read (1)`, `Write (2)`, `Any (3)`.

## Device Registration Flow

```
Computer constructor:
  1. Create Memory128k, AppleBus, CPU
  2. Create and register IOU, MMU, IntC8Handler, KeylatchHandler, SlotHandler
     → each calls bus.AddDevice(IAddressInterceptDevice)
     → populates dispatch tables

Computer.AddDiskIIController(slot):
  1. Create DiskIISlotDevice
  2. Store in SlotDevices[slot]
  3. Load ROM into Memory128k

Computer.Build():
  1. Fill empty slots with EmptySlotDevice
  2. Load all slot ROMs into Memory128k
```

# Apple IIe Emulator — Design Document

## Project Overview

This is a complete Apple IIe emulator written in C#, built on MonoGame for rendering and audio. The project targets the enhanced Apple IIe with a WDC 65C02 CPU, 128KB RAM, and standard peripheral cards.

---

## Solution Structure

| Project | Purpose |
|---|---|
| `InnoWerks.Processors` | Opcode and instruction set definitions |
| `InnoWerks.Simulators.Sim6502` | 6502/65C02 CPU emulation core |
| `InnoWerks.Computers.Apple` | Apple II hardware (Computer, memory, bus, devices) |
| `InnoWerks.Emulators.AppleIIe` | Main emulator app (MonoGame UI, renderers, audio) |
| `InnoWerks.Assemblers.Asm6502` | 6502 assembler (used to build device ROMs at runtime) |
| `InnoWerks.Disassemblers.Dasm6502` | 6502 disassembler (debug tooling) |
| Test projects | Unit tests and Harte 65x02 test suite integration |

The `ConsoleTools` projects are developer utilities not part of the emulator itself.

---

## Architecture Overview

```
┌──────────────────── Emulator (MonoGame Game) ────────────────────┐
│                                                                    │
│  ┌──────────┐   ┌──────────┐   ┌────────────┐   ┌────────────┐ │
│  │ Toolbar  │   │ Display  │   │  Audio     │   │  Input     │ │
│  │ (disks,  │   │ (text,   │   │  Renderer  │   │  (kbd,     │ │
│  │  reset)  │   │  hires)  │   │  (speaker  │   │   mouse,   │ │
│  │          │   │          │   │  + PSG)    │   │   paddle)  │ │
│  └────┬─────┘   └────┬─────┘   └─────┬──────┘   └─────┬──────┘ │
│       └───────────────┴───────────────┴─────────────────┘        │
│                               │                                    │
│                      ┌────────▼────────┐                          │
│                      │    Computer     │                          │
│                      │  (composition   │                          │
│                      │    root)        │                          │
│                      └────────┬────────┘                          │
└───────────────────────────────┼────────────────────────────────────┘
                                │
     ┌──────────────────────────┼──────────────────────────────┐
     │                          │                                │
     │               ┌─────────▼──────────┐                    │
     │               │     AppleBus       │                    │
     │               │  (64K dispatch     │                    │
     │               │   tables)          │                    │
     │               └─────────┬──────────┘                    │
     │    ┌────────────┬───────┼───────┬────────────┐         │
     │    │            │       │       │            │         │
     │  IOU          MMU   SlotHandler  IntC8     Memory     │
     │ (kbd,       (LC,    (slot I/O,   Handler   128k       │
     │  video,     ROM,    Cx/C8 ROM)            (main +    │
     │  paddles)   aux)                           aux)       │
     │                                                         │
     │  ┌──────────────── Slot Devices ──────────────────┐   │
     │  │ [1] ThunderClock    [4] Mockingboard            │   │
     │  │ [2] Mouse           [5] ProDOS                  │   │
     │  │ [6] Disk II         [7] ProDOS                  │   │
     │  │ NSC (intercept)                                  │   │
     │  └──────────────────────────────────────────────────┘   │
     └─────────────────────────────────────────────────────────┘
```

---

## Computer Class

**File:** `InnoWerks.Computers.Apple/Computer.cs`

`Computer` is the composition root for all Apple IIe hardware. It owns the CPU, bus, memory, machine state, and all devices. The emulator interacts with the hardware exclusively through `Computer`.

**Responsibilities:**
- Constructs and wires CPU, bus, memory, IOU, MMU, IntC8Handler, KeylatchHandler, SlotHandler
- Factory methods for adding devices: `AddDiskIIController()`, `AddMockingboard()`, `AddMouse()`, `AddThunderclock()`, `AddGenericBlockDevice()`, `AddNoSlotClock()`
- `Build()` fills unoccupied slots (1-7) with `EmptySlotDevice`
- `Reset()` cascades to all soft switches, intercept devices, slot devices, bus, memory, and CPU
- Exposes `CycleCount`, `Processor`, `Bus`, `Memory`, `MachineState`, `SlotDevices`

**Timing constants:** `CyclesPerSecond` (1,020,484), `FramesPerSecond` (59.94), `FrameCycles` (17,030), `VblStart` (12,480)

---

## CPU Subsystem

**Location:** `InnoWerks.Simulators.Sim6502/`

The CPU is modeled in two layers:

- **`Cpu6502Core`** (abstract) — registers, interrupt handling, stack operations, reset vectors, memory access via `IBus`
- **`Cpu6502`** (concrete) — NMOS 6502 with all 256 opcodes including undocumented instructions
- **`Cpu65C02`** (concrete) — WDC 65C02 instruction dispatch, cycle-accurate execution, decimal mode extra cycle
- **`Cpu65SC02`** — Synertek 65SC02 variant (no BBR/BBS/RMB/SMB)
- **`CpuR65C02`** — Rockwell R65C02 variant

### NMOS 6502 Undocumented Opcodes

All 256 opcode slots are implemented in `Cpu6502.cs`:

| Category | Mnemonics | Count |
|---|---|---|
| RMW combos | SLO, RLA, SRE, RRA, DCP, ISC | 42 |
| Store/load combos | SAX, LAX | 10 |
| Immediate ALU | ANC, ALR, ARR, AXS, USBC | 6 |
| Unstable | ANE, LXA, SHA, SHX, SHY, TAS, LAS | 8 |
| Multi-byte NOPs | DOP | 27 |
| Processor halt | KIL | 12 |

The magic constant for ANE/LXA is `0xEE` (matches Harte test expectations).

### 65C02 Unknown Opcode Dispatch

Unimplemented opcodes across 65C02 variants are handled as NOPs with cycle-accurate bus timing:

- **WDC/Rockwell:** `$5C` is a special 8-cycle NOP. `Bytes == 1` guard catches 1-byte NOPs (x3/xB). All others dispatch by addressing mode. No BBR/BBS/RMB/SMB in the Unknown path (implemented natively).
- **Synertek:** Same as above, plus Relative mode with `b & 4` check for BBS extra cycle. B=5 (SMB slots) use ZeroPageXIndexed addressing from the C=1 pattern.

---

## Bus Architecture

**File:** `InnoWerks.Computers.Apple/AppleBus.cs`

`AppleBus` implements `IBus` and is the single routing point for all CPU memory transactions.

### 64K Dispatch Tables

Two arrays of `List<IAddressInterceptDevice>` — one for reads, one for writes — provide O(1) lookup by address. When a device registers via `AddDevice(IAddressInterceptDevice)`, its address ranges are enumerated and the device is inserted into each relevant list, sorted by `InterceptPriority`.

```csharp
public byte Read(ushort address)
{
    Tick();
    CheckClearKeystrobe(address);

    var readers = readDispatch[address];
    if (readers != null)
        foreach (var device in readers)
            if (device.DoRead(address, out var value))
                return value;

    return memoryBlocks.Read(address);
}
```

### Slot Device Routing

Slot devices (`$C090-$C0FF`, `$Cn00-$CnFF`, `$C800-$CFFF`) are routed through `SlotHandler`, which:
1. Extracts the slot number from the address
2. Checks soft switches (`IntCxRomEnabled`, `IntC8RomEnabled`)
3. Sets `CurrentSlot` and triggers `Remap()` when the selected slot changes
4. Delegates to the concrete `ISlotDevice` if soft switch state allows

---

## Device Interface Hierarchy

### IAddressInterceptDevice

The unified interface for devices that monitor bus addresses:

```csharp
public interface IAddressInterceptDevice
{
    string Name { get; }
    InterceptPriority InterceptPriority { get; }
    bool DoRead(ushort address, out byte value);
    bool DoWrite(ushort address, byte value);
    IReadOnlyList<AddressRange> AddressRanges { get; }
    void Tick();
    void Reset();
}
```

`DoRead`/`DoWrite` return `true` if the device handled the access, `false` to let the bus continue to the next device or fall through to memory.

### InterceptPriority

Devices are sorted by priority within each dispatch table entry:

| Priority | Value | Examples |
|---|---|---|
| AddressIntercept | 0 | NoSlotClockDevice |
| IntC8 | 1 | IntC8Handler |
| SoftSwitch | 2 | IOU, MMU, KeylatchHandler |
| SlotDevice | 3 | SlotHandler |
| Default | 4 | DefaultSoftSwitchHandler |

### AddressRange

Supports three construction modes:
- **Contiguous:** `new AddressRange(0xC000, 0xC08F, MemoryAccessType.Any)`
- **Discrete:** `new AddressRange(new HashSet<ushort> { 0xC000, 0xC010 }, MemoryAccessType.Read)`
- **Single:** `new AddressRange(0xCFFF, MemoryAccessType.Any)`

### ISlotDevice

For slot-based peripheral cards:

```csharp
public interface ISlotDevice
{
    int Slot { get; }
    string Name { get; }
    bool HandlesRead(ushort address);
    bool HandlesWrite(ushort address);
    byte Read(ushort address);
    void Write(ushort address, byte value);
    void Tick();
    void Reset();
}
```

### SlotRomDevice (abstract base)

Base class for all slot devices. Provides:
- Address range helpers (`IoBaseAddressLo/Hi`, `RomBaseAddressLo/Hi`, `ExpansionBaseAddressLo/Hi`)
- ROM and expansion ROM storage
- Abstract methods: `DoIo()`, `DoCx()`, `DoC8()`
- `BuildAddressRanges()` with `activeInCxRange`/`activeInC8Range` flags

---

## Memory Subsystem

**Location:** `InnoWerks.Computers.Apple/Memory/`

The Apple IIe's 128KB is organized as 64KB main + 64KB auxiliary RAM. Two independent maps — `activeRead[]` and `activeWrite[]` — point each page to the appropriate physical backing store. `Remap()` recalculates both maps whenever any soft switch changes.

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

All soft-switch flags live in `MachineState` as a `Dictionary<SoftSwitch, bool>`.

### IOU

Implements `IAddressInterceptDevice` with discrete address sets for read and write. Handles:
- Keyboard latch (`$C000-$C00F` mirrors), strobe (`$C010`), and queue
- Speaker toggle (`$C030`)
- Paddle/joystick inputs and game strobe
- Annunciators (4 outputs)
- VBL status
- 80-column mode, double hi-res, alt charset

### MMU

Implements `IAddressInterceptDevice`. Handles:
- Language card read/write/bank control (`$C080-$C08F`)
- Auxiliary memory routing
- CxROM/C3ROM/C8ROM banking
- Store 80 mode

### IntC8Handler

Observes `$C300-$C3FF` and `$CFFF` accesses for expansion ROM state management. Returns `false` from `DoRead`/`DoWrite` — it never intercepts, only observes (triggers `IntC8RomEnabled` flag and `Remap()`).

### KeylatchHandler

Handles keyboard latch mirroring (`$C000-$C00F`) and strobe clearing (`$C010-$C01F`) at `SoftSwitch` priority.

### DefaultSoftSwitchHandler

Low-priority catch-all for `$C000-$C08F`. Returns `0x00` on reads and absorbs writes for any address not handled by IOU or MMU.

---

## Peripheral Devices

### Disk II Controller

**File:** `Devices/DiskIISlotDevice.cs`

Emulates the Apple Disk II controller with two drives. State change callback (`OnDriveStateChanged`) notifies the UI of motor on/off, disk insert/eject.

`InsertDisk(drive, path)` and `EjectDisk(drive)` are the public API for disk management. Write-through flushing occurs on motor-off and on reset/reboot.

`FloppyDisk` handles nibble encoding/decoding using 6-and-2 GCR. The denibblizer follows JACE's algorithm for correct XOR chain reversal and bit unscrambling.

### Mockingboard A/B

**File:** `Devices/MockingboardSlotDevice.cs`

Two 6522 VIAs + two AY-3-8910 PSGs. No ROM (original A/B model). Detection is timer-based — VIA Timer 1 free-runs continuously even after reset.

VIA1 at `$Cn00-$Cn0F`, VIA2 at `$Cn80-$Cn8F` (selected by address bit 7). Port A carries PSG data, Port B bits 0-2 carry bus control (BC1, BDIR, ~RESET).

Sets `activeInCxRange: true` in `BuildAddressRanges` since VIA registers are active hardware at `$Cn00`.

### ThunderClock Plus

**File:** `Devices/ThunderClockSlotDevice.cs`

NEC UPD1990AC real-time clock emulation. 2KB ROM with expansion ROM support. Timer interrupt at 64/256/2048 Hz rates. Interrupt fires only when enabled and not already asserted.

### ProDOS Block Device

**File:** `Devices/ProDOSSlotDevice.cs`

SmartPort protocol for up to 4 block storage devices. ROM assembled at runtime. CPU intercept handles block read/write/status/format commands.

### Mouse Interface Card

**File:** `Devices/MouseSlotDevice.cs`

Apple Mouse Interface Card with firmware interception. CPU intercept handlers for SetMouse, ServeMouse, ReadMouse, ClearMouse, PosMouse, ClampMouse, HomeMouse, InitMouse. Position tracked in screen holes.

### No-Slot-Clock (DS1215)

**File:** `Devices/NoSlotClockDevice.cs`

Implements `IAddressInterceptDevice`. Monitors `$C100-$CFFF` for a 64-bit unlock sequence in address bit 0. Once unlocked, serves BCD-encoded clock data (hundredths, seconds, minutes, hours, day-of-week, day, month, year). Locks again after 64 bits read. Only intercepts when internal ROM is being served (`IntCxRomEnabled`, or `$C300` with `SlotC3RomEnabled` false, or `$C800` with `IntC8RomEnabled` true).

### EmptySlotDevice

**File:** `Devices/EmptySlotDevice.cs`

Fills unoccupied slots 1-7. Returns `0xFF` for all I/O reads. ROM filled with `0xFF`. Ensures slot scanning finds consistent values.

### Via6522

**File:** `Devices/Via6522.cs`

MOS 6522 Versatile Interface Adapter. Full register set (ORB/ORA, DDRB/DDRA, T1/T2 timers, shift register, ACR, PCR, IFR, IER). Timer 1 always free-runs (critical for Mockingboard detection). One-shot and free-running modes. IRQ callback on state change.

### AY38910

**File:** `Devices/AY38910.cs`

AY-3-8910 Programmable Sound Generator. 3 tone channels, noise generator (17-bit LFSR), envelope generator with 4-bit shape control. Logarithmic volume table. Bus control protocol via Port B (BC1, BDIR, ~RESET).

---

## Video Subsystem

**Location:** `InnoWerks.Emulators.AppleIIe/Renderers/`

`Display` reads the active soft switches each frame and delegates to the appropriate renderer.

| Mode | Renderer | Resolution | Notes |
|---|---|---|---|
| Text 40-col | `TextModeRenderer` | 40×24 characters | 8×8 font from character ROM |
| Text 80-col | `TextModeRenderer` | 80×24 characters | Interleaved main/aux RAM |
| Lo-res | `LoresRenderer` | 40×48 blocks | 16 colors, 7×4 pixels/block |
| Hi-res | `HiresRenderer` | 280×192 | Color artifacts; rendered at 560px wide |
| Double hi-res | `DhiresRenderer` | 560×192 | 16-color; requires aux RAM |

---

## Audio Subsystem

**Location:** `InnoWerks.Emulators.AppleIIe/Audio/`

### Speaker

1-bit speaker toggled by writes to `$C030`. `AppleIIAudioSource` queues each toggle with a CPU cycle timestamp. `AudioRenderer` converts to 44.1kHz PCM with DSP chain: low-pass → DC blocker → silence snap → gain → clip.

### Mockingboard

`MockingboardSlotDevice.GenerateSample()` clocks both PSGs and mixes output. `AudioRenderer.UpdateAudio()` accepts an optional Mockingboard device, clocking PSGs at the correct rate (~23.14 clocks per audio sample via fractional accumulator) and mixing with the speaker signal.

---

## Toolbar UI

**Location:** `InnoWerks.Emulators.AppleIIe/Renderers/ToolbarRenderer.cs`

A toolbar above the Apple display shows:
- **Reset** button (soft reset, same as Ctrl+F1)
- **Reboot** button (hard reboot, same as Ctrl+F2)
- **Disk II drive icons** — four states: disk inserted motor off, disk inserted motor on, empty motor off, empty motor on
- **ProDOS hard drive icons** — informational
- **Click actions:** eject disk (when inserted), open file chooser (when empty, via NativeFileDialogNET)

`DiskIISlotDevice.OnDriveStateChanged` callback notifies the toolbar of motor/disk state changes.

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
  4. Toolbar.Draw()
  5. Display.Render() → select renderer → draw to RenderTarget2D → blit
  6. DebugTools.Draw() → registers, soft switches, CPU trace
```

---

## Configuration

**Location:** `InnoWerks.Emulators.AppleIIe/configurations/`

JSON configuration files specify:
- Apple model (`appleIIeEnhanced`)
- Slot device assignments (type, slot number, disk images)
- `noSlotClock: true` to install the DS1215
- Monochrome mode and color
- Breakpoints

Example:
```json
{
  "appleModel": "appleIIeEnhanced",
  "noSlotClock": true,
  "slots": [
    { "deviceType": "mouse", "slotNumber": 2 },
    { "deviceType": "mockingboard", "slotNumber": 4 },
    { "deviceType": "diskii", "slotNumber": 6,
      "driveOne": { "image": "disks/dos33.dsk" } }
  ]
}
```

---

## Test Infrastructure

| Test Suite | What it covers |
|---|---|
| **Harte 6502/65C02** | Cycle-accurate JSON tests for all 256 opcodes across 4 CPU variants |
| **Via6522Tests** | Timer countdown, IRQ flag management, port callbacks, register read/write |
| **AY38910Tests** | Bus control protocol, register masking, clock output |
| **ComputerTests** | Construction, Build, Reset, device factory methods |
| **NoSlotClockTests** | Unlock sequence, clock data BCD, soft switch gating |
| **DiskIISlotDeviceTests** | Drive access, insert/eject, state change callbacks |
| **FloppyDiskTests** | Nibblize/denibblize round-trip, sector-level verification |
| **SlotRomDeviceTests** | I/O dispatch, Cx/C8 ROM routing, address ranges |
| **AppleBusTests** | Cycle counting, transactions, soft switch routing, memory access |
| **IouTests** | Display mode switches, keyboard, paddles, VBL, annunciators |
| **MmuTests** | Language card sequencing, memory banking, ROM control |
| **IntC8HandlerTests** | C300/CFFF side effects, address ranges |
| **AddressRangeTests** | Contiguous, discrete, single-address, access type filtering |
| **Memory128kTests** | Page mapping, read/write routing |

---

## Key Design Patterns

| Pattern | Where used |
|---|---|
| **Composition Root** | `Computer` class orchestrates all hardware |
| **Template Method** | `Cpu6502Core` (abstract) / `Cpu65C02` (concrete) |
| **Strategy** | Video renderers (one per display mode) |
| **Observer** | Soft switch writes trigger `Remap()` on memory |
| **Dispatch Table** | 64K bus dispatch arrays; CPU opcode dictionary |
| **Intercept Hook** | `ProDOSSlotDevice` SmartPort driver in managed code |
| **DSP Chain** | Audio filter pipeline (LP → HP → gate → gain → clip) |
| **Priority Queue** | Dispatch table entries sorted by `InterceptPriority` |

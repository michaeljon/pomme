# pomme - Apple IIe Emulator Notes

## Project Overview

- C# .NET 10 Apple IIe emulator using MonoGame for rendering
- Main emulator: InnoWerks.Emulators.AppleIIe
- Core machine: InnoWerks.Computers.Apple
- Ignore: ConsoleTools, Modules, 65x02 directories

## Code Style

- Uses jagged arrays (`bool[][]`) not multidimensional arrays (`bool[,]`) — enforced by project rules
- Developer prefers explicit parentheses for operator precedence clarity (e.g. `((a / b) % c) == 0`) but won't always insist on it mid-session
- Color/rendering code lives in InnoWerks.Emulators.AppleIIe/Renderers/

## Video Rendering Architecture

- Display.cs coordinates 12 renderer instances; LoadContent(Color? monochromeColor, ContentManager) drives all modes
- Monochrome mode activated via --monochrome green|amber|white CLI flag; null = color mode
- Text color defaults to AmberText when monochromeColor is null; debug renderer always uses Color.White
- ResolveMonochromeColor() in Emulator.cs maps CLI string to Color?

## DHIRES Monochrome — Important Implementation Note

- Color mode: 4 bits = 1 color pixel displayed as 4 screen pixels wide → 140 color pixels/row
- Monochrome mode: each bit = 1 screen pixel → 560 pixels/row (full resolution)
- These are FUNDAMENTALLY DIFFERENT rendering paths, not just a color transformation
- DhiresRenderer.DrawMonochrome() reads raw bits via DhiresMemoryReader.ReadDhiresMonochromePage()
- Bit order: LSB-first per byte, alternating aux/main bytes, 7 usable bits per byte (bit 7 ignored)
- 80 bytes × 7 bits = 560 pixels per row
- Applying luminance/ToMonochrome to the color pipeline (the wrong approach) causes blurry text because it still renders at 140-pixel resolution

## HIRES Monochrome

- Uses ToMonochrome(drawColor, monochromeColor) after normal artifact color/white selection
- Back-propagation in white-collision path also applies ToMonochrome
- Has REVIEW comment about pixel phase logic correctness

## LORES/DLORES Monochrome

- LoresCell.Top/Bottom use ToMonochrome(paletteColor, monochromeColor) — luminance-based gradient
- Preserves brightness differences from the original 16-color palette
- Has REVIEW comment questioning whether the 'hires' parameter to Top/Bottom is still necessary

## DisplayCharacteristics.ToMonochrome

- Uses ITU-R BT.709 luminance: `0.2126*R + 0.7152*G + 0.0722*B`
- Scales monochromeColor by source luminance to preserve brightness differences

## Slot Device Architecture

- Slot 4: Mouse (--mouse flag) — MouseSlotDevice
- Slot 5: ProDOS hard disk (--harddisk1/2/3/4) — ProDOSSlotDevice; supports up to 4 drives
- Slot 6: Disk II floppy (--disk1/2) — DiskIISlotDevice

## CPU Intercept Model

- Handlers are `Func<ICpu, IBus, bool>` — return true for auto-RTS, false to fall through
- See [feedback_intercept_model.md](feedback_intercept_model.md) for full details

## Mouse Interface Card

- Fully implemented and working with A2DeskTop
- Two-level dispatch: vector table ($12–$1A) → handler offsets ($92–$9A)
- See [project_mouse.md](project_mouse.md) and Documentation/MOUSE.md for full details

## Memory Entries

- [project_mouse.md](project_mouse.md) — Mouse card implementation status and design
- [feedback_intercept_model.md](feedback_intercept_model.md) — CPU intercept handler bool return convention
- [feedback_rely_on_source.md](feedback_rely_on_source.md) — Use Apple hardware manuals, not secondary sources

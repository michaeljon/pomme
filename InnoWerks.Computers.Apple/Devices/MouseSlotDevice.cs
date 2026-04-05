using System;
using System.Collections.Generic;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
    public sealed class MouseSlotDevice : SlotRomDevice
    {
        [Flags]
        private enum MouseStatusFlags : byte
        {
            Reserved0 = 0x00,
            MouseMoveInterrupt = 0x01,
            ButtonPressInterrupt = 0x02,
            ScreenRefreshInt = 0x04,
            Reserved4 = 0x10,
            MouseMoved = 0x20,
            ButtonWasDown = 0x40,
            ButtonIsDown = 0x80
        }

        [Flags]
        private enum MouseModeFlags : byte
        {
            Clear = 0x00,
            Enabled = 0x01,
            InterruptOnMove = 0x02,
            InterruptOnButton = 0x04,
            InterruptOnVbl = 0x08,
        }

        // Firmware entry point offsets within the slot ROM ($Cn00 + offset)
        private const byte SetMouseVector = 0x12;
        private const byte ServeMouseVector = 0x13;
        private const byte ReadMouseVector = 0x14;
        private const byte ClearMouseVector = 0x15;
        private const byte PosMouseVector = 0x16;
        private const byte ClampMouseVector = 0x17;
        private const byte HomeMouseVector = 0x18;
        private const byte InitMouseVector = 0x19;
        private const byte GetClampVector = 0x1A;

        // where we're going to stuff our handlers
        private const byte InterceptBase = 0x80;

        // Screen hole base addresses per-slot (slot n is base + n)
        // watch out of the HandleClampMouse which does not use the slot offset
        private const ushort XLoScreenHole = 0x0478;
        private const ushort YLoScreenHole = 0x04F8;

        private const ushort XHiScreenHole = 0x0578;
        private const ushort YHiScreenHole = 0x05F8;

        private const ushort StatScreenHole = 0x0778;
        private const ushort ModeScreenHole = 0x07F8;

        // private const int CyclesPerUpdate = (int)(1020484L / 60L);

        // Mouse state
        private int mouseX;
        private int mouseY;
        private bool buttonCurrentlyDown;
        private bool buttonPreviouslyDown;
        private bool mouseMoved;

        private int clampMinX;
        private int clampMaxX = 0x03FF;
        private int clampMinY;
        private int clampMaxY = 0x03FF;

        private MouseModeFlags mouseMode;

        public bool IsMouseCaptured { get; private set; }

        public MouseSlotDevice(int slot, ICpu cpu, IAppleBus bus, MachineState machineState)
            : base(slot, "Apple Mouse Interface Card", cpu, bus, machineState)
        {
            ArgumentNullException.ThrowIfNull(cpu, nameof(cpu));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            var initializers = new List<(byte vector, Func<ICpu, IBus, bool> handler)>
            {
                (SetMouseVector, HandleSetMouse),
                (ServeMouseVector, HandleServeMouse),
                (ReadMouseVector, HandleReadMouse),
                (ClearMouseVector, HandleClearMouse),
                (PosMouseVector, HandlePosMouse),
                (ClampMouseVector, HandleClampMouse),
                (HomeMouseVector, HandleHomeMouse),
                (InitMouseVector, HandleInitMouse),
                (GetClampVector, HandleGetClamp),
            };

            HasRom = true;
            Rom = new byte[MemoryPage.PageSize];

            Array.Fill(Rom, (byte)0x00);

            // Identification bytes — scanned by ProDOS and applications like A2DeskTop
            // to locate the mouse card.  These specific values match the original Apple
            // Mouse Interface Card ROM signature.
            Rom[0x05] = 0x38;  // ID byte
            Rom[0x07] = 0x18;  // ID byte
            Rom[0x0B] = 0x01;  // ID byte
            Rom[0x0C] = 0x20;  // Device type = mouse
            Rom[0xFB] = 0xD6;  // ID byte (mouse technote #5)

            // pascal signature bytes
            Rom[0x08] = 0x01;  // ID byte
            Rom[0x11] = 0x00;  // ID byte

            var baseAddr = (ushort)(0xC000 + (slot * 0x100));
            foreach (var (vector, handler) in initializers)
            {
                // mouse card routine entry vectors, these redirect to
                // the actual handler in the rom, which are intercepted below
                Rom[vector] = (byte)(InterceptBase + vector);

                // now wire up our intercept handlers per the vectors above
                cpu.AddIntercept((ushort)(baseAddr + Rom[vector]), handler);
            }

            bus.AddDevice(this);
        }

        // Called by Emulator.cs each frame with the current host mouse state.
        // displayX/Y/Width/Height describe the pixel region of the host window
        // that corresponds to the Apple II screen (hostLayout.AppleDisplay).
        public void UpdateFromHost(int hostX, int hostY, bool leftButton,
            int displayX, int displayY, int displayWidth, int displayHeight)
        {
            // not captured
            if (!IsMouseCaptured) return;

            // not enabled from program
            if ((mouseMode & MouseModeFlags.Enabled) == 0x00)
            {
                return;
            }

            // Map host pixel position within the Apple display rect to the current clamp range
            var clampWidth = clampMaxX - clampMinX;
            var clampHeight = clampMaxY - clampMinY;

            var newX = clampMinX + ((hostX - displayX) * clampWidth / displayWidth);
            var newY = clampMinY + ((hostY - displayY) * clampHeight / displayHeight);

            newX = Math.Clamp(newX, clampMinX, clampMaxX);
            newY = Math.Clamp(newY, clampMinY, clampMaxY);

            mouseMoved = ((newX != mouseX) || (newY != mouseY));
            mouseX = newX;
            mouseY = newY;

            buttonPreviouslyDown = buttonCurrentlyDown;
            buttonCurrentlyDown = leftButton;
        }

        public void Capture() => IsMouseCaptured = true;

        public void Release() => IsMouseCaptured = false;

        // --- Firmware intercept handlers ---

        private bool HandleInitMouse(ICpu cpu, IBus bus)
        {
            // verify that X and Y are correct on input
            if (cpu.Registers.X != 0xC0 + Slot || cpu.Registers.Y != (byte)(Slot << 4))
            {
                // this is a failure case, really, because the protocol says this should hold
            }

            mouseX = 0;
            mouseY = 0;
            buttonCurrentlyDown = false;
            buttonPreviouslyDown = false;
            mouseMoved = false;
            mouseMode = MouseModeFlags.Clear;
            clampMinX = 0; clampMaxX = 0x03FF;
            clampMinY = 0; clampMaxY = 0x03FF;

            cpu.Registers.Carry = false;
            return true;
        }

        private bool HandleSetMouse(ICpu cpu, IBus bus)
        {
            // verify that X and Y are correct on input
            if (cpu.Registers.X != 0xC0 + Slot || cpu.Registers.Y != (byte)(Slot << 4))
            {
                // this is a failure case, really, because the protocol says this should hold
            }

            if (cpu.Registers.A > 0x0F)
            {
                cpu.Registers.Carry = true;
            }
            else
            {
                cpu.Registers.Carry = false;
                mouseMode = (MouseModeFlags)cpu.Registers.A;
            }

            return true;
        }

        private bool HandleReadMouse(ICpu cpu, IBus bus)
        {
            // verify that X and Y are correct on input
            if (cpu.Registers.X != 0xC0 + Slot || cpu.Registers.Y != (byte)(Slot << 4))
            {
                // this is a failure case, really, because the protocol says this should hold
            }

            WriteMouseData(bus);

            mouseMoved = false;   // clear movement flag after reporting
            buttonPreviouslyDown = buttonCurrentlyDown;

            cpu.Registers.Carry = false;
            return true;
        }

        private bool HandleClearMouse(ICpu cpu, IBus bus)
        {
            // verify that X and Y are correct on input
            if (cpu.Registers.X != 0xC0 + Slot || cpu.Registers.Y != (byte)(Slot << 4))
            {
                // this is a failure case, really, because the protocol says this should hold
            }

            mouseX = 0;
            mouseY = 0;
            buttonCurrentlyDown = false;
            buttonPreviouslyDown = false;
            mouseMoved = false;

            WriteMouseData(bus);

            cpu.Registers.Carry = false;
            return true;
        }

        private bool HandleHomeMouse(ICpu cpu, IBus bus)
        {
            // verify that X and Y are correct on input
            if (cpu.Registers.X != 0xC0 + Slot || cpu.Registers.Y != (byte)(Slot << 4))
            {
                // this is a failure case, really, because the protocol says this should hold
            }

            mouseX = 0;
            mouseY = 0;
            mouseMoved = false;

            cpu.Registers.Carry = false;
            return true;
        }

        private bool HandlePosMouse(ICpu cpu, IBus bus)
        {
            // what do we do for this? can't bounce the mouse around
            cpu.Registers.Carry = false;
            return true;
        }

        private bool HandleGetClamp(ICpu cpu, IBus bus)
        {
            var reg = bus.Peek(0x0478);
            var val = (reg - 0x47) switch
            {
                0x00 => (clampMinX >> 8) & 0xFF,
                0x01 => (clampMinY >> 8) & 0xFF,
                0x02 => clampMinX & 0xFF,
                0x03 => clampMinY & 0xFF,

                0x04 => (clampMaxX >> 8) & 0xFF,
                0x05 => (clampMaxY >> 8) & 0xFF,
                0x06 => clampMaxX & 0xFF,
                0x07 => clampMaxY & 0xFF,

                _ => 0,
            };

            bus.Write(0x0578, (byte)val);

            cpu.Registers.Carry = false;
            return true;
        }

        private bool HandleClampMouse(ICpu cpu, IBus bus)
        {
            // verify that X and Y are correct on input
            if (cpu.Registers.X != 0xC0 + Slot || cpu.Registers.Y != (byte)(Slot << 4))
            {
                // this is a failure case, really, because the protocol says this should hold
            }

            var min = bus.Read(XLoScreenHole) | (bus.Read(XHiScreenHole) << 8);
            var max = bus.Read(YLoScreenHole) | (bus.Read(YHiScreenHole) << 8);

            if (min >= 32768)
            {
                min -= 65536;
            }
            if (max >= 32768)
            {
                max -= 65536;
            }

            if (cpu.Registers.A == 0)
            {
                clampMinX = min;
                clampMaxX = max;
            }
            else
            {
                clampMinY = min;
                clampMaxY = max;
            }

            cpu.Registers.Carry = false;
            return true;
        }

        private bool HandleServeMouse(ICpu cpu, IBus bus)
        {
            // this one DOES NOT check X and Y because it's called via interrupt
            MouseStatusFlags status = MouseStatusFlags.Reserved0;

            if (mouseMoved) status |= MouseStatusFlags.MouseMoveInterrupt;
            if (buttonCurrentlyDown != buttonPreviouslyDown) status |= MouseStatusFlags.ButtonPressInterrupt;

            bus.Write((ushort)(StatScreenHole + Slot), (byte)status);

            cpu.Registers.Carry = false;
            return true;
        }

        // --- Helpers ---

        private void WriteMouseData(IBus bus)
        {
            bus.Write((ushort)(XLoScreenHole + Slot), (byte)(mouseX & 0xFF));
            bus.Write((ushort)(XHiScreenHole + Slot), (byte)((mouseX >> 8) & 0xFF));
            bus.Write((ushort)(YLoScreenHole + Slot), (byte)(mouseY & 0xFF));
            bus.Write((ushort)(YHiScreenHole + Slot), (byte)((mouseY >> 8) & 0xFF));
            bus.Write((ushort)(StatScreenHole + Slot), (byte)BuildStatusByte());
            bus.Write((ushort)(ModeScreenHole + Slot), (byte)mouseMode);
        }

        private MouseStatusFlags BuildStatusByte()
        {
            MouseStatusFlags status = MouseStatusFlags.Reserved0;

            if (buttonPreviouslyDown) status |= MouseStatusFlags.ButtonWasDown;
            if (mouseMoved) status |= MouseStatusFlags.MouseMoved;
            if (buttonCurrentlyDown) status |= MouseStatusFlags.ButtonIsDown;

            return status;
        }

        // --- SlotRomDevice required overrides ---

        protected override byte DoIo(MemoryAccessType ioType, ushort address, byte value) => 0xFF;

        public override void Tick()
        {
            if ((mouseMode & MouseModeFlags.Enabled) == 0x00)
            {
                return;
            }

            // if we've not been asked for interrupts...
            if ((mouseMode & MouseModeFlags.InterruptOnMove) == 0x00 || (mouseMode & MouseModeFlags.InterruptOnButton) == 0x00)
            {
                // return;
            }
        }

        public override void Reset()
        {
            mouseX = 0;
            mouseY = 0;
            buttonCurrentlyDown = false;
            buttonPreviouslyDown = false;
            mouseMoved = false;
            mouseMode = MouseModeFlags.Clear;
            clampMinX = 0; clampMaxX = 0x03FF;
            clampMinY = 0; clampMaxY = 0x03FF;
            IsMouseCaptured = false;
        }
    }
}

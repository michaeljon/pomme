using System;
using System.IO;
using InnoWerks.Processors;
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
        private const byte GetClampVector = 0x19;

        private const byte SetMouseIntercept = 0x80;
        private const byte ServeMouseIntercept = 0x81;
        private const byte ReadMouseIntercept = 0x82;
        private const byte ClearMouseIntercept = 0x83;
        private const byte PosMouseIntercept = 0x84;
        private const byte ClampMouseIntercept = 0x85;
        private const byte HomeMouseIntercept = 0x86;
        private const byte InitMouseIntercept = 0x87;
        private const byte GetClampIntercept = 0x98;

        // Screen hole base addresses per-slot (slot n is base + n)
        private const ushort XLoScreenHole = 0x0478;
        private const ushort YLoScreenHole = 0x04F8;

        private const ushort XHiScreenHole = 0x0578;
        private const ushort YHiScreenHole = 0x05F8;

        private const ushort StatScreenHole = 0x0778;
        private const ushort ModeScreenHole = 0x07F8;

        private const int CyclesPerUpdate = (int)(1020484L / 60L);

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

        public MouseSlotDevice(int slot, ICpu cpu, IBus bus, MachineState machineState)
            : base(slot, "Apple Mouse Interface Card", cpu, bus, machineState)
        {
            HasRom = true;
            Rom = new byte[MemoryPage.PageSize];

            // Fill with RTS ($60) as a safe fallback for any unintercepted reads
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

            // mouse card routine entry vectors, these redirect to
            // the actual handler in the rom, which are intercepted below
            Rom[SetMouseVector] = SetMouseIntercept;
            Rom[ServeMouseVector] = ServeMouseIntercept;
            Rom[ReadMouseVector] = ReadMouseIntercept;
            Rom[ClearMouseVector] = ClearMouseIntercept;
            Rom[PosMouseVector] = PosMouseIntercept;
            Rom[ClampMouseVector] = ClampMouseIntercept;
            Rom[HomeMouseVector] = HomeMouseIntercept;
            Rom[InitMouseVector] = InitMouseIntercept;
            Rom[GetClampVector] = GetClampIntercept;

            ArgumentNullException.ThrowIfNull(bus, nameof(bus));
            bus.AddDevice(this);

            // now wire up our intercept handlers per the vectors above
            ArgumentNullException.ThrowIfNull(cpu, nameof(cpu));
            var baseAddr = (ushort)(0xC000 + (slot * 0x100));
            cpu.AddIntercept((ushort)(baseAddr + SetMouseIntercept), HandleSetMouse);
            cpu.AddIntercept((ushort)(baseAddr + ServeMouseIntercept), HandleServeMouse);
            cpu.AddIntercept((ushort)(baseAddr + ReadMouseIntercept), HandleReadMouse);
            cpu.AddIntercept((ushort)(baseAddr + ClearMouseIntercept), HandleClearMouse);
            cpu.AddIntercept((ushort)(baseAddr + PosMouseIntercept), HandlePosMouse);
            cpu.AddIntercept((ushort)(baseAddr + ClampMouseIntercept), HandleClampMouse);
            cpu.AddIntercept((ushort)(baseAddr + HomeMouseIntercept), HandleHomeMouse);
            cpu.AddIntercept((ushort)(baseAddr + InitMouseIntercept), HandleInitMouse);
            cpu.AddIntercept((ushort)(baseAddr + GetClampIntercept), HandleGetClamp);
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

        private void HandleInitMouse(ICpu cpu, IBus bus)
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
            ((Cpu6502Core)cpu).RTS(0, 0);
        }

        private void HandleSetMouse(ICpu cpu, IBus bus)
        {
            // verify that X and Y are correct on input
            if (cpu.Registers.X != 0xC0 + Slot || cpu.Registers.Y != (byte)(Slot << 4))
            {
                // this is a failure case, really, because the protocol says this should hold
            }

            if (cpu.Registers.A > 0x0F)
            {
                cpu.Registers.Carry = true;
                return;
            }
            cpu.Registers.Carry = false;

            mouseMode = (MouseModeFlags)cpu.Registers.A;

            ((Cpu6502Core)cpu).RTS(0, 0);
        }

        private void HandleReadMouse(ICpu cpu, IBus bus)
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
            ((Cpu6502Core)cpu).RTS(0, 0);
        }

        private void HandleClearMouse(ICpu cpu, IBus bus)
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
            ((Cpu6502Core)cpu).RTS(0, 0);
        }

        private void HandleHomeMouse(ICpu cpu, IBus bus)
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
            ((Cpu6502Core)cpu).RTS(0, 0);
        }

        private void HandlePosMouse(ICpu cpu, IBus bus)
        {
            // what do we do for this? can't bounce the mouse around
            cpu.Registers.Carry = false;
            ((Cpu6502Core)cpu).RTS(0, 0);
        }

        private void HandleGetClamp(ICpu cpu, IBus bus)
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
            ((Cpu6502Core)cpu).RTS(0, 0);
        }

        private void HandleClampMouse(ICpu cpu, IBus bus)
        {
            // verify that X and Y are correct on input
            if (cpu.Registers.X != 0xC0 + Slot || cpu.Registers.Y != (byte)(Slot << 4))
            {
                // this is a failure case, really, because the protocol says this should hold
            }

            // todo: these are the correct values, but they're different from the
            //       screen hole convention for reporting (x,y) locations
            var min = bus.Read(0x0478) | (bus.Read(0x0578) << 8);
            var max = bus.Read(0x04F8) | (bus.Read(0x05F8) << 8);

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
            ((Cpu6502Core)cpu).RTS(0, 0);
        }

        private void HandleServeMouse(ICpu cpu, IBus bus)
        {
            // this one DOES NOT check X and Y because it's called via interrupt
            MouseStatusFlags status = MouseStatusFlags.Reserved0;

            if (mouseMoved) status |= MouseStatusFlags.MouseMoveInterrupt;
            if (buttonCurrentlyDown != buttonPreviouslyDown) status |= MouseStatusFlags.ButtonPressInterrupt;

            bus.Write((ushort)(StatScreenHole + Slot), (byte)status);

            cpu.Registers.Carry = false;
            ((Cpu6502Core)cpu).RTS(0, 0);
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

        protected override byte DoIo(CardIoType ioType, byte address, byte value) => 0xFF;

        protected override byte DoCx(CardIoType ioType, ushort address, byte value) => 0x00;

        protected override byte DoC8(CardIoType ioType, ushort address, byte value) => 0x00;

        public override bool HandlesRead(ushort address) => (address >= IoBaseAddressLo && address <= IoBaseAddressHi);

        public override bool HandlesWrite(ushort address) => (address >= IoBaseAddressLo && address <= IoBaseAddressHi);

        private int delay = CyclesPerUpdate;

        public override void Tick(int cycles)
        {
            if ((mouseMode & MouseModeFlags.Enabled) == 0x00)
            {
                return;
            }

            delay += cycles;

            // if we've not been asked for interrupts...
            if ((mouseMode & MouseModeFlags.InterruptOnMove) == 0x00 || (mouseMode & MouseModeFlags.InterruptOnButton) == 0x00)
            {
                return;
            }

            if ((mouseMode & MouseModeFlags.InterruptOnButton) != 0x00)
            {

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

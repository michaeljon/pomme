// #define DEBUG_READ
// #define DEBUG_WRITE

using System;
using System.Collections.Generic;

namespace InnoWerks.Computers.Apple
{
    public class IOU : IAddressInterceptDevice
    {
        // Apple II paddle timing: each PDL unit ≈ 11 µs at 1 MHz ≈ 11 cycles
        private const int CyclesPerPaddleUnit = 11;

        private readonly IAppleBus bus;

        private readonly Memory128k memoryBlocks;

        private readonly MachineState machineState;

        // Analog paddle values (0–255), updated each frame from the host joystick
        private readonly int[] paddleValues = new int[4];

        // Bus cycle count when PTRIG was last fired;
        // -1 means not yet triggered
        private long paddleTimerStartCycle = -1;

        private readonly HashSet<ushort> handlesRead = [
            //
            // DISPLAY
            //
            SoftSwitchAddress.TXTCLR,
            SoftSwitchAddress.TXTSET,
            SoftSwitchAddress.MIXCLR,
            SoftSwitchAddress.MIXSET,
            SoftSwitchAddress.TXTPAGE1,
            SoftSwitchAddress.TXTPAGE2,
            SoftSwitchAddress.LORES,
            SoftSwitchAddress.HIRES,
            SoftSwitchAddress.DHIRESON,
            SoftSwitchAddress.DHIRESOFF,

            SoftSwitchAddress.RDTEXT,
            SoftSwitchAddress.RDMIXED,
            SoftSwitchAddress.RDPAGE2,
            SoftSwitchAddress.RDHIRES,
            SoftSwitchAddress.RDALTCHR,
            SoftSwitchAddress.RD80COL,
            SoftSwitchAddress.RDDHIRES,

            //
            // KEYBOARD
            //
            SoftSwitchAddress.KBD,
            SoftSwitchAddress.KBDSTRB,
            SoftSwitchAddress.OPENAPPLE,
            SoftSwitchAddress.SOLIDAPPLE,
            SoftSwitchAddress.SHIFT,

            //
            // ANNUNCIATORS
            //
            SoftSwitchAddress.CLRAN0,
            SoftSwitchAddress.SETAN0,
            SoftSwitchAddress.CLRAN1,
            SoftSwitchAddress.SETAN1,
            SoftSwitchAddress.CLRAN2,
            SoftSwitchAddress.SETAN2,
            // SoftSwitchAddress.CLRAN3,
            // SoftSwitchAddress.SETAN3,

            //
            // VBL
            //
            SoftSwitchAddress.RDVBLBAR,

            //
            // CASSETTE
            //
            SoftSwitchAddress.TAPEIN,
            SoftSwitchAddress.TAPEOUT,

            //
            // SPEAKER
            //
            SoftSwitchAddress.SPKR,

            //
            // PADDLES AND BUTTONS
            //
            SoftSwitchAddress.PADDLE0,
            SoftSwitchAddress.PADDLE1,
            SoftSwitchAddress.PADDLE2,
            SoftSwitchAddress.PADDLE3,

            // SoftSwitchAddress.PTRIG,
            0xC070, 0xC071, 0xC072, 0xC073, 0xC074, 0xC075, 0xC076, 0xC077,
            0xC078, 0xC079, 0xC07A, 0xC07B, 0xC07C, 0xC07D,

            // SoftSwitchAddress.BUTTON0,
            // SoftSwitchAddress.BUTTON1,
            // SoftSwitchAddress.BUTTON2,
            SoftSwitchAddress.STROBE,

            //
            // IOU
            //
            SoftSwitchAddress.RDIOUDIS
        ];

        private readonly HashSet<ushort> handlesWrite =
        [
            //
            // DISPLAY
            //
            SoftSwitchAddress.TXTCLR,
            SoftSwitchAddress.TXTSET,
            SoftSwitchAddress.MIXCLR,
            SoftSwitchAddress.MIXSET,
            SoftSwitchAddress.TXTPAGE1,
            SoftSwitchAddress.TXTPAGE2,
            SoftSwitchAddress.LORES,
            SoftSwitchAddress.HIRES,
            SoftSwitchAddress.DHIRESON,
            SoftSwitchAddress.DHIRESOFF,

            SoftSwitchAddress.CLR80COL,
            SoftSwitchAddress.SET80COL,
            SoftSwitchAddress.CLRALTCHAR,
            SoftSwitchAddress.SETALTCHAR,

            //
            // KEYBOARD
            //
            SoftSwitchAddress.KBDSTRB,

            //
            // ANNUNCIATORS
            //
            SoftSwitchAddress.CLRAN0,
            SoftSwitchAddress.SETAN0,
            SoftSwitchAddress.CLRAN1,
            SoftSwitchAddress.SETAN1,
            SoftSwitchAddress.CLRAN2,
            SoftSwitchAddress.SETAN2,
            // SoftSwitchAddress.CLRAN3,
            // SoftSwitchAddress.SETAN3,

            //
            // PADDLES
            //
            // SoftSwitchAddress.PTRIG,
            0xC070, 0xC071, 0xC072, 0xC073, 0xC074, 0xC075, 0xC076, 0xC077,
            0xC078, 0xC079, 0xC07A, 0xC07B, 0xC07C, 0xC07D,

            //
            // IOU
            //
            SoftSwitchAddress.IOUDISON,
            SoftSwitchAddress.IOUDISOFF,
        ];

        public string Name => $"IOU";

        public InterceptPriority InterceptPriority => InterceptPriority.SoftSwitch;

        public IOU(Memory128k memoryBlocks, MachineState machineState, IAppleBus bus)
        {
            ArgumentNullException.ThrowIfNull(machineState, nameof(machineState));
            ArgumentNullException.ThrowIfNull(memoryBlocks, nameof(memoryBlocks));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            this.machineState = machineState;
            this.memoryBlocks = memoryBlocks;
            this.bus = bus;

            AddressRanges =
            [
                new (handlesRead, MemoryAccessType.Read),
                new (handlesWrite, MemoryAccessType.Write),
            ];

            bus.AddDevice(this);
        }

        public IReadOnlyList<AddressRange> AddressRanges
        {
            get;
            init;
        }

        public bool DoRead(ushort address, out byte value)
        {
            value = 0;

            switch (address)
            {
                //
                // DISPLAY
                //
                case SoftSwitchAddress.TXTCLR:
                    value = machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.TextMode, false, true);
                    break;
                case SoftSwitchAddress.TXTSET:
                    value = machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.TextMode, true, true);
                    break;

                case SoftSwitchAddress.MIXCLR:
                    value = machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.MixedMode, false, true);
                    break;
                case SoftSwitchAddress.MIXSET:
                    value = machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.MixedMode, true, true);
                    break;

                case SoftSwitchAddress.TXTPAGE1:
                    value = machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.Page2, false, true);
                    break;
                case SoftSwitchAddress.TXTPAGE2:
                    value = machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.Page2, true);
                    break;

                case SoftSwitchAddress.LORES:
                    value = machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.HiRes, false);
                    break;
                case SoftSwitchAddress.HIRES:
                    value = machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.HiRes, true);
                    break;

                // see CLRAN3
                case SoftSwitchAddress.DHIRESON:
                    if (machineState.State[SoftSwitch.IOUDisabled] == true)
                    {
                        value = machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.DoubleHiRes, true);
                    }
                    break;

                // see SETAN3
                case SoftSwitchAddress.DHIRESOFF:
                    if (machineState.State[SoftSwitch.IOUDisabled] == true)
                    {
                        value = machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.DoubleHiRes, false);
                    }
                    break;

                case SoftSwitchAddress.RDTEXT:
                    value = (byte)(machineState.State[SoftSwitch.TextMode] ? 0x80 : 0x00);
                    break;
                case SoftSwitchAddress.RDMIXED:
                    value = (byte)(machineState.State[SoftSwitch.MixedMode] ? 0x80 : 0x00);
                    break;
                case SoftSwitchAddress.RDPAGE2:
                    value = (byte)(machineState.State[SoftSwitch.Page2] ? 0x80 : 0x00);
                    break;
                case SoftSwitchAddress.RDHIRES:
                    value = (byte)(machineState.State[SoftSwitch.HiRes] ? 0x80 : 0x00);
                    break;
                case SoftSwitchAddress.RDALTCHR:
                    // if (InVerticalBlank() == true)
                    // {
                    //     return 0x00;
                    // }

                    value = (byte)(machineState.State[SoftSwitch.AltCharSet] ? 0x80 : 0x00);
                    break;

                case SoftSwitchAddress.RD80COL:
                    // if (InVerticalBlank() == true)
                    // {
                    //     return 0x00;
                    // }

                    value = (byte)(machineState.State[SoftSwitch.EightyColumnMode] ? 0x80 : 0x00);
                    break;

                case SoftSwitchAddress.RDDHIRES:
                    value = (byte)(machineState.State[SoftSwitch.DoubleHiRes] ? 0x80 : 0x00);
                    break;

                //
                // KEYBOARD
                //

                // this looks like a keyboard here, because it is
                // this is also known as CLR80STORE
                case SoftSwitchAddress.KBD:
                    value = machineState.ReadKeyboardData();
                    break;

                case SoftSwitchAddress.KBDSTRB:
                    machineState.ClearKeyboardStrobe();
                    value = 0x00;
                    break;

                case SoftSwitchAddress.OPENAPPLE:
                    value = (byte)(machineState.State[SoftSwitch.OpenApple] ? 0x80 : 0x00);
                    break;
                case SoftSwitchAddress.SOLIDAPPLE:
                    value = (byte)(machineState.State[SoftSwitch.SolidApple] ? 0x80 : 0x00);
                    break;
                case SoftSwitchAddress.SHIFT:
                    value = (byte)(machineState.State[SoftSwitch.ShiftKey] ? 0x80 : 0x00);
                    break;

                //
                // ANNUNCIATORS
                //
                // handle if IOU == true case
                case SoftSwitchAddress.CLRAN0:
                    machineState.State[SoftSwitch.Annunciator0] = false;
                    value = 0x00;
                    break;
                case SoftSwitchAddress.SETAN0:
                    machineState.State[SoftSwitch.Annunciator0] = true;
                    value = 0x00;
                    break;
                case SoftSwitchAddress.CLRAN1:
                    machineState.State[SoftSwitch.Annunciator1] = false;
                    value = 0x00;
                    break;
                case SoftSwitchAddress.SETAN1:
                    machineState.State[SoftSwitch.Annunciator1] = true;
                    value = 0x00;
                    break;
                case SoftSwitchAddress.CLRAN2:
                    machineState.State[SoftSwitch.Annunciator2] = false;
                    value = 0x00;
                    break;
                case SoftSwitchAddress.SETAN2:
                    machineState.State[SoftSwitch.Annunciator2] = true;
                    value = 0x00;
                    break;

                //
                // VBL
                //
                case SoftSwitchAddress.RDVBLBAR:
                    value = InVerticalBlank();
                    break;

                //
                // CASSETTE
                //
                case SoftSwitchAddress.TAPEOUT:
                    machineState.State[SoftSwitch.TapeOut] = !machineState.State[SoftSwitch.TapeOut];
                    value = 0x00;
                    break;
                case SoftSwitchAddress.TAPEIN:
                    value = (byte)(machineState.State[SoftSwitch.TapeIn] ? 0x80 : 0x00);
                    break;

                //
                // SPEAKER
                //
                case SoftSwitchAddress.SPKR:
                    machineState.State[SoftSwitch.Speaker] = !machineState.State[SoftSwitch.Speaker];
                    value = 0x00;
                    break;

                //
                // PADDLES AND BUTTONS
                //
                case SoftSwitchAddress.PADDLE0:
                    value = PaddleRead(0);
                    break;
                case SoftSwitchAddress.PADDLE1:
                    value = PaddleRead(1);
                    break;
                case SoftSwitchAddress.PADDLE2:
                    value = PaddleRead(2);
                    break;
                case SoftSwitchAddress.PADDLE3:
                    value = PaddleRead(3);
                    break;

                case SoftSwitchAddress.STROBE:
                    machineState.State[SoftSwitch.GameStrobe] = true;
                    value = 0x00;
                    break;

                //
                // IOU
                //
                case SoftSwitchAddress.RDIOUDIS:
                    value = (byte)(machineState.State[SoftSwitch.IOUDisabled] ? 0x80 : 0x00);
                    break;
            }

            //
            // PADDLES
            //
            if (address >= 0xC070 && address <= 0xC07D)
            {
                paddleTimerStartCycle = (long)bus.CycleCount;
            }

            return true;
        }

        public bool DoWrite(ushort address, byte value)
        {
            switch (address)
            {
                //
                // DISPLAY
                //
                case SoftSwitchAddress.TXTCLR:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.TextMode, false);
                    break;
                case SoftSwitchAddress.TXTSET:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.TextMode, true);
                    break;
                case SoftSwitchAddress.MIXCLR:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.MixedMode, false);
                    break;
                case SoftSwitchAddress.MIXSET:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.MixedMode, true);
                    break;
                case SoftSwitchAddress.TXTPAGE1:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.Page2, false);
                    break;
                case SoftSwitchAddress.TXTPAGE2:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.Page2, true);
                    break;
                case SoftSwitchAddress.LORES:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.HiRes, false);
                    break;
                case SoftSwitchAddress.HIRES:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.HiRes, true);
                    break;

                case SoftSwitchAddress.DHIRESON:
                    if (machineState.State[SoftSwitch.IOUDisabled] == true)
                    {
                        machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.DoubleHiRes, true);
                    }
                    break;
                case SoftSwitchAddress.DHIRESOFF:
                    if (machineState.State[SoftSwitch.IOUDisabled] == true)
                    {
                        machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.DoubleHiRes, false);
                    }
                    break;

                case SoftSwitchAddress.CLR80COL:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.EightyColumnMode, false);
                    break;
                case SoftSwitchAddress.SET80COL:
                    machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.EightyColumnMode, true);
                    break;
                case SoftSwitchAddress.CLRALTCHAR:
                    machineState.State[SoftSwitch.AltCharSet] = false;
                    break;
                case SoftSwitchAddress.SETALTCHAR:
                    machineState.State[SoftSwitch.AltCharSet] = true;
                    break;

                //
                // KEYBOARD
                //
                case SoftSwitchAddress.KBDSTRB:
                    machineState.ClearKeyboardStrobe();
                    break;

                //
                // ANNUNCIATORS
                //
                // handle weirdness with IOU
                case SoftSwitchAddress.CLRAN0:
                    machineState.State[SoftSwitch.Annunciator0] = false;
                    break;
                case SoftSwitchAddress.SETAN0:
                    machineState.State[SoftSwitch.Annunciator0] = true;
                    break;
                case SoftSwitchAddress.CLRAN1:
                    machineState.State[SoftSwitch.Annunciator1] = false;
                    break;
                case SoftSwitchAddress.SETAN1:
                    machineState.State[SoftSwitch.Annunciator1] = true;
                    break;
                case SoftSwitchAddress.CLRAN2:
                    machineState.State[SoftSwitch.Annunciator2] = false;
                    break;
                case SoftSwitchAddress.SETAN2:
                    machineState.State[SoftSwitch.Annunciator2] = true;
                    break;

                //
                // IOU
                //
                case SoftSwitchAddress.IOUDISON:
                    machineState.State[SoftSwitch.IOUDisabled] = true;
                    break;
                case SoftSwitchAddress.IOUDISOFF:
                    machineState.State[SoftSwitch.IOUDisabled] = false;
                    break;
            }

            //
            // PADDLES
            //
            if (address >= 0xC070 && address <= 0xC07D)
            {
                paddleTimerStartCycle = (long)bus.CycleCount;
            }

            return true;
        }

        public void Tick() { /* NO-OP */ }

        public void Reset()
        {
            machineState.ResetKeyboard();

            machineState.State[SoftSwitch.TextMode] = true;
            machineState.State[SoftSwitch.IOUDisabled] = true;
        }

        private byte InVerticalBlank()
        {
            var frameIndex = bus.CycleCount / VideoTiming.FrameCycles;
            var frameCycle = (int)(bus.CycleCount % VideoTiming.FrameCycles);

            bool inVbl = frameCycle >= VideoTiming.VblStart;

            // SimDebugger.Info($" --> busCycle={bus.CycleCount}, frame#={frameIndex}, frameCycle={frameCycle}, inVbl=={inVbl}\n");

            return inVbl ? (byte)0x00 : (byte)0x80;
        }

        private byte PaddleRead(int index)
        {
            if (paddleTimerStartCycle < 0)
                return 0x00;

            long elapsed = (long)bus.CycleCount - paddleTimerStartCycle;
            long threshold = (long)paddleValues[index] * CyclesPerPaddleUnit;
            return elapsed < threshold ? (byte)0x80 : (byte)0x00;
        }

        /// <summary>
        /// Updates the joystick/paddle state from the host each frame.
        /// pdl0–pdl3 are 0–255;
        /// button0 and button1 map to PB0/PB1.
        /// </summary>
        public void UpdateJoystick(int pdl0, int pdl1, int pdl2, int pdl3, bool button0, bool button1)
        {
            paddleValues[0] = pdl0;
            paddleValues[1] = pdl1;
            paddleValues[2] = pdl2;
            paddleValues[3] = pdl3;

            // PB0 = OpenApple, PB1 = SolidApple — OR'd with keyboard in Emulator
            machineState.State[SoftSwitch.Button0] = button0;
            machineState.State[SoftSwitch.Button1] = button1;
        }

        /// <summary>
        /// Injects a key from the host system.
        /// </summary>
        public void InjectKey(byte ascii)
        {
            machineState.EnqueueKey(ascii);
        }

        public void OpenApple(bool pressed)
        {
            machineState.State[SoftSwitch.OpenApple] = pressed;
        }

        public void SolidApple(bool pressed)
        {
            machineState.State[SoftSwitch.SolidApple] = pressed;
        }
    }
}

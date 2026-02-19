// #define DEBUG_READ
// #define DEBUG_WRITE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using InnoWerks.Processors;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
    public class IOU : ISoftSwitchDevice
    {
        private readonly IBus bus;

        private readonly Memory128k memoryBlocks;

        private readonly MachineState machineState;

        private readonly List<ushort> handlesRead = [
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
            SoftSwitchAddress.PTRIG,
            // SoftSwitchAddress.BUTTON0,
            // SoftSwitchAddress.BUTTON1,
            // SoftSwitchAddress.BUTTON2,
            SoftSwitchAddress.STROBE,

            //
            // IOU
            //
            SoftSwitchAddress.RDIOUDIS
        ];

        private readonly List<ushort> handlesWrite =
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
            // IOU
            //
            SoftSwitchAddress.IOUDISON,
            SoftSwitchAddress.IOUDISOFF,
        ];

        public string Name => $"IOU";

        public IOU(Memory128k memoryBlocks, MachineState machineState, IBus bus)
        {
            ArgumentNullException.ThrowIfNull(machineState, nameof(machineState));
            ArgumentNullException.ThrowIfNull(memoryBlocks, nameof(memoryBlocks));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            this.machineState = machineState;
            this.memoryBlocks = memoryBlocks;
            this.bus = bus;

            bus.AddDevice(this);
        }

        public bool HandlesRead(ushort address) => handlesRead.Contains(address);

        public bool HandlesWrite(ushort address) => handlesWrite.Contains(address);

        public byte Read(ushort address)
        {
#if DEBUG_READ
            if (address != SoftSwitchAddress.KBD && address != SoftSwitchAddress.KBDSTRB && address != SoftSwitchAddress.SPKR && address != SoftSwitchAddress.RD80COL)
            {
                SimDebugger.Info($"Read IOU({address:X4}) [{SoftSwitchAddress.LookupAddress(address)}]\n");
            }
#endif

            switch (address)
            {
                //
                // DISPLAY
                //
                case SoftSwitchAddress.TXTCLR: return machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.TextMode, false, true);
                case SoftSwitchAddress.TXTSET: return machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.TextMode, true, true);

                case SoftSwitchAddress.MIXCLR: return machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.MixedMode, false, true);
                case SoftSwitchAddress.MIXSET: return machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.MixedMode, true, true);

                case SoftSwitchAddress.TXTPAGE1: return machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.Page2, false, true);
                case SoftSwitchAddress.TXTPAGE2: return machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.Page2, true);

                case SoftSwitchAddress.LORES: return machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.HiRes, false);
                case SoftSwitchAddress.HIRES: return machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.HiRes, true);

                // see CLRAN3
                case SoftSwitchAddress.DHIRESON:
                    if (machineState.State[SoftSwitch.IOUDisabled] == true)
                    {
                        return machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.DoubleHiRes, true);
                    }
                    return 0x00;

                // see SETAN3
                case SoftSwitchAddress.DHIRESOFF:
                    if (machineState.State[SoftSwitch.IOUDisabled] == true)
                    {
                        return machineState.HandleReadStateToggle(memoryBlocks, SoftSwitch.DoubleHiRes, false);
                    }
                    return 0x00;

                case SoftSwitchAddress.RDTEXT: return (byte)(machineState.State[SoftSwitch.TextMode] ? 0x80 : 0x00);
                case SoftSwitchAddress.RDMIXED: return (byte)(machineState.State[SoftSwitch.MixedMode] ? 0x80 : 0x00);
                case SoftSwitchAddress.RDPAGE2: return (byte)(machineState.State[SoftSwitch.Page2] ? 0x80 : 0x00);
                case SoftSwitchAddress.RDHIRES: return (byte)(machineState.State[SoftSwitch.HiRes] ? 0x80 : 0x00);
                case SoftSwitchAddress.RDALTCHR:
                    // if (InVerticalBlank() == true)
                    // {
                    //     return 0x00;
                    // }

                    return (byte)(machineState.State[SoftSwitch.AltCharSet] ? 0x80 : 0x00);

                case SoftSwitchAddress.RD80COL:
                    // if (InVerticalBlank() == true)
                    // {
                    //     return 0x00;
                    // }

                    return (byte)(machineState.State[SoftSwitch.EightyColumnMode] ? 0x80 : 0x00);

                case SoftSwitchAddress.RDDHIRES: return (byte)(machineState.State[SoftSwitch.DoubleHiRes] ? 0x80 : 0x00);

                //
                // KEYBOARD
                //

                // this looks like a keyboard here, because it is
                // this is also known as CLR80STORE
                case SoftSwitchAddress.KBD:
                    return machineState.ReadKeyboardData();

                case SoftSwitchAddress.KBDSTRB:
                    machineState.ClearKeyboardStrobe();
                    return 0x00;

                case SoftSwitchAddress.OPENAPPLE: return (byte)(machineState.State[SoftSwitch.OpenApple] ? 0x80 : 0x00);
                case SoftSwitchAddress.SOLIDAPPLE: return (byte)(machineState.State[SoftSwitch.SolidApple] ? 0x80 : 0x00);
                case SoftSwitchAddress.SHIFT: return (byte)(machineState.State[SoftSwitch.ShiftKey] ? 0x80 : 0x00);

                //
                // ANNUNCIATORS
                //
                // handle if IOU == true case
                case SoftSwitchAddress.CLRAN0: machineState.State[SoftSwitch.Annunciator0] = false; return 0x00;
                case SoftSwitchAddress.SETAN0: machineState.State[SoftSwitch.Annunciator0] = true; return 0x00;
                case SoftSwitchAddress.CLRAN1: machineState.State[SoftSwitch.Annunciator1] = false; return 0x00;
                case SoftSwitchAddress.SETAN1: machineState.State[SoftSwitch.Annunciator1] = true; return 0x00;
                case SoftSwitchAddress.CLRAN2: machineState.State[SoftSwitch.Annunciator2] = false; return 0x00;
                case SoftSwitchAddress.SETAN2: machineState.State[SoftSwitch.Annunciator2] = true; return 0x00;
                // case SoftSwitchAddress.CLRAN3:  machineState.State[SoftSwitch.Annunciator3] = false; return 0x00;
                // case SoftSwitchAddress.SETAN3:  machineState.State[SoftSwitch.Annunciator3] = true; return 0x00;

                //
                // VBL
                //
                case SoftSwitchAddress.RDVBLBAR:
                    return InVerticalBlank();

                //
                // CASSETTE
                //
                case SoftSwitchAddress.TAPEOUT:
                    machineState.State[SoftSwitch.TapeOut] = !machineState.State[SoftSwitch.TapeOut];
                    return 0x00;
                case SoftSwitchAddress.TAPEIN: return (byte)(machineState.State[SoftSwitch.TapeIn] ? 0x80 : 0x00);

                //
                // SPEAKER
                //
                case SoftSwitchAddress.SPKR:
                    machineState.State[SoftSwitch.Speaker] = !machineState.State[SoftSwitch.Speaker];
                    return 0x00;

                //
                // PADDLES AND BUTTONS
                //
                case SoftSwitchAddress.PADDLE0: return (byte)(machineState.State[SoftSwitch.Paddle0] ? 0x80 : 0x00);
                case SoftSwitchAddress.PADDLE1: return (byte)(machineState.State[SoftSwitch.Paddle1] ? 0x80 : 0x00);
                case SoftSwitchAddress.PADDLE2: return (byte)(machineState.State[SoftSwitch.Paddle2] ? 0x80 : 0x00);
                case SoftSwitchAddress.PADDLE3: return (byte)(machineState.State[SoftSwitch.Paddle3] ? 0x80 : 0x00);
                case SoftSwitchAddress.PTRIG: return 0x00;

                // case SoftSwitchAddress.BUTTON0: return (byte)(machineState.State[SoftSwitch.Button0] ? 0x80 : 0x00);
                // case SoftSwitchAddress.BUTTON1: return (byte)(machineState.State[SoftSwitch.Button1] ? 0x80 : 0x00);
                // case SoftSwitchAddress.BUTTON2: return (byte)(machineState.State[SoftSwitch.Button2] ? 0x80 : 0x00);

                case SoftSwitchAddress.STROBE:
                    machineState.State[SoftSwitch.GameStrobe] = true;
                    return 0x00;

                //
                // IOU
                //
                case SoftSwitchAddress.RDIOUDIS: return (byte)(machineState.State[SoftSwitch.IOUDisabled] ? 0x80 : 0x00);

                default:
                    throw new ArgumentOutOfRangeException(nameof(address), $"Read {address:X4} is not supported in this device");
            }
        }

        public void Write(ushort address, byte value)
        {
#if DEBUG_WRITE
            if (address != SoftSwitchAddress.KBD && address != SoftSwitchAddress.KBDSTRB && address != SoftSwitchAddress.SPKR)
            {
                SimDebugger.Info($"Write IOU({address:X4}, {value:X2}) [{SoftSwitchAddress.LookupAddress(address)}]\n");
            }
#endif

            switch (address)
            {
                //
                // DISPLAY
                //
                case SoftSwitchAddress.TXTCLR: machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.TextMode, false); break;
                case SoftSwitchAddress.TXTSET: machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.TextMode, true); break;
                case SoftSwitchAddress.MIXCLR: machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.MixedMode, false); break;
                case SoftSwitchAddress.MIXSET: machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.MixedMode, true); break;
                case SoftSwitchAddress.TXTPAGE1: machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.Page2, false); break;
                case SoftSwitchAddress.TXTPAGE2: machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.Page2, true); break;
                case SoftSwitchAddress.LORES: machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.HiRes, false); break;
                case SoftSwitchAddress.HIRES: machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.HiRes, true); break;

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

                case SoftSwitchAddress.CLR80COL: machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.EightyColumnMode, false); break;
                case SoftSwitchAddress.SET80COL: machineState.HandleWriteStateToggle(memoryBlocks, SoftSwitch.EightyColumnMode, true); break;
                case SoftSwitchAddress.CLRALTCHAR: machineState.State[SoftSwitch.AltCharSet] = false; break;
                case SoftSwitchAddress.SETALTCHAR: machineState.State[SoftSwitch.AltCharSet] = true; break;

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
                case SoftSwitchAddress.CLRAN0: machineState.State[SoftSwitch.Annunciator0] = false; break;
                case SoftSwitchAddress.SETAN0: machineState.State[SoftSwitch.Annunciator0] = true; break;
                case SoftSwitchAddress.CLRAN1: machineState.State[SoftSwitch.Annunciator1] = false; break;
                case SoftSwitchAddress.SETAN1: machineState.State[SoftSwitch.Annunciator1] = true; break;
                case SoftSwitchAddress.CLRAN2: machineState.State[SoftSwitch.Annunciator2] = false; break;
                case SoftSwitchAddress.SETAN2: machineState.State[SoftSwitch.Annunciator2] = true; break;
                // case SoftSwitchAddress.CLRAN3:  machineState.State[SoftSwitch.Annunciator3] = false; break;
                // case SoftSwitchAddress.SETAN3:  machineState.State[SoftSwitch.Annunciator3] = true; break;

                //
                // IOU
                //
                case SoftSwitchAddress.IOUDISON: machineState.State[SoftSwitch.IOUDisabled] = true; break;
                case SoftSwitchAddress.IOUDISOFF: machineState.State[SoftSwitch.IOUDisabled] = false; break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(address), $"Write {address:X4} is not supported in this device");
            }
        }

        public void Tick(int cycles) { /* NO-OP */ }

        public void Reset()
        {
            machineState.ResetKeyboard();

            machineState.State[SoftSwitch.TextMode] = true;
            machineState.State[SoftSwitch.IOUDisabled] = true;

            // OpenApple();
            // SolidApple();
        }

        private byte InVerticalBlank()
        {
            var frameIndex = bus.CycleCount / VideoTiming.FrameCycles;
            var frameCycle = (int)(bus.CycleCount % VideoTiming.FrameCycles);

            bool inVbl = frameCycle >= VideoTiming.VblStart;

            // SimDebugger.Info($" --> busCycle={bus.CycleCount}, frame#={frameIndex}, frameCycle={frameCycle}, inVbl=={inVbl}\n");

            return inVbl ? (byte)0x00 : (byte)0x80;
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

#define RENDER_DHIRES_PAGE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection.PortableExecutable;
using InnoWerks.Computers.Apple;
using InnoWerks.Processors;
using InnoWerks.Simulators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA2213 // Disposable fields should be disposed
#pragma warning disable CS0169 // Make field read-only
#pragma warning disable IDE0051 // Make field read-only
#pragma warning disable RCS1169 // Make field read-only
#pragma warning disable CA1823 // Avoid unused private fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable RCS1213 // Remove unused member declaration

namespace InnoWerks.Emulators.AppleIIe
{
    public class Emulator : Game
    {
        //
        // Command line options
        //
        private readonly CliOptions cliOptions;

        //
        // The Apple IIe itself
        //
        private AppleBus appleBus;
        private Memory128k memoryBlocks;
        private MachineState machineState;
        private IOU iou;
        private MMU mmu;
        private Cpu65C02 cpu;

        //
        // debug, etc.
        //
        private CpuTraceBuffer cpuTraceBuffer = new(128);
        private bool cpuPaused;
        private bool stepRequested;
        private readonly HashSet<ushort> breakpoints = [];

        private KeyboardState prevKeyboard;
        private MouseState prevMouse;

        //
        // display renderer
        //
        private Display display;

        //
        // MonoGame stuff
        //
        private readonly GraphicsDeviceManager graphicsDeviceManager;

        //
        // layout stuff
        //
        private HostLayout hostLayout;

        //
        // state stuff
        //
        private KeyboardState previousKeyboardState;
        private double lastTimer;
        private bool flashOn = true;

        public Emulator(CliOptions cliOptions)
        {
            this.cliOptions = cliOptions;

            graphicsDeviceManager = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1160,   // initial width
                PreferredBackBufferHeight = 780,   // initial height
                IsFullScreen = false
            };
            graphicsDeviceManager.ApplyChanges();

            hostLayout = HostLayout.ComputeLayout(
                graphicsDeviceManager.PreferredBackBufferWidth,
                graphicsDeviceManager.PreferredBackBufferHeight
            );

            // Make window resizable
            // Window.AllowUserResizing = true;
            Window.ClientSizeChanged += HandleResize;

            Content.RootDirectory = "Content";

            IsMouseVisible = true;
            IsFixedTimeStep = false;

            graphicsDeviceManager.SynchronizeWithVerticalRetrace = false;
        }

        protected override void Initialize()
        {
            Window.Title = "Apple IIe";

            var mainRom = File.ReadAllBytes("roms/apple2e-16k.rom");
            var diskIIRom = File.ReadAllBytes("roms/DiskII.rom");

            var config = new AppleConfiguration(AppleModel.AppleIIe)
            {
                CpuClass = CpuClass.WDC65C02,
                HasAuxMemory = true,
                Has80Column = true,
                HasLowercase = true,
                RamSize = 128
            };

            machineState = new MachineState();
            memoryBlocks = new Memory128k(machineState);

            appleBus = new AppleBus(config, memoryBlocks, machineState);
            iou = new IOU(memoryBlocks, machineState, appleBus);
            mmu = new MMU(memoryBlocks, machineState, appleBus);

            var disk = new DiskIISlotDevice(appleBus, machineState, diskIIRom);
            disk.GetDrive(1).InsertDisk(cliOptions.Disk1);
            if (string.IsNullOrEmpty(cliOptions.Disk2) == false)
            {
                disk.GetDrive(2).InsertDisk(cliOptions.Disk2);
            }

            cpu = new Cpu65C02(
                appleBus,
                (cpu, programCounter) => { },
                (cpu) => { });

            appleBus.LoadProgramToRom(mainRom);

            // var audit = File.ReadAllBytes("tests/audit.o");
            // appleBus.LoadProgramToRam(audit, 0x6000);

            cpu.Reset();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            display = new Display(GraphicsDevice, cpu, appleBus, memoryBlocks, machineState);

            display.LoadContent(DisplayCharacteristics.AmberText, Content);
        }

        protected override void Update(GameTime gameTime)
        {
            ArgumentNullException.ThrowIfNull(gameTime);

            HandleKeyboardInput();

            var mouse = Mouse.GetState();
            if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
            {
                var cpuTraceEntry = display.HandleTraceClick(hostLayout, cpuTraceBuffer, mouse.Position);

                if (cpuTraceEntry != null)
                {
                    if (breakpoints.Add(cpuTraceEntry.Value.ProgramCounter) == false)
                    {
                        breakpoints.Remove(cpuTraceEntry.Value.ProgramCounter);
                    }
                }
            }
            prevMouse = mouse;

            RunEmulator();

            // Toggle flashing every 100ms - should be about 1 in 10 frames,
            // this would be much better handled by tracking number of cycles,
            // which is closer to frame count and possibly VBL state
            if (gameTime.ElapsedGameTime.TotalMilliseconds - lastTimer >= 100)
            {
                lastTimer = 0;
                flashOn = !flashOn;
            }
            else
            {
                lastTimer = gameTime.ElapsedGameTime.TotalMilliseconds;
            }

            base.Update(gameTime);
        }

        private void RunEmulator()
        {
            if (!cpuPaused)
            {
                RunCpuForFrame();
            }
            else if (stepRequested)
            {
                StepCpuOnce();
                stepRequested = false;
            }
        }

        private void RunCpuForFrame()
        {
            var targetCycles = appleBus.CycleCount + VideoTiming.FrameCycles;
            while (appleBus.CycleCount < targetCycles)
            {
                var nextInstruction = cpu.PeekInstruction();

                if (breakpoints.Contains(nextInstruction.ProgramCounter))
                {
                    cpuPaused = true;
                    break;
                }

                StepCpuOnce();
            }
        }

        private void StepCpuOnce()
        {
            var nextInstruction = cpu.PeekInstruction();

            cpuTraceBuffer.Add(nextInstruction);

            cpu.Step();
        }

        protected override void Draw(GameTime gameTime)
        {
            display.Draw(hostLayout, cpuTraceBuffer, breakpoints, flashOn);
            base.Draw(gameTime);
        }

#if RENDER_DHIRES_PAGE
        private int currentHiresTestPattern;
        private List<(ushort page, bool page2, byte main, byte aux)> hiresPatterns = [
            (0x2000, true, 0xFF, 0xFF),
            (0x2000, true, 0x7F, 0x7F),
            (0x2000, true, 0x55, 0xAA),

            (0x4000, false, 0xFF, 0xFF),
            (0x4000, false, 0x7F, 0x7F),
            (0x4000, false, 0x55, 0xAA),
        ];

        private int currentDHiresTestPattern;
        private List<(ushort page, bool page2, byte main, byte aux)> dhiresPatterns = [
            (0x2000, true, 0x55, 0xAA), // Alternating green/purple stripes (Tier 1)
            (0x2000, true, 0xF0, 0x0F), // Creates single-neighbor ON pixels → Tier 2 (orange/blue)
            (0x2000, true, 0x00, 0xFF), // All Aux ON, Main OFF → purple/green Tier 1
            (0x2000, true, 0xFF, 0x00), // All Main ON, Aux OFF → purple/green Tier 1
            (0x2000, true, 0x55, 0x2A), // Mix → checkerboard of Tier 1 + Tier 2
            (0x2000, true, 0x2A, 0x55), // Tier 1 + Tier 2 → stripes with small black gaps

            (0x4000, false, 0x55, 0xAA), // Alternating green/purple stripes (Tier 1)
            (0x4000, false, 0xF0, 0x0F), // Creates single-neighbor ON pixels → Tier 2 (orange/blue)
            (0x4000, false, 0x00, 0xFF), // All Aux ON, Main OFF → purple/green Tier 1
            (0x4000, false, 0xFF, 0x00), // All Main ON, Aux OFF → purple/green Tier 1
            (0x4000, false, 0x55, 0x2A), // Mix → checkerboard of Tier 1 + Tier 2
            (0x4000, false, 0x2A, 0x55), // Tier 1 + Tier 2 → stripes with small black gaps
        ];
#endif

        private void HandleKeyboardInput()
        {
            var state = Keyboard.GetState();

            foreach (var key in state.GetPressedKeys())
            {
                if (previousKeyboardState.IsKeyUp(key))
                {
                    // Toggle pause
                    if (state.IsKeyDown(Keys.F5) && !prevKeyboard.IsKeyDown(Keys.F5))
                    {
                        cpuPaused = !cpuPaused;
                    }

                    // Single-step
                    if (state.IsKeyDown(Keys.F6) && !prevKeyboard.IsKeyDown(Keys.F6))
                    {
                        if (cpuPaused)
                        {
                            stepRequested = true;
                        }
                    }

                    // Rebooet
                    if (state.IsKeyDown(Keys.F1) && !prevKeyboard.IsKeyDown(Keys.F1))
                    {
                        cpuPaused = true;
                        cpu.Reset();
                        cpuPaused = false;
                    }

                    if (state.IsKeyDown(Keys.F2) && !prevKeyboard.IsKeyDown(Keys.F2))
                    {
                        cpuPaused = true;
                        cpu.Reset();
                        memoryBlocks.Reset();
                        cpuPaused = false;
                    }

#if RENDER_DHIRES_PAGE
                    if (state.IsKeyDown(Keys.F9) && !prevKeyboard.IsKeyDown(Keys.F9))
                    {
                        var (addr, page2, main, aux) = hiresPatterns[currentHiresTestPattern];

                        machineState.State[SoftSwitch.Page2] = !page2;
                        machineState.State[SoftSwitch.TextMode] = false;
                        machineState.State[SoftSwitch.MixedMode] = false;
                        machineState.State[SoftSwitch.HiRes] = true;
                        machineState.State[SoftSwitch.IOUDisabled] = false;
                        machineState.State[SoftSwitch.DoubleHiRes] = false;
                        machineState.State[SoftSwitch.Store80] = true;

                        SimDebugger.Info(
                            "test={0} addr={1:X4} main={2:X2} aux={3:X2} page1={4} text={5} mixed={6} hires={7} ioudis={8} dhires={9} 80col={10}\n",
                            currentHiresTestPattern,
                            addr,
                            main,
                            aux,
                            machineState.State[SoftSwitch.Page2] ? 1 : 0,
                            machineState.State[SoftSwitch.TextMode] ? 1 : 0,
                            machineState.State[SoftSwitch.MixedMode] ? 1 : 0,
                            machineState.State[SoftSwitch.HiRes] ? 1 : 0,
                            machineState.State[SoftSwitch.IOUDisabled] ? 1 : 0,
                            machineState.State[SoftSwitch.DoubleHiRes] ? 1 : 0,
                            machineState.State[SoftSwitch.Store80] ? 1 : 0
                        );

                        memoryBlocks.Remap();

                        currentHiresTestPattern++;
                        currentHiresTestPattern %= hiresPatterns.Count;
                        DoHiresTest(addr, main, aux);
                    }

                    if (state.IsKeyDown(Keys.F10) && !prevKeyboard.IsKeyDown(Keys.F10))
                    {
                        var (addr, page2, main, aux) = dhiresPatterns[currentDHiresTestPattern];

                        machineState.State[SoftSwitch.Page2] = !page2;
                        machineState.State[SoftSwitch.TextMode] = false;
                        machineState.State[SoftSwitch.MixedMode] = false;
                        machineState.State[SoftSwitch.HiRes] = true;
                        machineState.State[SoftSwitch.IOUDisabled] = true;
                        machineState.State[SoftSwitch.DoubleHiRes] = true;
                        machineState.State[SoftSwitch.Store80] = true;

                        SimDebugger.Info(
                            "test={0} addr={1:X4} main={2:X2} aux={3:X2} page1={4} text={5} mixed={6} hires={7} ioudis={8} dhires={9} 80col={10}\n",
                            currentDHiresTestPattern,
                            addr,
                            main,
                            aux,
                            machineState.State[SoftSwitch.Page2] ? 1 : 0,
                            machineState.State[SoftSwitch.TextMode] ? 1 : 0,
                            machineState.State[SoftSwitch.MixedMode] ? 1 : 0,
                            machineState.State[SoftSwitch.HiRes] ? 1 : 0,
                            machineState.State[SoftSwitch.IOUDisabled] ? 1 : 0,
                            machineState.State[SoftSwitch.DoubleHiRes] ? 1 : 0,
                            machineState.State[SoftSwitch.Store80] ? 1 : 0
                        );

                        memoryBlocks.Remap();

                        currentDHiresTestPattern++;
                        currentDHiresTestPattern %= dhiresPatterns.Count;
                        DoDHiresTest(addr, main, aux);
                    }

                    if (state.IsKeyDown(Keys.F11) && !prevKeyboard.IsKeyDown(Keys.F11))
                    {
                        machineState.State[SoftSwitch.Page2] = !machineState.State[SoftSwitch.Page2];
                        machineState.State[SoftSwitch.TextMode] = false;
                        machineState.State[SoftSwitch.MixedMode] = false;
                        machineState.State[SoftSwitch.HiRes] = true;
                        machineState.State[SoftSwitch.IOUDisabled] = true;
                        machineState.State[SoftSwitch.DoubleHiRes] = true;
                        machineState.State[SoftSwitch.Store80] = true;

                        memoryBlocks.Remap();

                        WriteDhiresTest((ushort)(machineState.State[SoftSwitch.Page2] ? 0x4000 : 0x2000));

                        SimDebugger.Info(
                            "test={0} page1={1} text={2} mixed={3} hires={4} ioudis={5} dhires={6} 80col={7}\n",
                            99,
                            machineState.State[SoftSwitch.Page2] ? 1 : 0,
                            machineState.State[SoftSwitch.TextMode] ? 1 : 0,
                            machineState.State[SoftSwitch.MixedMode] ? 1 : 0,
                            machineState.State[SoftSwitch.HiRes] ? 1 : 0,
                            machineState.State[SoftSwitch.IOUDisabled] ? 1 : 0,
                            machineState.State[SoftSwitch.DoubleHiRes] ? 1 : 0,
                            machineState.State[SoftSwitch.Store80] ? 1 : 0
                        );
                    }
#endif

                    if (KeyMapper.TryMap(key, state, out byte ascii))
                    {
                        iou.InjectKey(ascii);
                        break; // Apple II only accepts one key at a time
                    }
                }
            }

            previousKeyboardState = state;
        }

#if RENDER_DHIRES_PAGE
        private void DoDHiresTest(int pageBase, byte aux, byte main)
        {
            for (ushort addr = (ushort)pageBase; addr < pageBase + 0x1000; addr++)
            {
                memoryBlocks.SetMain(addr, 0x00);
                memoryBlocks.SetAux(addr, 0x00);
            }

            // Fill every HIRES byte with 0xFF (all pixels ON)
            for (int y = 0; y < 192; y++)
            {
                int rowAddr =
                    pageBase +
                    ((y & 0x07) << 10) +       // (y % 8) * 0x400
                    (((y >> 3) & 0x07) << 7) + // ((y / 8) % 8) * 0x80
                    ((y >> 6) * 40);           // (y / 64) * 40

                for (int byteCol = 0; byteCol < 40; byteCol++)
                {
                    ushort addr = (ushort)(rowAddr + byteCol);

                    memoryBlocks.SetMain(addr, main);
                    memoryBlocks.SetAux(addr, aux);
                }
            }
        }

        private void DoHiresTest(int pageBase, byte aux, byte main)
        {
            // Fill every HIRES byte with 0xFF (all pixels ON)
            for (int y = 0; y < 192; y++)
            {
                int rowAddr =
                    pageBase +
                    ((y & 0x07) << 10) +       // (y % 8) * 0x400
                    (((y >> 3) & 0x07) << 7) + // ((y / 8) % 8) * 0x80
                    ((y >> 6) * 40);           // (y / 64) * 40

                for (int byteCol = 0; byteCol < 40; byteCol++)
                {
                    ushort addr = (ushort)(rowAddr + byteCol);

                    memoryBlocks.SetMain(addr, main);
                    memoryBlocks.SetAux(addr, aux);
                }
            }
        }

        // Fill first few bytes with test patterns
        void WriteDhiresTest(ushort pageBase)
        {
            for (ushort addr = pageBase; addr < pageBase + 0x2000; addr++)
            {
                memoryBlocks.SetMain(addr, 0x00);
                memoryBlocks.SetAux(addr, 0x00);
            }

            for (ushort addr = pageBase; addr < pageBase + 0x2000; addr += 5)
            {
                // Byte 0 → alternating bits to produce Tier 1 stripes
                memoryBlocks.SetMain((ushort)(addr + 0), 0x55);
                memoryBlocks.SetAux((ushort)(addr + 0), 0xAA);

                // Byte 1 → single neighbor ON → Tier 2 (orange/blue)
                memoryBlocks.SetMain((ushort)(addr + 1), 0x0F);
                memoryBlocks.SetAux((ushort)(addr + 1), 0xF0);

                // Byte 2 → solid Aux ON → Tier 1
                memoryBlocks.SetMain((ushort)(addr + 2), 0x00);
                memoryBlocks.SetAux((ushort)(addr + 2), 0xFF);

                // Byte 3 → solid Main ON → Tier 1
                memoryBlocks.SetMain((ushort)(addr + 3), 0xFF);
                memoryBlocks.SetAux((ushort)(addr + 3), 0x00);

                // Byte 4 → mix for checkerboard Tier 1/2
                memoryBlocks.SetMain((ushort)(addr + 4), 0x2A);
                memoryBlocks.SetAux((ushort)(addr + 4), 0x55);
            }
        }
#endif

        private void HandleResize(object sender, EventArgs e)
        {
            Window.ClientSizeChanged -= HandleResize;

            graphicsDeviceManager.PreferredBackBufferWidth = Window.ClientBounds.Width;
            graphicsDeviceManager.PreferredBackBufferHeight = Window.ClientBounds.Height;

            Debug.WriteLine($"{Window.ClientBounds.Width} x {Window.ClientBounds.Height}");

            hostLayout = HostLayout.ComputeLayout(
                Window.ClientBounds.Width,
                Window.ClientBounds.Height
            );

            graphicsDeviceManager.ApplyChanges();

            Window.ClientSizeChanged += HandleResize;
        }
    }
}

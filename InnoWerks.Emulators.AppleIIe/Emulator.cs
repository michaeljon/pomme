using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection.PortableExecutable;
using InnoWerks.Assemblers;
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
        // Constants
        private const int AppleClockSpeed = 1020484;
        private const float FramesPerSecond = 59.94f;

        private long totalCyclesExecuted;

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

        //
        // display renderer
        //
        private Display display;

        //
        // MonoGame stuff
        //
        private readonly GraphicsDeviceManager graphicsDeviceManager;

        private readonly AppleIIAudioSource audioSource;
        private readonly AudioRenderer audioRenderer;

        //
        // layout stuff
        //
        private HostLayout hostLayout;

        //
        // state stuff
        //
        private KeyboardState previousKeyboardState;
        private MouseState prevMouse;
        private bool appleCapsLock = true;

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

            hostLayout = HostLayout.ComputeLayout(
                graphicsDeviceManager.PreferredBackBufferWidth,
                graphicsDeviceManager.PreferredBackBufferHeight
            );

            Content.RootDirectory = "Content";

            // Make window resizable
            // Window.AllowUserResizing = true;
            Window.ClientSizeChanged += HandleResize;
            Window.TextInput += OnTextInput;

            IsMouseVisible = true;
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / FramesPerSecond);

            audioSource = new();
            audioRenderer = new();

            graphicsDeviceManager.SynchronizeWithVerticalRetrace = true;
            graphicsDeviceManager.ApplyChanges();
        }

        protected override void Initialize()
        {
            Window.Title = "Rotten Apple IIe";

            var mainRom = File.ReadAllBytes("roms/Apple2e_Enhanced.rom");
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

            // see if we want to do anything here with the emulator keys
            KeyboardState currentState = Keyboard.GetState();
            UpdateHostControls(currentState, previousKeyboardState);

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

            // send other keys on to the "computer"
            UpdateKeyboard(currentState, previousKeyboardState);
            audioRenderer.UpdateAudio(appleBus.CycleCount, audioSource);

            previousKeyboardState = currentState;

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

            bool previousSpeakerState = machineState.State[SoftSwitch.Speaker];

            cpuTraceBuffer.Add(nextInstruction);
            cpu.Step();

            // Check for toggles
            if (machineState.State[SoftSwitch.Speaker] != previousSpeakerState)
            {
                audioSource.TouchSpeaker(appleBus.CycleCount);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            display.Draw(hostLayout, cpuTraceBuffer, breakpoints, flashOn);
            base.Draw(gameTime);
        }

        private void UpdateHostControls(KeyboardState currentState, KeyboardState previousState)
        {
            // Helper functions
            bool IsJustPressed(Keys key) => currentState.IsKeyDown(key) && !previousState.IsKeyDown(key);
            bool isCtrlDown = currentState.IsKeyDown(Keys.LeftControl) || currentState.IsKeyDown(Keys.RightControl);
            bool isShiftDown = currentState.IsKeyDown(Keys.LeftShift) || currentState.IsKeyDown(Keys.RightShift);

            if (isCtrlDown)
            {
                if (IsJustPressed(Keys.F1))
                {
                    cpuPaused = true;
                    cpu.Reset();

                    audioRenderer.Clear();
                    audioSource.Clear();
                    cpuPaused = false;
                }

                if (IsJustPressed(Keys.F2))
                {
                    cpuPaused = true;
                    cpu.Reset();
                    memoryBlocks.Reset();

                    audioRenderer.Clear();
                    audioSource.Clear();
                    cpuPaused = false;
                }

                var state = Keyboard.GetState();
                foreach (var key in state.GetPressedKeys())
                {
                    if (KeyMapper.TryMap(key, state, out byte ascii))
                    {
                        // 3. Handle Ctrl+Letter combinations (ASCII 1 to 26)
                        // The OS automatically generates these when holding Ctrl!
                        if ((ascii & 0x1f) >= 1 && (ascii & 0x1f) <= 26)
                        {
                            iou.InjectKey((byte)(ascii & 0x1f));
                        }
                    }
                }
            }

            if (IsJustPressed(Keys.F5))
            {
                cpuPaused = !cpuPaused;
            }

            if (IsJustPressed(Keys.F6))
            {
                if (cpuPaused)
                {
                    stepRequested = true;
                }
            }

            // F11: Toggle Fullscreen
            if (IsJustPressed(Keys.F11))
            {
                graphicsDeviceManager.ToggleFullScreen();
            }
        }

        public void UpdateKeyboard(KeyboardState currentState, KeyboardState previousState)
        {
            bool IsJustPressed(Keys key) => currentState.IsKeyDown(key) && !previousState.IsKeyDown(key);

            // --- TOGGLE CAPS LOCK ---
            // You can bind this to the physical CapsLock key, or something safe like F6
            if (IsJustPressed(Keys.CapsLock))
            {
                appleCapsLock = !appleCapsLock;

                // Optional: Trigger a tiny UI popup or sound so the user knows it changed
                // uiManager.ShowNotification("Caps Lock: " + (appleCapsLock ? "ON" : "OFF"));
            }

            // --- ARROW KEYS ---
            if (IsJustPressed(Keys.Left)) iou.InjectKey(0x88);
            if (IsJustPressed(Keys.Right)) iou.InjectKey(0x95);
            if (IsJustPressed(Keys.Down)) iou.InjectKey(0x8A);
            if (IsJustPressed(Keys.Up)) iou.InjectKey(0x8B);

            // --- OPEN / SOLID APPLE ---
            iou.OpenApple(currentState.IsKeyDown(Keys.LeftAlt));
            iou.SolidApple(currentState.IsKeyDown(Keys.RightAlt));
        }

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

        private void OnTextInput(object sender, TextInputEventArgs e)
        {
            char c = e.Character;

            // 2. Handle Printable Characters (ASCII 32 to 126)
            if (c >= 32 && c <= 126)
            {
                // Apply Emulated Caps Lock (Only shift 'a' through 'z')
                if (appleCapsLock && c >= 'a' && c <= 'z')
                {
                    c = (char)(c - 32); // Convert to uppercase ASCII
                }

                byte appleAscii = (byte)((c & 0x7F) | 0x80);
                iou.InjectKey(appleAscii);
            }
            // 3. Handle Ctrl+Letter combinations (ASCII 1 to 26)
            // The OS automatically generates these when holding Ctrl!
            else if (c >= 1 && c <= 26)
            {
                // c is already the correct 1-26 value. Just add the Apple II high-bit.
                byte appleCtrlAscii = (byte)(c | 0x80);
                iou.InjectKey(appleCtrlAscii);
            }
            else
            {
                iou.InjectKey((byte)c);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using InnoWerks.Computers.Apple;
using InnoWerks.Simulators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1823 // Avoid unused private fields
#pragma warning disable CA2213 // Disposable fields should be disposed
#pragma warning disable CS0169 // Make field read-only
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Make field read-only
#pragma warning disable RCS1169 // Make field read-only
#pragma warning disable RCS1213 // Remove unused member declaration

namespace InnoWerks.Emulators.AppleIIe
{
    public class Emulator : Game
    {
        private long totalCyclesExecuted;

        //
        // Command line options
        //
        private readonly EmulatorConfiguration emulatorConfiguration;

        //
        // The Apple IIe itself
        //
        private Computer computer;
        private MouseSlotDevice mouseDevice;
        private MockingboardSlotDevice mockingboardDevice;

        //
        // debug, etc.
        //
        private CpuTraceBuffer cpuTraceBuffer = new(48);
        private bool cpuPaused;
        private bool stepRequested;
        private readonly HashSet<ushort> breakpoints = [
        ];

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
        private bool joystickButton0;
        private bool joystickButton1;
        private bool appleCapsLock = true;

        private double lastTimer;
        private bool flashOn = true;

        public Emulator(EmulatorConfiguration emulatorConfiguration)
        {
            ArgumentNullException.ThrowIfNull(emulatorConfiguration);
            this.emulatorConfiguration = emulatorConfiguration;

            graphicsDeviceManager = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1160,   // initial width
                PreferredBackBufferHeight = 780,   // initial height
                IsFullScreen = false
            };

            hostLayout = HostLayout.ComputeLayout(
                emulatorConfiguration.ShowInternals,
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
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / Computer.FramesPerSecond);

            audioSource = new();
            audioRenderer = new();

            graphicsDeviceManager.SynchronizeWithVerticalRetrace = true;
            graphicsDeviceManager.ApplyChanges();
        }

        protected override void Initialize()
        {
            Window.Title = "Une pomme pourrie - Apple IIe";

            var mainRom = File.ReadAllBytes("roms/Apple2e_Enhanced.rom");

            computer = new(emulatorConfiguration.AppleModel, mainRom);

            if (emulatorConfiguration.NoSlotClock)
            {
                computer.AddNoSlotClock();
            }

            foreach (var slot in emulatorConfiguration.Slots)
            {
                switch (slot.DeviceType)
                {
                    case DeviceType.Mouse:
                        mouseDevice = computer.AddMouse(slot.SlotNumber);
                        break;

                    case DeviceType.HardDisk:
                        var hdDevice = slot as ConfiguredHardDisk;
                        var hardDrive = computer.AddGenericBlockDevice(slot.SlotNumber);

                        if (hdDevice.DriveOne != null && string.IsNullOrEmpty(hdDevice.DriveOne.Image) == false)
                        {
                            hardDrive.InsertDisk(hdDevice.DriveOne.Image, 0);
                        }

                        if (hdDevice.DriveTwo != null && string.IsNullOrEmpty(hdDevice.DriveTwo.Image) == false)
                        {
                            hardDrive.InsertDisk(hdDevice.DriveTwo.Image, 1);
                        }

                        if (hdDevice.DriveThree != null && string.IsNullOrEmpty(hdDevice.DriveThree.Image) == false)
                        {
                            hardDrive.InsertDisk(hdDevice.DriveThree.Image, 2);
                        }

                        if (hdDevice.DriveFour != null && string.IsNullOrEmpty(hdDevice.DriveFour.Image) == false)
                        {
                            hardDrive.InsertDisk(hdDevice.DriveFour.Image, 3);
                        }

                        break;

                    case DeviceType.DiskII:
                        var diskiiDevice = slot as ConfiguredDiskIIDevice;
                        var diskiiController = computer.AddDiskIIController(slot.SlotNumber);

                        if (diskiiDevice.DriveOne != null && string.IsNullOrEmpty(diskiiDevice.DriveOne.Image) == false)
                        {
                            diskiiController.InsertDisk(0, diskiiDevice.DriveOne.Image);
                        }

                        if (diskiiDevice.DriveTwo != null && string.IsNullOrEmpty(diskiiDevice.DriveTwo.Image) == false)
                        {
                            diskiiController.InsertDisk(1, diskiiDevice.DriveTwo.Image);
                        }

                        break;

                    case DeviceType.ThunderClock:
                        _ = computer.AddThunderclock(slot.SlotNumber);
                        break;

                    case DeviceType.Mockingboard:
                        mockingboardDevice = computer.AddMockingboard(slot.SlotNumber);
                        break;
                }
            }

            // load any breakpoints
            foreach (var bp in emulatorConfiguration.Breakpoints)
            {
                breakpoints.Add(bp);
            }

            computer.Build();
            computer.Reset();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            display = new Display(GraphicsDevice, computer, emulatorConfiguration.ShowInternals);

            display.LoadContent(emulatorConfiguration.ResolveMonochromeColor(), Content);
            display.ConfigureToolbar(computer.SlotDevices);
        }

        private static Color? ResolveMonochromeColor(string monochrome) =>
            monochrome?.ToLowerInvariant() switch
            {
                "green" => DisplayCharacteristics.GreenText,
                "amber" => DisplayCharacteristics.AmberText,
                "white" => DisplayCharacteristics.WhiteText,
                _ => null
            };

        protected override void Update(GameTime gameTime)
        {
            ArgumentNullException.ThrowIfNull(gameTime);

            var mouse = Mouse.GetState();
            var leftButtonJustPressed = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
            var inAppleDisplay = hostLayout.AppleDisplay.Contains(mouse.Position);

            if (mouseDevice != null)
            {
                mouseDevice.UpdateFromHost(
                    mouse.X, mouse.Y, mouse.LeftButton == ButtonState.Pressed,
                    hostLayout.AppleDisplay.X, hostLayout.AppleDisplay.Y,
                    hostLayout.AppleDisplay.Width, hostLayout.AppleDisplay.Height);

                // Level detection: fires the first frame the button is down in the Apple
                // display area.  Once captured, IsMouseCaptured gates this off.
                if (mouse.LeftButton == ButtonState.Pressed && inAppleDisplay && !mouseDevice.IsMouseCaptured)
                {
                    mouseDevice.Capture();
                    IsMouseVisible = false;
                }
            }

            if (leftButtonJustPressed && inAppleDisplay == false)
            {
                if (mouseDevice?.IsMouseCaptured == true)
                {
                    mouseDevice.Release();
                    IsMouseVisible = true;
                }

                // check toolbar clicks first
                if (hostLayout.Toolbar.Contains(mouse.Position))
                {
                    HandleToolbarClick(mouse.Position);
                }
                else if (emulatorConfiguration.ShowInternals == true)
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
            }

            prevMouse = mouse;

            UpdateGamepad(GamePad.GetState(PlayerIndex.One));

            // see if we want to do anything here with the emulator keys
            KeyboardState currentState = Keyboard.GetState();
            UpdateHostControls(currentState, previousKeyboardState);

            RunEmulator();

            // Toggle flashing every ~250ms (close to the real Apple IIe
            // flash rate of about 1.9 Hz)
            lastTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if (lastTimer >= 250)
            {
                lastTimer -= 250;
                flashOn = flashOn == false;
            }

            // send other keys on to the "computer"
            UpdateKeyboard(currentState, previousKeyboardState);
            audioRenderer.UpdateAudio(computer.CycleCount, audioSource, mockingboardDevice);

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
            var targetCycles = computer.CycleCount + Computer.FrameCycles;

            while (computer.CycleCount < targetCycles)
            {
                if (breakpoints.Contains(computer.Processor.Registers.ProgramCounter))
                {
                    cpuPaused = true;
                    break;
                }

                StepCpuOnce();
            }
        }

        private void StepCpuOnce()
        {
            if (emulatorConfiguration.ShowInternals == true)
            {
                var nextInstruction = computer.Processor.PeekInstruction();
                cpuTraceBuffer.Add(nextInstruction);
            }

            var previousSpeakerState = computer.MachineState.State[SoftSwitch.Speaker];

            computer.Processor.Step();

            // Check for toggles
            if (computer.MachineState.State[SoftSwitch.Speaker] != previousSpeakerState)
            {
                audioSource.TouchSpeaker(computer.CycleCount);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            display.Draw(hostLayout, cpuTraceBuffer, breakpoints, flashOn);
            base.Draw(gameTime);
        }

        private void FlushAllDisks()
        {
            foreach (var device in computer.SlotDevices)
            {
                if (device is DiskIISlotDevice diskDevice)
                {
                    diskDevice.FlushAll();
                }
            }
        }

        private void HandleToolbarClick(Point mousePos)
        {
            var (action, device, driveNumber) = display.HandleToolbarClick(mousePos);

            switch (action)
            {
                case ToolbarAction.Reset:
                    PerformBootSequence(coldBoot: false);
                    break;

                case ToolbarAction.Reboot:
                    PerformBootSequence(coldBoot: true);
                    break;

                case ToolbarAction.DiskEject:
                    device?.EjectDisk(driveNumber);
                    break;

                case ToolbarAction.DiskInsert:
                    {
                        using var nfd = new NativeFileDialogNET.NativeFileDialog();
                        using var dialog = nfd
                            .SelectFile()
                            .AddFilter("Disk Images", "dsk,po,2mg")
                            .AddFilter("All Files", "*");

                        var result = dialog.Open(out string[] selectedFiles, "disks");
                        if (result == NativeFileDialogNET.DialogResult.Okay && selectedFiles?.Length > 0)
                        {
                            device?.InsertDisk(driveNumber, selectedFiles[0]);
                        }
                    }
                    break;
            }
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
                    PerformBootSequence(coldBoot: false);
                }

                if (IsJustPressed(Keys.F2))
                {
                    PerformBootSequence(coldBoot: true);
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
                            computer.IOU.InjectKey((byte)(ascii & 0x1f));
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

            // F12: Toggle mouse capture
            if (IsJustPressed(Keys.F12) && mouseDevice != null)
            {
                if (mouseDevice.IsMouseCaptured)
                {
                    mouseDevice.Release();
                    IsMouseVisible = true;
                }
                else
                {
                    mouseDevice.Capture();
                    IsMouseVisible = false;
                }
            }
        }

        private void UpdateGamepad(GamePadState current)
        {
            if (!current.IsConnected)
                return;

            int pdl1;
            int pdl3;

            if (emulatorConfiguration.JoystickInverted)
            {
                pdl1 = (int)((-current.ThumbSticks.Left.Y + 1.0f) / 2.0f * 255);
                pdl3 = (int)((-current.ThumbSticks.Right.Y + 1.0f) / 2.0f * 255);
            }
            else
            {
                pdl1 = (int)((current.ThumbSticks.Left.Y + 1.0f) / 2.0f * 255);
                pdl3 = (int)((current.ThumbSticks.Right.Y + 1.0f) / 2.0f * 255);
            }
            // if ! inverted

            // Map left thumbstick axes (-1..1) to Apple II paddle range (0..255)
            int pdl0 = (int)((current.ThumbSticks.Left.X + 1.0f) / 2.0f * 255);
            int pdl2 = (int)((current.ThumbSticks.Right.X + 1.0f) / 2.0f * 255);

            joystickButton0 = current.Buttons.A == ButtonState.Pressed;
            joystickButton1 = current.Buttons.B == ButtonState.Pressed;

            computer.IOU.UpdateJoystick(pdl0, pdl1, pdl2, pdl3, joystickButton0, joystickButton1);
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
            if (IsJustPressed(Keys.Left)) computer.IOU.InjectKey(0x88);
            if (IsJustPressed(Keys.Right)) computer.IOU.InjectKey(0x95);
            if (IsJustPressed(Keys.Down)) computer.IOU.InjectKey(0x8A);
            if (IsJustPressed(Keys.Up)) computer.IOU.InjectKey(0x8B);

            // --- OPEN / SOLID APPLE (keyboard OR joystick button) ---
            computer.IOU.OpenApple(currentState.IsKeyDown(Keys.LeftAlt) || joystickButton0);
            computer.IOU.SolidApple(currentState.IsKeyDown(Keys.RightAlt) || joystickButton1);
        }

        private void HandleResize(object sender, EventArgs e)
        {
            Window.ClientSizeChanged -= HandleResize;

            graphicsDeviceManager.PreferredBackBufferWidth = Window.ClientBounds.Width;
            graphicsDeviceManager.PreferredBackBufferHeight = Window.ClientBounds.Height;

            Debug.WriteLine($"{Window.ClientBounds.Width} x {Window.ClientBounds.Height}");

            hostLayout = HostLayout.ComputeLayout(
                emulatorConfiguration.ShowInternals,
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
                computer.IOU.InjectKey(appleAscii);
            }
            // 3. Handle Ctrl+Letter combinations (ASCII 1 to 26)
            // The OS automatically generates these when holding Ctrl!
            else if (c >= 1 && c <= 26)
            {
                // c is already the correct 1-26 value. Just add the Apple II high-bit.
                byte appleCtrlAscii = (byte)(c | 0x80);
                computer.IOU.InjectKey(appleCtrlAscii);
            }
            else
            {
                computer.IOU.InjectKey((byte)c);
            }
        }

        private void PerformBootSequence(bool coldBoot)
        {
            cpuPaused = true;
            FlushAllDisks();
            computer.Reset(coldBoot);
            audioRenderer.Clear();
            audioSource.Clear();
            cpuPaused = false;

            // --- OPEN / SOLID APPLE (keyboard OR joystick button) ---
            var currentState = Keyboard.GetState();

            computer.IOU.OpenApple(currentState.IsKeyDown(Keys.LeftAlt) || joystickButton0);
            computer.IOU.SolidApple(currentState.IsKeyDown(Keys.RightAlt) || joystickButton1);
        }
    }
}

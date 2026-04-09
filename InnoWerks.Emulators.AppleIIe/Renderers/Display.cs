using System;
using System.Collections.Generic;
using InnoWerks.Computers.Apple;
using InnoWerks.Simulators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

#pragma warning disable CA1859 // Use concrete types when possible for improved performance

namespace InnoWerks.Emulators.AppleIIe
{
    public class Display : IDisposable
    {
        //
        // MonoGame stuff
        //
        private readonly GraphicsDevice graphicsDevice;
        private SpriteBatch spriteBatch;
        private Texture2D whitePixel;
        private RenderTarget2D appleTarget;

        private readonly Computer computer;

        private TextModeRenderer textPage1Renderer;
        private TextModeRenderer textPage2Renderer;
        private LoresRenderer loresPage1Renderer;
        private LoresRenderer loresPage2Renderer;
        private HiresRenderer hiresPage1Renderer;
        private HiresRenderer hiresPage2Renderer;

        private TextModeRenderer text80Page1Renderer;
        private TextModeRenderer text80Page2Renderer;
        private LoresRenderer dloresPage1Renderer;
        private LoresRenderer dloresPage2Renderer;
        private DhiresRenderer dhiresPage1Renderer;
        private DhiresRenderer dhiresPage2Renderer;

        private Renderer currentTextRenderer;
        private Renderer currentGraphicsRenderer;

        private DebugToolsRenderer debugToolsRenderer;
        private ToolbarRenderer toolbarRenderer;

        private bool hiresMode;
        private bool dhiresMode;

        private readonly bool showInternals;

        private bool disposed;

        public Display(
            GraphicsDevice graphicsDevice,
            Computer computer,
            bool showInternals)
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            ArgumentNullException.ThrowIfNull(computer);

            this.graphicsDevice = graphicsDevice;
            this.computer = computer;
            this.showInternals = showInternals;
        }

        public void LoadContent(Color? monochromeColor, ContentManager contentManager)
        {
            ArgumentNullException.ThrowIfNull(contentManager);

            var textColor = monochromeColor ?? DisplayCharacteristics.AmberText;

            spriteBatch = new SpriteBatch(graphicsDevice);

            appleTarget = new RenderTarget2D(
                            graphicsDevice,
                            DisplayCharacteristics.HiresAppleWidth,
                            DisplayCharacteristics.AppleDisplayHeight,
                            false,
                            SurfaceFormat.Color,
                            DepthFormat.None,
                            0,
                            RenderTargetUsage.PreserveContents
                        );

            whitePixel = new Texture2D(graphicsDevice, 1, 1);
            whitePixel.SetData([Color.White]);

            textPage1Renderer = new(graphicsDevice, computer, false, 1, textColor);
            textPage2Renderer = new(graphicsDevice, computer, false, 2, textColor);
            loresPage1Renderer = new(graphicsDevice, computer, false, 1, monochromeColor);
            loresPage2Renderer = new(graphicsDevice, computer, false, 2, monochromeColor);
            hiresPage1Renderer = new(graphicsDevice, computer, 1, monochromeColor);
            hiresPage2Renderer = new(graphicsDevice, computer, 2, monochromeColor);

            text80Page1Renderer = new(graphicsDevice, computer, true, 1, textColor);
            text80Page2Renderer = new(graphicsDevice, computer, true, 2, textColor);
            dloresPage1Renderer = new(graphicsDevice, computer, true, 1, monochromeColor);
            dloresPage2Renderer = new(graphicsDevice, computer, true, 2, monochromeColor);
            dhiresPage1Renderer = new(graphicsDevice, computer, 1, monochromeColor);
            dhiresPage2Renderer = new(graphicsDevice, computer, 2, monochromeColor);

            currentTextRenderer = textPage1Renderer;
            currentGraphicsRenderer = loresPage1Renderer;

            // debug stuff is always white
            if (showInternals == true)
            {
                debugToolsRenderer = new(graphicsDevice, computer, contentManager, Color.White);
            }

            toolbarRenderer = new ToolbarRenderer(graphicsDevice);
            toolbarRenderer.LoadContent(contentManager);
        }

        public void ConfigureToolbar(ISlotDevice[] slotDevices)
        {
            toolbarRenderer.ConfigureItems(slotDevices);
        }

        public void Draw(
            HostLayout hostLayout,
            CpuTraceBuffer cpuTraceBuffer,
            HashSet<ushort> breakpoints,
            bool flashOn)
        {
            ArgumentNullException.ThrowIfNull(hostLayout);
            ArgumentNullException.ThrowIfNull(cpuTraceBuffer);
            ArgumentNullException.ThrowIfNull(breakpoints);

            // render the apple ii surface
            DrawAppleRegion(hostLayout, appleTarget, flashOn);

            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.LinearClamp);

            // draw the apple target onto the application surface
            spriteBatch.Draw(
                appleTarget,
                hostLayout.AppleDisplay,
                new Rectangle(0, 0, DisplayCharacteristics.HiresAppleWidth, DisplayCharacteristics.AppleDisplayHeight),
                Color.White);

            // draw toolbar
            toolbarRenderer.Draw(spriteBatch, hostLayout);

            // draw debug windows onto the surface
            if (showInternals == true)
            {
                debugToolsRenderer.Draw(spriteBatch, hostLayout, cpuTraceBuffer, breakpoints);
            }

            spriteBatch.End();
        }

        private void DrawAppleRegion(HostLayout hostLayout, RenderTarget2D appleTarget, bool flashOn)
        {
            //
            // draw the content to the off-screen buffer
            //
            graphicsDevice.SetRenderTarget(appleTarget);
            graphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                samplerState: SamplerState.PointClamp);

            var page2 = computer.MachineState.State[SoftSwitch.Page2] == true && computer.MachineState.State[SoftSwitch.Store80] == false;

            hiresMode = computer.MachineState.State[SoftSwitch.DoubleHiRes] == false;
            dhiresMode = computer.MachineState.State[SoftSwitch.EightyColumnMode] == true &&
                         computer.MachineState.State[SoftSwitch.DoubleHiRes] == true &&
                         computer.MachineState.State[SoftSwitch.HiRes] == true;

            currentTextRenderer
                    = computer.MachineState.State[SoftSwitch.EightyColumnMode]
                            ? page2
                                    ? text80Page2Renderer : text80Page1Renderer
                            : page2
                                    ? textPage2Renderer : textPage1Renderer;

            currentGraphicsRenderer
                    = computer.MachineState.State[SoftSwitch.EightyColumnMode] && computer.MachineState.State[SoftSwitch.DoubleHiRes]
                            ? computer.MachineState.State[SoftSwitch.HiRes]
                                    ? page2
                                            ? dhiresPage2Renderer : dhiresPage1Renderer
                                    : page2
                                            ? dloresPage2Renderer : dloresPage1Renderer
                            : computer.MachineState.State[SoftSwitch.HiRes]
                                    ? page2
                                            ? hiresPage2Renderer : hiresPage1Renderer
                                    : page2
                                            ? loresPage2Renderer : loresPage1Renderer;

            if (computer.MachineState.State[SoftSwitch.TextMode])
            {
                currentTextRenderer.Draw(spriteBatch, new Rectangle(0, 0, 560, 192), 0, 192);
            }
            else if (computer.MachineState.State[SoftSwitch.MixedMode])
            {
                currentGraphicsRenderer.Draw(spriteBatch, new Rectangle(0, 0, 560, 192 - 4 * DisplayCharacteristics.AppleCellHeight), 0, 192 - 4 * DisplayCharacteristics.AppleCellHeight);
                currentTextRenderer.Draw(spriteBatch, new Rectangle(0, 0, 560, 4 * DisplayCharacteristics.AppleCellHeight), 192 - 4 * DisplayCharacteristics.AppleCellHeight, 4 * DisplayCharacteristics.AppleCellHeight);
            }
            else
            {
                currentGraphicsRenderer.Draw(spriteBatch, new Rectangle(0, 0, 560, 192), 0, 192);
            }

            spriteBatch.End();
        }

        public CpuTraceEntry? HandleTraceClick(HostLayout hostLayout, CpuTraceBuffer cpuTraceBuffer, Point mousePos)
        {
            return debugToolsRenderer.HandleTraceClick(hostLayout, cpuTraceBuffer, mousePos);
        }

        public (ToolbarAction action, DiskIISlotDevice device, int driveNumber) HandleToolbarClick(Point mousePos)
        {
            return toolbarRenderer.HandleClick(mousePos);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed == true)
            {
                return;
            }

            if (disposing)
            {
                spriteBatch?.Dispose();
                whitePixel?.Dispose();
                appleTarget?.Dispose();

                textPage1Renderer?.Dispose();
                textPage2Renderer?.Dispose();
                loresPage1Renderer?.Dispose();
                loresPage2Renderer?.Dispose();
                hiresPage1Renderer?.Dispose();
                hiresPage2Renderer?.Dispose();

                text80Page1Renderer?.Dispose();
                text80Page2Renderer?.Dispose();
                dloresPage1Renderer?.Dispose();
                dloresPage2Renderer?.Dispose();
                dhiresPage1Renderer?.Dispose();
                dhiresPage2Renderer?.Dispose();

                debugToolsRenderer?.Dispose();
                toolbarRenderer?.Dispose();
            }

            disposed = true;
        }
    }
}

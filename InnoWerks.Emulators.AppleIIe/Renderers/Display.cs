using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Reflection.PortableExecutable;
using InnoWerks.Computers.Apple;
using InnoWerks.Processors;
using InnoWerks.Simulators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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
        private readonly Cpu6502Core cpu;
        private readonly IBus bus;
        private readonly Memory128k memoryBlocks;
        private readonly MachineState machineState;

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

        private bool hiresMode;
        private bool dhiresMode;

        private bool disposed;

        public Display(
            GraphicsDevice graphicsDevice,
            Cpu6502Core cpu,
            IBus bus,
            Memory128k memoryBlocks,
            MachineState machineState)
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            ArgumentNullException.ThrowIfNull(cpu);
            ArgumentNullException.ThrowIfNull(bus);
            ArgumentNullException.ThrowIfNull(memoryBlocks);
            ArgumentNullException.ThrowIfNull(machineState);

            this.graphicsDevice = graphicsDevice;
            this.machineState = machineState;
            this.cpu = cpu;
            this.bus = bus;
            this.memoryBlocks = memoryBlocks;
        }

        public void LoadContent(Color textColor, ContentManager contentManager)
        {
            ArgumentNullException.ThrowIfNull(contentManager);

            spriteBatch = new SpriteBatch(graphicsDevice);

            whitePixel = new Texture2D(graphicsDevice, 1, 1);
            whitePixel.SetData([Color.White]);

            textPage1Renderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, false, 1, textColor);
            textPage2Renderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, false, 2, textColor);
            loresPage1Renderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, false, 1);
            loresPage2Renderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, false, 2);
            hiresPage1Renderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, 1);
            hiresPage2Renderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, 2);

            text80Page1Renderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, true, 1, textColor);
            text80Page2Renderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, true, 2, textColor);
            dloresPage1Renderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, true, 1);
            dloresPage2Renderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, true, 2);
            dhiresPage1Renderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, 1);
            dhiresPage2Renderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, 2);

            currentTextRenderer = textPage1Renderer;
            currentGraphicsRenderer = loresPage1Renderer;

            // debug stuff, for now, this becomes an option later
            debugToolsRenderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, contentManager, textColor);
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

            using var appleTarget = new RenderTarget2D(
                graphicsDevice,

                // machineState.State[SoftSwitch.EightyColumnMode] || machineState.State[SoftSwitch.DoubleHiRes] ?
                //     DisplayCharacteristics.HiresAppleWidth :
                //     DisplayCharacteristics.LoresAppleWidth,

                DisplayCharacteristics.HiresAppleWidth,
                DisplayCharacteristics.AppleDisplayHeight,
                false,
                SurfaceFormat.Color,
                DepthFormat.None,
                0,
                RenderTargetUsage.PreserveContents
            );

            // render the apple ii surface
            DrawAppleRegion(hostLayout, appleTarget, flashOn);

            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.LinearClamp);

            // draw the apple target onto the application surface
            spriteBatch.Draw(appleTarget, hostLayout.AppleDisplay, Color.White);

            // draw debug windows onto the surface
            debugToolsRenderer.Draw(spriteBatch, hostLayout, cpuTraceBuffer, breakpoints);

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

            var page2 = machineState.State[SoftSwitch.Page2] == true && machineState.State[SoftSwitch.Store80] == false;

            hiresMode = machineState.State[SoftSwitch.DoubleHiRes] == false;
            dhiresMode = machineState.State[SoftSwitch.EightyColumnMode] == true &&
                         machineState.State[SoftSwitch.DoubleHiRes] == true &&
                         machineState.State[SoftSwitch.HiRes] == true;

            currentTextRenderer
                    = machineState.State[SoftSwitch.EightyColumnMode]
                            ? page2
                                    ? text80Page2Renderer : text80Page1Renderer
                            : page2
                                    ? textPage2Renderer : textPage1Renderer;

            currentGraphicsRenderer
                    = machineState.State[SoftSwitch.Store80] && machineState.State[SoftSwitch.DoubleHiRes]
                            ? machineState.State[SoftSwitch.HiRes]
                                    ? page2
                                            ? dhiresPage2Renderer : dhiresPage1Renderer
                                    : page2
                                            ? dloresPage2Renderer : dloresPage1Renderer
                            : machineState.State[SoftSwitch.HiRes]
                                    ? page2
                                            ? hiresPage2Renderer : hiresPage1Renderer
                                    : page2
                                            ? loresPage2Renderer : loresPage1Renderer;

            if (machineState.State[SoftSwitch.TextMode])
            {
                currentTextRenderer.Draw(spriteBatch, 0, 192);
            }
            else if (machineState.State[SoftSwitch.MixedMode])
            {
                currentGraphicsRenderer.Draw(spriteBatch, 0, 192 - 4 * DisplayCharacteristics.AppleCellHeight);
                currentTextRenderer.Draw(spriteBatch, 192 - 4 * DisplayCharacteristics.AppleCellHeight, 4 * DisplayCharacteristics.AppleCellHeight);
            }
            else
            {
                currentGraphicsRenderer.Draw(spriteBatch, 0, 192);
            }

            spriteBatch.End();
        }

        public CpuTraceEntry? HandleTraceClick(HostLayout hostLayout, CpuTraceBuffer cpuTraceBuffer, Point mousePos)
        {
            return debugToolsRenderer.HandleTraceClick(hostLayout, cpuTraceBuffer, mousePos);
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
            }

            disposed = true;
        }
    }
}

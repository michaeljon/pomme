using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using InnoWerks.Computers.Apple;
using InnoWerks.Processors;
using InnoWerks.Simulators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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

        private TextModeRenderer textModeRenderer;
        private LoresRenderer loresRenderer;
        private HiresRenderer hiresRenderer;
        private DhiresRenderer dhiresRenderer;
        private DebugToolsRenderer debugToolsRenderer;

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

            textModeRenderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, contentManager, textColor);
            loresRenderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, contentManager);
            hiresRenderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, contentManager);
            dhiresRenderer = new(graphicsDevice, cpu, bus, memoryBlocks, machineState, contentManager);
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
                machineState.State[SoftSwitch.EightyColumnMode] || machineState.State[SoftSwitch.DoubleHiRes] ? DisplayCharacteristics.HiresAppleWidth : DisplayCharacteristics.LoresAppleWidth,
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

            if (machineState.State[SoftSwitch.DoubleHiRes])
            {
                dhiresRenderer.Draw(spriteBatch, 0, 192);
            }
            else if (machineState.State[SoftSwitch.HiRes])
            {
                if (machineState.State[SoftSwitch.MixedMode] == false)
                {
                    hiresRenderer.Draw(spriteBatch, false, 0, 192);
                }
                else
                {
                    hiresRenderer.Draw(spriteBatch, false, 0, 192 - 4 * DisplayCharacteristics.AppleCellHeight);
                    textModeRenderer.Draw(spriteBatch, 20, 4, flashOn);
                }
            }
            else if (machineState.State[SoftSwitch.TextMode] == false)
            {
                if (machineState.State[SoftSwitch.MixedMode] == false)
                {
                    loresRenderer.Draw(spriteBatch, 0, 24);
                }
                else
                {
                    loresRenderer.Draw(spriteBatch, 0, 20);
                    textModeRenderer.Draw(spriteBatch, 20, 4, flashOn);
                }
            }
            else if (machineState.State[SoftSwitch.TextMode] == true)
            {
                textModeRenderer.Draw(spriteBatch, 0, 24, flashOn);
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

                textModeRenderer?.Dispose();
                loresRenderer?.Dispose();
                hiresRenderer?.Dispose();
                dhiresRenderer?.Dispose();
                debugToolsRenderer?.Dispose();
            }

            disposed = true;
        }
    }
}

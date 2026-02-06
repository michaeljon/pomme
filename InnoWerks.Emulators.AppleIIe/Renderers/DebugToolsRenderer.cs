using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using InnoWerks.Computers.Apple;
using InnoWerks.Simulators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace InnoWerks.Emulators.AppleIIe
{
    public class DebugToolsRenderer : IDisposable
    {
        private readonly List<SoftSwitch> debugSwitchesBlock1 =
        [
            SoftSwitch.TextMode,
            SoftSwitch.MixedMode,
            SoftSwitch.Page2,
            SoftSwitch.HiRes,
            SoftSwitch.DoubleHiRes,
            SoftSwitch.IOUDisabled,
        ];

        private readonly List<SoftSwitch> debugSwitchesBlock2 =
        [
            SoftSwitch.Store80,
            SoftSwitch.AuxRead,
            SoftSwitch.AuxWrite,
            SoftSwitch.ZpAux,
            SoftSwitch.EightyColumnMode,
            SoftSwitch.AltCharSet,
        ];

        private readonly List<SoftSwitch> debugSwitchesBlock3 =
        [
            SoftSwitch.IntCxRomEnabled,
            SoftSwitch.SlotC3RomEnabled,
            SoftSwitch.IntC8RomEnabled,
            SoftSwitch.LcBank2,
            SoftSwitch.LcReadEnabled,
            SoftSwitch.LcWriteEnabled,
        ];

        private readonly Dictionary<SoftSwitch, string> debugSwitchDisplay = new()
        {
            { SoftSwitch.TextMode, "TEXT" },
            { SoftSwitch.MixedMode, "MIXED" },
            { SoftSwitch.Page2, "PAGE2" },
            { SoftSwitch.HiRes, "HIRES" },
            { SoftSwitch.DoubleHiRes, "DHIRES" },
            { SoftSwitch.IOUDisabled, "IOUDIS" },

            { SoftSwitch.Store80, "STOR80" },
            { SoftSwitch.AuxRead, "AUXRD" },
            { SoftSwitch.AuxWrite, "AUXWR" },
            { SoftSwitch.ZpAux, "ZPAUX" },
            { SoftSwitch.EightyColumnMode, "80COL" },
            { SoftSwitch.AltCharSet, "ALTCHR" },

            { SoftSwitch.IntCxRomEnabled, "INTCX" },
            { SoftSwitch.SlotC3RomEnabled, "SLOTC3" },
            { SoftSwitch.IntC8RomEnabled, "INTC8" },
            { SoftSwitch.LcBank2, "BANK2" },
            { SoftSwitch.LcReadEnabled, "LCRD" },
            { SoftSwitch.LcWriteEnabled, "LCWRT" },
        };

        //
        // MonoGame stuff
        //
        private readonly SpriteFont debugFont;
        private readonly Texture2D whitePixel;

        private Color textColor;

        private readonly Cpu6502Core cpu;
        private readonly IBus bus;
        private readonly MachineState machineState;

        private bool disposed;

        public DebugToolsRenderer(
            GraphicsDevice graphicsDevice,
            Cpu6502Core cpu,
            IBus bus,
            Memory128k memoryBlocks,
            MachineState machineState,

            ContentManager contentManager,
            Color textColor
            )
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            ArgumentNullException.ThrowIfNull(cpu);
            ArgumentNullException.ThrowIfNull(bus);
            ArgumentNullException.ThrowIfNull(memoryBlocks);
            ArgumentNullException.ThrowIfNull(machineState);

            ArgumentNullException.ThrowIfNull(contentManager);

            this.machineState = machineState;
            this.cpu = cpu;
            this.bus = bus;

            this.textColor = textColor;

            whitePixel = new Texture2D(graphicsDevice, 1, 1);
            whitePixel.SetData([Color.White]);

            debugFont = contentManager.Load<SpriteFont>("DebugFont");
        }

        public void Draw(SpriteBatch spriteBatch, HostLayout hostLayout, CpuTraceBuffer cpuTraceBuffer, HashSet<ushort> breakpoints)
        {
            ArgumentNullException.ThrowIfNull(spriteBatch);

            ArgumentNullException.ThrowIfNull(cpuTraceBuffer);
            ArgumentNullException.ThrowIfNull(hostLayout);
            ArgumentNullException.ThrowIfNull(breakpoints);

            DrawRegisters(spriteBatch, hostLayout.Registers);
            DrawDebugSection(spriteBatch, hostLayout.Block1, debugSwitchesBlock1);
            DrawDebugSection(spriteBatch, hostLayout.Block2, debugSwitchesBlock2);
            DrawDebugSection(spriteBatch, hostLayout.Block3, debugSwitchesBlock3);
            DrawCpuTrace(spriteBatch, hostLayout.CpuTrace, cpuTraceBuffer, breakpoints);
        }

        public CpuTraceEntry? HandleTraceClick(HostLayout hostLayout, CpuTraceBuffer cpuTraceBuffer, Point mousePos)
        {
            ArgumentNullException.ThrowIfNull(hostLayout);
            ArgumentNullException.ThrowIfNull(cpuTraceBuffer);

            if (hostLayout.CpuTrace.Contains(mousePos) == false)
            {
                return null;
            }

            int lineHeight = debugFont.LineSpacing;
            int bottom = hostLayout.CpuTrace.Bottom - 8;

            int indexFromBottom = (bottom - mousePos.Y) / lineHeight;
            if (indexFromBottom < 0)
            {
                return null;
            }

            var entries = cpuTraceBuffer.Entries.ToList();
            entries.Reverse(); // newest first

            if (indexFromBottom >= entries.Count)
            {
                return null;
            }

            return entries[indexFromBottom];
        }

        private void DrawRegisters(SpriteBatch spriteBatch, Rectangle rectangle)
        {
            DrawPanel(spriteBatch, rectangle);

            int x = rectangle.X + 8;
            int y = rectangle.Y + 8;

            DrawKeyValue(spriteBatch, $"PC:", $"{cpu.Registers.ProgramCounter:X4}", x, ref y);
            DrawKeyValue(spriteBatch, $"A:", $"{cpu.Registers.A:X2}", x, ref y);
            DrawKeyValue(spriteBatch, $"X:", $"{cpu.Registers.X:X2}", x, ref y);
            DrawKeyValue(spriteBatch, $"Y:", $"{cpu.Registers.Y:X2}", x, ref y);
            DrawKeyValue(spriteBatch, $"SP:", $"{cpu.Registers.StackPointer:X2}", x, ref y);
            DrawKeyValue(spriteBatch, $"PS:", $"{cpu.Registers.InternalGetFlagsDisplay}", x, ref y);
        }

        private void DrawDebugSection(SpriteBatch spriteBatch, Rectangle rectangle, List<SoftSwitch> softSwitches)
        {
            DrawPanel(spriteBatch, rectangle);

            int x = rectangle.X + 8;
            int y = rectangle.Y + 8;

            foreach (var sw in softSwitches)
            {
                DrawKeyValue(spriteBatch, $"{debugSwitchDisplay[sw]}:", $"{(machineState.State[sw] ? 1 : 0)}", x, ref y);
            }
        }

        private void DrawCpuTrace(SpriteBatch spriteBatch, Rectangle rectangle, CpuTraceBuffer cpuTraceBuffer, HashSet<ushort> breakpoints)
        {
            DrawPanel(spriteBatch, rectangle);

            int x = rectangle.X + 12;
            int y = rectangle.Bottom - debugFont.LineSpacing - 12;

            for (var i = cpuTraceBuffer.Count - 1; i >= 0; i--)
            {
                var entry = cpuTraceBuffer[i];

                if (y < rectangle.Y + 8)
                {
                    break;
                }

                DrawTraceLine(spriteBatch, entry, breakpoints, x, y);

                y -= debugFont.LineSpacing;
            }
        }

        private void DrawPanel(SpriteBatch spriteBatch, Rectangle rectangle)
        {
            spriteBatch.Draw(whitePixel, rectangle, Color.White);
            spriteBatch.Draw(whitePixel, new Rectangle(rectangle.X + 1, rectangle.Y + 1, rectangle.Width - 2, rectangle.Height - 2), new Color(20, 20, 20));
        }

        private void DrawKeyValue(
            SpriteBatch spriteBatch,
            string key,
            string value,
            int x,
            ref int y)
        {
            spriteBatch.DrawString(debugFont, key, new Vector2(x, y), textColor);
            spriteBatch.DrawString(debugFont, value, new Vector2(x + 64, y), Color.White);
            y += debugFont.LineSpacing;
        }

        private void DrawTraceLine(
            SpriteBatch spriteBatch,
            CpuTraceEntry cpuTraceEntry,
            HashSet<ushort> breakpoints,
            int x,
            int y)
        {
            var opcode = cpuTraceEntry.OpCode;
            var pc = cpuTraceEntry.ProgramCounter;
            var decoded = cpuTraceEntry.OpCode.DecodeOperand(cpuTraceEntry.ProgramCounter, bus);

            if (breakpoints.Contains(cpuTraceEntry.ProgramCounter))
            {
                spriteBatch.DrawString(
                    debugFont,
                    ">",
                    new Vector2(x - 8, y),
                    Color.Red);
            }

            spriteBatch.DrawString(debugFont, $"{pc:X4}", new Vector2(x, y), textColor);

            spriteBatch.DrawString(
                debugFont,
                $"{opcode.OpCodeValue:X2}",
                new Vector2(x + 52, y),
                Color.Gray);

            if (decoded.Length > 1)
                spriteBatch.DrawString(debugFont, $"{decoded.Operand1:X2}", new Vector2(x + 80, y), Color.Gray);
            if (decoded.Length > 2)
                spriteBatch.DrawString(debugFont, $"{decoded.Operand2:X2}", new Vector2(x + 108, y), Color.Gray);

            spriteBatch.DrawString(
                debugFont,
                cpuTraceEntry.Mnemonic,
                new Vector2(x + 150, y),
                Color.White);

            spriteBatch.DrawString(
                debugFont,
                decoded.Display,
                new Vector2(x + 200, y),
                Color.White);

            // todo: lookup symbols here??!!
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
                whitePixel?.Dispose();
            }

            disposed = true;
        }
    }
}

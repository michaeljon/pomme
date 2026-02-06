using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using InnoWerks.Assemblers;
using InnoWerks.Computers.Apple;
using InnoWerks.Processors;
using InnoWerks.Simulators;

#pragma warning disable CA1822, RCS1213, SYSLIB1045

namespace Sim6502
{
    internal sealed class Program
    {
        private static bool keepRunning = true;

        private static readonly AppleConfiguration configuration = new(AppleModel.AppleIIe)
        {
            CpuClass = CpuClass.WDC65C02,
            Has80Column = true,
            HasAuxMemory = true,
            HasLowercase = true
        };

        private readonly MachineState machineState;

        private readonly Memory128k memoryBlocks;

        private readonly AppleBus bus;

        private readonly Dictionary<ushort, byte> breakpoints = [];

        private int stepSpeed = 1;

        private bool verboseSteps = true;

        public static void Main(string[] args)
        {
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
            {
                e.Cancel = true;
                keepRunning = false;

                Console.WriteLine("Interrupt received.");
            };

            var result = Parser.Default.ParseArguments<CliOptions>(args);

            result.MapResult(
                o => new Program().RunSimulator(o),

                errors =>
                {
                    foreach (var error in errors)
                    {
                        Console.Error.WriteLine(error.ToString());
                    }

                    return 1;
                }
            );
        }

        private Program()
        {
            machineState = new();
            memoryBlocks = new(machineState);
            bus = new(configuration, memoryBlocks, machineState);
        }

        private int RunSimulator(CliOptions options)
        {
            var programLines = File.ReadAllLines(options.Input);
            var assembler = new Assembler(
                programLines,
                options.Origin
            );
            assembler.Assemble();

            bus.LoadProgramToRam(assembler.ObjectCode, options.Origin);

            Console.WriteLine($"Debugging {options.Input}");
            Console.WriteLine("? for help");

            // power up initialization
            bus.Poke(Cpu6502Core.RstVectorH, (byte)((options.Origin & 0xff00) >> 8));
            bus.Poke(Cpu6502Core.RstVectorL, (byte)(options.Origin & 0xff));

            ICpu cpu = options.CpuClass == CpuClass.WDC6502 ?
                new Cpu6502(
                    bus,
                    (cpu, programCounter) => { },
                    (cpu) =>
                    {
                        Console.WriteLine();
                        Console.WriteLine($"PC:{cpu.Registers.ProgramCounter:X4} {cpu.Registers.GetRegisterDisplay} {cpu.Registers.InternalGetFlagsDisplay}");
                    }) :
                new Cpu65C02(
                    bus,
                    (cpu, programCounter) => { },
                    (cpu) =>
                    {
                        Console.WriteLine();
                        Console.WriteLine($"PC:{cpu.Registers.ProgramCounter:X4} {cpu.Registers.GetRegisterDisplay} {cpu.Registers.InternalGetFlagsDisplay}");
                    });

            cpu.Reset();

            DebugTheThing(cpu, assembler);

            return 0;
        }

        private void DebugTheThing(ICpu cpu, Assembler assembler)
        {
            // commands
            var quitRegex = new Regex("^(q|quit)$");

            var stepRegex = new Regex("^s$");
            var traceRegex = new Regex("^t (?<steps>[0-9]+)$");
            var goRegex = new Regex("^g$");

            var setProgramCounterRegex = new Regex("^pc (?<addr>[a-f0-9]{4})$");
            var jsrRegex = new Regex("^jsr (?<addr>[a-f0-9]{4})$");

            var setBreakpointRegex = new Regex("^sb (?<addr>[a-f0-9a-f]{4})$");
            var clearBreakpointRegex = new Regex("^cb (?<addr>[a-f0-9]{4})$");
            var clearAllBreakpointsRegex = new Regex("^ca$");
            var listBreakpointsRegex = new Regex("^lb$");

            var dumpFlagsRegex = new Regex("^df$");
            var setFlagRegex = new Regex("^sf (?<flag>[cnvz])$");
            var clearFlagRegex = new Regex("^cf (?<flag>[cnvz])$");

            var dumpRegistersRegex = new Regex("^dr$");
            var setRegisterRegex = new Regex("^sr (?<register>[axys]) (?<value>[a-f0-9]{1,2})$");
            var zeroRegisterRegex = new Regex("^zr (?<register>[axys])$");

            var writeRegex = new Regex("^w (?<addr>[a-f0-9]{1,4}) (?<values>[a-f0-9]{1,2}( [a-f0-9]{1,2})*)$");
            var readRegex = new Regex("^r (?<addr>[a-f0-9]{1,4}) (?<len>[0-9]*)$");
            var writePageRegex = new Regex("^d (?<page>[a-f0-9]{1,2})$");

            // options
            var setTraceSpeedRegex = new Regex("^o ts (?<speed>[0-9]+)$");
            var setTraceVerbosityRegex = new Regex("^o tv (?<flag>(true|false))$");

            // help
            var helpRegex = new Regex("^(\\?|h)$");

            var simulationComplete = false;

            while (simulationComplete == false)
            {
                // set the flag to keep going, but be ready to jump
                keepRunning = true;

                var programCounter = cpu.Registers.ProgramCounter;

                if (assembler.ProgramByAddress != null)
                {
                    if (assembler.ProgramByAddress.TryGetValue(programCounter, out var lineInformation))
                    {
                        Console.WriteLine($"{lineInformation.EffectiveAddress:X4} | {lineInformation.MachineCodeAsString,-10}| {lineInformation.RawInstructionText}");
                    }
                    else
                    {
                        Console.WriteLine($"{programCounter:X4} | {{no assembly found}}");
                    }
                }

                Console.Write("<dbg> ");
                var command = Console.ReadLine().ToLowerInvariant();

                if (quitRegex.IsMatch(command) == true)
                {
                    simulationComplete = true;
                }
                else if (stepRegex.IsMatch(command) == true)
                {
                    Step(cpu);
                }
                else if (traceRegex.IsMatch(command) == true)
                {
                    var captures = traceRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("steps", out string steps);

                    Trace(cpu, int.Parse(steps, CultureInfo.InvariantCulture));
                }
                else if (goRegex.IsMatch(command) == true)
                {
                    Go(cpu);
                }
                else if (setProgramCounterRegex.IsMatch(command) == true)
                {
                    var captures = setProgramCounterRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("addr", out string addr);

                    cpu.Registers.ProgramCounter = ushort.Parse(addr, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
                else if (jsrRegex.IsMatch(command) == true)
                {
                    var captures = jsrRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("addr", out string addr);

                    cpu.StackPush((byte)(((cpu.Registers.ProgramCounter + 2) & 0xff00) >> 8));
                    cpu.StackPush((byte)((cpu.Registers.ProgramCounter + 2) & 0x00ff));

                    cpu.Registers.ProgramCounter = ushort.Parse(addr, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
                else if (setBreakpointRegex.IsMatch(command) == true)
                {
                    var captures = setBreakpointRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("addr", out string addr);

                    SetBreakpoint(cpu, ushort.Parse(addr, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
                else if (clearBreakpointRegex.IsMatch(command) == true)
                {
                    var captures = clearBreakpointRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("addr", out string addr);

                    ClearBreakpoint(cpu, ushort.Parse(addr, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
                else if (clearAllBreakpointsRegex.IsMatch(command) == true)
                {
                    ClearAllBreakpoints(cpu);
                }
                else if (listBreakpointsRegex.IsMatch(command) == true)
                {
                    ListBreakpoints(cpu);
                }
                else if (setFlagRegex.IsMatch(command) == true)
                {
                    var captures = setFlagRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("flag", out string flag);

                    SetFlag(cpu, Enum.Parse<ProcessorFlag>(flag, true));
                }
                else if (clearFlagRegex.IsMatch(command) == true)
                {
                    var captures = clearFlagRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("flag", out string flag);

                    ClearFlag(cpu, Enum.Parse<ProcessorFlag>(flag, true));
                }
                else if (dumpFlagsRegex.IsMatch(command) == true)
                {
                    DumpFlags(cpu);
                }
                else if (dumpRegistersRegex.IsMatch(command) == true)
                {
                    DumpRegisters(cpu);
                }
                else if (setRegisterRegex.IsMatch(command) == true)
                {
                    var captures = setRegisterRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("register", out string register);
                    captures.TryGetValue("value", out string value);

                    SetRegister(cpu, Enum.Parse<ProcessorRegister>(register, true), byte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
                else if (zeroRegisterRegex.IsMatch(command) == true)
                {
                    var captures = zeroRegisterRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("register", out string register);

                    ZeroRegister(cpu, Enum.Parse<ProcessorRegister>(register, true));
                }
                else if (writeRegex.IsMatch(command) == true)
                {
                    var captures = writeRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("addr", out string addr);
                    captures.TryGetValue("values", out string values);

                    var valueList = values
                        .Split(' ')
                        .Select(v => byte.Parse(v, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                        .ToList();

                    WriteMemory(cpu, ushort.Parse(addr, NumberStyles.HexNumber, CultureInfo.InvariantCulture), valueList);
                }
                else if (readRegex.IsMatch(command) == true)
                {
                    var captures = readRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("addr", out string addr);
                    captures.TryGetValue("len", out string len);

                    if (string.IsNullOrEmpty(len) == true)
                    {
                        len = "1";
                    }

                    ReadMemory(
                        cpu,
                        ushort.Parse(addr, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                        ushort.Parse(len, CultureInfo.InvariantCulture));
                }
                else if (writePageRegex.IsMatch(command) == true)
                {
                    var captures = writePageRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("page", out string page);

                    DumpPage(cpu, byte.Parse(page, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
                else if (setTraceSpeedRegex.IsMatch(command) == true)
                {
                    var captures = setTraceSpeedRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("speed", out string speed);

                    stepSpeed = int.Parse(speed, CultureInfo.InvariantCulture);
                    Console.WriteLine($"step speed set to {1} instruction(s) per second");
                }
                else if (setTraceVerbosityRegex.IsMatch(command) == true)
                {
                    var captures = setTraceVerbosityRegex.MatchNamedCaptures(command);
                    captures.TryGetValue("flag", out string flag);

                    verboseSteps = bool.Parse(flag);
                    Console.WriteLine($"CPU instruction printing set to {verboseSteps}");
                }
                else if (helpRegex.IsMatch(command) == true)
                {
                    PrintHelp();
                }
                else
                {
                    Console.WriteLine($"Unknown command {command}");
                }
            }
        }

        private int Step(ICpu cpu)
        {
            var cycleCount = cpu.Step(writeInstructions: verboseSteps, returnPriorToBreak: true);

            if (cycleCount == 0)
            {
                Console.WriteLine($"BRK encountered at ${cpu.Registers.ProgramCounter:X4}");
            }

            return cycleCount;
        }

        private bool Trace(ICpu cpu, int steps)
        {
            var instructionCount = 0;
            var cycleCount = 0;

            // well, we might as well run for a while
            if (steps == 0)
            {
                steps = int.MaxValue;
            }

            for (var step = 0; step < steps; step++)
            {
                var stepCycleCount = cpu.Step(writeInstructions: verboseSteps, returnPriorToBreak: true);

                instructionCount++;
                cycleCount += stepCycleCount;

                if (stepCycleCount == 0)
                {
                    Console.WriteLine($"BRK encountered at ${cpu.Registers.ProgramCounter:X4}");
                    return true;
                }
                else
                {
                    var t = Task.Run(async delegate
                                  {
                                      await Task.Delay(new TimeSpan((long)(1.0 / stepSpeed * 1000) * TimeSpan.TicksPerMillisecond));
                                      return 0;
                                  });
                    t.Wait();
                }

                if (keepRunning == false)
                {
                    return false;
                }
            }

            return false;
        }

        private bool Go(ICpu cpu)
        {
            var breakEncountered = false;

            while (breakEncountered == false)
            {
                var stepCycleCount = cpu.Step(writeInstructions: verboseSteps, returnPriorToBreak: true);

                if (stepCycleCount == 0)
                {
                    breakEncountered = true;
                }

                if (keepRunning == false)
                {
                    return false;
                }
            }

            Console.WriteLine($"BRK encountered at ${cpu.Registers.ProgramCounter:X4}");
            return breakEncountered;
        }

        private void SetBreakpoint(ICpu cpu, ushort addr)
        {
            breakpoints.Add(addr, bus.Peek(addr));
            bus.Poke(addr, 0x00);

            Console.WriteLine($"Breakpoint at {addr:X4} set");
        }

        private void ClearBreakpoint(ICpu cpu, ushort addr)
        {
            bus.Poke(addr, breakpoints[addr]);
            breakpoints.Remove(addr);

            Console.WriteLine($"Breakpoint at {addr:X4} cleared");
        }

        private void ClearAllBreakpoints(ICpu cpu)
        {
            if (breakpoints.Count == 0)
            {
                Console.WriteLine("No breakpoints set");
                return;
            }

            foreach (var (addr, value) in breakpoints)
            {
                bus.Poke(addr, value);
            }

            Console.WriteLine("Breakpoints cleared");
        }

        private void ListBreakpoints(ICpu cpu)
        {
            if (breakpoints.Count == 0)
            {
                Console.WriteLine("No breakpoints set");
                return;
            }

            var bp = 1;

            foreach (var (addr, _) in breakpoints)
            {
                Console.WriteLine($"{bp++}:  {addr:X4}");
            }
        }

        private void SetFlag(ICpu cpu, ProcessorFlag processorFlag)
        {
            switch (processorFlag)
            {
                case ProcessorFlag.C:
                    cpu.Registers.Carry = true;
                    break;

                case ProcessorFlag.Z:
                    cpu.Registers.Zero = true;
                    break;

                case ProcessorFlag.V:
                    cpu.Registers.Overflow = true;
                    break;

                case ProcessorFlag.N:
                    cpu.Registers.Negative = true;
                    break;
            }

            Console.WriteLine($"Flag {processorFlag} set");
        }

        private void ClearFlag(ICpu cpu, ProcessorFlag processorFlag)
        {
            switch (processorFlag)
            {
                case ProcessorFlag.C:
                    cpu.Registers.Carry = false;
                    break;

                case ProcessorFlag.Z:
                    cpu.Registers.Zero = false;
                    break;

                case ProcessorFlag.V:
                    cpu.Registers.Overflow = false;
                    break;

                case ProcessorFlag.N:
                    cpu.Registers.Negative = false;
                    break;
            }

            Console.WriteLine($"Flag {processorFlag} cleared");
        }

        private void DumpFlags(ICpu cpu)
        {
            Console.Write($"N: {(cpu.Registers.Negative ? '1' : '0')}  ");
            Console.Write($"V: {(cpu.Registers.Overflow ? '1' : '0')}  ");
            Console.Write($"u: {(cpu.Registers.Unused ? '1' : '0')}  ");
            Console.Write($"D: {(cpu.Registers.Decimal ? '1' : '0')}  ");
            Console.Write($"B: {(cpu.Registers.Break ? '1' : '0')}  ");
            Console.Write($"I: {(cpu.Registers.Interrupt ? '1' : '0')}  ");
            Console.Write($"Z: {(cpu.Registers.Zero ? '1' : '0')}  ");
            Console.WriteLine($"C: {(cpu.Registers.Carry ? '1' : '0')}");
            Console.WriteLine();
        }

        private void DumpRegisters(ICpu cpu)
        {
            Console.WriteLine($"PC:{cpu.Registers.ProgramCounter:X4} {cpu.Registers.GetRegisterDisplay} {cpu.Registers.InternalGetFlagsDisplay}");
            Console.WriteLine();
        }

        private void SetRegister(ICpu cpu, ProcessorRegister processorRegister, byte value)
        {
            switch (processorRegister)
            {
                case ProcessorRegister.A:
                    cpu.Registers.A = value;
                    break;

                case ProcessorRegister.X:
                    cpu.Registers.X = value;
                    break;

                case ProcessorRegister.Y:
                    cpu.Registers.Y = value;
                    break;

                case ProcessorRegister.S:
                    cpu.Registers.ProcessorStatus = value;
                    break;
            }

            Console.WriteLine($"Flag {processorRegister} set");
        }

        private void ZeroRegister(ICpu cpu, ProcessorRegister processorRegister)
        {
            switch (processorRegister)
            {
                case ProcessorRegister.A:
                    cpu.Registers.A = 0;
                    break;

                case ProcessorRegister.X:
                    cpu.Registers.X = 0;
                    break;

                case ProcessorRegister.Y:
                    cpu.Registers.Y = 0;
                    break;
            }

            Console.WriteLine($"Flag {processorRegister} cleared");
        }

        private void WriteMemory(ICpu cpu, ushort addr, List<byte> bytes)
        {
            for (var i = 0; i < bytes.Count; i++)
            {
                bus.Poke((ushort)(addr + i), bytes[i]);
            }

            Console.WriteLine($"{bytes.Count} bytes written to {addr:X4}");
        }

        private void ReadMemory(ICpu cpu, ushort addr, ushort len)
        {
            // get page number
            // round down to 16 byte boundary
            // print blank values until start of dump
            // continue printing until we've written all the bytes

            // but for now
            for (var i = 0; i < len; i++)
            {
                Console.WriteLine($"{(addr + i):X4}: ${bus.Peek((ushort)(addr + i)):X2}");
            }
        }

        private void DumpPage(ICpu cpu, byte page)
        {
            Console.Write("       ");
            for (var b = 0; b < 32; b++)
            {
                if (b > 0x00 && b % 0x08 == 0)
                {
                    Console.Write("  ");
                }

                Console.Write("{0:X2} ", b);
            }

            Console.WriteLine();

            for (var l = page * 0x100; l < (page + 1) * 0x100; l += 32)
            {
                Console.Write("{0:X4}:  ", l);

                for (var b = 0; b < 32; b++)
                {
                    if (b > 0x00 && b % 0x08 == 0)
                    {
                        Console.Write("  ");
                    }

                    Console.Write("{0:X2} ", bus.Peek((ushort)(l + b)));
                }

                Console.WriteLine();
            }

            Console.WriteLine();
        }

        private void PrintHelp()
        {
            Console.WriteLine("q                     - quit");
            Console.WriteLine("");
            Console.WriteLine("s                     - step");
            Console.WriteLine("t <steps>             - run n <steps>");
            Console.WriteLine("g                     - go");
            Console.WriteLine("pc <addr>             - set PC to <addr> (PC <- addr)");
            Console.WriteLine("jsr <addr>            - call subroutine ad <addr> (S <- PC + 2, PC <- addr)");
            Console.WriteLine("sb <addr>             - set breakpoint at <addr>");
            Console.WriteLine("cb <addr>             - clear breakpoint at <addr>");
            Console.WriteLine("ca                    - clear all breakpoints");
            Console.WriteLine("lb                    - list breakpoints");
            Console.WriteLine("sf <flag>             - set flag (CVNZ) to true");
            Console.WriteLine("cf <flag>             - set flag (CVNZ) to false");
            Console.WriteLine("df                    - dump flags");
            Console.WriteLine("");
            Console.WriteLine("sr <reg> <value>      - set register (A,X,Y,S) to value");
            Console.WriteLine("zr <reg>              - set register (A,X,Y,S) to 0 (shortcut)");
            Console.WriteLine("dr                    - dump registers");
            Console.WriteLine("");
            Console.WriteLine("w <addr> <byte>...    - write <byte> starting at <addr>");
            Console.WriteLine("r <addr> <len>        - read <len>");
            Console.WriteLine("d <page>              - dump page <page>");
            Console.WriteLine("");
            Console.WriteLine("o ts <steps/sec>      - set trace speed to steps / second");
            Console.WriteLine("");
            Console.WriteLine("? | h                 - display this list");
        }
    }
}

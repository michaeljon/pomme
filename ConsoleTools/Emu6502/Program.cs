using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using InnoWerks.Computers.Apple;
using InnoWerks.Processors;
using InnoWerks.Simulators;

#pragma warning disable CA1859, CS0169, CA1823, IDE0005

namespace Emu6502
{
    internal sealed class Program
    {
        private static bool keepRunning = true;

        public static void Main(string[] args)
        {
            Console.TreatControlCAsInput = false;

            var result = Parser.Default.ParseArguments<CliOptions>(args);

            result.MapResult(
                RunEmulator,

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

        private static int RunEmulator(CliOptions options)
        {
            var mainRom = File.ReadAllBytes("roms/apple2e-16k.rom");
            var audit = File.ReadAllBytes("tests/audit.o");

            var config = new AppleConfiguration(AppleModel.AppleIIe)
            {
                CpuClass = CpuClass.WDC65C02,
                HasAuxMemory = true,
                Has80Column = false,
                HasLowercase = false,
                RamSize = 64
            };

            var machineState = new MachineState();
            var memoryBlocks = new Memory128k(machineState);

            var bus = new AppleBus(config, memoryBlocks, machineState);
            var iou = new IOU(memoryBlocks, machineState, bus);
            var mmu = new MMU(memoryBlocks, machineState, bus);

            var cpu = new Cpu65C02(
                bus,
                (cpu, programCounter) => { },
                (cpu) =>
                {
                    if (keepRunning == false)
                    {
                        Console.CursorVisible = true;
                        Environment.Exit(0);
                    }
                });

            var disk = new DiskIISlotDevice(6, cpu, bus, machineState);
            disk.GetDrive(1).InsertDisk("disks/dos33.dsk");

            bus.AddDevice(disk);

            foreach (var (address, name) in SoftSwitchAddress.Lookup.OrderBy(a => a.Key))
            {
                bool assigned = iou.HandlesRead(address) || iou.HandlesWrite(address) || mmu.HandlesRead(address) || mmu.HandlesWrite(address);
                if (assigned == false)
                {
                    SimDebugger.Info("Address {0:X4} ({1}) is not assigned to any device", address, name);
                }
            }

            var keyListener = Task.Run(() =>
            {
                while (keepRunning)
                {
                    if (!Console.KeyAvailable)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    var key = Console.ReadKey(intercept: true);

                    iou.InjectKey(MapToAppleKey(key));
                }
            });

            var renderer = Task.Run(() =>
            {
                while (keepRunning)
                {
                    // Run roughly one frame worth of cycles
                    ulong target = bus.CycleCount + VideoTiming.FrameCycles;

                    while (bus.CycleCount < target)
                    {
                        Thread.Sleep(1);
                    }

                    Render(machineState, memoryBlocks);

                    Thread.Sleep(16);
                }
            });

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;

                Console.CursorVisible = true;

                Console.SetCursorPosition(0, 25);
                Console.WriteLine("Interrupt received.");
                Console.WriteLine(cpu.Registers);
                Console.Write("[QINRST]> ");

                var key = Console.ReadKey();

                switch (key.KeyChar)
                {
                    case 'Q':
                    case 'q':
                        keepRunning = false;

                        Console.CursorVisible = true;
                        Environment.Exit(0);
                        break;

                    case 'I':
                    case 'i':
                        cpu.InjectInterrupt(false);
                        break;

                    case 'N':
                    case 'n':
                        cpu.InjectInterrupt(true);
                        break;

                    case 'R':
                    case 'r':
                        cpu.Reset();
                        break;

                    case 'S':
                    case 's':
                        options.SingleStep = !options.SingleStep;
                        break;

                    case 'T':
                    case 't':
                        options.Trace = !options.Trace;
                        break;
                }

                Console.CursorVisible = false;
            };

            bus.LoadProgramToRom(mainRom);
            bus.LoadProgramToRam(audit, 0x6000);

            cpu.Reset();

            Console.CursorVisible = false;
            Console.Clear();

            while (keepRunning)
            {
                // Run roughly one frame worth of cycles
                ulong target = bus.CycleCount + VideoTiming.FrameCycles;

                while (bus.CycleCount < target)
                {
                    if (options.SingleStep == true)
                    {
                        var traceEntry = cpu.PeekInstruction();

                        Console.Write($"{traceEntry.DecodedOperation}\n");
                        Console.Write("> ");
                        var key = Console.ReadKey();
                        if (key.KeyChar == 'G')
                        {
                            options.SingleStep = false;
                        }
                    }

                    cpu.Step();

                    if (options.SingleStep == true)
                    {
                        Console.WriteLine(cpu.Registers);
                    }
                }
            }

            Console.ResetColor();
            Console.CursorVisible = true;

            // not-reached
#pragma warning disable CS0162 // Unreachable code detected
            return 0;
#pragma warning restore CS0162 // Unreachable code detected
        }

        // todo: use apple iie ref table 2-3 to construct full mapping
        static byte MapToAppleKey(ConsoleKeyInfo key)
        {
            if (key.Key == ConsoleKey.Backspace)
                return 0x08;

            if (key.Key == ConsoleKey.LeftArrow)
                return 0x08;

            if (key.Key == ConsoleKey.RightArrow)
                return 0x15;

            if (key.Key == ConsoleKey.UpArrow)
                return 0x0B;

            if (key.Key == ConsoleKey.DownArrow)
                return 0x0A;

            // char c = char.ToUpperInvariant(key.KeyChar);

            // if (c >= 0x20 && c <= 0x7E)
            //     return (byte)c;

            return (byte)key.KeyChar;
        }

        public static void Render(MachineState machineState, Memory128k memoryBlocks)
        {
            if (machineState.State[SoftSwitch.EightyColumnMode] == false)
            {
                Render40Column(machineState, memoryBlocks);
            }
            else
            {
                Render80Column(machineState, memoryBlocks);
            }
        }

        private static void Render40Column(MachineState machineState, Memory128k memoryBlocks)
        {
            Span<char> line = stackalloc char[40];

            Console.CursorVisible = false;
            Console.SetCursorPosition(0, 0);

            bool page2 = machineState.State[SoftSwitch.Page2];

            for (int row = 0; row < 24; row++)
            {
                for (int col = 0; col < 40; col++)
                {
                    ushort addr = GetTextAddress(row, col, page2);
                    byte b = memoryBlocks.Read(addr);

                    line[col] = DecodeAppleChar(b);
                }

                Console.WriteLine(line);
            }
        }

        private static void Render80Column(MachineState machineState, Memory128k memoryBlocks)
        {
            Span<char> line = stackalloc char[80];

            Console.CursorVisible = false;
            Console.SetCursorPosition(0, 0);

            for (int row = 0; row < 24; row++)
            {
                for (int col = 0; col < 40; col++)
                {
                    ushort addr = GetTextAddress(row, col, false);
                    byte b = memoryBlocks.GetAux(addr);
                    line[2 * col] = DecodeAppleChar(b);

                    b = memoryBlocks.GetMain(addr);
                    line[(2 * col) + 1] = DecodeAppleChar(b);
                }

                Console.WriteLine(line);
            }
        }

        private static char DecodeAppleChar(byte b)
        {
            // Ignore inverse/flash for now
            b &= 0x7F;

            // Apple II uses ASCII-ish set
            if (b < 0x20)
                return ' ';

            return (char)b;
        }

        private static ushort GetTextAddress(int row, int col, bool page2)
        {
            int pageOffset = page2 ? 0x800 : 0x400;

            return (ushort)(
                pageOffset +
                textRowBase[row & 0x07] +
                (row >> 3) * 40 +
                col
            );
        }

        private static readonly int[] textRowBase =
        [
            0x000, 0x080, 0x100, 0x180,
            0x200, 0x280, 0x300, 0x380
        ];

    }
}

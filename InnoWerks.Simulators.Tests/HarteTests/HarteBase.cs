#define DUMP_TEST_DATA
#define POST_STEP_MEMORY
#define VALIDATE_BUS_ACCESSES
#define VERBOSE_BATCH_OUTPUT

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.Generic;
using InnoWerks.Processors;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

#if VALIDATE_BUS_ACCESSES
using System.Linq;
#endif

#pragma warning disable CA1002, CA1822, CA1508

namespace InnoWerks.Simulators.Tests
{
    [TestClass]
    public class HarteBase : TestBase
    {
        protected virtual string BasePath { get; }

        protected virtual CpuClass CpuClass { get; }

        protected string TestRoot => ".";

        protected static readonly JsonSerializerOptions SerializerOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IgnoreReadOnlyProperties = false,
            IgnoreReadOnlyFields = false,
            AllowTrailingCommas = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new HarteCycleConverter(),
                new HarteRamConverter()
            }
        };

        protected void RunNamedBatch(string batch)
        {
            if (string.IsNullOrEmpty(batch))
            {
                Assert.Inconclusive("No batch name provided to RunNamedBatch");
                return;
            }

            List<string> results = [];

            var file = $"{BasePath}/{batch}.json";

            using (var fs = File.OpenRead(file))
            {
                if (fs.Length == 0)
                {
                    return;
                }

                foreach (var test in JsonSerializer.Deserialize<List<JsonHarteTestStructure>>(fs, SerializerOptions).ToList())
                {
                    RunIndividualTest(test, results);
                }

#if VERBOSE_BATCH_OUTPUT
                foreach (var result in results)
                {
                    TestContext.WriteLine(result);
                }
#endif

                Assert.IsTrue(results.Count == 0, $"Failed with {results.Count} messages");
            }
        }

        protected bool RunIndividualTest(JsonHarteTestStructure test, List<string> results)
        {
            ArgumentNullException.ThrowIfNull(test);
            ArgumentNullException.ThrowIfNull(results);

            var batch = test.Name.Split(' ')[0];
            var instructionSet = CpuInstructions.GetInstructionSet(CpuClass);
            var ocd = instructionSet[byte.Parse(batch, NumberStyles.HexNumber, CultureInfo.InvariantCulture)];

            var bus = new AccessCountingBus();

            // set up initial memory state
            bus.Initialize(test.Initial.Ram);

            ICpu cpu = CpuClass == CpuClass.WDC6502 ?
                new Cpu6502(
                    bus,
                    // (cpu, pc) => FlagsTraceCallback(cpu, pc, memory),
                    // (cpu) => FlagsLoggerCallback(cpu, memory, 0))
                    (cpu, pc) => DummyTraceCallback(cpu, pc, bus),
                    (cpu) => DummyLoggerCallback(cpu, bus, 0)) :
                new Cpu65C02(
                    bus,
                    // (cpu, pc) => FlagsTraceCallback(cpu, pc, memory),
                    // (cpu) => FlagsLoggerCallback(cpu, memory, 0))
                    (cpu, pc) => DummyTraceCallback(cpu, pc, bus),
                    (cpu) => DummyLoggerCallback(cpu, bus, 0));

            cpu.Reset();

            // initialize processor
            cpu.Registers.ProgramCounter = test.Initial.ProgramCounter;
            cpu.Registers.StackPointer = test.Initial.S;
            cpu.Registers.A = test.Initial.A;
            cpu.Registers.X = test.Initial.X;
            cpu.Registers.Y = test.Initial.Y;
            cpu.Registers.ProcessorStatus = test.Initial.P;

            // initial registers in local format
            var initialRegisters = new Registers()
            {
                ProgramCounter = test.Initial.ProgramCounter,
                StackPointer = test.Initial.S,
                A = test.Initial.A,
                X = test.Initial.X,
                Y = test.Initial.Y,
                ProcessorStatus = test.Initial.P,
            };

            // run test
            var cycleCount = cpu.Step();

            var finalRegisters = new Registers()
            {
                ProgramCounter = test.Final.ProgramCounter,
                StackPointer = test.Final.S,
                A = test.Final.A,
                X = test.Final.X,
                Y = test.Final.Y,
                ProcessorStatus = test.Final.P,
            };

            var testFailed = false;

            // we can run these tests to this extent, after this i haven't implemented
            // the "undocumented" opcodes because, well, they're undocumented and
            // probably don't always behave like they should
            if (CpuClass == CpuClass.WDC6502 && ocd.OpCode != OpCode.Unknown)
            {
                // verify results
                if (test.Final.ProgramCounter != cpu.Registers.ProgramCounter) { testFailed = true; results.Add($"{test.Name}: ProgramCounter expected {test.Final.ProgramCounter:X4} actual {cpu.Registers.ProgramCounter:X4}"); }

                if (test.Final.S != cpu.Registers.StackPointer) { testFailed = true; results.Add($"{test.Name}: StackPointer expected {test.Final.S:X2} actual {cpu.Registers.StackPointer:X2}"); }
                if (test.Final.A != cpu.Registers.A) { testFailed = true; results.Add($"{test.Name}: A expected {test.Final.A:X2} actual {cpu.Registers.A:X2}"); }
                if (test.Final.X != cpu.Registers.X) { testFailed = true; results.Add($"{test.Name}: X expected {test.Final.X:X2} actual {cpu.Registers.Y:X2}"); }
                if (test.Final.Y != cpu.Registers.Y) { testFailed = true; results.Add($"{test.Name}: Y expected {test.Final.Y:X2} actual {cpu.Registers.X:X2}"); }
                if (test.Final.P != cpu.Registers.ProcessorStatus) { testFailed = true; results.Add($"{test.Name}: ProcessorStatus expected {test.Final.P:X2} actual {cpu.Registers.ProcessorStatus:X2}"); }

#if POST_STEP_MEMORY
                // verify memory
                (var ramMatches, var ramDiffersAtAddr, byte ramExpectedValue, byte ramActualValue) =
                    bus.ValidateMemory(test.Final.Ram);
                if (ramMatches == false) { testFailed = true; results.Add($"{test.Name}: Expected memory at {ramDiffersAtAddr} to be {ramExpectedValue} but is {ramActualValue}"); }
#endif

#if VALIDATE_BUS_ACCESSES
                // verify bus accesses
                if (test.BusAccesses.Count() != bus.BusAccesses.Count)
                {
                    { testFailed = true; results.Add($"{test.Name}: Expected {test.BusAccesses.Count()} memory accesses but got {bus.BusAccesses.Count} instead "); }
                }
                else
                {
                    (var cyclesMatches, var cyclesDiffersAtAddr, var cyclesExpectedValue, var cyclesActualValue) =
                        bus.ValidateCycles(test.BusAccesses);
                    if (cyclesMatches == false) { testFailed = true; results.Add($"{test.Name}: Expected access at {cyclesDiffersAtAddr} to be {cyclesExpectedValue} but is {cyclesActualValue}"); }
                }
#endif
            }

#if DUMP_TEST_DATA
            if (testFailed == true)
            {
                TestContext.WriteLine("");
                TestContext.WriteLine($"{((testFailed == true) ? "Failed" : "Passed")} TestName:     {test.Name}");
                TestContext.WriteLine($"OpCode:              ${batch} {ocd.OpCode} {ocd.AddressingMode}");
                TestContext.WriteLine($"Initial registers    {initialRegisters}");
                TestContext.WriteLine($"Expected registers   {finalRegisters}");
                TestContext.WriteLine($"Computed registers   {cpu.Registers}");

                TestContext.WriteLine("Expected bus accesses");
                var time = 0;
                foreach (var busAccess in test.BusAccesses)
                {
                    TestContext.WriteLine($"T{time++}: {busAccess}");
                }

                TestContext.WriteLine("Actual bus accesses");
                time = 0;
                foreach (var busAccess in bus.BusAccesses)
                {
                    TestContext.WriteLine($"T{time++}: {busAccess}");
                }
            }
#endif

            return testFailed;
        }

        protected void RunNamedTest(string testName)
        {
#pragma warning disable IDE0002
            ArgumentNullException.ThrowIfNullOrEmpty(testName);
#pragma warning restore IDE0002

            List<string> results = [];

            var batch = testName.Split(' ')[0];
            var file = $"{BasePath}/{batch}.json";

            var instructionSet = CpuInstructions.GetInstructionSet(CpuClass);
            var ocd = instructionSet[byte.Parse(batch, NumberStyles.HexNumber, CultureInfo.InvariantCulture)];

            TestContext.WriteLine($"Running test {testName}");
            TestContext.WriteLine($"OpCode: ${batch} {ocd.OpCode} {ocd.AddressingMode}");
            TestContext.WriteLine("");

            using (var fs = File.OpenRead(file))
            {
                var tests = JsonSerializer.Deserialize<List<JsonHarteTestStructure>>(fs, SerializerOptions);
                var test = tests.Find(t => t.Name == testName);

                if (test == null)
                {
                    Assert.Inconclusive($"Unable to locate test {testName}");
                    return;
                }

                var json = JsonSerializer.Serialize(test.Clone(), SerializerOptions);
                File.WriteAllText("foo.json", json);

                RunIndividualTest(test, results);
            }

            foreach (var result in results)
            {
                TestContext.WriteLine(result);
            }

            Assert.AreEqual(0, results.Count, $"Failed some tests");
        }

        protected void RunAllBatches()
        {
            bool[] ignored = LoadIgnored();
            List<string> results = [];

            var files = Directory
                .GetFiles(BasePath, "*.json")
                .OrderBy(f => f);

            Parallel.ForEach(files, file =>
            {
                using (var fs = File.OpenRead(file))
                {
                    if (fs.Length == 0)
                    {
                        return;
                    }

                    var index = byte.Parse(Path.GetFileNameWithoutExtension(file), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                    if (ignored[index] == false)
                    {
                        foreach (var test in JsonSerializer.Deserialize<List<JsonHarteTestStructure>>(fs, SerializerOptions))
                        {
                            RunIndividualTest(test, results);
                        }
                    }
                }
            });

#if VERBOSE_BATCH_OUTPUT
            foreach (var result in results)
            {
                TestContext.WriteLine(result);
            }
#endif

            Assert.AreEqual(0, results.Count, $"Failed some tests");
        }

        protected void RunAllBatchesWithRandomSampling()
        {
            bool[] ignored = LoadIgnored();
            List<string> results = [];

            var files = Directory
                .GetFiles(BasePath, "*.json")
                .OrderBy(f => f);

            var lockObject = new object();

            Parallel.ForEach(files, file =>
            {
                using (var fs = File.OpenRead(file))
                {
                    if (fs.Length == 0)
                    {
                        return;
                    }

                    var index = byte.Parse(Path.GetFileNameWithoutExtension(file), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                    if (ignored[index] == false)
                    {
                        // note:
                        // this test can take upwards of 30 minutes to run if we go through all
                        // possible opcodes and all 10,000 tests per opcode. this trick selects
                        // a random samples from the test. we still hit all the opcodes, but we
                        // run just 1/10th of the tests. big time savings...
                        //
                        // and no, we aren't skipping tests. all of the individual "batch" tests
                        // below, which can run in parallel, will test the entire 10,000
                        // tests in the batch
                        var tests = JsonSerializer.Deserialize<List<JsonHarteTestStructure>>(fs, SerializerOptions)
                            .OrderBy(_ => Guid.NewGuid())
                            .Take(1000);

                        List<string> testResults = [];
                        foreach (var test in tests)
                        {
                            RunIndividualTest(test, testResults);
                        }

                        lock (lockObject)
                        {
                            results.AddRange(testResults);
                        }
                    }
                }
            });

#if VERBOSE_BATCH_OUTPUT
            foreach (var result in results)
            {
                TestContext.WriteLine(result);
            }
#endif

            Assert.AreEqual(0, results.Count, $"Failed some tests");
        }

        protected bool[] LoadIgnored()
        {
            var ignored = new bool[256];

            if (CpuClass == CpuClass.WDC6502)
            {
                // from https://www.masswerk.at/nowgobang/2021/6502-illegal-opcodes
                foreach (var kill in (byte[])[0x02, 0x12, 0x22, 0x32, 0x42, 0x52, 0x62, 0x72, 0x92, 0xB2, 0xD2, 0xF2])
                {
                    ignored[kill] = true;
                }
            }

            return ignored;
        }
    }
}

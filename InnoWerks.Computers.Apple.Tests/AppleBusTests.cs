using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class AppleBusTests
    {
        // ------------------------------------------------------------------ //
        // Helpers
        // ------------------------------------------------------------------ //

        private static AppleConfiguration DefaultConfig =>
            new AppleConfiguration(AppleModel.AppleIIe);

        /// <summary>
        /// Creates a bare AppleBus with no soft-switch devices registered.
        /// </summary>
        private static (AppleBus Bus, Memory128k Memory, MachineState State) CreateBare()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();
            var bus = new AppleBus(memory, []);
            return (bus, memory, state);
        }

        /// <summary>
        /// Creates an AppleBus with MMU and IOU registered, which is the
        /// standard Apple IIe configuration used for routing tests.
        /// </summary>
        private static (AppleBus Bus, Memory128k Memory, MachineState State) CreateWithDevices()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();
            var bus = new AppleBus(memory, []);
            _ = new MMU(memory, state, bus);
            _ = new IOU(memory, state, bus);
            return (bus, memory, state);
        }

        // ------------------------------------------------------------------ //
        // Cycle counting
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void CycleCountStartsAtZero()
        {
            var (bus, _, _) = CreateBare();
            Assert.AreEqual(0UL, bus.CycleCount);
        }

        [TestMethod]
        public void ReadIncrementsCycleCount()
        {
            var (bus, _, _) = CreateBare();
            bus.Read(0x0100);
            Assert.AreEqual(1UL, bus.CycleCount);
        }

        [TestMethod]
        public void WriteIncrementsCycleCount()
        {
            var (bus, _, _) = CreateBare();
            bus.Write(0x0100, 0x00);
            Assert.AreEqual(1UL, bus.CycleCount);
        }

        [TestMethod]
        public void PeekDoesNotIncrementCycleCount()
        {
            var (bus, _, _) = CreateBare();
            bus.Peek(0x0100);
            Assert.AreEqual(0UL, bus.CycleCount);
        }

        [TestMethod]
        public void PokeDoesNotIncrementCycleCount()
        {
            var (bus, _, _) = CreateBare();
            bus.Poke(0x0100, 0x00);
            Assert.AreEqual(0UL, bus.CycleCount);
        }

        [TestMethod]
        public void MultipleCycleOperationsAccumulate()
        {
            var (bus, _, _) = CreateBare();
            bus.Read(0x0100);
            bus.Write(0x0200, 0x00);
            bus.Read(0x0300);
            Assert.AreEqual(3UL, bus.CycleCount);
        }

        // ------------------------------------------------------------------ //
        // Transaction tracking
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void BeginEndTransactionReturnsZeroWhenNoAccesses()
        {
            var (bus, _, _) = CreateBare();
            bus.BeginTransaction();
            Assert.AreEqual(0, bus.EndTransaction());
        }

        [TestMethod]
        public void TransactionCountsReadCycles()
        {
            var (bus, _, _) = CreateBare();
            bus.BeginTransaction();
            bus.Read(0x0100);
            bus.Read(0x0200);
            Assert.AreEqual(2, bus.EndTransaction());
        }

        [TestMethod]
        public void BeginTransactionResetsCount()
        {
            var (bus, _, _) = CreateBare();
            bus.BeginTransaction();
            bus.Read(0x0100);
            bus.BeginTransaction(); // restart
            bus.Read(0x0200);
            Assert.AreEqual(1, bus.EndTransaction());
        }

        // ------------------------------------------------------------------ //
        // Reset
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ResetZeroesCycleCount()
        {
            var (bus, _, _) = CreateWithDevices();
            bus.Read(0x0100);
            bus.Reset();
            Assert.AreEqual(0UL, bus.CycleCount);
        }

        [TestMethod]
        public void ResetClearsAllSoftSwitchStates()
        {
            var (bus, _, state) = CreateWithDevices();
            state.State[SoftSwitch.AuxRead] = true;
            state.State[SoftSwitch.AuxWrite] = true;
            state.State[SoftSwitch.ZpAux] = true;
            bus.Reset();
            // All switches are forced to false, then device resets re-apply their defaults
            Assert.IsFalse(state.State[SoftSwitch.AuxRead]);
            Assert.IsFalse(state.State[SoftSwitch.AuxWrite]);
            Assert.IsFalse(state.State[SoftSwitch.ZpAux]);
        }

        [TestMethod]
        public void ResetAppliesIouDefaults()
        {
            var (bus, _, state) = CreateWithDevices();
            state.State[SoftSwitch.TextMode] = false;
            bus.Reset();
            // IOU.Reset() sets TextMode=true and IOUDisabled=true
            Assert.IsTrue(state.State[SoftSwitch.TextMode]);
            Assert.IsTrue(state.State[SoftSwitch.IOUDisabled]);
        }

        [TestMethod]
        public void ResetAppliesMmuDefaults()
        {
            var (bus, _, state) = CreateWithDevices();
            state.State[SoftSwitch.LcBank2] = false;
            bus.Reset();
            // MMU.Reset() sets LcBank2=true
            Assert.IsTrue(state.State[SoftSwitch.LcBank2]);
        }

        // ------------------------------------------------------------------ //
        // Peek / Poke — bypass devices, go straight to Memory128k
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void PokeThenPeekRoundTrips()
        {
            var (bus, _, _) = CreateBare();
            bus.Poke(0x0300, 0x42);
            Assert.AreEqual((byte)0x42, bus.Peek(0x0300));
        }

        [TestMethod]
        public void PeekAndPokeDoNotTriggerSoftSwitches()
        {
            var (bus, _, state) = CreateWithDevices();
            // Poke to $C051 (TXTSET) — if bus processed it as a soft-switch it
            // would set TextMode=true. Peek/Poke bypass devices entirely.
            state.State[SoftSwitch.TextMode] = false;
            bus.Poke(SoftSwitchAddress.TXTSET, 0xFF);
            Assert.IsFalse(state.State[SoftSwitch.TextMode]);
        }

        // ------------------------------------------------------------------ //
        // Read routing — main memory
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadBelowC000RoutesToMemory()
        {
            var (bus, _, _) = CreateBare();
            bus.Poke(0x0500, 0xAB);
            Assert.AreEqual((byte)0xAB, bus.Read(0x0500));
        }

        [TestMethod]
        public void ReadFromRomRangeReturnsMemoryContent()
        {
            var rom = new byte[16 * 1024];
            rom[0x2000] = 0xEA;  // intEFRom[0] → page $E0, offset 0

            var (memory, state) = Memory128kFactory.CreateWith16kRom(rom);
            var bus = new AppleBus(memory, []);

            Assert.AreEqual((byte)0xEA, bus.Read(0xE000));
        }

        // ------------------------------------------------------------------ //
        // Read routing — soft switches ($C000–$C08F)
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadC015RoutesThroughMmuReturnsIntCxRomStatus()
        {
            var (bus, _, state) = CreateWithDevices();
            state.State[SoftSwitch.IntCxRomEnabled] = false;
            Assert.AreEqual((byte)0x00, bus.Read(SoftSwitchAddress.RDCXROM));
        }

        [TestMethod]
        public void ReadC015ReturnsHighWhenIntCxRomEnabled()
        {
            var (bus, _, state) = CreateWithDevices();
            state.State[SoftSwitch.IntCxRomEnabled] = true;
            Assert.AreEqual((byte)0x80, bus.Read(SoftSwitchAddress.RDCXROM));
        }

        [TestMethod]
        public void ReadC050RoutesThroughIouClearingTextMode()
        {
            var (bus, _, state) = CreateWithDevices();
            state.State[SoftSwitch.TextMode] = true;
            bus.Read(SoftSwitchAddress.TXTCLR);
            Assert.IsFalse(state.State[SoftSwitch.TextMode]);
        }

        [TestMethod]
        public void ReadC01ASoftSwitchReturnsTextModeStatus()
        {
            var (bus, _, state) = CreateWithDevices();
            state.State[SoftSwitch.TextMode] = true;
            Assert.AreEqual((byte)0x80, bus.Read(SoftSwitchAddress.RDTEXT));
        }

        // ------------------------------------------------------------------ //
        // Read routing — slot I/O ($C090–$C0FF)
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadC090ToC0FFWithNoSlotDeviceReturnsFF()
        {
            var (bus, _, _) = CreateBare();
            Assert.AreEqual((byte)0xFF, bus.Read(0xC090));
        }

        // ------------------------------------------------------------------ //
        // Write routing — soft switches ($C000–$C08F)
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void WriteC001RoutesThroughMmuSetsStore80()
        {
            var (bus, _, state) = CreateWithDevices();
            bus.Write(SoftSwitchAddress.SET80STORE, 0);
            Assert.IsTrue(state.State[SoftSwitch.Store80]);
        }

        [TestMethod]
        public void WriteC051RoutesThroughIouSetsTextMode()
        {
            var (bus, _, state) = CreateWithDevices();
            state.State[SoftSwitch.TextMode] = false;
            bus.Write(SoftSwitchAddress.TXTSET, 0);
            Assert.IsTrue(state.State[SoftSwitch.TextMode]);
        }

        // ------------------------------------------------------------------ //
        // Write routing — main memory
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void WriteBelowC000RoutesToMemory()
        {
            var (bus, _, _) = CreateBare();
            bus.Write(0x0500, 0xCC);
            Assert.AreEqual((byte)0xCC, bus.Peek(0x0500));
        }

        // ------------------------------------------------------------------ //
        // LoadProgramToRom / LoadProgramToRam
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void LoadProgramToRomIsReadableAtEFRange()
        {
            var (bus, _, _) = CreateBare();
            var rom = new byte[16 * 1024];
            rom[8 * 1024] = 0xFE; // intEFRom[0] → $E000
            bus.LoadProgramToRom(rom);
            Assert.AreEqual((byte)0xFE, bus.Read(0xE000));
        }

        [TestMethod]
        public void LoadProgramToRamIsReadableViaRead()
        {
            var (bus, _, _) = CreateBare();
            var program = new byte[] { 0xEA, 0x60 };
            bus.LoadProgramToRam(program, 0x0300);
            Assert.AreEqual((byte)0xEA, bus.Read(0x0300));
            Assert.AreEqual((byte)0x60, bus.Read(0x0301));
        }

        // ------------------------------------------------------------------ //
        // AddDevice — soft-switch
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void AddSoftSwitchDeviceCallsResetOnDevice()
        {
            // MMU.Reset sets LcBank2=true; verify it was called on AddDevice
            var (memory, state) = Memory128kFactory.CreateDefault();
            state.State[SoftSwitch.LcBank2] = false;
            var bus = new AppleBus(memory, []);
            _ = new MMU(memory, state, bus); // constructor calls bus.AddDevice(this)
            Assert.IsTrue(state.State[SoftSwitch.LcBank2]);
        }
    }
}

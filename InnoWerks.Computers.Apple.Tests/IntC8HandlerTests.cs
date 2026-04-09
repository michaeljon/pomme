using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class IntC8HandlerTests
    {
        //
        // Helpers
        //

        private static (IntC8Handler Handler, Memory128k Memory, MachineState State) CreateHandler()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();
            var bus = new AppleBusTestDouble();
            var handler = new IntC8Handler(memory, state, bus);
            return (handler, memory, state);
        }

        //
        // Name / identity
        //

        [TestMethod]
        public void NameIsIntC8Handler()
        {
            var (handler, _, _) = CreateHandler();
            Assert.AreEqual("IntC8Handler", handler.Name);
        }

        //
        // InterceptPriority
        //

        [TestMethod]
        public void PriorityIsSoftSwitch()
        {
            var (handler, _, _) = CreateHandler();
            Assert.AreEqual(InterceptPriority.SoftSwitch, handler.InterceptPriority);
        }

        //
        // Reset
        //

        [TestMethod]
        public void ResetClearsIntC8RomEnabled()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.IntC8RomEnabled] = true;
            handler.Reset();
            Assert.IsFalse(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        [TestMethod]
        public void ResetClearsIntCxRomEnabled()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.IntCxRomEnabled] = true;
            handler.Reset();
            Assert.IsFalse(state.State[SoftSwitch.IntCxRomEnabled]);
        }

        [TestMethod]
        public void ResetSetsCurrentSlotToZero()
        {
            var (handler, _, state) = CreateHandler();
            state.CurrentSlot = 3;
            handler.Reset();
            Assert.AreEqual(0, state.CurrentSlot);
        }

        [TestMethod]
        public void ResetSetsExpansionRomTypeToInternal()
        {
            var (handler, _, state) = CreateHandler();
            state.ExpansionRomType = ExpansionRomType.ExpRomPeripheral;
            handler.Reset();
            Assert.AreEqual(ExpansionRomType.ExpRomInternal, state.ExpansionRomType);
        }

        //
        // AddressRanges — covers $C300-$C3FF and $CFFF
        //

        [TestMethod]
        public void AddressRangesContainsC300ForRead()
        {
            var (handler, _, _) = CreateHandler();
            Assert.Contains(r => r.InterestedIn(0xC300, MemoryAccessType.Read), handler.AddressRanges);
        }

        [TestMethod]
        public void AddressRangesContainsC3FFForRead()
        {
            var (handler, _, _) = CreateHandler();
            Assert.Contains(r => r.InterestedIn(0xC3FF, MemoryAccessType.Read), handler.AddressRanges);
        }

        [TestMethod]
        public void AddressRangesContainsCFFFForRead()
        {
            var (handler, _, _) = CreateHandler();
            Assert.Contains(r => r.InterestedIn(0xCFFF, MemoryAccessType.Read), handler.AddressRanges);
        }

        [TestMethod]
        public void AddressRangesContainsC300ForWrite()
        {
            var (handler, _, _) = CreateHandler();
            Assert.Contains(r => r.InterestedIn(0xC300, MemoryAccessType.Write), handler.AddressRanges);
        }

        [TestMethod]
        public void AddressRangesContainsCFFFForWrite()
        {
            var (handler, _, _) = CreateHandler();
            Assert.Contains(r => r.InterestedIn(0xCFFF, MemoryAccessType.Write), handler.AddressRanges);
        }

        [TestMethod]
        public void AddressRangesDoesNotContainC2FF()
        {
            var (handler, _, _) = CreateHandler();
            Assert.DoesNotContain(r => r.InterestedIn(0xC2FF, MemoryAccessType.Read), handler.AddressRanges);
        }

        [TestMethod]
        public void AddressRangesDoesNotContainC400()
        {
            var (handler, _, _) = CreateHandler();
            Assert.DoesNotContain(r => r.InterestedIn(0xC400, MemoryAccessType.Read), handler.AddressRanges);
        }

        [TestMethod]
        public void AddressRangesDoesNotContainCFFE()
        {
            var (handler, _, _) = CreateHandler();
            Assert.DoesNotContain(r => r.InterestedIn(0xCFFE, MemoryAccessType.Read), handler.AddressRanges);
        }

        //
        // DoRead $C300–$C3FF — enables IntC8RomEnabled when SlotC3Rom is off
        //

        [TestMethod]
        public void DoReadC300WithSlotC3RomDisabledEnablesIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = false;
            state.State[SoftSwitch.IntC8RomEnabled] = false;

            handler.DoRead(0xC300, out _);

            Assert.IsTrue(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        [TestMethod]
        public void DoReadC3FFWithSlotC3RomDisabledEnablesIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = false;
            state.State[SoftSwitch.IntC8RomEnabled] = false;

            handler.DoRead(0xC3FF, out _);

            Assert.IsTrue(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        [TestMethod]
        public void DoReadC300WithSlotC3RomEnabledDoesNotEnableIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = true;
            state.State[SoftSwitch.IntC8RomEnabled] = false;

            handler.DoRead(0xC300, out _);

            Assert.IsFalse(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        [TestMethod]
        public void DoReadC300WhenIntC8RomAlreadyEnabledDoesNotChangeState()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = false;
            state.State[SoftSwitch.IntC8RomEnabled] = true;

            handler.DoRead(0xC300, out _);

            Assert.IsTrue(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        //
        // DoRead $CFFF — disables IntC8RomEnabled
        //

        [TestMethod]
        public void DoReadCfffDisablesIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.IntC8RomEnabled] = true;

            handler.DoRead(0xCFFF, out _);

            Assert.IsFalse(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        [TestMethod]
        public void DoReadCfffWhenAlreadyDisabledLeavesIntC8RomDisabled()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.IntC8RomEnabled] = false;

            handler.DoRead(0xCFFF, out _);

            Assert.IsFalse(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        //
        // DoWrite $C300–$C3FF — same enable logic as DoRead
        //

        [TestMethod]
        public void DoWriteC300WithSlotC3RomDisabledEnablesIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = false;
            state.State[SoftSwitch.IntC8RomEnabled] = false;

            handler.DoWrite(0xC300, 0);

            Assert.IsTrue(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        [TestMethod]
        public void DoWriteC300WithSlotC3RomEnabledDoesNotEnableIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = true;
            state.State[SoftSwitch.IntC8RomEnabled] = false;

            handler.DoWrite(0xC300, 0);

            Assert.IsFalse(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        //
        // DoWrite $CFFF — disables IntC8RomEnabled
        //

        [TestMethod]
        public void DoWriteCfffDisablesIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.IntC8RomEnabled] = true;

            handler.DoWrite(0xCFFF, 0);

            Assert.IsFalse(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        //
        // DoRead/DoWrite return value — handler observes but does not intercept
        //

        [TestMethod]
        public void DoReadC300ReturnsFalse()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = false;

            var handled = handler.DoRead(0xC300, out _);

            Assert.IsFalse(handled, "IntC8Handler should not intercept reads — it only observes");
        }

        [TestMethod]
        public void DoReadCfffReturnsFalse()
        {
            var (handler, _, _) = CreateHandler();

            var handled = handler.DoRead(0xCFFF, out _);

            Assert.IsFalse(handled, "IntC8Handler should not intercept reads — it only observes");
        }

        [TestMethod]
        public void DoWriteC300ReturnsFalse()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = false;

            var handled = handler.DoWrite(0xC300, 0);

            Assert.IsFalse(handled, "IntC8Handler should not intercept writes — it only observes");
        }

        //
        // State isolation — enabling C8 ROM changes active memory map
        //

        [TestMethod]
        public void DoReadC300WithSlotC3RomDisabledRemapsC8Range()
        {
            var state = MachineStateBuilder.Default().Build();
            var rom = new byte[16 * 1024];
            rom[0x800] = 0xAB;
            var memory = Memory128kFactory.CreateWithState(state);
            memory.LoadProgramToRom(rom);

            var bus = new AppleBusTestDouble();
            var handler = new IntC8Handler(memory, state, bus);

            state.State[SoftSwitch.SlotC3RomEnabled] = false;
            handler.DoRead(0xC300, out _);

            var page = memory.ResolveRead(0xC800);
            Assert.IsNotNull(page);
            Assert.AreEqual(MemoryPageType.Rom, page.MemoryPageType);
        }

        [TestMethod]
        public void DoReadCfffDisablesC8RangeRemap()
        {
            var state = MachineStateBuilder.Default().Build();
            var memory = Memory128kFactory.CreateWithState(state);

            var bus = new AppleBusTestDouble();
            var handler = new IntC8Handler(memory, state, bus);

            state.State[SoftSwitch.IntC8RomEnabled] = true;
            memory.Remap();
            handler.DoRead(0xCFFF, out _);

            var page = memory.ResolveRead(0xC800);
            Assert.IsNull(page);
        }
    }
}

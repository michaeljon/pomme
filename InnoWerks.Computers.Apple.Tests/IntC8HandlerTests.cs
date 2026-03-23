using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class IntC8HandlerTests
    {
        // ------------------------------------------------------------------ //
        // Helpers
        // ------------------------------------------------------------------ //

        private static (IntC8Handler Handler, Memory128k Memory, MachineState State) CreateHandler()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();
            var bus = new AppleBusTestDouble();
            var handler = new IntC8Handler(memory, state, bus);
            return (handler, memory, state);
        }

        // ------------------------------------------------------------------ //
        // Name / identity
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void NameIsIntC8Handler()
        {
            var (handler, _, _) = CreateHandler();
            Assert.AreEqual("IntC8Handler", handler.Name);
        }

        // ------------------------------------------------------------------ //
        // Reset
        // ------------------------------------------------------------------ //

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

        // ------------------------------------------------------------------ //
        // HandlesRead
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void HandlesReadReturnsTrueForC300()
        {
            var (handler, _, _) = CreateHandler();
            Assert.IsTrue(handler.HandlesRead(0xC300));
        }

        [TestMethod]
        public void HandlesReadReturnsTrueForC3FF()
        {
            var (handler, _, _) = CreateHandler();
            Assert.IsTrue(handler.HandlesRead(0xC3FF));
        }

        [TestMethod]
        public void HandlesReadReturnsTrueForCFFF()
        {
            var (handler, _, _) = CreateHandler();
            Assert.IsTrue(handler.HandlesRead(0xCFFF));
        }

        [TestMethod]
        public void HandlesReadReturnsFalseForC2FF()
        {
            var (handler, _, _) = CreateHandler();
            Assert.IsFalse(handler.HandlesRead(0xC2FF));
        }

        [TestMethod]
        public void HandlesReadReturnsFalseForC400()
        {
            var (handler, _, _) = CreateHandler();
            Assert.IsFalse(handler.HandlesRead(0xC400));
        }

        [TestMethod]
        public void HandlesReadReturnsFalseForCFFE()
        {
            var (handler, _, _) = CreateHandler();
            Assert.IsFalse(handler.HandlesRead(0xCFFE));
        }

        // ------------------------------------------------------------------ //
        // HandlesWrite
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void HandlesWriteReturnsTrueForC300()
        {
            var (handler, _, _) = CreateHandler();
            Assert.IsTrue(handler.HandlesWrite(0xC300));
        }

        [TestMethod]
        public void HandlesWriteReturnsTrueForC3FF()
        {
            var (handler, _, _) = CreateHandler();
            Assert.IsTrue(handler.HandlesWrite(0xC3FF));
        }

        [TestMethod]
        public void HandlesWriteReturnsTrueForCFFF()
        {
            var (handler, _, _) = CreateHandler();
            Assert.IsTrue(handler.HandlesWrite(0xCFFF));
        }

        [TestMethod]
        public void HandlesWriteReturnsFalseForC200()
        {
            var (handler, _, _) = CreateHandler();
            Assert.IsFalse(handler.HandlesWrite(0xC200));
        }

        // ------------------------------------------------------------------ //
        // Read $C300–$C3FF — enables IntC8RomEnabled when SlotC3Rom is off
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadC300WithSlotC3RomDisabledEnablesIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = false;
            state.State[SoftSwitch.IntC8RomEnabled] = false;

            handler.Read(0xC300);

            Assert.IsTrue(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        [TestMethod]
        public void ReadC3FFWithSlotC3RomDisabledEnablesIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = false;
            state.State[SoftSwitch.IntC8RomEnabled] = false;

            handler.Read(0xC3FF);

            Assert.IsTrue(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        [TestMethod]
        public void ReadC300WithSlotC3RomEnabledDoesNotEnableIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = true;
            state.State[SoftSwitch.IntC8RomEnabled] = false;

            handler.Read(0xC300);

            Assert.IsFalse(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        [TestMethod]
        public void ReadC300WhenIntC8RomAlreadyEnabledDoesNotChangState()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = false;
            state.State[SoftSwitch.IntC8RomEnabled] = true; // already enabled

            handler.Read(0xC300);

            Assert.IsTrue(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        // ------------------------------------------------------------------ //
        // Read $CFFF — disables IntC8RomEnabled
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadCfffDisablesIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.IntC8RomEnabled] = true;

            handler.Read(0xCFFF);

            Assert.IsFalse(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        [TestMethod]
        public void ReadCfffWhenAlreadyDisabledLeavesIntC8RomDisabled()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.IntC8RomEnabled] = false;

            handler.Read(0xCFFF);

            Assert.IsFalse(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        // ------------------------------------------------------------------ //
        // Write $C300–$C3FF — same enable logic as read
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void WriteC300WithSlotC3RomDisabledEnablesIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = false;
            state.State[SoftSwitch.IntC8RomEnabled] = false;

            handler.Write(0xC300, 0);

            Assert.IsTrue(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        [TestMethod]
        public void WriteC300WithSlotC3RomEnabledDoesNotEnableIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.SlotC3RomEnabled] = true;
            state.State[SoftSwitch.IntC8RomEnabled] = false;

            handler.Write(0xC300, 0);

            Assert.IsFalse(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        // ------------------------------------------------------------------ //
        // Write $CFFF — disables IntC8RomEnabled
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void WriteCfffDisablesIntC8Rom()
        {
            var (handler, _, state) = CreateHandler();
            state.State[SoftSwitch.IntC8RomEnabled] = true;

            handler.Write(0xCFFF, 0);

            Assert.IsFalse(state.State[SoftSwitch.IntC8RomEnabled]);
        }

        // ------------------------------------------------------------------ //
        // State isolation — enabling C8 ROM changes active memory map
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadC300WithSlotC3RomDisabledRemapsC8Range()
        {
            // After enabling IntC8RomEnabled the active read map at $C8–$CF
            // should no longer be null. Load a 16k ROM so intCxRom has data.
            var state = MachineStateBuilder.Default().Build();
            var rom = new byte[16 * 1024];
            rom[0x800] = 0xAB;   // intCxRom[8] = page $C8, offset 0
            var memory = Memory128kFactory.CreateWithState(state);
            memory.LoadProgramToRom(rom);

            var bus = new AppleBusTestDouble();
            var handler = new IntC8Handler(memory, state, bus);

            state.State[SoftSwitch.SlotC3RomEnabled] = false;
            handler.Read(0xC300); // enables IntC8RomEnabled and triggers Remap

            // $C800 should now resolve to intCxRom[8]
            var page = memory.ResolveRead(0xC800);
            Assert.IsNotNull(page);
            Assert.AreEqual(MemoryPageType.Rom, page.MemoryPageType);
        }

        [TestMethod]
        public void ReadCfffDisablesC8RangeRemap()
        {
            var state = MachineStateBuilder.Default().Build();
            var memory = Memory128kFactory.CreateWithState(state);

            var bus = new AppleBusTestDouble();
            var handler = new IntC8Handler(memory, state, bus);

            // First enable, then disable
            state.State[SoftSwitch.IntC8RomEnabled] = true;
            memory.Remap();
            handler.Read(0xCFFF); // disables IntC8RomEnabled, triggers Remap

            // With CurrentSlot=0 and IntC8RomEnabled=false, $C8–$CF is null
            var page = memory.ResolveRead(0xC800);
            Assert.IsNull(page);
        }
    }
}

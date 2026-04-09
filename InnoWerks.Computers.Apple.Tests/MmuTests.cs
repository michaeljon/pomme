using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class MmuTests
    {
        //
        // Helpers
        //

        private static (MMU Mmu, Memory128k Memory, MachineState State) CreateMmu()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();
            var bus = new AppleBusTestDouble();
            var mmu = new MMU(memory, state, bus);
            return (mmu, memory, state);
        }

        private static bool AddressInRange(MMU mmu, ushort address, MemoryAccessType accessType) =>
            mmu.AddressRanges.Any(r => r.InterestedIn(address, accessType));

        private static byte DoRead(MMU mmu, ushort address)
        {
            mmu.DoRead(address, out var value);
            return value;
        }

        //
        // Name / identity
        //

        [TestMethod]
        public void NameIsMmu()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.AreEqual("MMU", mmu.Name);
        }

        //
        // Reset
        //

        [TestMethod]
        public void ResetEnablesLcBank2()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.LcBank2] = false;
            mmu.Reset();
            Assert.IsTrue(state.State[SoftSwitch.LcBank2]);
        }

        [TestMethod]
        public void ResetDoesNotEnableLcReadOrWrite()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.LcReadEnabled] = true;
            state.State[SoftSwitch.LcWriteEnabled] = true;
            mmu.Reset();
            Assert.IsTrue(state.State[SoftSwitch.LcReadEnabled]);
            Assert.IsTrue(state.State[SoftSwitch.LcWriteEnabled]);
        }

        //
        // AddressRanges
        //

        [TestMethod]
        public void AddressRangesContainsRdLcBnk2ForRead()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(AddressInRange(mmu, SoftSwitchAddress.RDLCBNK2, MemoryAccessType.Read));
        }

        [TestMethod]
        public void AddressRangesContainsRdLcRamForRead()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(AddressInRange(mmu, SoftSwitchAddress.RDLCRAM, MemoryAccessType.Read));
        }

        [TestMethod]
        public void AddressRangesContainsRdRamRdForRead()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(AddressInRange(mmu, SoftSwitchAddress.RDRAMRD, MemoryAccessType.Read));
        }

        [TestMethod]
        public void AddressRangesContainsRdRamWrtForRead()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(AddressInRange(mmu, SoftSwitchAddress.RDRAMWRT, MemoryAccessType.Read));
        }

        [TestMethod]
        public void AddressRangesContainsRdAltStkZpForRead()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(AddressInRange(mmu, SoftSwitchAddress.RDALTSTKZP, MemoryAccessType.Read));
        }

        [TestMethod]
        public void AddressRangesContainsC080ForRead()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(AddressInRange(mmu, 0xC080, MemoryAccessType.Read));
        }

        [TestMethod]
        public void AddressRangesContainsC08FForRead()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(AddressInRange(mmu, 0xC08F, MemoryAccessType.Read));
        }

        [TestMethod]
        public void AddressRangesDoesNotContainC090()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsFalse(AddressInRange(mmu, 0xC090, MemoryAccessType.Read));
        }

        [TestMethod]
        public void AddressRangesDoesNotContainAddressBelowC000()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsFalse(AddressInRange(mmu, 0x1000, MemoryAccessType.Read));
        }

        //
        // AddressRanges — write
        //

        [TestMethod]
        public void AddressRangesContainsClr80StoreForWrite()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(AddressInRange(mmu, SoftSwitchAddress.CLR80STORE, MemoryAccessType.Write));
        }

        [TestMethod]
        public void AddressRangesContainsC080ForWrite()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(AddressInRange(mmu, 0xC080, MemoryAccessType.Write));
        }

        [TestMethod]
        public void AddressRangesDoesNotContainC090ForWrite()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsFalse(AddressInRange(mmu, 0xC090, MemoryAccessType.Write));
        }

        //
        // Status register reads
        //

        [TestMethod]
        public void ReadRdLcBnk2Returns0x80WhenLcBank2IsTrue()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.LcBank2] = true;
            Assert.AreEqual((byte)0x80, DoRead(mmu, SoftSwitchAddress.RDLCBNK2));
        }

        [TestMethod]
        public void ReadRdLcBnk2Returns0x00WhenLcBank2IsFalse()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.LcBank2] = false;
            Assert.AreEqual((byte)0x00, DoRead(mmu, SoftSwitchAddress.RDLCBNK2));
        }

        [TestMethod]
        public void ReadRdLcRamReflectsLcReadEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.LcReadEnabled] = true;
            Assert.AreEqual((byte)0x80, DoRead(mmu, SoftSwitchAddress.RDLCRAM));
        }

        [TestMethod]
        public void ReadRdRamRdReflectsAuxRead()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.AuxRead] = true;
            Assert.AreEqual((byte)0x80, DoRead(mmu, SoftSwitchAddress.RDRAMRD));
        }

        [TestMethod]
        public void ReadRdRamWrtReflectsAuxWrite()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.AuxWrite] = true;
            Assert.AreEqual((byte)0x80, DoRead(mmu, SoftSwitchAddress.RDRAMWRT));
        }

        [TestMethod]
        public void ReadRdAltStkZpReflectsZpAux()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.ZpAux] = true;
            Assert.AreEqual((byte)0x80, DoRead(mmu, SoftSwitchAddress.RDALTSTKZP));
        }

        [TestMethod]
        public void ReadRd80StoreReflectsStore80()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.Store80] = true;
            Assert.AreEqual((byte)0x80, DoRead(mmu, SoftSwitchAddress.RD80STORE));
        }

        [TestMethod]
        public void ReadRdCxRomReflectsIntCxRomEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.IntCxRomEnabled] = true;
            Assert.AreEqual((byte)0x80, DoRead(mmu, SoftSwitchAddress.RDCXROM));
        }

        [TestMethod]
        public void ReadRdC3RomReflectsSlotC3RomEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.SlotC3RomEnabled] = true;
            Assert.AreEqual((byte)0x80, DoRead(mmu, SoftSwitchAddress.RDC3ROM));
        }

        //
        // Write switches — memory banking
        //

        [TestMethod]
        public void WriteClr80StoreClearsStore80()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.Store80] = true;
            mmu.DoWrite(SoftSwitchAddress.CLR80STORE, 0);
            Assert.IsFalse(state.State[SoftSwitch.Store80]);
        }

        [TestMethod]
        public void WriteSet80StoreSetsStore80()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.DoWrite(SoftSwitchAddress.SET80STORE, 0);
            Assert.IsTrue(state.State[SoftSwitch.Store80]);
        }

        [TestMethod]
        public void WriteRdMainRamClearsAuxRead()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.AuxRead] = true;
            mmu.DoWrite(SoftSwitchAddress.RDMAINRAM, 0);
            Assert.IsFalse(state.State[SoftSwitch.AuxRead]);
        }

        [TestMethod]
        public void WriteRdCardRamSetsAuxRead()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.DoWrite(SoftSwitchAddress.RDCARDRAM, 0);
            Assert.IsTrue(state.State[SoftSwitch.AuxRead]);
        }

        [TestMethod]
        public void WriteWrMainRamClearsAuxWrite()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.AuxWrite] = true;
            mmu.DoWrite(SoftSwitchAddress.WRMAINRAM, 0);
            Assert.IsFalse(state.State[SoftSwitch.AuxWrite]);
        }

        [TestMethod]
        public void WriteWrCardRamSetsAuxWrite()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.DoWrite(SoftSwitchAddress.WRCARDRAM, 0);
            Assert.IsTrue(state.State[SoftSwitch.AuxWrite]);
        }

        [TestMethod]
        public void WriteClrAltStkZpClearsZpAux()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.ZpAux] = true;
            mmu.DoWrite(SoftSwitchAddress.CLRALSTKZP, 0);
            Assert.IsFalse(state.State[SoftSwitch.ZpAux]);
        }

        [TestMethod]
        public void WriteSetAltStkZpSetsZpAux()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.DoWrite(SoftSwitchAddress.SETALTSTKZP, 0);
            Assert.IsTrue(state.State[SoftSwitch.ZpAux]);
        }

        //
        // Write switches — CxROM banking
        //

        [TestMethod]
        public void WriteSetSlotCxRomClearsIntCxRomEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.IntCxRomEnabled] = true;
            mmu.DoWrite(SoftSwitchAddress.SETSLOTCXROM, 0);
            Assert.IsFalse(state.State[SoftSwitch.IntCxRomEnabled]);
        }

        [TestMethod]
        public void WriteSetIntCxRomSetsIntCxRomEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.DoWrite(SoftSwitchAddress.SETINTCXROM, 0);
            Assert.IsTrue(state.State[SoftSwitch.IntCxRomEnabled]);
        }

        [TestMethod]
        public void WriteSetIntC3RomClearsSlotC3RomEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.SlotC3RomEnabled] = true;
            mmu.DoWrite(SoftSwitchAddress.SETINTC3ROM, 0);
            Assert.IsFalse(state.State[SoftSwitch.SlotC3RomEnabled]);
        }

        [TestMethod]
        public void WriteSetSlotC3RomSetsSlotC3RomEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.DoWrite(SoftSwitchAddress.SETSLOTC3ROM, 0);
            Assert.IsTrue(state.State[SoftSwitch.SlotC3RomEnabled]);
        }

        //
        // Language Card sequencing — $C080–$C08F
        //

        [TestMethod]
        public void ReadC080SelectsBank2AndEnablesReadOnly()
        {
            var (mmu, _, state) = CreateMmu();
            DoRead(mmu, 0xC080);
            Assert.IsTrue(state.State[SoftSwitch.LcBank2]);
            Assert.IsTrue(state.State[SoftSwitch.LcReadEnabled]);
            Assert.IsFalse(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void ReadC081FirstAccessDoesNotEnableWrite()
        {
            var (mmu, _, state) = CreateMmu();
            DoRead(mmu, 0xC081);
            Assert.IsFalse(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void ReadC081TwiceEnablesLcWrite()
        {
            var (mmu, _, state) = CreateMmu();
            DoRead(mmu, 0xC081);
            DoRead(mmu, 0xC081);
            Assert.IsTrue(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void ReadC081TwiceDisablesLcRead()
        {
            var (mmu, _, state) = CreateMmu();
            DoRead(mmu, 0xC081);
            DoRead(mmu, 0xC081);
            Assert.IsFalse(state.State[SoftSwitch.LcReadEnabled]);
        }

        [TestMethod]
        public void ReadC082DisablesLcReadAndWrite()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.LcReadEnabled] = true;
            state.State[SoftSwitch.LcWriteEnabled] = true;
            DoRead(mmu, 0xC082);
            Assert.IsFalse(state.State[SoftSwitch.LcReadEnabled]);
            Assert.IsFalse(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void ReadC083TwiceEnablesBothReadAndWrite()
        {
            var (mmu, _, state) = CreateMmu();
            DoRead(mmu, 0xC083);
            DoRead(mmu, 0xC083);
            Assert.IsTrue(state.State[SoftSwitch.LcReadEnabled]);
            Assert.IsTrue(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void WriteC08xClearsPrewriteSoSubsequentSingleReadDoesNotEnableWrite()
        {
            var (mmu, _, state) = CreateMmu();
            DoRead(mmu, 0xC081);      // sets preWrite
            mmu.DoWrite(0xC081, 0);    // clears preWrite
            DoRead(mmu, 0xC081);      // only one read — should not enable write
            Assert.IsFalse(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void ReadC088SelectsBank1()
        {
            var (mmu, _, state) = CreateMmu();
            DoRead(mmu, 0xC088);
            Assert.IsFalse(state.State[SoftSwitch.LcBank2]);
        }

        [TestMethod]
        public void ReadC08BTwiceEnablesWriteWithBank1()
        {
            var (mmu, _, state) = CreateMmu();
            DoRead(mmu, 0xC08B);
            DoRead(mmu, 0xC08B);
            Assert.IsFalse(state.State[SoftSwitch.LcBank2]);
            Assert.IsTrue(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void ReadC08ATwiceDoesNotEnableWriteBecauseA0IsZero()
        {
            var (mmu, _, state) = CreateMmu();
            DoRead(mmu, 0xC08A);
            DoRead(mmu, 0xC08A);
            Assert.IsFalse(state.State[SoftSwitch.LcWriteEnabled]);
        }
    }
}

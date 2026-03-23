using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class MmuTests
    {
        // ------------------------------------------------------------------ //
        // Helpers
        // ------------------------------------------------------------------ //

        private static (MMU Mmu, Memory128k Memory, MachineState State) CreateMmu()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();
            var bus = new AppleBusTestDouble();
            var mmu = new MMU(memory, state, bus);
            return (mmu, memory, state);
        }

        // ------------------------------------------------------------------ //
        // Name / identity
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void NameIsMmu()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.AreEqual("MMU", mmu.Name);
        }

        // ------------------------------------------------------------------ //
        // Reset
        // ------------------------------------------------------------------ //

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
            // Reset only sets LcBank2; it does not touch LcReadEnabled / LcWriteEnabled
            Assert.IsTrue(state.State[SoftSwitch.LcReadEnabled]);
            Assert.IsTrue(state.State[SoftSwitch.LcWriteEnabled]);
        }

        // ------------------------------------------------------------------ //
        // HandlesRead
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void HandlesReadReturnsTrueForRdLcBnk2()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(mmu.HandlesRead(SoftSwitchAddress.RDLCBNK2));
        }

        [TestMethod]
        public void HandlesReadReturnsTrueForRdLcRam()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(mmu.HandlesRead(SoftSwitchAddress.RDLCRAM));
        }

        [TestMethod]
        public void HandlesReadReturnsTrueForRdRamRd()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(mmu.HandlesRead(SoftSwitchAddress.RDRAMRD));
        }

        [TestMethod]
        public void HandlesReadReturnsTrueForRdRamWrt()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(mmu.HandlesRead(SoftSwitchAddress.RDRAMWRT));
        }

        [TestMethod]
        public void HandlesReadReturnsTrueForRdAltStkZp()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(mmu.HandlesRead(SoftSwitchAddress.RDALTSTKZP));
        }

        [TestMethod]
        public void HandlesReadReturnsTrueForC080()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(mmu.HandlesRead(0xC080));
        }

        [TestMethod]
        public void HandlesReadReturnsTrueForC08F()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(mmu.HandlesRead(0xC08F));
        }

        [TestMethod]
        public void HandlesReadReturnsFalseForC090()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsFalse(mmu.HandlesRead(0xC090));
        }

        [TestMethod]
        public void HandlesReadReturnsFalseForAddressBelowC000()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsFalse(mmu.HandlesRead(0x1000));
        }

        // ------------------------------------------------------------------ //
        // HandlesWrite
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void HandlesWriteReturnsTrueForClr80Store()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(mmu.HandlesWrite(SoftSwitchAddress.CLR80STORE));
        }

        [TestMethod]
        public void HandlesWriteReturnsTrueForC080()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsTrue(mmu.HandlesWrite(0xC080));
        }

        [TestMethod]
        public void HandlesWriteReturnsFalseForC090()
        {
            var (mmu, _, _) = CreateMmu();
            Assert.IsFalse(mmu.HandlesWrite(0xC090));
        }

        // ------------------------------------------------------------------ //
        // Status register reads — each returns 0x80 when set, 0x00 when clear
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadRdLcBnk2Returns0x80WhenLcBank2IsTrue()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.LcBank2] = true;
            Assert.AreEqual((byte)0x80, mmu.Read(SoftSwitchAddress.RDLCBNK2));
        }

        [TestMethod]
        public void ReadRdLcBnk2Returns0x00WhenLcBank2IsFalse()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.LcBank2] = false;
            Assert.AreEqual((byte)0x00, mmu.Read(SoftSwitchAddress.RDLCBNK2));
        }

        [TestMethod]
        public void ReadRdLcRamReflectsLcReadEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.LcReadEnabled] = true;
            Assert.AreEqual((byte)0x80, mmu.Read(SoftSwitchAddress.RDLCRAM));
        }

        [TestMethod]
        public void ReadRdRamRdReflectsAuxRead()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.AuxRead] = true;
            Assert.AreEqual((byte)0x80, mmu.Read(SoftSwitchAddress.RDRAMRD));
        }

        [TestMethod]
        public void ReadRdRamWrtReflectsAuxWrite()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.AuxWrite] = true;
            Assert.AreEqual((byte)0x80, mmu.Read(SoftSwitchAddress.RDRAMWRT));
        }

        [TestMethod]
        public void ReadRdAltStkZpReflectsZpAux()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.ZpAux] = true;
            Assert.AreEqual((byte)0x80, mmu.Read(SoftSwitchAddress.RDALTSTKZP));
        }

        [TestMethod]
        public void ReadRd80StoreReflectsStore80()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.Store80] = true;
            Assert.AreEqual((byte)0x80, mmu.Read(SoftSwitchAddress.RD80STORE));
        }

        [TestMethod]
        public void ReadRdCxRomReflectsIntCxRomEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.IntCxRomEnabled] = true;
            Assert.AreEqual((byte)0x80, mmu.Read(SoftSwitchAddress.RDCXROM));
        }

        [TestMethod]
        public void ReadRdC3RomReflectsSlotC3RomEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.SlotC3RomEnabled] = true;
            Assert.AreEqual((byte)0x80, mmu.Read(SoftSwitchAddress.RDC3ROM));
        }

        // ------------------------------------------------------------------ //
        // Write switches — memory banking
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void WriteClr80StoreClearsStore80()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.Store80] = true;
            mmu.Write(SoftSwitchAddress.CLR80STORE, 0);
            Assert.IsFalse(state.State[SoftSwitch.Store80]);
        }

        [TestMethod]
        public void WriteSet80StoreSetsStore80()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.Write(SoftSwitchAddress.SET80STORE, 0);
            Assert.IsTrue(state.State[SoftSwitch.Store80]);
        }

        [TestMethod]
        public void WriteRdMainRamClearsAuxRead()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.AuxRead] = true;
            mmu.Write(SoftSwitchAddress.RDMAINRAM, 0);
            Assert.IsFalse(state.State[SoftSwitch.AuxRead]);
        }

        [TestMethod]
        public void WriteRdCardRamSetsAuxRead()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.Write(SoftSwitchAddress.RDCARDRAM, 0);
            Assert.IsTrue(state.State[SoftSwitch.AuxRead]);
        }

        [TestMethod]
        public void WriteWrMainRamClearsAuxWrite()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.AuxWrite] = true;
            mmu.Write(SoftSwitchAddress.WRMAINRAM, 0);
            Assert.IsFalse(state.State[SoftSwitch.AuxWrite]);
        }

        [TestMethod]
        public void WriteWrCardRamSetsAuxWrite()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.Write(SoftSwitchAddress.WRCARDRAM, 0);
            Assert.IsTrue(state.State[SoftSwitch.AuxWrite]);
        }

        [TestMethod]
        public void WriteClrAltStkZpClearsZpAux()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.ZpAux] = true;
            mmu.Write(SoftSwitchAddress.CLRALSTKZP, 0);
            Assert.IsFalse(state.State[SoftSwitch.ZpAux]);
        }

        [TestMethod]
        public void WriteSetAltStkZpSetsZpAux()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.Write(SoftSwitchAddress.SETALTSTKZP, 0);
            Assert.IsTrue(state.State[SoftSwitch.ZpAux]);
        }

        // ------------------------------------------------------------------ //
        // Write switches — CxROM banking
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void WriteSetSlotCxRomClearsIntCxRomEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.IntCxRomEnabled] = true;
            mmu.Write(SoftSwitchAddress.SETSLOTCXROM, 0);
            Assert.IsFalse(state.State[SoftSwitch.IntCxRomEnabled]);
        }

        [TestMethod]
        public void WriteSetIntCxRomSetsIntCxRomEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.Write(SoftSwitchAddress.SETINTCXROM, 0);
            Assert.IsTrue(state.State[SoftSwitch.IntCxRomEnabled]);
        }

        [TestMethod]
        public void WriteSetIntC3RomClearsSlotC3RomEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.SlotC3RomEnabled] = true;
            mmu.Write(SoftSwitchAddress.SETINTC3ROM, 0);
            Assert.IsFalse(state.State[SoftSwitch.SlotC3RomEnabled]);
        }

        [TestMethod]
        public void WriteSetSlotC3RomSetsSlotC3RomEnabled()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.Write(SoftSwitchAddress.SETSLOTC3ROM, 0);
            Assert.IsTrue(state.State[SoftSwitch.SlotC3RomEnabled]);
        }

        // ------------------------------------------------------------------ //
        // Language Card sequencing — $C080–$C08F
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadC080SelectsBank2AndEnablesReadOnly()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.Read(0xC080);
            Assert.IsTrue(state.State[SoftSwitch.LcBank2]);
            Assert.IsTrue(state.State[SoftSwitch.LcReadEnabled]);
            Assert.IsFalse(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void ReadC081FirstAccessDoesNotEnableWrite()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.Read(0xC081);
            Assert.IsFalse(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void ReadC081TwiceEnablesLcWrite()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.Read(0xC081);
            mmu.Read(0xC081);
            Assert.IsTrue(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void ReadC081TwiceDisablesLcRead()
        {
            // $C081 A0A1=01, so LcReadEnabled = (low==0 || low==3) = false
            var (mmu, _, state) = CreateMmu();
            mmu.Read(0xC081);
            mmu.Read(0xC081);
            Assert.IsFalse(state.State[SoftSwitch.LcReadEnabled]);
        }

        [TestMethod]
        public void ReadC082DisablesLcReadAndWrite()
        {
            var (mmu, _, state) = CreateMmu();
            state.State[SoftSwitch.LcReadEnabled] = true;
            state.State[SoftSwitch.LcWriteEnabled] = true;
            mmu.Read(0xC082);
            Assert.IsFalse(state.State[SoftSwitch.LcReadEnabled]);
            Assert.IsFalse(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void ReadC083TwiceEnablesBothReadAndWrite()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.Read(0xC083);
            mmu.Read(0xC083);
            Assert.IsTrue(state.State[SoftSwitch.LcReadEnabled]);
            Assert.IsTrue(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void WriteC08xClearsPrewriteSoSubsequentSingleReadDoesNotEnableWrite()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.Read(0xC081);   // sets preWrite
            mmu.Write(0xC081, 0); // clears preWrite
            mmu.Read(0xC081);   // only one read — should not enable write
            Assert.IsFalse(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void ReadC088SelectsBank1()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.Read(0xC088); // A3=1 → bank 1
            Assert.IsFalse(state.State[SoftSwitch.LcBank2]);
        }

        [TestMethod]
        public void ReadC08BTwiceEnablesWriteWithBank1()
        {
            var (mmu, _, state) = CreateMmu();
            mmu.Read(0xC08B);
            mmu.Read(0xC08B);
            Assert.IsFalse(state.State[SoftSwitch.LcBank2]);
            Assert.IsTrue(state.State[SoftSwitch.LcWriteEnabled]);
        }

        [TestMethod]
        public void ReadC08ATwiceDoesNotEnableWriteBecauseA0IsZero()
        {
            // $C08A: A0=0 → preWrite cleared and LcWriteEnabled=false on every read
            var (mmu, _, state) = CreateMmu();
            mmu.Read(0xC08A);
            mmu.Read(0xC08A);
            Assert.IsFalse(state.State[SoftSwitch.LcWriteEnabled]);
        }
    }
}

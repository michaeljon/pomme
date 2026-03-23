using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class Memory128kTests
    {
        // ------------------------------------------------------------------ //
        // Helpers
        // ------------------------------------------------------------------ //

        private static void AdvanceCycles(AppleBusTestDouble bus, int cycles)
        {
            for (var i = 0; i < cycles; i++)
            {
                bus.Read(0x0000);
            }
        }

        // ------------------------------------------------------------------ //
        // Address decomposition
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void GetPageExtractsHighByte()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            Assert.AreEqual((byte)0x12, memory.GetPage(0x1234));
        }

        [TestMethod]
        public void GetOffsetExtractsLowByte()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            Assert.AreEqual((byte)0x34, memory.GetOffset(0x1234));
        }

        [TestMethod]
        public void GetPageOnPageBoundaryReturnsCorrectPage()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            Assert.AreEqual((byte)0xFF, memory.GetPage(0xFF00));
        }

        [TestMethod]
        public void GetOffsetAtPageBoundaryReturnsZero()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            Assert.AreEqual((byte)0x00, memory.GetOffset(0x0500));
        }

        // ------------------------------------------------------------------ //
        // Basic read / write — main RAM
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadAfterWriteRoundTripsInMainRam()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            memory.Write(0x1000, 0xAB);
            Assert.AreEqual(0xAB, memory.Read(0x1000));
        }

        [TestMethod]
        public void WriteToRomIsNoOp()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            // By default the $E000–$FFFF ROM is zeroed and not writable
            memory.Write(0xE000, 0x99);
            Assert.AreEqual(0x00, memory.Read(0xE000));
        }

        [TestMethod]
        public void ReadFromNullPageReturnsFF()
        {
            // $C0xx is always forced to null after Remap()
            var (memory, _) = Memory128kFactory.CreateDefault();
            Assert.AreEqual(0xFF, memory.Read(0xC000));
        }

        // ------------------------------------------------------------------ //
        // Reset
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ResetZeroesMainRam()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            memory.Write(0x1000, 0xFF);
            memory.Reset();
            Assert.AreEqual(0x00, memory.Read(0x1000));
        }

        [TestMethod]
        public void ResetZeroesAuxRam()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            memory.SetAux(0x2000, 0xFF);
            memory.Reset();
            Assert.AreEqual(0x00, memory.GetAux(0x2000));
        }

        // ------------------------------------------------------------------ //
        // Direct access — GetMain / SetMain / GetAux / SetAux
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void SetMainAndGetMainBypassActiveMapping()
        {
            // Even with AuxRead active, SetMain/GetMain always go to main RAM
            var state = MachineStateBuilder.Default().WithAuxRead().Build();
            var memory = Memory128kFactory.CreateWithState(state);

            memory.SetMain(0x2000, 0xBB);
            Assert.AreEqual(0xBB, memory.GetMain(0x2000));
        }

        [TestMethod]
        public void SetAuxAndGetAuxBypassActiveMapping()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            memory.SetAux(0x3000, 0xCC);
            Assert.AreEqual(0xCC, memory.GetAux(0x3000));
        }

        [TestMethod]
        public void ZeroMainClearsEntirePage()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            for (ushort i = 0x1000; i < 0x1100; i++)
            {
                memory.SetMain(i, 0xFF);
            }
            memory.ZeroMain(0x1000);
            for (ushort i = 0x1000; i < 0x1100; i++)
            {
                Assert.AreEqual(0x00, memory.GetMain(i), $"byte at ${i:X4} was not zeroed");
            }
        }

        [TestMethod]
        public void ZeroAuxClearsEntirePage()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            for (ushort i = 0x2000; i < 0x2100; i++)
            {
                memory.SetAux(i, 0xAA);
            }
            memory.ZeroAux(0x2000);
            for (ushort i = 0x2000; i < 0x2100; i++)
            {
                Assert.AreEqual(0x00, memory.GetAux(i), $"aux byte at ${i:X4} was not zeroed");
            }
        }

        // ------------------------------------------------------------------ //
        // Auxiliary memory routing
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void DefaultMappingReadsFromMainMemory()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            memory.SetMain(0x2000, 0x11);
            memory.SetAux(0x2000, 0xDD);
            Assert.AreEqual(0x11, memory.Read(0x2000));
        }

        [TestMethod]
        public void AuxReadRoutesReadsBelowC000ToAuxMemory()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();
            memory.SetMain(0x2000, 0x11);
            memory.SetAux(0x2000, 0xDD);

            state.State[SoftSwitch.AuxRead] = true;
            memory.Remap();

            Assert.AreEqual(0xDD, memory.Read(0x2000));
        }

        [TestMethod]
        public void AuxWriteRoutesWritesToAuxMemory()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();

            state.State[SoftSwitch.AuxWrite] = true;
            memory.Remap();

            memory.Write(0x2000, 0xEE);
            Assert.AreEqual(0xEE, memory.GetAux(0x2000));
            Assert.AreEqual(0x00, memory.GetMain(0x2000));
        }

        [TestMethod]
        public void DefaultMappingWritesToMainMemory()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            memory.Write(0x2000, 0x42);
            Assert.AreEqual(0x42, memory.GetMain(0x2000));
            Assert.AreEqual(0x00, memory.GetAux(0x2000));
        }

        // ------------------------------------------------------------------ //
        // Zero page / stack auxiliary routing
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ZpAuxRemapsZeroPageToAuxMemory()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();
            memory.SetMain(0x0050, 0x00);
            memory.SetAux(0x0050, 0xFF);

            state.State[SoftSwitch.ZpAux] = true;
            memory.Remap();

            Assert.AreEqual(0xFF, memory.Read(0x0050));
        }

        [TestMethod]
        public void ZpAuxRemapsStackToAuxMemory()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();
            memory.SetMain(0x01FF, 0x00);
            memory.SetAux(0x01FF, 0xAA);

            state.State[SoftSwitch.ZpAux] = true;
            memory.Remap();

            Assert.AreEqual(0xAA, memory.Read(0x01FF));
        }

        [TestMethod]
        public void ZpAuxDoesNotAffectPagesAboveStack()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();
            memory.SetMain(0x0200, 0x55);
            memory.SetAux(0x0200, 0xAA);

            state.State[SoftSwitch.ZpAux] = true;
            memory.Remap();

            // $0200 is outside zero-page+stack; AuxRead=false so reads main
            Assert.AreEqual(0x55, memory.Read(0x0200));
        }

        // ------------------------------------------------------------------ //
        // 80-column store (text-page routing)
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void Store80WithPage2RoutesTextPage1WritesToAux()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();
            memory.SetMain(0x0400, 0x11);
            memory.SetAux(0x0400, 0x00);

            state.State[SoftSwitch.Store80] = true;
            state.State[SoftSwitch.Page2] = true;
            memory.Remap();

            memory.Write(0x0400, 0x55);
            Assert.AreEqual(0x55, memory.GetAux(0x0400));
            Assert.AreEqual(0x11, memory.GetMain(0x0400));
        }

        [TestMethod]
        public void Store80WithPage1WritesToMainTextPage()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();

            state.State[SoftSwitch.Store80] = true;
            state.State[SoftSwitch.Page2] = false;
            memory.Remap();

            memory.Write(0x0400, 0x77);
            Assert.AreEqual(0x77, memory.GetMain(0x0400));
        }

        // ------------------------------------------------------------------ //
        // Language Card — read enable
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void LcReadDisabledReadsRomAtHighAddresses()
        {
            // LcReadEnabled=false by default; $E000 maps to intEFRom (zeroed)
            var (memory, _) = Memory128kFactory.CreateDefault();
            Assert.AreEqual(0x00, memory.Read(0xE000));
        }

        [TestMethod]
        public void LcReadEnabledAllowsReadFromLanguageCardRam()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();

            state.State[SoftSwitch.LcWriteEnabled] = true;
            state.State[SoftSwitch.LcReadEnabled] = true;
            state.State[SoftSwitch.LcBank2] = false;
            memory.Remap();

            memory.Write(0xE000, 0x42);
            Assert.AreEqual(0x42, memory.Read(0xE000));
        }

        [TestMethod]
        public void LcBank2SelectsBank2AtD000()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();

            state.State[SoftSwitch.LcWriteEnabled] = true;
            state.State[SoftSwitch.LcReadEnabled] = true;
            state.State[SoftSwitch.LcBank2] = true;
            memory.Remap();

            memory.Write(0xD000, 0x77);
            Assert.AreEqual(0x77, memory.Read(0xD000));
        }

        [TestMethod]
        public void LcBank1SelectsBank1AtD000()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();

            state.State[SoftSwitch.LcWriteEnabled] = true;
            state.State[SoftSwitch.LcReadEnabled] = true;
            state.State[SoftSwitch.LcBank2] = false;
            memory.Remap();

            memory.Write(0xD000, 0x88);
            Assert.AreEqual(0x88, memory.Read(0xD000));
        }

        [TestMethod]
        public void LcBank2AndBank1HoldIndependentDataAtD000()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();

            // Write to bank 2
            state.State[SoftSwitch.LcWriteEnabled] = true;
            state.State[SoftSwitch.LcReadEnabled] = true;
            state.State[SoftSwitch.LcBank2] = true;
            memory.Remap();
            memory.Write(0xD000, 0x22);

            // Switch to bank 1 and write a different value
            state.State[SoftSwitch.LcBank2] = false;
            memory.Remap();
            memory.Write(0xD000, 0x33);

            // Bank 1 visible → 0x33
            Assert.AreEqual(0x33, memory.Read(0xD000));

            // Switch back to bank 2 → 0x22
            state.State[SoftSwitch.LcBank2] = true;
            memory.Remap();
            Assert.AreEqual(0x22, memory.Read(0xD000));
        }

        // ------------------------------------------------------------------ //
        // ROM loading
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void Load16kRomIsReadableAtEFRange()
        {
            var rom = new byte[16 * 1024];
            rom[8 * 1024] = 0xAB;   // intEFRom[0] → page $E0, offset 0
            var (memory, _) = Memory128kFactory.CreateWith16kRom(rom);
            Assert.AreEqual(0xAB, memory.Read(0xE000));
        }

        [TestMethod]
        public void Load16kRomIsReadableInCxRangeWhenIntCxRomEnabled()
        {
            var state = MachineStateBuilder.Default().WithIntCxRomEnabled().Build();
            var memory = Memory128kFactory.CreateWithState(state);

            var rom = new byte[16 * 1024];
            rom[0x100] = 0xCD;      // intCxRom[1] → page $C1, offset 0
            memory.LoadProgramToRom(rom);
            memory.Remap();

            Assert.AreEqual(0xCD, memory.Read(0xC100));
        }

        [TestMethod]
        public void Load32kRomIsReadableAtEFRange()
        {
            var rom = new byte[32 * 1024];
            rom[24 * 1024] = 0xEF;  // intEFRom[0] in 32k layout → page $E0, offset 0
            var (memory, _) = Memory128kFactory.CreateWith32kRom(rom);
            Assert.AreEqual(0xEF, memory.Read(0xE000));
        }

        [TestMethod]
        public void LoadProgramToRamWritesSequentialBytesToMainMemory()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            var program = new byte[] { 0xEA, 0xEA, 0x60 };
            memory.LoadProgramToRam(program, 0x0300);

            Assert.AreEqual(0xEA, memory.GetMain(0x0300));
            Assert.AreEqual(0xEA, memory.GetMain(0x0301));
            Assert.AreEqual(0x60, memory.GetMain(0x0302));
        }

        // ------------------------------------------------------------------ //
        // Slot ROM loading
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void LoadSlotCxRomIsAccessibleWhenIntCxRomDisabled()
        {
            var slotRom = new byte[MemoryPage.PageSize];
            slotRom[0] = 0x55;

            var (memory, state) = Memory128kFactory.CreateWithSlotCxRom(2, slotRom);
            // IntCxRomEnabled=false by default; slot 2 ROM appears at $C200
            memory.Remap();

            Assert.AreEqual(0x55, memory.Read(0xC200));
        }

        [TestMethod]
        public void LoadSlotCxRomIsNotVisibleWhenIntCxRomEnabled()
        {
            var slotRom = new byte[MemoryPage.PageSize];
            slotRom[0] = 0x55;

            var (memory, state) = Memory128kFactory.CreateWithSlotCxRom(2, slotRom);
            state.State[SoftSwitch.IntCxRomEnabled] = true;
            memory.Remap();

            // Internal ROM (zeroed) is visible instead
            Assert.AreNotEqual(0x55, memory.Read(0xC200));
        }

        // ------------------------------------------------------------------ //
        // ResolveRead / ResolveWrite
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ResolveReadReturnsMainMemoryPageForRamAddress()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            var page = memory.ResolveRead(0x1000);
            Assert.IsNotNull(page);
            Assert.AreEqual(MemoryPageType.Ram, page.MemoryPageType);
        }

        [TestMethod]
        public void ResolveReadReturnsNullForC0Page()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            var page = memory.ResolveRead(0xC000);
            Assert.IsNull(page);
        }

        [TestMethod]
        public void ResolveWriteReturnsNullForRomAddress()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            var page = memory.ResolveWrite(0xE000);
            Assert.IsNull(page);
        }

        [TestMethod]
        public void ResolveWriteReturnsMainMemoryForRamAddress()
        {
            var (memory, _) = Memory128kFactory.CreateDefault();
            var page = memory.ResolveWrite(0x1000);
            Assert.IsNotNull(page);
        }
    }
}

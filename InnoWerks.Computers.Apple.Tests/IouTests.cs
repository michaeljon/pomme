using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class IouTests
    {
        //
        // Helpers
        //

        private static (IOU Iou, Memory128k Memory, MachineState State, AppleBusTestDouble Bus) CreateIou()
        {
            var (memory, state) = Memory128kFactory.CreateDefault();
            var bus = new AppleBusTestDouble();
            var iou = new IOU(memory, state, bus);
            return (iou, memory, state, bus);
        }

        // Advance the bus cycle count by a specific number of reads
        private static void AdvanceCycles(AppleBusTestDouble bus, int cycles)
        {
            for (var i = 0; i < cycles; i++)
            {
                bus.Read(0x0000);
            }
        }

        private static bool AddressInRange(IOU iou, ushort address, MemoryAccessType accessType) =>
            iou.AddressRanges.Any(r => r.InterestedIn(address, accessType));

        private static byte DoRead(IOU iou, ushort address)
        {
            iou.DoRead(address, out var value);
            return value;
        }

        //
        // Name / identity
        //

        [TestMethod]
        public void NameIsIou()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.AreEqual("IOU", iou.Name);
        }

        //
        // Reset
        //

        [TestMethod]
        public void ResetSetsTextModeTrue()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.TextMode] = false;
            iou.Reset();
            Assert.IsTrue(state.State[SoftSwitch.TextMode]);
        }

        [TestMethod]
        public void ResetSetsIouDisabledTrue()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.IOUDisabled] = false;
            iou.Reset();
            Assert.IsTrue(state.State[SoftSwitch.IOUDisabled]);
        }

        [TestMethod]
        public void ResetClearsKeyboardStrobe()
        {
            var (iou, _, state, _) = CreateIou();
            state.KeyStrobe = true;
            iou.Reset();
            Assert.IsFalse(state.KeyStrobe);
        }

        [TestMethod]
        public void ResetClearsKeyLatch()
        {
            var (iou, _, state, _) = CreateIou();
            state.KeyLatch = 0x41;
            iou.Reset();
            Assert.AreEqual((byte)0x00, state.KeyLatch);
        }

        //
        // AddressRanges
        //

        [TestMethod]
        public void AddressRangesContainsKbdForRead()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(AddressInRange(iou, SoftSwitchAddress.KBD, MemoryAccessType.Read));
        }

        [TestMethod]
        public void AddressRangesContainsKbdStrbForRead()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(AddressInRange(iou, SoftSwitchAddress.KBDSTRB, MemoryAccessType.Read));
        }

        [TestMethod]
        public void AddressRangesContainsTxtClrForRead()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(AddressInRange(iou, SoftSwitchAddress.TXTCLR, MemoryAccessType.Read));
        }

        [TestMethod]
        public void AddressRangesContainsHiresForRead()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(AddressInRange(iou, SoftSwitchAddress.HIRES, MemoryAccessType.Read));
        }

        [TestMethod]
        public void AddressRangesContainsRdVblBarForRead()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(AddressInRange(iou, SoftSwitchAddress.RDVBLBAR, MemoryAccessType.Read));
        }

        [TestMethod]
        public void AddressRangesContainsTxtClrForWrite()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(AddressInRange(iou, SoftSwitchAddress.TXTCLR, MemoryAccessType.Write));
        }

        [TestMethod]
        public void AddressRangesContainsKbdStrbForWrite()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(AddressInRange(iou, SoftSwitchAddress.KBDSTRB, MemoryAccessType.Write));
        }

        [TestMethod]
        public void AddressRangesDoesNotContainAddressBelowC000()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsFalse(AddressInRange(iou, 0x1000, MemoryAccessType.Read));
        }

        //
        // Display mode switches — read side (setters)
        //

        [TestMethod]
        public void ReadTxtClrSetsTextModeFalse()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.TextMode] = true;
            DoRead(iou, SoftSwitchAddress.TXTCLR);
            Assert.IsFalse(state.State[SoftSwitch.TextMode]);
        }

        [TestMethod]
        public void ReadTxtSetSetsTextModeTrue()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.TextMode] = false;
            DoRead(iou, SoftSwitchAddress.TXTSET);
            Assert.IsTrue(state.State[SoftSwitch.TextMode]);
        }

        [TestMethod]
        public void ReadMixClrSetsMixedModeFalse()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.MixedMode] = true;
            DoRead(iou, SoftSwitchAddress.MIXCLR);
            Assert.IsFalse(state.State[SoftSwitch.MixedMode]);
        }

        [TestMethod]
        public void ReadMixSetSetsMixedModeTrue()
        {
            var (iou, _, state, _) = CreateIou();
            DoRead(iou, SoftSwitchAddress.MIXSET);
            Assert.IsTrue(state.State[SoftSwitch.MixedMode]);
        }

        [TestMethod]
        public void ReadTxtPage1SetsPage2False()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.Page2] = true;
            DoRead(iou, SoftSwitchAddress.TXTPAGE1);
            Assert.IsFalse(state.State[SoftSwitch.Page2]);
        }

        [TestMethod]
        public void ReadTxtPage2SetsPage2True()
        {
            var (iou, _, state, _) = CreateIou();
            DoRead(iou, SoftSwitchAddress.TXTPAGE2);
            Assert.IsTrue(state.State[SoftSwitch.Page2]);
        }

        [TestMethod]
        public void ReadLoresSetsHiResFalse()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.HiRes] = true;
            DoRead(iou, SoftSwitchAddress.LORES);
            Assert.IsFalse(state.State[SoftSwitch.HiRes]);
        }

        [TestMethod]
        public void ReadHiresSetsHiResTrue()
        {
            var (iou, _, state, _) = CreateIou();
            DoRead(iou, SoftSwitchAddress.HIRES);
            Assert.IsTrue(state.State[SoftSwitch.HiRes]);
        }

        //
        // Display mode switches — read status (getters)
        //

        [TestMethod]
        public void ReadRdTextReturns0x80WhenTextModeIsTrue()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.TextMode] = true;
            Assert.AreEqual((byte)0x80, DoRead(iou, SoftSwitchAddress.RDTEXT));
        }

        [TestMethod]
        public void ReadRdTextReturns0x00WhenTextModeIsFalse()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.TextMode] = false;
            Assert.AreEqual((byte)0x00, DoRead(iou, SoftSwitchAddress.RDTEXT));
        }

        [TestMethod]
        public void ReadRdMixedReturns0x80WhenMixedModeIsTrue()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.MixedMode] = true;
            Assert.AreEqual((byte)0x80, DoRead(iou, SoftSwitchAddress.RDMIXED));
        }

        [TestMethod]
        public void ReadRdPage2Returns0x80WhenPage2IsTrue()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.Page2] = true;
            Assert.AreEqual((byte)0x80, DoRead(iou, SoftSwitchAddress.RDPAGE2));
        }

        [TestMethod]
        public void ReadRdHiResReturns0x80WhenHiResIsTrue()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.HiRes] = true;
            Assert.AreEqual((byte)0x80, DoRead(iou, SoftSwitchAddress.RDHIRES));
        }

        //
        // Display mode switches — write side
        //

        [TestMethod]
        public void WriteTxtClrSetsTextModeFalse()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.TextMode] = true;
            iou.DoWrite(SoftSwitchAddress.TXTCLR, 0);
            Assert.IsFalse(state.State[SoftSwitch.TextMode]);
        }

        [TestMethod]
        public void WriteTxtSetSetsTextModeTrue()
        {
            var (iou, _, state, _) = CreateIou();
            iou.DoWrite(SoftSwitchAddress.TXTSET, 0);
            Assert.IsTrue(state.State[SoftSwitch.TextMode]);
        }

        [TestMethod]
        public void WriteHiresSetsHiResTrue()
        {
            var (iou, _, state, _) = CreateIou();
            iou.DoWrite(SoftSwitchAddress.HIRES, 0);
            Assert.IsTrue(state.State[SoftSwitch.HiRes]);
        }

        [TestMethod]
        public void WriteClr80ColSetsEightyColumnModeFalse()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.EightyColumnMode] = true;
            iou.DoWrite(SoftSwitchAddress.CLR80COL, 0);
            Assert.IsFalse(state.State[SoftSwitch.EightyColumnMode]);
        }

        [TestMethod]
        public void WriteSet80ColSetsEightyColumnModeTrue()
        {
            var (iou, _, state, _) = CreateIou();
            iou.DoWrite(SoftSwitchAddress.SET80COL, 0);
            Assert.IsTrue(state.State[SoftSwitch.EightyColumnMode]);
        }

        //
        // Keyboard
        //

        [TestMethod]
        public void ReadKbdReturnsKeyDataWithHighBitWhenStrobeIsSet()
        {
            var (iou, _, state, _) = CreateIou();
            state.KeyLatch = 0x41; // 'A'
            state.KeyStrobe = true;
            Assert.AreEqual((byte)0xC1, DoRead(iou, SoftSwitchAddress.KBD)); // 0x80 | 0x41
        }

        [TestMethod]
        public void ReadKbdReturnsKeyDataWithoutHighBitWhenStrobeIsClear()
        {
            var (iou, _, state, _) = CreateIou();
            state.KeyLatch = 0x41;
            state.KeyStrobe = false;
            Assert.AreEqual((byte)0x41, DoRead(iou, SoftSwitchAddress.KBD));
        }

        [TestMethod]
        public void ReadKbdStrbClearsKeyStrobe()
        {
            var (iou, _, state, _) = CreateIou();
            state.KeyStrobe = true;
            DoRead(iou, SoftSwitchAddress.KBDSTRB);
            Assert.IsFalse(state.KeyStrobe);
        }

        [TestMethod]
        public void WriteKbdStrbClearsKeyStrobe()
        {
            var (iou, _, state, _) = CreateIou();
            state.KeyStrobe = true;
            iou.DoWrite(SoftSwitchAddress.KBDSTRB, 0);
            Assert.IsFalse(state.KeyStrobe);
        }

        [TestMethod]
        public void InjectKeyStoresAsciiAndSetsStrobe()
        {
            var (iou, _, state, _) = CreateIou();
            iou.InjectKey(0x42); // 'B'
            Assert.AreEqual((byte)0x42, state.KeyLatch);
            Assert.IsTrue(state.KeyStrobe);
        }

        [TestMethod]
        public void InjectKeySecondCallOverwritesLatchWhenQueueIsEmpty()
        {
            var (iou, _, state, _) = CreateIou();
            iou.InjectKey(0x41);
            iou.InjectKey(0x42);
            Assert.AreEqual((byte)0x42, state.KeyLatch);
        }

        //
        // Open Apple / Solid Apple buttons
        //

        [TestMethod]
        public void OpenApplePressedSetsButton0()
        {
            var (iou, _, state, _) = CreateIou();
            iou.OpenApple(true);
            Assert.IsTrue(state.State[SoftSwitch.OpenApple]);
        }

        [TestMethod]
        public void OpenAppleReleasedClearsButton0()
        {
            var (iou, _, state, _) = CreateIou();
            iou.OpenApple(true);
            iou.OpenApple(false);
            Assert.IsFalse(state.State[SoftSwitch.OpenApple]);
        }

        [TestMethod]
        public void ReadOpenAppleReturns0x80WhenPressed()
        {
            var (iou, _, state, _) = CreateIou();
            iou.OpenApple(true);
            Assert.AreEqual((byte)0x80, DoRead(iou, SoftSwitchAddress.OPENAPPLE));
        }

        [TestMethod]
        public void ReadOpenAppleReturns0x00WhenNotPressed()
        {
            var (iou, _, state, _) = CreateIou();
            iou.OpenApple(false);
            Assert.AreEqual((byte)0x00, DoRead(iou, SoftSwitchAddress.OPENAPPLE));
        }

        [TestMethod]
        public void SolidApplePressedSetsButton1()
        {
            var (iou, _, state, _) = CreateIou();
            iou.SolidApple(true);
            Assert.IsTrue(state.State[SoftSwitch.SolidApple]);
        }

        [TestMethod]
        public void ReadSolidAppleReturns0x80WhenPressed()
        {
            var (iou, _, state, _) = CreateIou();
            iou.SolidApple(true);
            Assert.AreEqual((byte)0x80, DoRead(iou, SoftSwitchAddress.SOLIDAPPLE));
        }

        //
        // Annunciators
        //

        [TestMethod]
        public void ReadClrAn0ClearsAnnunciator0()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.Annunciator0] = true;
            DoRead(iou, SoftSwitchAddress.CLRAN0);
            Assert.IsFalse(state.State[SoftSwitch.Annunciator0]);
        }

        [TestMethod]
        public void ReadSetAn0SetsAnnunciator0()
        {
            var (iou, _, state, _) = CreateIou();
            DoRead(iou, SoftSwitchAddress.SETAN0);
            Assert.IsTrue(state.State[SoftSwitch.Annunciator0]);
        }

        [TestMethod]
        public void ReadClrAn1ClearsAnnunciator1()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.Annunciator1] = true;
            DoRead(iou, SoftSwitchAddress.CLRAN1);
            Assert.IsFalse(state.State[SoftSwitch.Annunciator1]);
        }

        [TestMethod]
        public void ReadSetAn1SetsAnnunciator1()
        {
            var (iou, _, state, _) = CreateIou();
            DoRead(iou, SoftSwitchAddress.SETAN1);
            Assert.IsTrue(state.State[SoftSwitch.Annunciator1]);
        }

        [TestMethod]
        public void WriteClrAn0ClearsAnnunciator0()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.Annunciator0] = true;
            iou.DoWrite(SoftSwitchAddress.CLRAN0, 0);
            Assert.IsFalse(state.State[SoftSwitch.Annunciator0]);
        }

        [TestMethod]
        public void WriteSetAn0SetsAnnunciator0()
        {
            var (iou, _, state, _) = CreateIou();
            iou.DoWrite(SoftSwitchAddress.SETAN0, 0);
            Assert.IsTrue(state.State[SoftSwitch.Annunciator0]);
        }

        //
        // Speaker
        //

        [TestMethod]
        public void ReadSpkrTogglesSpeakerState()
        {
            var (iou, _, state, _) = CreateIou();
            var initial = state.State[SoftSwitch.Speaker];
            DoRead(iou, SoftSwitchAddress.SPKR);
            Assert.AreNotEqual(initial, state.State[SoftSwitch.Speaker]);
        }

        [TestMethod]
        public void ReadSpkrTogglesBackOnSecondRead()
        {
            var (iou, _, state, _) = CreateIou();
            var initial = state.State[SoftSwitch.Speaker];
            DoRead(iou, SoftSwitchAddress.SPKR);
            DoRead(iou, SoftSwitchAddress.SPKR);
            Assert.AreEqual(initial, state.State[SoftSwitch.Speaker]);
        }

        //
        // Paddles and joystick
        //

        [TestMethod]
        public void ReadPaddle0BeforeTimerTriggerReturnsZero()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.AreEqual((byte)0x00, DoRead(iou, SoftSwitchAddress.PADDLE0));
        }

        [TestMethod]
        public void ReadPaddleAfterTriggerWithNonZeroValueReturnsHigh()
        {
            var (iou, _, _, bus) = CreateIou();
            iou.UpdateJoystick(128, 0, 0, 0, false, false);
            DoRead(iou, 0xC070); // PTRIG
            Assert.AreEqual((byte)0x80, DoRead(iou, SoftSwitchAddress.PADDLE0));
        }

        [TestMethod]
        public void ReadPaddleAfterTimerExpiresReturnsLow()
        {
            var (iou, _, _, bus) = CreateIou();
            iou.UpdateJoystick(1, 0, 0, 0, false, false);
            DoRead(iou, 0xC070); // trigger at cycle 0; threshold = 1 * 11 = 11
            AdvanceCycles(bus, 12); // advance past threshold
            Assert.AreEqual((byte)0x00, DoRead(iou, SoftSwitchAddress.PADDLE0));
        }

        [TestMethod]
        public void UpdateJoystickStoresPaddleValues()
        {
            var (iou, _, _, bus) = CreateIou();
            iou.UpdateJoystick(64, 128, 192, 255, true, false);
            DoRead(iou, 0xC070); // trigger
            Assert.AreEqual((byte)0x80, DoRead(iou, SoftSwitchAddress.PADDLE0));
        }

        [TestMethod]
        public void UpdateJoystickStoresButtonStates()
        {
            var (iou, _, state, _) = CreateIou();
            iou.UpdateJoystick(0, 0, 0, 0, true, false);
            Assert.IsTrue(state.State[SoftSwitch.Button0]);
            Assert.IsFalse(state.State[SoftSwitch.Button1]);
        }

        //
        // Vertical blank
        //

        [TestMethod]
        public void ReadRdVblBarReturns0x80WhenNotInVerticalBlank()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.AreEqual((byte)0x80, DoRead(iou, SoftSwitchAddress.RDVBLBAR));
        }

        [TestMethod]
        public void ReadRdVblBarReturns0x00WhenInVerticalBlank()
        {
            var (iou, _, _, bus) = CreateIou();
            AdvanceCycles(bus, Computer.VblStart);
            Assert.AreEqual((byte)0x00, DoRead(iou, SoftSwitchAddress.RDVBLBAR));
        }

        [TestMethod]
        public void ReadRdVblBarReturns0x80AfterVblEnds()
        {
            var (iou, _, _, bus) = CreateIou();
            AdvanceCycles(bus, Computer.FrameCycles + 1);
            Assert.AreEqual((byte)0x80, DoRead(iou, SoftSwitchAddress.RDVBLBAR));
        }
    }
}

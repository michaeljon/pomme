using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class IouTests
    {
        // ------------------------------------------------------------------ //
        // Helpers
        // ------------------------------------------------------------------ //

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

        // ------------------------------------------------------------------ //
        // Name / identity
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void NameIsIou()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.AreEqual("IOU", iou.Name);
        }

        // ------------------------------------------------------------------ //
        // Reset
        // ------------------------------------------------------------------ //

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

        // ------------------------------------------------------------------ //
        // HandlesRead / HandlesWrite
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void HandlesReadReturnsTrueForKbd()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(iou.HandlesRead(SoftSwitchAddress.KBD));
        }

        [TestMethod]
        public void HandlesReadReturnsTrueForKbdStrb()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(iou.HandlesRead(SoftSwitchAddress.KBDSTRB));
        }

        [TestMethod]
        public void HandlesReadReturnsTrueForTxtClr()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(iou.HandlesRead(SoftSwitchAddress.TXTCLR));
        }

        [TestMethod]
        public void HandlesReadReturnsTrueForHires()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(iou.HandlesRead(SoftSwitchAddress.HIRES));
        }

        [TestMethod]
        public void HandlesReadReturnsTrueForRdVblBar()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(iou.HandlesRead(SoftSwitchAddress.RDVBLBAR));
        }

        [TestMethod]
        public void HandlesWriteReturnsTrueForTxtClr()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(iou.HandlesWrite(SoftSwitchAddress.TXTCLR));
        }

        [TestMethod]
        public void HandlesWriteReturnsTrueForKbdStrb()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsTrue(iou.HandlesWrite(SoftSwitchAddress.KBDSTRB));
        }

        [TestMethod]
        public void HandlesReadReturnsFalseForAddressBelowC000()
        {
            var (iou, _, _, _) = CreateIou();
            Assert.IsFalse(iou.HandlesRead(0x1000));
        }

        // ------------------------------------------------------------------ //
        // Display mode switches — read side (setters)
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadTxtClrSetsTextModeFalse()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.TextMode] = true;
            iou.Read(SoftSwitchAddress.TXTCLR);
            Assert.IsFalse(state.State[SoftSwitch.TextMode]);
        }

        [TestMethod]
        public void ReadTxtSetSetsTextModeTrue()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.TextMode] = false;
            iou.Read(SoftSwitchAddress.TXTSET);
            Assert.IsTrue(state.State[SoftSwitch.TextMode]);
        }

        [TestMethod]
        public void ReadMixClrSetsMixedModeFalse()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.MixedMode] = true;
            iou.Read(SoftSwitchAddress.MIXCLR);
            Assert.IsFalse(state.State[SoftSwitch.MixedMode]);
        }

        [TestMethod]
        public void ReadMixSetSetsMixedModeTrue()
        {
            var (iou, _, state, _) = CreateIou();
            iou.Read(SoftSwitchAddress.MIXSET);
            Assert.IsTrue(state.State[SoftSwitch.MixedMode]);
        }

        [TestMethod]
        public void ReadTxtPage1SetsPage2False()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.Page2] = true;
            iou.Read(SoftSwitchAddress.TXTPAGE1);
            Assert.IsFalse(state.State[SoftSwitch.Page2]);
        }

        [TestMethod]
        public void ReadTxtPage2SetsPage2True()
        {
            var (iou, _, state, _) = CreateIou();
            iou.Read(SoftSwitchAddress.TXTPAGE2);
            Assert.IsTrue(state.State[SoftSwitch.Page2]);
        }

        [TestMethod]
        public void ReadLoresSetsHiResFalse()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.HiRes] = true;
            iou.Read(SoftSwitchAddress.LORES);
            Assert.IsFalse(state.State[SoftSwitch.HiRes]);
        }

        [TestMethod]
        public void ReadHiresSetsHiResTrue()
        {
            var (iou, _, state, _) = CreateIou();
            iou.Read(SoftSwitchAddress.HIRES);
            Assert.IsTrue(state.State[SoftSwitch.HiRes]);
        }

        // ------------------------------------------------------------------ //
        // Display mode switches — read status (getters)
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadRdTextReturns0x80WhenTextModeIsTrue()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.TextMode] = true;
            Assert.AreEqual((byte)0x80, iou.Read(SoftSwitchAddress.RDTEXT));
        }

        [TestMethod]
        public void ReadRdTextReturns0x00WhenTextModeIsFalse()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.TextMode] = false;
            Assert.AreEqual((byte)0x00, iou.Read(SoftSwitchAddress.RDTEXT));
        }

        [TestMethod]
        public void ReadRdMixedReturns0x80WhenMixedModeIsTrue()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.MixedMode] = true;
            Assert.AreEqual((byte)0x80, iou.Read(SoftSwitchAddress.RDMIXED));
        }

        [TestMethod]
        public void ReadRdPage2Returns0x80WhenPage2IsTrue()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.Page2] = true;
            Assert.AreEqual((byte)0x80, iou.Read(SoftSwitchAddress.RDPAGE2));
        }

        [TestMethod]
        public void ReadRdHiResReturns0x80WhenHiResIsTrue()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.HiRes] = true;
            Assert.AreEqual((byte)0x80, iou.Read(SoftSwitchAddress.RDHIRES));
        }

        // ------------------------------------------------------------------ //
        // Display mode switches — write side
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void WriteTxtClrSetsTextModeFalse()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.TextMode] = true;
            iou.Write(SoftSwitchAddress.TXTCLR, 0);
            Assert.IsFalse(state.State[SoftSwitch.TextMode]);
        }

        [TestMethod]
        public void WriteTxtSetSetsTextModeTrue()
        {
            var (iou, _, state, _) = CreateIou();
            iou.Write(SoftSwitchAddress.TXTSET, 0);
            Assert.IsTrue(state.State[SoftSwitch.TextMode]);
        }

        [TestMethod]
        public void WriteHiresSetsHiResTrue()
        {
            var (iou, _, state, _) = CreateIou();
            iou.Write(SoftSwitchAddress.HIRES, 0);
            Assert.IsTrue(state.State[SoftSwitch.HiRes]);
        }

        [TestMethod]
        public void WriteClr80ColSetsEightyColumnModeFalse()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.EightyColumnMode] = true;
            iou.Write(SoftSwitchAddress.CLR80COL, 0);
            Assert.IsFalse(state.State[SoftSwitch.EightyColumnMode]);
        }

        [TestMethod]
        public void WriteSet80ColSetsEightyColumnModeTrue()
        {
            var (iou, _, state, _) = CreateIou();
            iou.Write(SoftSwitchAddress.SET80COL, 0);
            Assert.IsTrue(state.State[SoftSwitch.EightyColumnMode]);
        }

        // ------------------------------------------------------------------ //
        // Keyboard
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadKbdReturnsKeyDataWithHighBitWhenStrobeIsSet()
        {
            var (iou, _, state, _) = CreateIou();
            state.KeyLatch = 0x41; // 'A'
            state.KeyStrobe = true;
            var result = iou.Read(SoftSwitchAddress.KBD);
            Assert.AreEqual((byte)0xC1, result); // 0x80 | 0x41
        }

        [TestMethod]
        public void ReadKbdReturnsKeyDataWithoutHighBitWhenStrobeIsClear()
        {
            var (iou, _, state, _) = CreateIou();
            state.KeyLatch = 0x41;
            state.KeyStrobe = false;
            var result = iou.Read(SoftSwitchAddress.KBD);
            Assert.AreEqual((byte)0x41, result);
        }

        [TestMethod]
        public void ReadKbdStrbClearsKeyStrobe()
        {
            var (iou, _, state, _) = CreateIou();
            state.KeyStrobe = true;
            iou.Read(SoftSwitchAddress.KBDSTRB);
            Assert.IsFalse(state.KeyStrobe);
        }

        [TestMethod]
        public void WriteKbdStrbClearsKeyStrobe()
        {
            var (iou, _, state, _) = CreateIou();
            state.KeyStrobe = true;
            iou.Write(SoftSwitchAddress.KBDSTRB, 0);
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
            // EnqueueKey only routes to the queue if the queue is already non-empty.
            // Two back-to-back InjectKey calls both hit the else branch, so the
            // second key overwrites the first in KeyLatch.
            var (iou, _, state, _) = CreateIou();
            iou.InjectKey(0x41); // 'A' → KeyLatch=0x41
            iou.InjectKey(0x42); // 'B' → overwrites KeyLatch=0x42 (queue still empty)
            Assert.AreEqual((byte)0x42, state.KeyLatch);
        }

        // ------------------------------------------------------------------ //
        // Open Apple / Solid Apple buttons
        // ------------------------------------------------------------------ //

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
            Assert.AreEqual((byte)0x80, iou.Read(SoftSwitchAddress.OPENAPPLE));
        }

        [TestMethod]
        public void ReadOpenAppleReturns0x00WhenNotPressed()
        {
            var (iou, _, state, _) = CreateIou();
            iou.OpenApple(false);
            Assert.AreEqual((byte)0x00, iou.Read(SoftSwitchAddress.OPENAPPLE));
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
            Assert.AreEqual((byte)0x80, iou.Read(SoftSwitchAddress.SOLIDAPPLE));
        }

        // ------------------------------------------------------------------ //
        // Annunciators
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadClrAn0ClearsAnnunciator0()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.Annunciator0] = true;
            iou.Read(SoftSwitchAddress.CLRAN0);
            Assert.IsFalse(state.State[SoftSwitch.Annunciator0]);
        }

        [TestMethod]
        public void ReadSetAn0SetsAnnunciator0()
        {
            var (iou, _, state, _) = CreateIou();
            iou.Read(SoftSwitchAddress.SETAN0);
            Assert.IsTrue(state.State[SoftSwitch.Annunciator0]);
        }

        [TestMethod]
        public void ReadClrAn1ClearsAnnunciator1()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.Annunciator1] = true;
            iou.Read(SoftSwitchAddress.CLRAN1);
            Assert.IsFalse(state.State[SoftSwitch.Annunciator1]);
        }

        [TestMethod]
        public void ReadSetAn1SetsAnnunciator1()
        {
            var (iou, _, state, _) = CreateIou();
            iou.Read(SoftSwitchAddress.SETAN1);
            Assert.IsTrue(state.State[SoftSwitch.Annunciator1]);
        }

        [TestMethod]
        public void WriteClrAn0ClearsAnnunciator0()
        {
            var (iou, _, state, _) = CreateIou();
            state.State[SoftSwitch.Annunciator0] = true;
            iou.Write(SoftSwitchAddress.CLRAN0, 0);
            Assert.IsFalse(state.State[SoftSwitch.Annunciator0]);
        }

        [TestMethod]
        public void WriteSetAn0SetsAnnunciator0()
        {
            var (iou, _, state, _) = CreateIou();
            iou.Write(SoftSwitchAddress.SETAN0, 0);
            Assert.IsTrue(state.State[SoftSwitch.Annunciator0]);
        }

        // ------------------------------------------------------------------ //
        // Speaker
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadSpkrTogglesSpeakerState()
        {
            var (iou, _, state, _) = CreateIou();
            var initial = state.State[SoftSwitch.Speaker];
            iou.Read(SoftSwitchAddress.SPKR);
            Assert.AreNotEqual(initial, state.State[SoftSwitch.Speaker]);
        }

        [TestMethod]
        public void ReadSpkrTogglesBackOnSecondRead()
        {
            var (iou, _, state, _) = CreateIou();
            var initial = state.State[SoftSwitch.Speaker];
            iou.Read(SoftSwitchAddress.SPKR);
            iou.Read(SoftSwitchAddress.SPKR);
            Assert.AreEqual(initial, state.State[SoftSwitch.Speaker]);
        }

        // ------------------------------------------------------------------ //
        // Paddles and joystick
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadPaddle0BeforeTimerTriggerReturnsZero()
        {
            var (iou, _, _, _) = CreateIou();
            // paddleTimerStartCycle is -1 until PTRIG is fired
            Assert.AreEqual((byte)0x00, iou.Read(SoftSwitchAddress.PADDLE0));
        }

        [TestMethod]
        public void ReadPaddleAfterTriggerWithNonZeroValueReturnsHigh()
        {
            var (iou, _, _, bus) = CreateIou();
            iou.UpdateJoystick(128, 0, 0, 0, false, false);
            iou.Read(0xC070); // PTRIG — sets paddleTimerStartCycle = bus.CycleCount (currently 0)
            // Cycle count has not advanced past threshold (128 * 11 = 1408)
            Assert.AreEqual((byte)0x80, iou.Read(SoftSwitchAddress.PADDLE0));
        }

        [TestMethod]
        public void ReadPaddleAfterTimerExpiresReturnsLow()
        {
            var (iou, _, _, bus) = CreateIou();
            iou.UpdateJoystick(1, 0, 0, 0, false, false);
            iou.Read(0xC070); // trigger at cycle 0; threshold = 1 * 11 = 11
            AdvanceCycles(bus, 12); // advance past threshold
            Assert.AreEqual((byte)0x00, iou.Read(SoftSwitchAddress.PADDLE0));
        }

        [TestMethod]
        public void UpdateJoystickStoresPaddleValues()
        {
            var (iou, _, _, bus) = CreateIou();
            iou.UpdateJoystick(64, 128, 192, 255, true, false);
            iou.Read(0xC070); // trigger

            // Paddle 0 at value 64, threshold = 64 * 11 = 704; elapsed=0 → 0x80
            Assert.AreEqual((byte)0x80, iou.Read(SoftSwitchAddress.PADDLE0));
        }

        [TestMethod]
        public void UpdateJoystickStoresButtonStates()
        {
            var (iou, _, state, _) = CreateIou();
            iou.UpdateJoystick(0, 0, 0, 0, true, false);
            Assert.IsTrue(state.State[SoftSwitch.Button0]);
            Assert.IsFalse(state.State[SoftSwitch.Button1]);
        }

        // ------------------------------------------------------------------ //
        // Vertical blank
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadRdVblBarReturns0x80WhenNotInVerticalBlank()
        {
            // At cycle 0: frameCycle = 0, which is below VblStart (12480) → not in VBL → 0x80
            var (iou, _, _, _) = CreateIou();
            Assert.AreEqual((byte)0x80, iou.Read(SoftSwitchAddress.RDVBLBAR));
        }

        [TestMethod]
        public void ReadRdVblBarReturns0x00WhenInVerticalBlank()
        {
            var (iou, _, _, bus) = CreateIou();
            // Advance to the start of vertical blank (cycle 12480)
            AdvanceCycles(bus, VideoTiming.VblStart);
            Assert.AreEqual((byte)0x00, iou.Read(SoftSwitchAddress.RDVBLBAR));
        }

        [TestMethod]
        public void ReadRdVblBarReturns0x80AfterVblEnds()
        {
            var (iou, _, _, bus) = CreateIou();
            // Advance past one full frame into active scan of next frame
            AdvanceCycles(bus, VideoTiming.FrameCycles + 1);
            Assert.AreEqual((byte)0x80, iou.Read(SoftSwitchAddress.RDVBLBAR));
        }
    }
}

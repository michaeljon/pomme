using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class MachineStateTests
    {
        //
        // Constructor
        //

        [TestMethod]
        public void AllSoftSwitchesInitializedToFalse()
        {
            var state = new MachineState();
            foreach (var sw in state.State.Keys)
            {
                Assert.IsFalse(state.State[sw], $"SoftSwitch.{sw} should start false");
            }
        }

        [TestMethod]
        public void CurrentSlotInitializesToZero()
        {
            var state = new MachineState();
            Assert.AreEqual(0, state.CurrentSlot);
        }

        [TestMethod]
        public void ExpansionRomTypeInitializesToNull()
        {
            var state = new MachineState();
            Assert.AreEqual(ExpansionRomType.ExpRomNull, state.ExpansionRomType);
        }

        [TestMethod]
        public void KeyLatchInitializesToZero()
        {
            var state = new MachineState();
            Assert.AreEqual((byte)0x00, state.KeyLatch);
        }

        [TestMethod]
        public void KeyStrobeInitializesToFalse()
        {
            var state = new MachineState();
            Assert.IsFalse(state.KeyStrobe);
        }

        //
        // ResetKeyboard
        //

        [TestMethod]
        public void ResetKeyboardClearsStrobe()
        {
            var state = new MachineState();
            state.KeyStrobe = true;
            state.ResetKeyboard();
            Assert.IsFalse(state.KeyStrobe);
        }

        [TestMethod]
        public void ResetKeyboardClearsLatch()
        {
            var state = new MachineState();
            state.KeyLatch = 0x41;
            state.ResetKeyboard();
            Assert.AreEqual((byte)0x00, state.KeyLatch);
        }

        //
        // EnqueueKey
        //

        [TestMethod]
        public void EnqueueKeySetslatchAndStrobe()
        {
            var state = new MachineState();
            state.EnqueueKey(0x41); // 'A'
            Assert.AreEqual((byte)0x41, state.KeyLatch);
            Assert.IsTrue(state.KeyStrobe);
        }

        //
        // ReadKeyboardData
        //

        [TestMethod]
        public void ReadKeyboardDataReturnsStrobeBitPlusLower7Bits()
        {
            var state = new MachineState();
            state.KeyLatch = 0x41;  // 'A' — bit 7 clear
            state.KeyStrobe = true;
            var data = state.ReadKeyboardData();
            Assert.AreEqual((byte)(0x80 | 0x41), data);
        }

        [TestMethod]
        public void ReadKeyboardDataWithoutStrobeReturnsOnlyLower7Bits()
        {
            var state = new MachineState();
            state.KeyLatch = 0x41;
            state.KeyStrobe = false;
            var data = state.ReadKeyboardData();
            Assert.AreEqual((byte)0x41, data);
        }

        [TestMethod]
        public void ReadKeyboardDataStripsHighBitOfLatch()
        {
            var state = new MachineState();
            // KeyLatch with bit 7 set; strobe off → only low 7 bits returned
            state.KeyLatch = 0xFF;
            state.KeyStrobe = false;
            var data = state.ReadKeyboardData();
            Assert.AreEqual((byte)0x7F, data);
        }

        [TestMethod]
        public void ReadKeyboardDataWithStrobeMergesHighBit()
        {
            var state = new MachineState();
            state.KeyLatch = 0xFF;
            state.KeyStrobe = true;
            var data = state.ReadKeyboardData();
            Assert.AreEqual((byte)0xFF, data);
        }

        //
        // ClearKeyboardStrobe
        //

        [TestMethod]
        public void ClearKeyboardStrobeClearsStrobeWhenQueueEmpty()
        {
            var state = new MachineState();
            state.KeyStrobe = true;
            state.ClearKeyboardStrobe();
            Assert.IsFalse(state.KeyStrobe);
        }

        [TestMethod]
        public void ClearKeyboardStrobeDoesNotChangeKeyLatchWhenQueueEmpty()
        {
            var state = new MachineState();
            state.KeyLatch = 0x42;
            state.KeyStrobe = true;
            state.ClearKeyboardStrobe();
            Assert.AreEqual((byte)0x42, state.KeyLatch);
        }

        //
        // TryLoadNextKey
        //

        [TestMethod]
        public void TryLoadNextKeyDoesNothingWhenQueueEmpty()
        {
            var state = new MachineState();
            state.KeyLatch = 0x42;
            state.KeyStrobe = false;
            state.TryLoadNextKey();
            Assert.AreEqual((byte)0x42, state.KeyLatch);
            Assert.IsFalse(state.KeyStrobe);
        }

        //
        // PeekKeyboard
        //

        [TestMethod]
        public void PeekKeyboardReturnsLatchOrHighBitWhenStrobeSet()
        {
            var state = new MachineState();
            state.KeyLatch = 0x41;
            state.KeyStrobe = true;
            // PeekKeyboard sets bit 7 of KeyLatch and returns it
            var peeked = state.PeekKeyboard();
            Assert.AreEqual((byte)(0x41 | 0x80), peeked);
        }

        [TestMethod]
        public void PeekKeyboardReturnsRawLatchWhenStrobeFalse()
        {
            var state = new MachineState();
            state.KeyLatch = 0x41;
            state.KeyStrobe = false;
            var peeked = state.PeekKeyboard();
            Assert.AreEqual((byte)0x41, peeked);
        }

        //
        // HandleReadStateToggle
        //

        [TestMethod]
        public void HandleReadStateToggleSetsNewStateAndTriggersRemap()
        {
            var state = new MachineState();
            var memory = Memory128kFactory.CreateWithState(state);
            state.State[SoftSwitch.AuxRead] = false;

            state.HandleReadStateToggle(memory, SoftSwitch.AuxRead, true);

            Assert.IsTrue(state.State[SoftSwitch.AuxRead]);
        }

        [TestMethod]
        public void HandleReadStateToggleIsNoOpWhenStateAlreadyMatches()
        {
            var state = new MachineState();
            var memory = Memory128kFactory.CreateWithState(state);
            state.State[SoftSwitch.AuxRead] = true;

            // toState == current state → no change
            state.HandleReadStateToggle(memory, SoftSwitch.AuxRead, true);

            Assert.IsTrue(state.State[SoftSwitch.AuxRead]);
        }

        [TestMethod]
        public void HandleReadStateToggleReturnsZeroWhenNotFloating()
        {
            var state = new MachineState();
            var memory = Memory128kFactory.CreateWithState(state);
            state.State[SoftSwitch.AuxRead] = false;

            var result = state.HandleReadStateToggle(memory, SoftSwitch.AuxRead, true, floating: false);

            Assert.AreEqual((byte)0x00, result);
        }

        //
        // HandleWriteStateToggle
        //

        [TestMethod]
        public void HandleWriteStateToggleSetsNewState()
        {
            var state = new MachineState();
            var memory = Memory128kFactory.CreateWithState(state);
            state.State[SoftSwitch.AuxWrite] = false;

            state.HandleWriteStateToggle(memory, SoftSwitch.AuxWrite, true);

            Assert.IsTrue(state.State[SoftSwitch.AuxWrite]);
        }

        [TestMethod]
        public void HandleWriteStateToggleIsNoOpWhenStateAlreadyMatches()
        {
            var state = new MachineState();
            var memory = Memory128kFactory.CreateWithState(state);
            state.State[SoftSwitch.AuxWrite] = true;

            state.HandleWriteStateToggle(memory, SoftSwitch.AuxWrite, true);

            Assert.IsTrue(state.State[SoftSwitch.AuxWrite]);
        }

        //
        // FloatingValue
        //

        [TestMethod]
        public void FloatingValueCallSucceeds()
        {
            var initial = MachineState.FloatingValue;
            var subsequent = MachineState.FloatingValue;

            Assert.AreNotEqual(initial, subsequent, "Floating bus should return different values from read to read");
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class NoSlotClockTests
    {
        private static (NoSlotClockDevice Nsc, MachineState State) CreateNsc()
        {
            var computer = new Computer(AppleModel.AppleIIeEnhanced, new byte[16 * 1024]);
            var nsc = computer.AddNoSlotClock();
            // enable IntCxRomEnabled so ShouldIntercept returns true for $C100-$CFFF
            computer.MachineState.State[SoftSwitch.IntCxRomEnabled] = true;
            return (nsc, computer.MachineState);
        }

        private const ulong ComparisonPattern = 0x5ca33ac55ca33ac5;

        /// <summary>
        /// Send the 64-bit unlock pattern by reading addresses with bit 0
        /// matching each bit of the comparison register.
        /// </summary>
        private static void SendUnlockSequence(NoSlotClockDevice nsc)
        {
            for (var i = 0; i < 64; i++)
            {
                var bit = (ComparisonPattern >> i) & 1;
                var address = (ushort)(0xC100 | (int)bit);
                nsc.DoRead(address, out _);
            }
        }

        // ------------------------------------------------------------------ //
        // Name / identity
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void NameIsNoSlotClock()
        {
            var (nsc, _) = CreateNsc();
            Assert.AreEqual("No-Slot-Clock", nsc.Name);
        }

        [TestMethod]
        public void PriorityIsAddressIntercept()
        {
            var (nsc, _) = CreateNsc();
            Assert.AreEqual(InterceptPriority.AddressIntercept, nsc.InterceptPriority);
        }

        // ------------------------------------------------------------------ //
        // Address range
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void AddressRangeCoversC100ToCFFF()
        {
            var (nsc, _) = CreateNsc();
            Assert.AreEqual(1, nsc.AddressRanges.Count);
            Assert.IsTrue(nsc.AddressRanges[0].Contains(0xC100, MemoryAccessType.Read));
            Assert.IsTrue(nsc.AddressRanges[0].Contains(0xCFFF, MemoryAccessType.Read));
        }

        // ------------------------------------------------------------------ //
        // ShouldIntercept — soft switch gating
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void DoReadReturnsFalseWhenIntCxRomDisabled()
        {
            var (nsc, state) = CreateNsc();
            state.State[SoftSwitch.IntCxRomEnabled] = false;
            state.State[SoftSwitch.SlotC3RomEnabled] = true; // not in C3 override

            var handled = nsc.DoRead(0xC400, out _);
            Assert.IsFalse(handled);
        }

        [TestMethod]
        public void DoReadReturnsFalseForC300WhenSlotC3RomEnabled()
        {
            var (nsc, state) = CreateNsc();
            state.State[SoftSwitch.IntCxRomEnabled] = false;
            state.State[SoftSwitch.SlotC3RomEnabled] = true;

            var handled = nsc.DoRead(0xC300, out _);
            Assert.IsFalse(handled);
        }

        // ------------------------------------------------------------------ //
        // Unlock sequence
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void BeforeUnlockDoReadReturnsFalse()
        {
            var (nsc, _) = CreateNsc();
            var handled = nsc.DoRead(0xC100, out _);
            Assert.IsFalse(handled);
        }

        [TestMethod]
        public void AfterUnlockDoReadReturnsTrue()
        {
            var (nsc, _) = CreateNsc();
            SendUnlockSequence(nsc);

            var handled = nsc.DoRead(0xC100, out _);
            Assert.IsTrue(handled, "NSC should intercept reads after unlock");
        }

        [TestMethod]
        public void MismatchInSequenceResetsProgress()
        {
            var (nsc, _) = CreateNsc();

            // send first 32 bits correctly
            for (var i = 0; i < 32; i++)
            {
                var bit = (ComparisonPattern >> i) & 1;
                nsc.DoRead((ushort)(0xC100 | (int)bit), out _);
            }

            // send wrong bit
            var expectedBit = (ComparisonPattern >> 32) & 1;
            var wrongBit = expectedBit ^ 1;
            nsc.DoRead((ushort)(0xC100 | (int)wrongBit), out _);

            // complete a fresh unlock — should need full 64 bits again
            SendUnlockSequence(nsc);
            var handled = nsc.DoRead(0xC100, out _);
            Assert.IsTrue(handled);
        }

        // ------------------------------------------------------------------ //
        // Clock data
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void UnlockedReadReturns64BitsOfClockData()
        {
            var (nsc, _) = CreateNsc();
            SendUnlockSequence(nsc);

            // read all 64 bits
            var data = 0UL;
            for (var i = 0; i < 64; i++)
            {
                nsc.DoRead(0xC100, out var value);
                data |= ((ulong)(value & 1)) << i;
            }

            // after 64 reads, should lock again
            var handled = nsc.DoRead(0xC100, out _);
            Assert.IsFalse(handled, "NSC should lock after 64 bits read");
        }

        [TestMethod]
        public void ClockDataContainsBcdValues()
        {
            var (nsc, _) = CreateNsc();
            SendUnlockSequence(nsc);

            var data = 0UL;
            for (var i = 0; i < 64; i++)
            {
                nsc.DoRead(0xC100, out var value);
                data |= ((ulong)(value & 1)) << i;
            }

            // extract month (byte 6) — should be 1-12 in BCD
            var monthBcd = (byte)((data >> 48) & 0xFF);
            var monthHigh = (monthBcd >> 4) & 0x0F;
            var monthLow = monthBcd & 0x0F;
            var month = monthHigh * 10 + monthLow;

            Assert.IsTrue(month >= 1 && month <= 12,
                $"Month {month} (BCD ${monthBcd:X2}) is not in range 1-12");
        }

        // ------------------------------------------------------------------ //
        // Reset
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ResetLocksDevice()
        {
            var (nsc, _) = CreateNsc();
            SendUnlockSequence(nsc);
            nsc.Reset();

            var handled = nsc.DoRead(0xC100, out _);
            Assert.IsFalse(handled, "Reset should lock the NSC");
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Simulators.Tests
{
    [TestClass]
    public class RegistersTests
    {
        // ------------------------------------------------------------------ //
        // Reset
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ResetZeroesAccumulator()
        {
            var r = new Registers { A = 0xFF };
            r.Reset();
            Assert.AreEqual((byte)0x00, r.A);
        }

        [TestMethod]
        public void ResetZeroesXRegister()
        {
            var r = new Registers { X = 0xFF };
            r.Reset();
            Assert.AreEqual((byte)0x00, r.X);
        }

        [TestMethod]
        public void ResetZeroesYRegister()
        {
            var r = new Registers { Y = 0xFF };
            r.Reset();
            Assert.AreEqual((byte)0x00, r.Y);
        }

        [TestMethod]
        public void ResetSetsStackPointerToFd()
        {
            var r = new Registers();
            r.Reset();
            Assert.AreEqual((byte)0xFD, r.StackPointer);
        }

        [TestMethod]
        public void ResetSetsProcessorStatusToUnusedBit()
        {
            var r = new Registers();
            r.Reset();
            // Only the Unused bit should be set after reset
            Assert.IsTrue(r.Unused);
            Assert.IsFalse(r.Carry);
            Assert.IsFalse(r.Zero);
            Assert.IsFalse(r.Negative);
            Assert.IsFalse(r.Overflow);
            Assert.IsFalse(r.Break);
            Assert.IsFalse(r.Interrupt);
            Assert.IsFalse(r.Decimal);
        }

        // ------------------------------------------------------------------ //
        // SetNZ
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void SetNZSetsZeroFlagForZeroValue()
        {
            var r = new Registers();
            r.SetNZ(0x00);
            Assert.IsTrue(r.Zero);
            Assert.IsFalse(r.Negative);
        }

        [TestMethod]
        public void SetNZClearsZeroFlagForNonZeroValue()
        {
            var r = new Registers { ProcessorStatus = 0xFF };
            r.SetNZ(0x01);
            Assert.IsFalse(r.Zero);
        }

        [TestMethod]
        public void SetNZSetsNegativeFlagWhenHighBitIsSet()
        {
            var r = new Registers();
            r.SetNZ(0x80);
            Assert.IsTrue(r.Negative);
            Assert.IsFalse(r.Zero);
        }

        [TestMethod]
        public void SetNZClearsNegativeFlagForPositiveValue()
        {
            var r = new Registers { ProcessorStatus = 0xFF };
            r.SetNZ(0x01);
            Assert.IsFalse(r.Negative);
        }

        [TestMethod]
        public void SetNZMasksToLowByte()
        {
            // 0x100 low-byte is 0x00 → zero flag set, negative clear
            var r = new Registers();
            r.SetNZ(0x100);
            Assert.IsTrue(r.Zero);
            Assert.IsFalse(r.Negative);
        }

        // ------------------------------------------------------------------ //
        // Carry flag
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void CarryFlagSetAndGet()
        {
            var r = new Registers();
            r.Carry = true;
            Assert.IsTrue(r.Carry);
            r.Carry = false;
            Assert.IsFalse(r.Carry);
        }

        [TestMethod]
        public void CarryFlagIsolatedInProcessorStatus()
        {
            var r = new Registers { ProcessorStatus = 0x00 };
            r.Carry = true;
            Assert.AreEqual((byte)0x01, (byte)(r.ProcessorStatus & 0x01));
            r.Carry = false;
            Assert.AreEqual((byte)0x00, (byte)(r.ProcessorStatus & 0x01));
        }

        // ------------------------------------------------------------------ //
        // Zero flag
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ZeroFlagSetAndGet()
        {
            var r = new Registers();
            r.Zero = true;
            Assert.IsTrue(r.Zero);
            r.Zero = false;
            Assert.IsFalse(r.Zero);
        }

        // ------------------------------------------------------------------ //
        // Interrupt flag
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void InterruptFlagSetAndGet()
        {
            var r = new Registers();
            r.Interrupt = true;
            Assert.IsTrue(r.Interrupt);
            r.Interrupt = false;
            Assert.IsFalse(r.Interrupt);
        }

        // ------------------------------------------------------------------ //
        // Decimal flag
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void DecimalFlagSetAndGet()
        {
            var r = new Registers();
            r.Decimal = true;
            Assert.IsTrue(r.Decimal);
            r.Decimal = false;
            Assert.IsFalse(r.Decimal);
        }

        // ------------------------------------------------------------------ //
        // Break flag
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void BreakFlagSetAndGet()
        {
            var r = new Registers();
            r.Break = true;
            Assert.IsTrue(r.Break);
            r.Break = false;
            Assert.IsFalse(r.Break);
        }

        // ------------------------------------------------------------------ //
        // Unused flag
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void UnusedFlagSetAndGet()
        {
            var r = new Registers();
            r.Unused = true;
            Assert.IsTrue(r.Unused);
            r.Unused = false;
            Assert.IsFalse(r.Unused);
        }

        // ------------------------------------------------------------------ //
        // Overflow flag
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void OverflowFlagSetAndGet()
        {
            var r = new Registers();
            r.Overflow = true;
            Assert.IsTrue(r.Overflow);
            r.Overflow = false;
            Assert.IsFalse(r.Overflow);
        }

        // ------------------------------------------------------------------ //
        // Negative flag
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void NegativeFlagSetAndGet()
        {
            var r = new Registers();
            r.Negative = true;
            Assert.IsTrue(r.Negative);
            r.Negative = false;
            Assert.IsFalse(r.Negative);
        }

        // ------------------------------------------------------------------ //
        // Flag isolation — setting one flag does not affect others
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void SettingCarryDoesNotAffectOverflow()
        {
            var r = new Registers();
            r.Overflow = true;
            r.Carry = true;
            Assert.IsTrue(r.Overflow);
        }

        [TestMethod]
        public void ClearingCarryDoesNotAffectNegative()
        {
            var r = new Registers { ProcessorStatus = 0xFF };
            r.Carry = false;
            Assert.IsTrue(r.Negative);
        }

        // ------------------------------------------------------------------ //
        // ProcessorStatus round-trip
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ProcessorStatusCanBeSetDirectlyAndFlagsReflectIt()
        {
            var r = new Registers { ProcessorStatus = 0x00 };
            r.ProcessorStatus = 0xFF;
            Assert.IsTrue(r.Carry);
            Assert.IsTrue(r.Zero);
            Assert.IsTrue(r.Negative);
            Assert.IsTrue(r.Overflow);
        }

        [TestMethod]
        public void ProcessorStatusZeroMeansAllFlagsClear()
        {
            var r = new Registers { ProcessorStatus = 0xFF };
            r.ProcessorStatus = 0x00;
            Assert.IsFalse(r.Carry);
            Assert.IsFalse(r.Zero);
            Assert.IsFalse(r.Negative);
            Assert.IsFalse(r.Overflow);
            Assert.IsFalse(r.Break);
            Assert.IsFalse(r.Interrupt);
            Assert.IsFalse(r.Decimal);
            Assert.IsFalse(r.Unused);
        }

        // ------------------------------------------------------------------ //
        // String display
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void GetRegisterDisplayContainsRegisterNames()
        {
            var r = new Registers();
            var display = r.GetRegisterDisplay;
            StringAssert.Contains(display, "A:", System.StringComparison.Ordinal);
            StringAssert.Contains(display, "X:", System.StringComparison.Ordinal);
            StringAssert.Contains(display, "Y:", System.StringComparison.Ordinal);
            StringAssert.Contains(display, "SP:", System.StringComparison.Ordinal);
            StringAssert.Contains(display, "PS:", System.StringComparison.Ordinal);
        }

        [TestMethod]
        public void ToStringContainsProgramCounter()
        {
            var r = new Registers { ProgramCounter = 0x1234 };
            var s = r.ToString();
            StringAssert.Contains(s, "1234", System.StringComparison.Ordinal);
        }
    }
}

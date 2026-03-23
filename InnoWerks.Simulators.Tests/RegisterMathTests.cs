using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Simulators.Tests
{
    [TestClass]
    public class RegisterMathTests
    {
        // ------------------------------------------------------------------ //
        // Inc
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void IncReturnsOnePlusInput()
        {
            Assert.AreEqual((byte)0x01, RegisterMath.Inc(0x00));
        }

        [TestMethod]
        public void IncWrapsAtMaxByte()
        {
            Assert.AreEqual((byte)0x00, RegisterMath.Inc(0xFF));
        }

        [TestMethod]
        public void IncMidRangeValue()
        {
            Assert.AreEqual((byte)0x80, RegisterMath.Inc(0x7F));
        }

        // ------------------------------------------------------------------ //
        // Dec
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void DecReturnsOneMinusInput()
        {
            Assert.AreEqual((byte)0x01, RegisterMath.Dec(0x02));
        }

        [TestMethod]
        public void DecWrapsAtZero()
        {
            Assert.AreEqual((byte)0xFF, RegisterMath.Dec(0x00));
        }

        [TestMethod]
        public void DecMidRangeValue()
        {
            Assert.AreEqual((byte)0x7F, RegisterMath.Dec(0x80));
        }

        // ------------------------------------------------------------------ //
        // IsZero / IsNonZero
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void IsZeroReturnsTrueForZero()
        {
            Assert.IsTrue(RegisterMath.IsZero(0x00));
        }

        [TestMethod]
        public void IsZeroReturnsFalseForNonZero()
        {
            Assert.IsFalse(RegisterMath.IsZero(0x01));
        }

        [TestMethod]
        public void IsZeroMasksToLowByte()
        {
            // 0x100 low byte is 0x00 → zero
            Assert.IsTrue(RegisterMath.IsZero(0x100));
        }

        [TestMethod]
        public void IsNonZeroReturnsTrueForNonZero()
        {
            Assert.IsTrue(RegisterMath.IsNonZero(0x01));
        }

        [TestMethod]
        public void IsNonZeroReturnsFalseForZero()
        {
            Assert.IsFalse(RegisterMath.IsNonZero(0x00));
        }

        [TestMethod]
        public void IsNonZeroMasksToLowByte()
        {
            // 0x100 low byte is 0x00 → not non-zero
            Assert.IsFalse(RegisterMath.IsNonZero(0x100));
        }

        // ------------------------------------------------------------------ //
        // IsHighBitSet / IsHighBitClear
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void IsHighBitSetReturnsTrueWhenBit7IsSet()
        {
            Assert.IsTrue(RegisterMath.IsHighBitSet(0x80));
        }

        [TestMethod]
        public void IsHighBitSetReturnsTrueForAllHighValues()
        {
            Assert.IsTrue(RegisterMath.IsHighBitSet(0xFF));
        }

        [TestMethod]
        public void IsHighBitSetReturnsFalseWhenBit7IsClear()
        {
            Assert.IsFalse(RegisterMath.IsHighBitSet(0x7F));
        }

        [TestMethod]
        public void IsHighBitSetReturnsFalseForZero()
        {
            Assert.IsFalse(RegisterMath.IsHighBitSet(0x00));
        }

        [TestMethod]
        public void IsHighBitClearReturnsTrueWhenBit7IsClear()
        {
            Assert.IsTrue(RegisterMath.IsHighBitClear(0x7F));
        }

        [TestMethod]
        public void IsHighBitClearReturnsFalseWhenBit7IsSet()
        {
            Assert.IsFalse(RegisterMath.IsHighBitClear(0x80));
        }

        // ------------------------------------------------------------------ //
        // TruncateToByte
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void TruncateToByteKeepsLowByte()
        {
            Assert.AreEqual((byte)0xAB, RegisterMath.TruncateToByte(0xAB));
        }

        [TestMethod]
        public void TruncateToByteDiscardsHighBits()
        {
            Assert.AreEqual((byte)0xCD, RegisterMath.TruncateToByte(0x1234CD));
        }

        [TestMethod]
        public void TruncateToByteOnZeroReturnsZero()
        {
            Assert.AreEqual((byte)0x00, RegisterMath.TruncateToByte(0x00));
        }

        // ------------------------------------------------------------------ //
        // LowByte / HighByte
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void LowByteExtractsLowOctet()
        {
            Assert.AreEqual((byte)0x34, RegisterMath.LowByte(0x1234));
        }

        [TestMethod]
        public void LowByteOnZeroReturnsZero()
        {
            Assert.AreEqual((byte)0x00, RegisterMath.LowByte(0x0000));
        }

        [TestMethod]
        public void HighByteExtractsHighOctet()
        {
            Assert.AreEqual((byte)0x12, RegisterMath.HighByte(0x1234));
        }

        [TestMethod]
        public void HighByteOnZeroReturnsZero()
        {
            Assert.AreEqual((byte)0x00, RegisterMath.HighByte(0x0000));
        }

        [TestMethod]
        public void LowByteOfPageBoundaryIsZero()
        {
            Assert.AreEqual((byte)0x00, RegisterMath.LowByte(0xFF00));
        }

        [TestMethod]
        public void HighByteOfPageBoundaryIsFF()
        {
            Assert.AreEqual((byte)0xFF, RegisterMath.HighByte(0xFF00));
        }

        // ------------------------------------------------------------------ //
        // MakeShort
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void MakeShortCombinesHighAndLowBytes()
        {
            Assert.AreEqual((ushort)0x1234, RegisterMath.MakeShort(0x12, 0x34));
        }

        [TestMethod]
        public void MakeShortWithZeroHighByteReturnsLowByte()
        {
            Assert.AreEqual((ushort)0x00AB, RegisterMath.MakeShort(0x00, 0xAB));
        }

        [TestMethod]
        public void MakeShortWithZeroLowByteReturnsShiftedHigh()
        {
            Assert.AreEqual((ushort)0xAB00, RegisterMath.MakeShort(0xAB, 0x00));
        }

        [TestMethod]
        public void MakeShortRoundTripsWithLowAndHighByte()
        {
            ushort original = 0xBEEF;
            var reconstructed = RegisterMath.MakeShort(RegisterMath.HighByte(original), RegisterMath.LowByte(original));
            Assert.AreEqual(original, reconstructed);
        }
    }
}

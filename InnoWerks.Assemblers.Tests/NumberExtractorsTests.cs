namespace InnoWerks.Assemblers.Tests
{
    [TestClass]
    public class NumberExtractorsTests
    {
        //
        // Dollar-sign hex prefix ($)
        //

        [TestMethod]
        public void DollarPrefixParsesHexByte()
        {
            Assert.AreEqual((ushort)0xFF, NumberExtractors.ResolveNumber("$FF"));
        }

        [TestMethod]
        public void DollarPrefixParsesHexWord()
        {
            Assert.AreEqual((ushort)0x1234, NumberExtractors.ResolveNumber("$1234"));
        }

        [TestMethod]
        public void DollarPrefixParsesLowercaseHex()
        {
            Assert.AreEqual((ushort)0xABCD, NumberExtractors.ResolveNumber("$abcd"));
        }

        [TestMethod]
        public void DollarPrefixParsesZero()
        {
            Assert.AreEqual((ushort)0x0000, NumberExtractors.ResolveNumber("$00"));
        }

        [TestMethod]
        public void DollarPrefixParsesSingleDigit()
        {
            Assert.AreEqual((ushort)0x000A, NumberExtractors.ResolveNumber("$A"));
        }

        //
        // 0x hex prefix
        //

        [TestMethod]
        public void ZeroXPrefixParsesHexByte()
        {
            Assert.AreEqual((ushort)0xFF, NumberExtractors.ResolveNumber("0xFF"));
        }

        [TestMethod]
        public void ZeroXPrefixParsesHexWord()
        {
            Assert.AreEqual((ushort)0x1234, NumberExtractors.ResolveNumber("0x1234"));
        }

        [TestMethod]
        public void ZeroXPrefixParsesLowercaseHex()
        {
            Assert.AreEqual((ushort)0xABCD, NumberExtractors.ResolveNumber("0xabcd"));
        }

        //
        // Percent binary prefix (%)
        //

        [TestMethod]
        public void PercentPrefixParsesBinaryByte()
        {
            Assert.AreEqual((ushort)0b10101010, NumberExtractors.ResolveNumber("%10101010"));
        }

        [TestMethod]
        public void PercentPrefixParsesAllZeros()
        {
            Assert.AreEqual((ushort)0, NumberExtractors.ResolveNumber("%00000000"));
        }

        [TestMethod]
        public void PercentPrefixParsesAllOnes()
        {
            Assert.AreEqual((ushort)0xFF, NumberExtractors.ResolveNumber("%11111111"));
        }

        [TestMethod]
        public void PercentPrefixParsesSingleBit()
        {
            Assert.AreEqual((ushort)1, NumberExtractors.ResolveNumber("%1"));
        }

        //
        // 0b binary prefix
        //

        [TestMethod]
        public void ZeroBPrefixParsesBinaryByte()
        {
            Assert.AreEqual((ushort)0b10101010, NumberExtractors.ResolveNumber("0b10101010"));
        }

        [TestMethod]
        public void ZeroBPrefixParsesAllZeros()
        {
            Assert.AreEqual((ushort)0, NumberExtractors.ResolveNumber("0b00000000"));
        }

        [TestMethod]
        public void ZeroBPrefixParsesAllOnes()
        {
            Assert.AreEqual((ushort)0xFF, NumberExtractors.ResolveNumber("0b11111111"));
        }

        //
        // Plain decimal
        //

        [TestMethod]
        public void PlainDecimalParsesZero()
        {
            Assert.AreEqual((ushort)0, NumberExtractors.ResolveNumber("0"));
        }

        [TestMethod]
        public void PlainDecimalParsesSmallNumber()
        {
            Assert.AreEqual((ushort)42, NumberExtractors.ResolveNumber("42"));
        }

        [TestMethod]
        public void PlainDecimalParsesLargeNumber()
        {
            Assert.AreEqual((ushort)65535, NumberExtractors.ResolveNumber("65535"));
        }

        [TestMethod]
        public void PlainDecimalParsesByteMax()
        {
            Assert.AreEqual((ushort)255, NumberExtractors.ResolveNumber("255"));
        }

        //
        // Equivalent representations
        //

        [TestMethod]
        public void DollarAndZeroXProduceSameResult()
        {
            Assert.AreEqual(
                NumberExtractors.ResolveNumber("$1A"),
                NumberExtractors.ResolveNumber("0x1A"));
        }

        [TestMethod]
        public void PercentAndZeroBProduceSameResult()
        {
            Assert.AreEqual(
                NumberExtractors.ResolveNumber("%10110011"),
                NumberExtractors.ResolveNumber("0b10110011"));
        }

        [TestMethod]
        public void HexAndDecimalProduceSameValueForFF()
        {
            Assert.AreEqual(
                NumberExtractors.ResolveNumber("$FF"),
                NumberExtractors.ResolveNumber("255"));
        }
    }
}

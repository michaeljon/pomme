using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class AddressRangeTests
    {
        // ------------------------------------------------------------------ //
        // Contains — address within range
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ContainsReturnsTrueForAddressAtStart()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsTrue(range.Contains(0xC300, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsTrueForAddressAtEnd()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsTrue(range.Contains(0xC3FF, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsTrueForAddressInMiddle()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsTrue(range.Contains(0xC380, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsFalseForAddressBelowRange()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsFalse(range.Contains(0xC2FF, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsFalseForAddressAboveRange()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsFalse(range.Contains(0xC400, MemoryAccessType.Read));
        }

        // ------------------------------------------------------------------ //
        // Contains — access type matching
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ContainsReturnsTrueWhenAccessTypeMatchesRead()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsTrue(range.Contains(0xC300, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsFalseWhenAccessTypeIsWriteButRangeIsReadOnly()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsFalse(range.Contains(0xC300, MemoryAccessType.Write));
        }

        [TestMethod]
        public void ContainsReturnsTrueWhenAccessTypeMatchesWrite()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Write);
            Assert.IsTrue(range.Contains(0xC300, MemoryAccessType.Write));
        }

        [TestMethod]
        public void ContainsReturnsFalseWhenAccessTypeIsReadButRangeIsWriteOnly()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Write);
            Assert.IsFalse(range.Contains(0xC300, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsTrueForReadWhenRangeIsAny()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Any);
            Assert.IsTrue(range.Contains(0xC300, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsTrueForWriteWhenRangeIsAny()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Any);
            Assert.IsTrue(range.Contains(0xC300, MemoryAccessType.Write));
        }

        // ------------------------------------------------------------------ //
        // Contains — both address and type must match
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ContainsReturnsFalseWhenAddressOutOfRangeEvenIfTypeMatches()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Any);
            Assert.IsFalse(range.Contains(0xC400, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsFalseWhenTypeDoesNotMatchEvenIfAddressInRange()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsFalse(range.Contains(0xC350, MemoryAccessType.Write));
        }

        // ------------------------------------------------------------------ //
        // Equality
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void EqualRangesAreEqual()
        {
            var a = new AddressRange(0xC100, 0xCFFF, MemoryAccessType.Any);
            var b = new AddressRange(0xC100, 0xCFFF, MemoryAccessType.Any);
            Assert.AreEqual(a, b);
            Assert.IsTrue(a == b);
        }

        [TestMethod]
        public void DifferentStartsAreNotEqual()
        {
            var a = new AddressRange(0xC100, 0xCFFF, MemoryAccessType.Any);
            var b = new AddressRange(0xC200, 0xCFFF, MemoryAccessType.Any);
            Assert.AreNotEqual(a, b);
            Assert.IsTrue(a != b);
        }

        [TestMethod]
        public void DifferentEndsAreNotEqual()
        {
            var a = new AddressRange(0xC100, 0xCFFF, MemoryAccessType.Any);
            var b = new AddressRange(0xC100, 0xCFFE, MemoryAccessType.Any);
            Assert.AreNotEqual(a, b);
        }

        // ------------------------------------------------------------------ //
        // Single-address range
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void SingleAddressRangeContainsOnlyThatAddress()
        {
            var range = new AddressRange(0xC030, 0xC030, MemoryAccessType.Read);
            Assert.IsTrue(range.Contains(0xC030, MemoryAccessType.Read));
            Assert.IsFalse(range.Contains(0xC02F, MemoryAccessType.Read));
            Assert.IsFalse(range.Contains(0xC031, MemoryAccessType.Read));
        }
    }
}

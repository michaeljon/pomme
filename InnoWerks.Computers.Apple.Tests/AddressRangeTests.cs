using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class AddressRangeTests
    {
        //
        // Contains — address within range
        //

        [TestMethod]
        public void ContainsReturnsTrueForAddressAtStart()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsTrue(range.InterestedIn(0xC300, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsTrueForAddressAtEnd()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsTrue(range.InterestedIn(0xC3FF, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsTrueForAddressInMiddle()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsTrue(range.InterestedIn(0xC380, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsFalseForAddressBelowRange()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsFalse(range.InterestedIn(0xC2FF, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsFalseForAddressAboveRange()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsFalse(range.InterestedIn(0xC400, MemoryAccessType.Read));
        }

        //
        // Contains — access type matching
        //

        [TestMethod]
        public void ContainsReturnsTrueWhenAccessTypeMatchesRead()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsTrue(range.InterestedIn(0xC300, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsFalseWhenAccessTypeIsWriteButRangeIsReadOnly()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsFalse(range.InterestedIn(0xC300, MemoryAccessType.Write));
        }

        [TestMethod]
        public void ContainsReturnsTrueWhenAccessTypeMatchesWrite()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Write);
            Assert.IsTrue(range.InterestedIn(0xC300, MemoryAccessType.Write));
        }

        [TestMethod]
        public void ContainsReturnsFalseWhenAccessTypeIsReadButRangeIsWriteOnly()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Write);
            Assert.IsFalse(range.InterestedIn(0xC300, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsTrueForReadWhenRangeIsAny()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Any);
            Assert.IsTrue(range.InterestedIn(0xC300, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsTrueForWriteWhenRangeIsAny()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Any);
            Assert.IsTrue(range.InterestedIn(0xC300, MemoryAccessType.Write));
        }

        //
        // Contains — both address and type must match
        //

        [TestMethod]
        public void ContainsReturnsFalseWhenAddressOutOfRangeEvenIfTypeMatches()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Any);
            Assert.IsFalse(range.InterestedIn(0xC400, MemoryAccessType.Read));
        }

        [TestMethod]
        public void ContainsReturnsFalseWhenTypeDoesNotMatchEvenIfAddressInRange()
        {
            var range = new AddressRange(0xC300, 0xC3FF, MemoryAccessType.Read);
            Assert.IsFalse(range.InterestedIn(0xC350, MemoryAccessType.Write));
        }

        //
        // Single-address range
        //

        [TestMethod]
        public void SingleAddressRangeContainsOnlyThatAddress()
        {
            var range = new AddressRange(0xC030, 0xC030, MemoryAccessType.Read);
            Assert.IsTrue(range.InterestedIn(0xC030, MemoryAccessType.Read));
            Assert.IsFalse(range.InterestedIn(0xC02F, MemoryAccessType.Read));
            Assert.IsFalse(range.InterestedIn(0xC031, MemoryAccessType.Read));
        }

        //
        // Discrete address set — Contains
        //

        [TestMethod]
        public void DiscreteContainsReturnsTrueForAddressInSet()
        {
            var addresses = new System.Collections.Generic.HashSet<ushort> { 0xC090, 0xC094, 0xC098 };
            var range = new AddressRange(addresses, MemoryAccessType.Read);
            Assert.IsTrue(range.InterestedIn(0xC090, MemoryAccessType.Read));
            Assert.IsTrue(range.InterestedIn(0xC094, MemoryAccessType.Read));
            Assert.IsTrue(range.InterestedIn(0xC098, MemoryAccessType.Read));
        }

        [TestMethod]
        public void DiscreteContainsReturnsFalseForAddressNotInSet()
        {
            var addresses = new System.Collections.Generic.HashSet<ushort> { 0xC090, 0xC094, 0xC098 };
            var range = new AddressRange(addresses, MemoryAccessType.Read);
            Assert.IsFalse(range.InterestedIn(0xC091, MemoryAccessType.Read));
            Assert.IsFalse(range.InterestedIn(0xC093, MemoryAccessType.Read));
            Assert.IsFalse(range.InterestedIn(0xC000, MemoryAccessType.Read));
        }

        [TestMethod]
        public void DiscreteContainsReturnsFalseForWrongAccessType()
        {
            var addresses = new System.Collections.Generic.HashSet<ushort> { 0xC090 };
            var range = new AddressRange(addresses, MemoryAccessType.Read);
            Assert.IsFalse(range.InterestedIn(0xC090, MemoryAccessType.Write));
        }

        [TestMethod]
        public void DiscreteContainsReturnsTrueForWriteWhenConfiguredForWrite()
        {
            var addresses = new System.Collections.Generic.HashSet<ushort> { 0xC090 };
            var range = new AddressRange(addresses, MemoryAccessType.Write);
            Assert.IsTrue(range.InterestedIn(0xC090, MemoryAccessType.Write));
        }

        [TestMethod]
        public void DiscreteContainsReturnsTrueForAnyAccessType()
        {
            var addresses = new System.Collections.Generic.HashSet<ushort> { 0xC090 };
            var range = new AddressRange(addresses, MemoryAccessType.Any);
            Assert.IsTrue(range.InterestedIn(0xC090, MemoryAccessType.Read));
            Assert.IsTrue(range.InterestedIn(0xC090, MemoryAccessType.Write));
        }

        [TestMethod]
        public void DiscreteDoesNotMatchContiguousAddressesBetweenSetMembers()
        {
            var addresses = new System.Collections.Generic.HashSet<ushort> { 0xC090, 0xC098 };
            var range = new AddressRange(addresses, MemoryAccessType.Read);
            Assert.IsFalse(range.InterestedIn(0xC094, MemoryAccessType.Read));
        }

        [TestMethod]
        public void DiscreteSingleAddressWorks()
        {
            var addresses = new System.Collections.Generic.HashSet<ushort> { 0xC030 };
            var range = new AddressRange(addresses, MemoryAccessType.Any);
            Assert.IsTrue(range.InterestedIn(0xC030, MemoryAccessType.Read));
            Assert.IsFalse(range.InterestedIn(0xC031, MemoryAccessType.Read));
        }

        //
        // Single-address constructor
        //

        [TestMethod]
        public void SingleAddressConstructorContainsExactAddress()
        {
            var range = new AddressRange(0xC030, MemoryAccessType.Read);
            Assert.IsTrue(range.InterestedIn(0xC030, MemoryAccessType.Read));
        }

        [TestMethod]
        public void SingleAddressConstructorDoesNotContainAdjacentAddresses()
        {
            var range = new AddressRange(0xC030, MemoryAccessType.Read);
            Assert.IsFalse(range.InterestedIn(0xC02F, MemoryAccessType.Read));
            Assert.IsFalse(range.InterestedIn(0xC031, MemoryAccessType.Read));
        }

        [TestMethod]
        public void SingleAddressConstructorRespectsAccessType()
        {
            var range = new AddressRange(0xC030, MemoryAccessType.Write);
            Assert.IsTrue(range.InterestedIn(0xC030, MemoryAccessType.Write));
            Assert.IsFalse(range.InterestedIn(0xC030, MemoryAccessType.Read));
        }

        [TestMethod]
        public void SingleAddressConstructorWithAnyMatchesBothAccessTypes()
        {
            var range = new AddressRange(0xC030, MemoryAccessType.Any);
            Assert.IsTrue(range.InterestedIn(0xC030, MemoryAccessType.Read));
            Assert.IsTrue(range.InterestedIn(0xC030, MemoryAccessType.Write));
        }
    }
}

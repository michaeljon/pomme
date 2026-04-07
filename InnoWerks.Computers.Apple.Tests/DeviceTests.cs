using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    /// <summary>
    /// Tests for MockingboardSlotDevice, ThunderClockSlotDevice, EmptySlotDevice,
    /// SlotHandler, and KeylatchHandler.
    /// </summary>
    [TestClass]
    public class MockingboardTests
    {
        private static (MockingboardSlotDevice Device, Computer Computer) CreateMockingboard(int slot = 4)
        {
            var computer = new Computer(AppleModel.AppleIIeEnhanced, new byte[16 * 1024]);
            var mb = computer.AddMockingboard(slot);
            return (mb, computer);
        }

        [TestMethod]
        public void NameIsMockingboard()
        {
            var (mb, _) = CreateMockingboard();
            Assert.AreEqual("Mockingboard", mb.Name);
        }

        [TestMethod]
        public void SlotReflectsConstructorArgument()
        {
            var (mb, _) = CreateMockingboard(3);
            Assert.AreEqual(3, mb.Slot);
        }

        [TestMethod]
        public void HandlesReadClaimsCnRange()
        {
            var (mb, _) = CreateMockingboard(4);
            Assert.IsTrue(mb.HandlesRead(0xC400));
            Assert.IsTrue(mb.HandlesRead(0xC4FF));
        }

        [TestMethod]
        public void HandlesReadDoesNotClaimIoRange()
        {
            var (mb, _) = CreateMockingboard(4);
            Assert.IsFalse(mb.HandlesRead(0xC0C0));
        }

        [TestMethod]
        public void GenerateSampleReturnsValueInRange()
        {
            var (mb, _) = CreateMockingboard();
            var sample = mb.GenerateSample();
            Assert.IsTrue(sample >= 0.0f && sample <= 1.0f);
        }

        [TestMethod]
        public void ResetDoesNotThrow()
        {
            var (mb, _) = CreateMockingboard();
            mb.Reset();
        }

        [TestMethod]
        public void TickDoesNotThrow()
        {
            var (mb, _) = CreateMockingboard();
            mb.Tick();
        }
    }

    [TestClass]
    public class ThunderClockTests
    {
        private static (ThunderClockSlotDevice Device, Computer Computer) CreateThunderClock(int slot = 1)
        {
            var computer = new Computer(AppleModel.AppleIIeEnhanced, new byte[16 * 1024]);
            var tc = computer.AddThunderclock(slot);
            return (tc, computer);
        }

        [TestMethod]
        public void NameIsThunderClockPlus()
        {
            var (tc, _) = CreateThunderClock();
            Assert.AreEqual("ThunderClock Plus", tc.Name);
        }

        [TestMethod]
        public void HasRomIsTrue()
        {
            var (tc, _) = CreateThunderClock();
            Assert.IsTrue(tc.HasRom);
        }

        [TestMethod]
        public void HasAuxRomIsTrue()
        {
            var (tc, _) = CreateThunderClock();
            Assert.IsTrue(tc.HasAuxRom);
        }

        [TestMethod]
        public void ResetDoesNotThrow()
        {
            var (tc, _) = CreateThunderClock();
            tc.Reset();
        }
    }

    [TestClass]
    public class EmptySlotDeviceTests
    {
        [TestMethod]
        public void SlotReflectsConstructorArgument()
        {
            var device = new EmptySlotDevice(3);
            Assert.AreEqual(3, device.Slot);
        }

        [TestMethod]
        public void ReadReturns0xFF()
        {
            var device = new EmptySlotDevice(1);
            Assert.AreEqual((byte)0xFF, device.Read(0xC090));
        }

        [TestMethod]
        public void WriteDoesNotThrow()
        {
            var device = new EmptySlotDevice(1);
            device.Write(0xC090, 0x42); // should not throw
        }

        [TestMethod]
        public void HandlesReadClaimsIoRange()
        {
            var device = new EmptySlotDevice(1);
            Assert.IsTrue(device.HandlesRead(0xC090));
            Assert.IsTrue(device.HandlesRead(0xC09F));
        }

        [TestMethod]
        public void HandlesReadDoesNotClaimOutsideIoRange()
        {
            var device = new EmptySlotDevice(1);
            Assert.IsFalse(device.HandlesRead(0xC080));
            Assert.IsFalse(device.HandlesRead(0xC0A0));
        }

        [TestMethod]
        public void RomIsFilledWithFF()
        {
            var device = new EmptySlotDevice(5);
            foreach (var b in device.Rom)
            {
                Assert.AreEqual((byte)0xFF, b);
            }
        }
    }

    [TestClass]
    public class SlotHandlerTests
    {
        [TestMethod]
        public void NameIsSlotHandler()
        {
            var computer = new Computer(AppleModel.AppleIIeEnhanced, new byte[16 * 1024]);
            Assert.AreEqual("SlotHandler", computer.SlotHandler.Name);
        }

        [TestMethod]
        public void PriorityIsSlotDevice()
        {
            var computer = new Computer(AppleModel.AppleIIeEnhanced, new byte[16 * 1024]);
            Assert.AreEqual(InterceptPriority.SlotDevice, computer.SlotHandler.InterceptPriority);
        }
    }

    [TestClass]
    public class KeylatchHandlerTests
    {
        [TestMethod]
        public void NameIsKeylatchHandler()
        {
            var computer = new Computer(AppleModel.AppleIIeEnhanced, new byte[16 * 1024]);
            Assert.AreEqual("KeylatchHandler", computer.KeylatchHandler.Name);
        }
    }
}

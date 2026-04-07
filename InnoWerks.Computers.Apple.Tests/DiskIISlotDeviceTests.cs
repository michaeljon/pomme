using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class DiskIISlotDeviceTests
    {
        private static (DiskIISlotDevice Device, Computer Computer) CreateDiskII(int slot = 6)
        {
            var computer = new Computer(AppleModel.AppleIIeEnhanced, new byte[16 * 1024]);
            var disk = computer.AddDiskIIController(slot);
            return (disk, computer);
        }

        // ------------------------------------------------------------------ //
        // Identity
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void SlotReflectsConstructorArgument()
        {
            var (disk, _) = CreateDiskII(5);
            Assert.AreEqual(5, disk.Slot);
        }

        [TestMethod]
        public void NameIsDiskIIController()
        {
            var (disk, _) = CreateDiskII();
            Assert.AreEqual("Disk II Controller", disk.Name);
        }

        [TestMethod]
        public void HasRomIsTrue()
        {
            var (disk, _) = CreateDiskII();
            Assert.IsTrue(disk.HasRom);
        }

        // ------------------------------------------------------------------ //
        // Drive access
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void GetDrive0ReturnsDrive()
        {
            var (disk, _) = CreateDiskII();
            Assert.IsNotNull(disk.GetDrive(0));
        }

        [TestMethod]
        public void GetDrive1ReturnsDrive()
        {
            var (disk, _) = CreateDiskII();
            Assert.IsNotNull(disk.GetDrive(1));
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentOutOfRangeException))]
        public void GetDrive2Throws()
        {
            var (disk, _) = CreateDiskII();
            disk.GetDrive(2);
        }

        // ------------------------------------------------------------------ //
        // InsertDisk / EjectDisk
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void InsertDiskSetsDiskPresent()
        {
            var (disk, _) = CreateDiskII();
            disk.InsertDisk(0, "testdata/dos33.dsk");
            Assert.IsTrue(disk.GetDrive(0).HasDisk);
        }

        [TestMethod]
        public void EjectDiskClearsDiskPresent()
        {
            var (disk, _) = CreateDiskII();
            disk.InsertDisk(0, "testdata/dos33.dsk");
            disk.EjectDisk(0);
            Assert.IsFalse(disk.GetDrive(0).HasDisk);
        }

        // ------------------------------------------------------------------ //
        // State change callback
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void InsertDiskFiresStateChangedCallback()
        {
            var (disk, _) = CreateDiskII();
            var callbackFired = false;
            disk.OnDriveStateChanged = (slot, drive) => callbackFired = true;

            disk.InsertDisk(0, "testdata/dos33.dsk");
            Assert.IsTrue(callbackFired);
        }

        [TestMethod]
        public void EjectDiskFiresStateChangedCallback()
        {
            var (disk, _) = CreateDiskII();
            disk.InsertDisk(0, "testdata/dos33.dsk");

            var callbackFired = false;
            disk.OnDriveStateChanged = (slot, drive) => callbackFired = true;

            disk.EjectDisk(0);
            Assert.IsTrue(callbackFired);
        }

        // ------------------------------------------------------------------ //
        // FlushAll
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void FlushAllDoesNotThrowWithNoDisk()
        {
            var (disk, _) = CreateDiskII();
            disk.FlushAll(); // should not throw
        }
    }
}

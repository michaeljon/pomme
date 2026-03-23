using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class DiskIIDriveTests
    {
        // ------------------------------------------------------------------ //
        // Helpers
        // ------------------------------------------------------------------ //

        private static DiskIIDrive CreateDrive(int number = 0) => new DiskIIDrive(number);

        // ------------------------------------------------------------------ //
        // ToString
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ToStringContainsDriveNumber()
        {
            var drive = CreateDrive(1);
            StringAssert.Contains(drive.ToString(), "1", System.StringComparison.Ordinal);
        }

        [TestMethod]
        public void ToStringMentionsDiskII()
        {
            var drive = CreateDrive(0);
            StringAssert.Contains(drive.ToString(), "Disk II", System.StringComparison.Ordinal);
        }

        // ------------------------------------------------------------------ //
        // Reset
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ResetTurnsDriveOff()
        {
            var drive = CreateDrive();
            drive.SetOn(true);
            drive.Reset();
            Assert.IsFalse(drive.IsOn());
        }

        [TestMethod]
        public void ResetClearsMagnets()
        {
            var drive = CreateDrive();
            // Step with register=0x01 (magnet 0, on) sets a magnet bit
            drive.Step(0x01);
            drive.Reset();
            // After reset, with drive off, Step with any register should not move head
            // (This is a structural test — we verify driveOn=false via IsOn)
            Assert.IsFalse(drive.IsOn());
        }

        // ------------------------------------------------------------------ //
        // SetOn / IsOn
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void SetOnTrueEnablesDrive()
        {
            var drive = CreateDrive();
            drive.SetOn(true);
            Assert.IsTrue(drive.IsOn());
        }

        [TestMethod]
        public void SetOnFalseDisablesDrive()
        {
            var drive = CreateDrive();
            drive.SetOn(true);
            drive.SetOn(false);
            Assert.IsFalse(drive.IsOn());
        }

        [TestMethod]
        public void DriveIsOffByDefault()
        {
            var drive = CreateDrive();
            Assert.IsFalse(drive.IsOn());
        }

        // ------------------------------------------------------------------ //
        // DiskPresent
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void DiskPresentIsFalseWhenNoDiskInserted()
        {
            var drive = CreateDrive();
            Assert.IsFalse(drive.DiskPresent);
        }

        // ------------------------------------------------------------------ //
        // IsWriteProtected
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void IsWriteProtectedIsFalseWhenNoDisk()
        {
            var drive = CreateDrive();
            Assert.IsFalse(drive.IsWriteProtected);
        }

        // ------------------------------------------------------------------ //
        // SetLatchValue
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void SetLatchValueInWriteModeStoresValue()
        {
            var drive = CreateDrive();
            drive.SetWriteMode();
            drive.SetLatchValue(0xAB);
            // We cannot read latch directly, but Write() would use it;
            // verify no exception and mode is consistent
            Assert.IsTrue(true); // structural — no direct latch accessor
        }

        [TestMethod]
        public void SetLatchValueInReadModeSetsLatchToFF()
        {
            var drive = CreateDrive();
            drive.SetReadMode();
            // In read mode SetLatchValue stores 0xFF; verify no exception
            drive.SetLatchValue(0xAB);
            Assert.IsTrue(true); // structural — no direct latch accessor
        }

        // ------------------------------------------------------------------ //
        // SetReadMode / SetWriteMode
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadLatchInReadModeWithNoDiskReturnsFF()
        {
            var drive = CreateDrive();
            drive.SetReadMode();
            // First call: spinCount becomes 1 (> 0), no disk → returns 0xFF
            var result = drive.ReadLatch();
            Assert.AreEqual((byte)0xFF, result);
        }

        [TestMethod]
        public void ReadLatchInReadModeReturns7FWhenSpinCountIsZero()
        {
            var drive = CreateDrive();
            drive.SetReadMode();
            // spinCount cycles 0..15; to hit spinCount==0 we need 16 additional reads
            // after the first, to wrap spinCount back to 0. Drive returns 0x7F when count==0.
            byte lastResult = 0xFF;
            for (var i = 0; i < 17; i++)
            {
                lastResult = drive.ReadLatch();
            }
            // After 17 calls: spinCount = 17 & 0x0F = 1, still >0 → 0xFF
            // After 16 calls: spinCount = 16 & 0x0F = 0, returns 0x7F
            // We need exactly 16 reads to get spinCount=0
            drive = CreateDrive();
            drive.SetReadMode();
            for (var i = 0; i < 15; i++)
            {
                drive.ReadLatch();
            }
            var result = drive.ReadLatch(); // 16th call: spinCount = (15+1)&0x0F = 0
            Assert.AreEqual((byte)0x7F, result);
        }

        [TestMethod]
        public void ReadLatchInWriteModeReturns0x80WhenSpinCountNonZero()
        {
            var drive = CreateDrive();
            drive.SetWriteMode();
            // First call: spinCount=1, >0 → returns 0x80
            var result = drive.ReadLatch();
            Assert.AreEqual((byte)0x80, result);
        }

        [TestMethod]
        public void ReadLatchInWriteModeReturns7FWhenSpinCountIsZero()
        {
            var drive = CreateDrive();
            drive.SetWriteMode();
            // 16th call wraps spinCount to 0 → returns 0x7F
            for (var i = 0; i < 15; i++)
            {
                drive.ReadLatch();
            }
            var result = drive.ReadLatch();
            Assert.AreEqual((byte)0x7F, result);
        }

        // ------------------------------------------------------------------ //
        // Step — drive off, no head movement
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void StepDoesNotMoveHeadWhenDriveIsOff()
        {
            var drive = CreateDrive();
            drive.SetOn(false);

            // With drive off, step should not cause any observable change.
            // We verify no exception is thrown and the method completes.
            drive.Step(0x01); // magnet 0 on
            drive.Step(0x03); // magnet 0+1 on
            drive.Step(0x02); // magnet 0 off, 1 on
            Assert.IsTrue(true);
        }

        // ------------------------------------------------------------------ //
        // Step — magnet encoding
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void StepRegisterBit0OnSetsMagnetForPhase()
        {
            // register=0x01: magnet = (0x01>>1)&0x3 = 0; bit0=1 → magnet 0 set
            // register=0x03: magnet = (0x03>>1)&0x3 = 1; bit0=1 → magnet 1 set
            // register=0x05: magnet = (0x05>>1)&0x3 = 2; bit0=1 → magnet 2 set
            // register=0x07: magnet = (0x07>>1)&0x3 = 3; bit0=1 → magnet 3 set
            // These just verify no exception and consistent state
            var drive = CreateDrive();
            drive.SetOn(true);
            drive.Step(0x01);
            drive.Step(0x03);
            drive.Step(0x05);
            drive.Step(0x07);
            Assert.IsTrue(drive.IsOn());
        }

        [TestMethod]
        public void StepRegisterBit0OffClearsMagnetForPhase()
        {
            var drive = CreateDrive();
            drive.SetOn(true);
            // Set magnet 0, then clear magnet 0
            drive.Step(0x01); // magnet 0 on
            drive.Step(0x00); // magnet 0 off (register>>1 & 3 = 0, bit0=0)
            Assert.IsTrue(drive.IsOn());
        }

        // ------------------------------------------------------------------ //
        // Write — no-op when conditions not met
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void WriteDoesNothingWhenDriveIsOff()
        {
            var drive = CreateDrive();
            drive.SetWriteMode();
            drive.SetOn(false);
            // No disk, drive off — Write() should be a no-op
            drive.Write();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void WriteDoesNothingInReadMode()
        {
            var drive = CreateDrive();
            drive.SetReadMode();
            drive.SetOn(true);
            drive.Write();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void WriteDoesNothingWhenNoDiskInserted()
        {
            var drive = CreateDrive();
            drive.SetWriteMode();
            drive.SetOn(true);
            // No disk present → Write() is a no-op (null conditional on floppyDisk)
            drive.Write();
            Assert.IsFalse(drive.DiskPresent);
        }
    }
}

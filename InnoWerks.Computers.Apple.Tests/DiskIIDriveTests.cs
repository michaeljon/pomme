using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class DiskIIDriveTests
    {
        //
        // Helpers
        //

        private static DiskIIDrive CreateDrive(int number = 0) => new DiskIIDrive(number);

        //
        // ToString
        //

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

        //
        // Reset
        //

        [TestMethod]
        public void ResetClearsMagnets()
        {
            var drive = CreateDrive();
            drive.Step(0x01, motorEnabled: true);
            Assert.AreNotEqual(0, drive.magnets, "magnets should be set after step");

            drive.Reset();
            Assert.AreEqual(0, drive.magnets, "magnets should be zero after reset");
        }

        [TestMethod]
        public void ResetClearsIsDirty()
        {
            var drive = CreateDrive();
            drive.InsertDisk("testdata/dos33.dsk");
            drive.SetWriteMode();
            drive.SetLatchValue(0xD5);
            drive.Write(motorEnabled: true);
            Assert.IsTrue(drive.isDirty, "isDirty should be set after write");

            drive.Reset();
            Assert.IsFalse(drive.isDirty, "isDirty should be false after reset");
        }

        //
        // DiskPresent
        //

        [TestMethod]
        public void DiskPresentIsFalseWhenNoDiskInserted()
        {
            var drive = CreateDrive();
            Assert.IsFalse(drive.DiskPresent, "no disk should be present on a new drive");
        }

        //
        // IsWriteProtected
        //

        [TestMethod]
        public void IsWriteProtectedIsFalseWhenNoDisk()
        {
            var drive = CreateDrive();
            Assert.IsFalse(drive.IsWriteProtected, "write-protect should be false without a disk");
        }

        //
        // SetLatchValue
        //

        [TestMethod]
        public void SetLatchValueInWriteModeStoresValue()
        {
            var drive = CreateDrive();
            drive.SetWriteMode();
            drive.SetLatchValue(0xAB);
            Assert.AreEqual(0xAB, drive.latch, "latch should hold the written value");
        }

        [TestMethod]
        public void SetLatchValueInReadModeSetsLatchToFF()
        {
            var drive = CreateDrive();
            drive.SetReadMode();
            drive.SetLatchValue(0xAB);
            Assert.AreEqual(0xFF, drive.latch, "latch should be 0xFF in read mode");
        }

        //
        // ReadLatch — read mode
        //

        [TestMethod]
        public void ReadLatchInReadModeWithNoDiskReturnsFF()
        {
            var drive = CreateDrive();
            drive.SetReadMode();
            var result = drive.ReadLatch(motorEnabled: true);
            Assert.AreEqual((byte)0xFF, result, "no disk present should return 0xFF");
        }

        [TestMethod]
        public void ReadLatchInReadModeReturns7FWhenSpinCountIsZero()
        {
            var drive = CreateDrive();
            drive.SetReadMode();
            // 16th call wraps spinCount to 0 -> returns 0x7F
            for (var i = 0; i < 15; i++)
            {
                drive.ReadLatch(motorEnabled: true);
            }
            var result = drive.ReadLatch(motorEnabled: true);
            Assert.AreEqual((byte)0x7F, result, "spinCount==0 should return 0x7F");
        }

        //
        // ReadLatch — write mode
        //

        [TestMethod]
        public void ReadLatchInWriteModeReturns0x80WhenSpinCountNonZero()
        {
            var drive = CreateDrive();
            drive.SetWriteMode();
            var result = drive.ReadLatch(motorEnabled: true);
            Assert.AreEqual((byte)0x80, result, "write mode with spinCount>0 should return 0x80");
        }

        [TestMethod]
        public void ReadLatchInWriteModeReturns7FWhenSpinCountIsZero()
        {
            var drive = CreateDrive();
            drive.SetWriteMode();
            for (var i = 0; i < 15; i++)
            {
                drive.ReadLatch(motorEnabled: true);
            }
            var result = drive.ReadLatch(motorEnabled: true);
            Assert.AreEqual((byte)0x7F, result, "spinCount==0 should return 0x7F in write mode");
        }

        //
        // ReadLatch — nibble offset advancement
        //

        [TestMethod]
        public void ReadLatchDoesNotAdvanceNibbleOffsetWhenMotorDisabled()
        {
            var drive = CreateDrive();
            drive.InsertDisk("testdata/dos33.dsk");
            drive.SetReadMode();

            // Read with motor off — should return same nibble repeatedly
            var a = drive.ReadLatch(motorEnabled: false);
            var b = drive.ReadLatch(motorEnabled: false);
            Assert.AreEqual(a, b, "nibble offset should not advance when motor is disabled");
        }

        //
        // Step — motor disabled, no head movement
        //

        [TestMethod]
        public void StepDoesNotMoveHeadWhenMotorDisabled()
        {
            var drive = CreateDrive();
            Assert.AreEqual(0, drive.halfTrack, "halfTrack should start at zero");

            // Energize magnets in a pattern that would step forward if motor were on
            drive.Step(0x01, motorEnabled: false);
            drive.Step(0x03, motorEnabled: false);
            drive.Step(0x02, motorEnabled: false);
            Assert.AreEqual(0, drive.halfTrack, "halfTrack should not change with motor disabled");
        }

        //
        // Step — magnet state
        //

        [TestMethod]
        public void StepRegisterBit0OnSetsMagnetForPhase()
        {
            var drive = CreateDrive();

            // register=0x01: magnet = (0x01>>1)&0x3 = 0; bit0=1 -> magnet 0 set
            drive.Step(0x01, motorEnabled: true);
            Assert.AreEqual(1, drive.magnets & 0x01, "magnet 0 should be set");

            // register=0x03: magnet = (0x03>>1)&0x3 = 1; bit0=1 -> magnet 1 set
            drive.Step(0x03, motorEnabled: true);
            Assert.AreEqual(2, drive.magnets & 0x02, "magnet 1 should be set");
        }

        [TestMethod]
        public void StepRegisterBit0OffClearsMagnetForPhase()
        {
            var drive = CreateDrive();

            // Set magnet 0
            drive.Step(0x01, motorEnabled: true);
            Assert.AreEqual(1, drive.magnets & 0x01, "magnet 0 should be set");

            // Clear magnet 0 (register=0x00: magnet 0, bit0=0)
            drive.Step(0x00, motorEnabled: true);
            Assert.AreEqual(0, drive.magnets & 0x01, "magnet 0 should be cleared");
        }

        //
        // Write — no-op when conditions not met
        //

        [TestMethod]
        public void WriteDoesNothingWhenMotorDisabled()
        {
            var drive = CreateDrive();
            drive.InsertDisk("testdata/dos33.dsk");
            drive.SetWriteMode();
            drive.SetLatchValue(0xD5);
            drive.Write(motorEnabled: false);
            Assert.IsFalse(drive.isDirty, "isDirty should remain false when motor is disabled");
        }

        [TestMethod]
        public void WriteDoesNothingInReadMode()
        {
            var drive = CreateDrive();
            drive.InsertDisk("testdata/dos33.dsk");
            drive.SetReadMode();
            drive.Write(motorEnabled: true);
            Assert.IsFalse(drive.isDirty, "isDirty should remain false in read mode");
        }

        [TestMethod]
        public void WriteDoesNothingWhenNoDiskInserted()
        {
            var drive = CreateDrive();
            drive.SetWriteMode();
            drive.Write(motorEnabled: true);
            Assert.IsFalse(drive.isDirty, "isDirty should remain false without a disk");
        }

        //
        // MotorOff
        //

        [TestMethod]
        public void MotorOffDoesNotThrowWithNoDisk()
        {
            var drive = CreateDrive();
            drive.MotorOff();
            Assert.IsFalse(drive.isDirty, "isDirty should be false after MotorOff with no disk");
        }
    }
}

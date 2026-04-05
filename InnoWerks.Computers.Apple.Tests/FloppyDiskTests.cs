using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CA1707

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class FloppyDiskTests
    {
        private const string TestDataDir = "testdata";

        // ------------------------------------------------------------------ //
        // Helpers
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates a working copy of a test DSK so the original is never modified.
        /// Returns the path to the temporary copy.
        /// </summary>
        private static string CreateWorkingCopy(string sourceName)
        {
            var source = Path.Combine(TestDataDir, sourceName);
            var temp = Path.Combine(Path.GetTempPath(), $"floppy_test_{Guid.NewGuid():N}.dsk");
            File.Copy(source, temp, overwrite: true);
            return temp;
        }

        private static void Cleanup(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        // ------------------------------------------------------------------ //
        // Round-trip: load DSK → save DSK → compare bytes
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void RoundTrip_Dos33Dsk_SaveProducesBitIdenticalOutput()
        {
            var originalPath = Path.Combine(TestDataDir, "dos33.dsk");
            var workingPath = CreateWorkingCopy("dos33.dsk");

            try
            {
                var originalBytes = File.ReadAllBytes(originalPath);

                var disk = new FloppyDisk(workingPath);
                disk.Save(workingPath);

                var savedBytes = File.ReadAllBytes(workingPath);

                Assert.AreEqual(originalBytes.Length, savedBytes.Length,
                    "Saved file length differs from original");

                for (var i = 0; i < originalBytes.Length; i++)
                {
                    if (originalBytes[i] != savedBytes[i])
                    {
                        Assert.Fail($"Byte mismatch at offset 0x{i:X4} (track {i / (16 * 256)}, " +
                                    $"sector {(i / 256) % 16}, byte {i % 256}): " +
                                    $"expected 0x{originalBytes[i]:X2}, got 0x{savedBytes[i]:X2}");
                    }
                }
            }
            finally
            {
                Cleanup(workingPath);
            }
        }

        [TestMethod]
        public void RoundTrip_BlankDsk_SaveProducesBitIdenticalOutput()
        {
            var originalPath = Path.Combine(TestDataDir, "blank.dsk");
            var workingPath = CreateWorkingCopy("blank.dsk");

            try
            {
                var originalBytes = File.ReadAllBytes(originalPath);

                var disk = new FloppyDisk(workingPath);
                disk.Save(workingPath);

                var savedBytes = File.ReadAllBytes(workingPath);

                Assert.AreEqual(originalBytes.Length, savedBytes.Length,
                    "Saved file length differs from original");

                CollectionAssert.AreEqual(originalBytes, savedBytes,
                    "Saved blank disk is not identical to original");
            }
            finally
            {
                Cleanup(workingPath);
            }
        }

        // ------------------------------------------------------------------ //
        // Round-trip: verify every sector individually
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void RoundTrip_Dos33_EverySectorDecodesCorrectly()
        {
            var originalPath = Path.Combine(TestDataDir, "dos33.dsk");
            var workingPath = CreateWorkingCopy("dos33.dsk");

            try
            {
                var originalBytes = File.ReadAllBytes(originalPath);

                var disk = new FloppyDisk(workingPath);
                disk.Save(workingPath);

                var savedBytes = File.ReadAllBytes(workingPath);

                for (var track = 0; track < FloppyDisk.TRACK_COUNT; track++)
                {
                    for (var sector = 0; sector < FloppyDisk.SECTOR_COUNT; sector++)
                    {
                        var offset = (track * FloppyDisk.SECTOR_COUNT + sector) * 256;
                        var originalSector = new byte[256];
                        var savedSector = new byte[256];

                        Array.Copy(originalBytes, offset, originalSector, 0, 256);
                        Array.Copy(savedBytes, offset, savedSector, 0, 256);

                        CollectionAssert.AreEqual(originalSector, savedSector,
                            $"Sector mismatch at track {track}, sector {sector}");
                    }
                }
            }
            finally
            {
                Cleanup(workingPath);
            }
        }

        // ------------------------------------------------------------------ //
        // Load → nibblize → denibblize consistency
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void NibblizeAndDenibblize_PreservesFileSize()
        {
            var workingPath = CreateWorkingCopy("dos33.dsk");

            try
            {
                var disk = new FloppyDisk(workingPath);

                Assert.AreEqual(FloppyDisk.DISK_NIBBLE_LENGTH, disk.Length,
                    "Nibblized disk should be DISK_NIBBLE_LENGTH bytes");
            }
            finally
            {
                Cleanup(workingPath);
            }
        }

        // ------------------------------------------------------------------ //
        // Write-protect prevents save
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void Save_DoesNotWriteWhenWriteProtected()
        {
            var workingPath = CreateWorkingCopy("dos33.dsk");

            try
            {
                var originalBytes = File.ReadAllBytes(workingPath);
                var originalTime = File.GetLastWriteTimeUtc(workingPath);

                var disk = new FloppyDisk(workingPath);
                disk.IsWriteProtected = true;

                // modify a nibble to ensure save would change the file
                disk.WriteNibble(0, 0x00);

                disk.Save(workingPath);

                var savedBytes = File.ReadAllBytes(workingPath);

                // file should be unchanged since write-protect was on
                CollectionAssert.AreEqual(originalBytes, savedBytes,
                    "Write-protected disk should not be modified on save");
            }
            finally
            {
                Cleanup(workingPath);
            }
        }

        // ------------------------------------------------------------------ //
        // Nibble-level write and read back
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void WriteNibble_ReadNibble_RoundTrips()
        {
            var workingPath = CreateWorkingCopy("blank.dsk");

            try
            {
                var disk = new FloppyDisk(workingPath);

                // write a known value at a specific position
                disk.WriteNibble(100, 0xAB);
                var readBack = disk.ReadNibble(100);

                Assert.AreEqual((byte)0xAB, readBack);
            }
            finally
            {
                Cleanup(workingPath);
            }
        }

        [TestMethod]
        public void WriteNibble_WriteProtected_DoesNotModify()
        {
            var workingPath = CreateWorkingCopy("blank.dsk");

            try
            {
                var disk = new FloppyDisk(workingPath);
                var original = disk.ReadNibble(100);

                disk.IsWriteProtected = true;
                disk.WriteNibble(100, 0xAB);

                Assert.AreEqual(original, disk.ReadNibble(100),
                    "Write-protected disk should not accept nibble writes");
            }
            finally
            {
                Cleanup(workingPath);
            }
        }

        // ------------------------------------------------------------------ //
        // Constants sanity
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void DiskPlainLength_Is143360()
        {
            Assert.AreEqual(143360, FloppyDisk.DISK_PLAIN_LENGTH);
            Assert.AreEqual(35 * 16 * 256, FloppyDisk.DISK_PLAIN_LENGTH);
        }

        [TestMethod]
        public void DiskNibbleLength_IsTrackCountTimesTrackNibbleLength()
        {
            Assert.AreEqual(FloppyDisk.TRACK_COUNT * FloppyDisk.TRACK_NIBBLE_LENGTH,
                            FloppyDisk.DISK_NIBBLE_LENGTH);
        }
    }
}

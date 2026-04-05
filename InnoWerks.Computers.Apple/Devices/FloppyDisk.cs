
using System;
using System.Collections.Generic;
using System.IO;
using InnoWerks.Processors;

namespace InnoWerks.Computers.Apple
{
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
#pragma warning disable CA5394 // Do not use insecure randomness
#pragma warning disable CA1707 // Identifiers should not contain underscores

#pragma warning disable IDE0052
#pragma warning disable CS0414
#pragma warning disable CA2201 // Do not raise reserved exception types

    public sealed class FloppyDisk
    {
        public const int TRACK_NIBBLE_LENGTH = 0x1A00;
        public const int TRACK_COUNT = 35;
        public const int SECTOR_COUNT = 16;
        public const int HALF_TRACK_COUNT = TRACK_COUNT * 2;
        public const int DISK_NIBBLE_LENGTH = TRACK_NIBBLE_LENGTH * TRACK_COUNT;
        public const int DISK_PLAIN_LENGTH = 143360;
        public const int DISK_2MG_NON_NIB_LENGTH = DISK_PLAIN_LENGTH + 0x40;
        public const int DISK_2MG_NIB_LENGTH = DISK_NIBBLE_LENGTH + 0x40;
        public const byte DEFAULT_VOLUME_NUMBER = 0xFE;

        enum SectorOrder
        {
            Unknown,

            Dos33,

            ProDOS,
        }

        private static readonly int[] dos33Interleave =
        [
            0x00, 0x07, 0x0E, 0x06, 0x0D, 0x05, 0x0C, 0x04,
            0x0B, 0x03, 0x0A, 0x02, 0x09, 0x01, 0x08, 0x0F
        ];

        private static readonly int[] prodosInterleave =
        [
            0x00, 0x08, 0x01, 0x09, 0x02, 0x0a, 0x03, 0x0b,
            0x04, 0x0c, 0x05, 0x0d, 0x06, 0x0e, 0x07, 0x0f
        ];

        private int[] currentInterleave =>
            sectorOrder == SectorOrder.Dos33 ? dos33Interleave : prodosInterleave;
        private SectorOrder sectorOrder;
        private bool isNibblizedImage;
        private int headerLength;
        private readonly string path;
        private byte[] nibbles = new byte[DISK_NIBBLE_LENGTH];

        private static readonly byte[] gcrTable =
        [
            0x96, 0x97, 0x9A, 0x9B, 0x9D, 0x9E, 0x9F, 0xA6,
            0xA7, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF, 0xB2, 0xB3,
            0xB4, 0xB5, 0xB6, 0xB7, 0xB9, 0xBA, 0xBB, 0xBC,
            0xBD, 0xBE, 0xBF, 0xCB, 0xCD, 0xCE, 0xCF, 0xD3,
            0xD6, 0xD7, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE,
            0xDF, 0xE5, 0xE6, 0xE7, 0xE9, 0xEA, 0xEB, 0xEC,
            0xED, 0xEE, 0xEF, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6,
            0xF7, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF
        ];

        private static byte[] reverseGcrTable;

        private static byte[] ReverseGcrTable
        {
            get
            {
                if (reverseGcrTable == null)
                {
                    reverseGcrTable = new byte[256];
                    for (int i = 0; i < gcrTable.Length; i++)
                    {
                        reverseGcrTable[gcrTable[i] & 0xff] = (byte)(0xff & i);
                    }
                }

                return reverseGcrTable;
            }
        }

        public bool IsWriteProtected { get; set; }

        public int VolumeNumber { get; set; } = DEFAULT_VOLUME_NUMBER;

        public int Length => nibbles.Length;

        public FloppyDisk(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            if (File.Exists(path) == false)
            {
                throw new ArgumentException(nameof(path), "File not found");
            }

            using var fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
            var assumedOrder = path.EndsWith(".po", StringComparison.OrdinalIgnoreCase) ?
                SectorOrder.ProDOS :
                SectorOrder.Dos33;
            ReadDisk(fileStream, assumedOrder);

            IsWriteProtected = new FileInfo(path).IsReadOnly;
            this.path = path;
        }

        public byte ReadNibble(int position)
        {
            return nibbles[position % nibbles.Length];
        }

        public void WriteNibble(int position, byte value)
        {
            if (!IsWriteProtected)
            {
                nibbles[position % nibbles.Length] = value;
            }
        }

        private void ReadDisk(FileStream fileStream, SectorOrder assumedOrder)
        {
            isNibblizedImage = true;
            VolumeNumber = DEFAULT_VOLUME_NUMBER;
            headerLength = 0;
            sectorOrder = assumedOrder;

            // we're not going to have files that are bigger than an int
            int fileSize = (int)fileStream.Length;
            var rawFileBytes = new byte[fileSize];
            var fileBytes = new byte[fileSize];
            fileStream.ReadExactly(rawFileBytes, 0, fileSize);

            switch (fileSize)
            {
                case DISK_2MG_NIB_LENGTH:
                case DISK_2MG_NON_NIB_LENGTH:
                    if (fileSize == DISK_2MG_NON_NIB_LENGTH)
                    {
                        sectorOrder = rawFileBytes[12] == 0x01 ?
                            SectorOrder.ProDOS :
                            SectorOrder.Dos33;
                    }

                    fileSize -= 0x40;
                    VolumeNumber = ((rawFileBytes[17] & 1) == 1) ? rawFileBytes[16] : 254;
                    Array.Copy(rawFileBytes, 040, fileBytes, 0, fileSize);
                    headerLength = 0x40;
                    break;

                default:
                    Array.Copy(rawFileBytes, 0, fileBytes, 0, fileSize);
                    break;
            }

            if (fileSize == DISK_PLAIN_LENGTH)
            {
                isNibblizedImage = false;
                nibbles = Nibblize(fileBytes);
                if (nibbles.Length != DISK_NIBBLE_LENGTH)
                {
                    throw new Exception("File size isn't lining up properly. Didn't nibblize the right number of bytes");
                }
            }
            else if (fileSize != DISK_NIBBLE_LENGTH)
            {
                throw new Exception("File size isn't lining up properly. Wrong number of nibbles in file");
            }
        }

        private static bool hasProdosVolumeAt(byte[] fileBytes, int offset)
        {
            // First two bytes are zero (no previous block)
            if (fileBytes[offset] != 0 || fileBytes[offset + 1] != 0)
            {
                return false;
            }

            // Next two bytes are either both zero or at least in the range of 3...280
            var nextBlock = (ushort)((fileBytes[offset + 3] << 8) | (fileBytes[offset + 2] & 0xff));
            if (nextBlock == 1 || nextBlock == 2 || nextBlock > 280)
            {
                return false;
            }

            // Now check total blocks at offset 0x29
            var totalBlocks = (ushort)((fileBytes[offset + 0x2a] << 8) | (fileBytes[offset + 0x29] & 0xff));
            return totalBlocks == 280;
        }

        private byte[] Nibblize(byte[] fileBytes)
        {
            var rng = new Random();
            var output = new List<byte>();

            for (var track = 0; track < TRACK_COUNT; track++)
            {
                for (var sector = 0; sector < SECTOR_COUNT; sector++)
                {
                    var gap2 = rng.Next(5, 9);

                    WriteNoiseBytes(output, 15);
                    WriteAddressBlock(output, track, sector);
                    WriteNoiseBytes(output, gap2);
                    NibblizeBlock(output, track, currentInterleave[sector], fileBytes);
                    WriteNoiseBytes(output, 38 - gap2);
                }
            }

            return [.. output];
        }

        private static void WriteNoiseBytes(List<byte> output, int cnt)
        {
            for (var b = 0; b < cnt; b++)
            {
                output.Add(0xFF);
            }
        }

        private static void WriteAddressBlock(List<byte> output, int track, int sector)
        {
            output.Add(0xD5);
            output.Add(0xAA);
            output.Add(0x96);

            var checksum = 0;

            checksum ^= DEFAULT_VOLUME_NUMBER;
            WriteOddEven(output, DEFAULT_VOLUME_NUMBER);

            checksum ^= track;
            WriteOddEven(output, (byte)track);

            checksum ^= sector;
            WriteOddEven(output, (byte)sector);

            WriteOddEven(output, (byte)(checksum & 0xFF));

            output.Add(0xDE);
            output.Add(0xAA);
            output.Add(0xEB);
        }

        private static void WriteOddEven(List<byte> output, byte value)
        {
            output.Add((byte)((value >> 1) | 0xAA));
            output.Add((byte)(value | 0xAA));
        }

        private static int DecodeOddEven(byte b1, byte b2)
        {
            return ((b1 << 1) | 1) & b2 & 0xFF;
        }

        private static void NibblizeBlock(List<byte> output, int track, int sector, byte[] nibbles)
        {
            var offset = ((track * SECTOR_COUNT) + sector) * 256;

            // leave this as int until the end, it'll reduce all the casting
            var temp = new int[342];
            for (var i = 0; i < 256; i++)
            {
                temp[i] = (nibbles[offset + i] & 0xFF) >> 2;
            }

            int hi = 0x01;
            int med = 0xAB;
            int lo = 0x55;

            for (var i = 0; i < 0x56; i++)
            {
                temp[i + 256] = ((nibbles[offset + hi] & 1) << 5) |
                                ((nibbles[offset + hi] & 2) << 3) |
                                ((nibbles[offset + med] & 1) << 3) |
                                ((nibbles[offset + med] & 2) << 1) |
                                ((nibbles[offset + lo] & 1) << 1) |
                                ((nibbles[offset + lo] & 2) >> 1);

                hi = (hi - 1) & 0xFF;
                med = (med - 1) & 0xFF;
                lo = (lo - 1) & 0xFF;
            }

            output.Add(0xD5);
            output.Add(0xAA);
            output.Add(0xAD);

            var last = 0;
            for (var i = temp.Length - 1; i > 255; i--)
            {
                var value = temp[i] ^ last;
                output.Add(gcrTable[value]);
                last = temp[i];
            }

            for (var i = 0; i < 256; i++)
            {
                var value = temp[i] ^ last;
                output.Add(gcrTable[value]);
                last = temp[i];
            }

            output.Add(gcrTable[last]);
            output.Add(0xDE);
            output.Add(0xAA);
            output.Add(0xEB);
        }

        public void Save()
        {
            if (!string.IsNullOrEmpty(path))
            {
                Save(path);
            }
        }

        public void Save(string path)
        {
            if (IsWriteProtected) return;

            var dskData = new byte[DISK_PLAIN_LENGTH];
            var allSectorsFound = true;

            for (var track = 0; track < TRACK_COUNT; track++)
            {
                var trackStart = track * TRACK_NIBBLE_LENGTH;
                var trackNibbles = new byte[TRACK_NIBBLE_LENGTH];

                for (var i = 0; i < TRACK_NIBBLE_LENGTH; i++)
                {
                    trackNibbles[i] = nibbles[trackStart + i];
                }

                var foundSectorsOnTrack = new bool[SECTOR_COUNT];
                var pos = 0;

                for (var i = 0; i < SECTOR_COUNT; i++)
                {
                    // find address header: D5 AA 96
                    pos = LocatePattern(pos, trackNibbles, 0xD5, 0xAA, 0x96);
                    if (pos < 0) break;

                    // decode sector number from address field
                    // address field layout after D5 AA 96: vol(2) track(2) sector(2) checksum(2) DE AA EB
                    var sector = DecodeOddEven(trackNibbles[(pos + 7) % TRACK_NIBBLE_LENGTH],
                                               trackNibbles[(pos + 8) % TRACK_NIBBLE_LENGTH]);

                    // skip to end of address block: DE AA
                    pos = LocatePattern(pos, trackNibbles, 0xDE, 0xAA);
                    if (pos < 0) break;

                    // find data header: D5 AA AD
                    pos = LocatePattern(pos, trackNibbles, 0xD5, 0xAA, 0xAD);
                    if (pos < 0) break;

                    // extract and decode the 342 GCR data bytes
                    var gcrData = new byte[342];
                    for (var j = 0; j < 342; j++)
                    {
                        gcrData[j] = trackNibbles[(pos + 3 + j) % TRACK_NIBBLE_LENGTH];
                    }

                    var sectorData = DenibblizeSector(gcrData);

                    // map physical sector to DSK offset using interleave table
                    var offset = currentInterleave[sector] * 256;
                    Array.Copy(sectorData, 0, dskData, track * SECTOR_COUNT * 256 + offset, 256);
                    foundSectorsOnTrack[currentInterleave[sector]] = true;

                    // skip to end of data block: DE AA EB
                    pos = LocatePattern(pos, trackNibbles, 0xDE, 0xAA, 0xEB);
                    if (pos < 0) break;
                }

                if (Array.Exists(foundSectorsOnTrack, f => !f))
                {
                    allSectorsFound = false;
                }
            }

            if (allSectorsFound)
            {
                File.WriteAllBytes(path, dskData);
            }
            else
            {
                SimDebugger.Error("Warning: Not all sectors could be decoded. DSK file not saved.");
            }
        }

        /// <summary>
        /// Scans for a byte pattern in the track nibble data, wrapping around.
        /// Returns the position of the first byte of the match, or -1 if not found.
        /// </summary>
        private static int LocatePattern(int startPos, byte[] data, params byte[] pattern)
        {
            var max = data.Length;
            var pos = startPos;

            while (max-- > 0)
            {
                var matched = true;
                for (var i = 0; i < pattern.Length; i++)
                {
                    if (data[(pos + i) % data.Length] != pattern[i])
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched) return pos;

                pos = (pos + 1) % data.Length;
            }

            return -1;
        }

        /// <summary>
        /// Decodes 342 GCR-encoded bytes into a 256-byte sector.
        /// </summary>
        internal static byte[] DenibblizeSector(byte[] source)
        {
            var temp = new int[342];
            var current = 0;
            var last = 0;

            // un-encode raw GCR data, reversing the XOR chain
            // aux bytes were encoded in reverse order (341 → 256)
            for (var i = temp.Length - 1; i > 255; i--)
            {
                var t = ReverseGcrTable[0xFF & source[current++]];
                temp[i] = t ^ last;
                last ^= t;
            }

            // main bytes were encoded in forward order (0 → 255)
            for (var i = 0; i < 256; i++)
            {
                var t = ReverseGcrTable[0xFF & source[current++]];
                temp[i] = t ^ last;
                last ^= t;
            }

            // decode the pre-nibblized bytes: recombine the 6-bit main data
            // with the 2-bit fragments stored in the aux area (256 → 341)
            var result = new byte[256];
            var p = temp.Length - 1;

            for (var i = 0; i < 256; i++)
            {
                var a = temp[i] << 2;
                a += ((temp[p] & 1) << 1) + ((temp[p] & 2) >> 1);
                result[i] = (byte)a;
                temp[p] >>= 2;
                p--;

                if (p < 256)
                {
                    p = temp.Length - 1;
                }
            }

            return result;
        }
    }
}

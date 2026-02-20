
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using InnoWerks.Processors;

namespace InnoWerks.Computers.Apple
{
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
#pragma warning disable CA5394 // Do not use insecure randomness
#pragma warning disable CA1707 // Identifiers should not contain underscores

    public sealed class FloppyDisk
    {
        public const int TRACK_NIBBLE_LENGTH = 0x1A00;
        public const int TRACK_COUNT = 35;
        public const int SECTOR_COUNT = 16;
        public const int HALF_TRACK_COUNT = TRACK_COUNT * 2;
        public const int DISK_NIBBLE_LENGTH = TRACK_NIBBLE_LENGTH * TRACK_COUNT;
        public const int DISK_PLAIN_LENGTH = 143360;
        public const byte DEFAULT_VOLUME_NUMBER = 0xFE;

        private static readonly int[] dos33Interleave =
        [
            0x00, 0x07, 0x0E, 0x06, 0x0D, 0x05, 0x0C, 0x04,
            0x0B, 0x03, 0x0A, 0x02, 0x09, 0x01, 0x08, 0x0F
        ];

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

        private static readonly Dictionary<byte, byte> reverseGcrTable =
            gcrTable.Select((val, i) => new { Key = val, Value = (byte)i })
                    .ToDictionary(x => x.Key, x => x.Value);

        private readonly string path;
        private readonly byte[] nibbles;

        public bool IsWriteProtected { get; set; }

        public int VolumeNumber { get; set; } = DEFAULT_VOLUME_NUMBER;

        public int Length => nibbles.Length;

        private FloppyDisk(byte[] nibbles, string path = null)
        {
            this.nibbles = nibbles;
            this.path = path;

            IsWriteProtected = string.IsNullOrEmpty(path) || new FileInfo(path).IsReadOnly;
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

        public static FloppyDisk FromDsk(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            return FromDsk(File.ReadAllBytes(path), path);
        }

        public static FloppyDisk FromDsk(byte[] dsk, string path = null)
        {
            ArgumentNullException.ThrowIfNull(dsk);

            if (dsk.Length != 143360)
            {
                throw new ArgumentException("Only standard 140K DOS 3.3 DSK images supported");
            }

            var nibbles = Nibblize(dsk);
            Debug.Assert(nibbles.Length == DISK_NIBBLE_LENGTH);
            return new FloppyDisk(nibbles, path);
        }

        private static byte[] Nibblize(byte[] nibbles)
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
                    NibblizeBlock(output, track, dos33Interleave[sector], nibbles);
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

            for (int track = 0; track < TRACK_COUNT; track++)
            {
                int trackStart = track * TRACK_NIBBLE_LENGTH;
                var foundSectorsOnTrack = new bool[SECTOR_COUNT];

                // A nibblized sector is 416 bytes long. We can iterate through these chunks.
                for (int physicalChunk = 0; physicalChunk < SECTOR_COUNT; physicalChunk++)
                {
                    int chunkStart = trackStart + (physicalChunk * 416);

                    // Find address header in this chunk to identify the physical sector number
                    int addressHeaderPos = -1;
                    for (int i = 0; i < 40; i++) // Search in the first 40 bytes for address header
                    {
                        if (ReadNibble(chunkStart + i) == 0xD5 && ReadNibble(chunkStart + i + 1) == 0xAA && ReadNibble(chunkStart + i + 2) == 0x96)
                        {
                            addressHeaderPos = chunkStart + i;
                            break;
                        }
                    }

                    if (addressHeaderPos == -1) continue;

                    int addrPtr = addressHeaderPos + 3;
                    byte vol = (byte)DecodeOddEven(ReadNibble(addrPtr), ReadNibble(addrPtr + 1)); addrPtr += 2;
                    byte t = (byte)DecodeOddEven(ReadNibble(addrPtr), ReadNibble(addrPtr + 1)); addrPtr += 2;
                    byte s = (byte)DecodeOddEven(ReadNibble(addrPtr), ReadNibble(addrPtr + 1)); addrPtr += 2;
                    byte checksum = (byte)DecodeOddEven(ReadNibble(addrPtr), ReadNibble(addrPtr + 1));

                    if (t != track || (vol ^ t ^ s) != checksum) continue;

                    // Now find data header
                    int dataHeaderPos = -1;
                    // Search in a window after the address header
                    for (int i = (addressHeaderPos - chunkStart) + 14; i < (addressHeaderPos - chunkStart) + 14 + 20; i++)
                    {
                        if (ReadNibble(chunkStart + i) == 0xD5 && ReadNibble(chunkStart + i + 1) == 0xAA && ReadNibble(chunkStart + i + 2) == 0xAD)
                        {
                            dataHeaderPos = chunkStart + i;
                            break;
                        }
                    }

                    if (dataHeaderPos == -1) continue;

                    byte[] sectorData = DenibblizeSector(dataHeaderPos + 3);
                    int logicalSector = Array.IndexOf(dos33Interleave, s);

                    if (logicalSector != -1)
                    {
                        int dskOffset = (track * SECTOR_COUNT + logicalSector) * 256;

                        Array.Copy(sectorData, 0, dskData, dskOffset, 256);
                        foundSectorsOnTrack[logicalSector] = true;
                    }
                }

                if (foundSectorsOnTrack.Any(f => !f))
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

        private byte[] DenibblizeSector(int startOffset)
        {
            var decodedTemp = new int[342];
            var lastVal = 0;

            for (var i = 0; i < 342; i++)
            {
                byte gcrByte = ReadNibble(startOffset + i);

                if (reverseGcrTable.TryGetValue(gcrByte, out byte value) == false)
                {
                    throw new InvalidDataException($"Invalid GCR byte {gcrByte:X2} at offset {startOffset + i}");
                }

                var currentVal = value ^ lastVal;

                decodedTemp[i] = currentVal;
                lastVal = currentVal;
            }

            var mainData = new int[256];
            var auxData = new int[86];

            Array.Copy(decodedTemp, 0, auxData, 0, 86);
            Array.Reverse(auxData);
            Array.Copy(decodedTemp, 86, mainData, 0, 256);

            var resultBytes = new byte[256];
            var unscrambledLowBits = new byte[256];

            int hi = 0x01;
            int med = 0xAB;
            int lo = 0x55;

            for (var i = 0; i < 0x56; i++)
            {
                var val = auxData[i];
                if ((val & 0x20) != 0) unscrambledLowBits[hi] |= 1;
                if ((val & 0x10) != 0) unscrambledLowBits[hi] |= 2;
                if ((val & 0x08) != 0) unscrambledLowBits[med] |= 1;
                if ((val & 0x04) != 0) unscrambledLowBits[med] |= 2;
                if ((val & 0x02) != 0) unscrambledLowBits[lo] |= 1;
                if ((val & 0x01) != 0) unscrambledLowBits[lo] |= 2;

                hi = (hi - 1) & 0xFF;
                med = (med - 1) & 0xFF;
                lo = (lo - 1) & 0xFF;
            }

            for (var i = 0; i < 256; i++)
            {
                resultBytes[i] = (byte)((mainData[i] << 2) | unscrambledLowBits[i]);
            }

            return resultBytes;
        }
    }
}

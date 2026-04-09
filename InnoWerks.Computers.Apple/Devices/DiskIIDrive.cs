
namespace InnoWerks.Computers.Apple
{
#pragma warning disable CA1822 // Mark members as static

    public class DiskIIDrive
    {
        FloppyDisk floppyDisk;

        internal int halfTrack;
        private int trackStartOffset;
        private int nibbleOffset;
        private bool writeMode;
        internal int magnets;
        internal byte latch;
        private int spinCount;
        internal bool isDirty;

        private readonly int driveNumber;

        public bool DiskPresent => floppyDisk != null;

        private readonly int[][] driveHeadStepDelta = new int[4][];

        public DiskIIDrive(int driveNumber)
        {
            this.driveNumber = driveNumber;

            driveHeadStepDelta[0] = [0, 0, 1, 1, 0, 0, 1, 1, -1, -1, 0, 0, -1, -1, 0, 0];  // phase 0
            driveHeadStepDelta[1] = [0, -1, 0, -1, 1, 0, 1, 0, 0, -1, 0, -1, 1, 0, 1, 0];  // phase 1
            driveHeadStepDelta[2] = [0, 0, -1, -1, 0, 0, -1, -1, 1, 1, 0, 0, 1, 1, 0, 0];  // phase 2
            driveHeadStepDelta[3] = [0, 1, 0, 1, -1, 0, -1, 0, 0, 1, 0, 1, -1, 0, -1, 0];  // phase 3
        }

        public void InsertDisk(string path)
        {
            EjectDisk();
            floppyDisk = new FloppyDisk(path);

            Reset();
        }

        public void EjectDisk()
        {
            if (isDirty)
            {
                floppyDisk?.Save();
            }

            floppyDisk = null;

            Reset();
        }

        public void Reset()
        {
            nibbleOffset = 0;
            isDirty = false;
            magnets = 0;
        }

        public void Step(int register, bool motorEnabled)
        {
            // switch drive head stepper motor magnets on/off
            int magnet = (register >> 1) & 0x3;
            magnets &= ~(1 << magnet);
            magnets |= (register & 0x1) << magnet;

            // step the drive head according to stepper magnet changes
            if (motorEnabled == true)
            {
                int delta = driveHeadStepDelta[halfTrack & 0x3][magnets];
                if (delta != 0)
                {
                    int newHalfTrack = halfTrack + delta;
                    if (newHalfTrack < 0)
                    {
                        newHalfTrack = 0;
                    }
                    else if (newHalfTrack > FloppyDisk.HALF_TRACK_COUNT)
                    {
                        newHalfTrack = FloppyDisk.HALF_TRACK_COUNT;
                    }

                    if (newHalfTrack != halfTrack)
                    {
                        halfTrack = newHalfTrack;

                        trackStartOffset = (halfTrack >> 1) * FloppyDisk.TRACK_NIBBLE_LENGTH;
                        if (trackStartOffset >= FloppyDisk.DISK_NIBBLE_LENGTH)
                        {
                            trackStartOffset = FloppyDisk.DISK_NIBBLE_LENGTH - FloppyDisk.TRACK_NIBBLE_LENGTH;
                        }

                        nibbleOffset = 0;

                        // SimDebugger.Info($"stepped to new half track {halfTrack}\n");
                    }
                }
            }
        }

        public void MotorOff()
        {
            // motor turning off — flush any pending writes
            Flush();
        }

        public void Flush()
        {
            if (isDirty)
            {
                floppyDisk?.Save();
                isDirty = false;
            }
        }

        public bool HasDisk => floppyDisk != null;

        public byte ReadLatch(bool motorEnabled)
        {
            var result = (byte)0x7F;

            if (writeMode == false)
            {
                spinCount = (spinCount + 1) & 0x0F;
                if (spinCount > 0)
                {
                    if (floppyDisk != null)
                    {
                        result = floppyDisk.ReadNibble(trackStartOffset + nibbleOffset);
                        if (motorEnabled == true)
                        {
                            nibbleOffset++;
                            if (nibbleOffset >= FloppyDisk.TRACK_NIBBLE_LENGTH)
                            {
                                nibbleOffset = 0;
                            }
                        }
                    }
                    else
                    {
                        result = (byte)0xFF;
                    }
                }
            }
            else
            {
                spinCount = (spinCount + 1) & 0x0F;
                if (spinCount > 0)
                {
                    result = (byte)0x80;
                }
            }

            return result;
        }

        public void Write(bool motorEnabled)
        {
            if (writeMode == true && motorEnabled == true && floppyDisk?.IsWriteProtected == false)
            {
                isDirty = true;

                floppyDisk.WriteNibble(trackStartOffset + nibbleOffset, latch);
                nibbleOffset++;
                if (nibbleOffset >= FloppyDisk.TRACK_NIBBLE_LENGTH)
                {
                    nibbleOffset = 0;
                }
            }
        }

        public bool IsWriteProtected => floppyDisk?.IsWriteProtected ?? false;

        public void SetReadMode()
        {
            writeMode = false;
        }

        public void SetWriteMode()
        {
            writeMode = true;
        }

        public void SetLatchValue(byte value)
        {
            if (writeMode)
            {
                latch = value;
            }
            else
            {
                latch = (byte)0xFF;
            }
        }

        public override string ToString() => $"Disk II Drive {driveNumber}";
    }
}

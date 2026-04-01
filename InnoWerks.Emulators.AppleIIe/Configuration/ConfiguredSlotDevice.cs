namespace InnoWerks.Emulators.AppleIIe
{
    public abstract class ConfiguredSlotDevice
    {
        public DeviceType DeviceType { get; set; }

        public int SlotNumber { get; set; }
    }

    public class DiskImage
    {
        public string Image { get; set; }

        public bool ReadOnly { get; set; }
    }

    public class ConfiguredDiskIIDevice : ConfiguredSlotDevice
    {
        public DiskImage DriveOne { get; set; }

        public DiskImage DriveTwo { get; set; }
    }

    public class ConfiguredHardDisk : ConfiguredSlotDevice
    {
        public DiskImage DriveOne { get; set; }

        public DiskImage DriveTwo { get; set; }

        public DiskImage DriveThree { get; set; }

        public DiskImage DriveFour { get; set; }
    }

    public class ConfiguredMouse : ConfiguredSlotDevice { }
}

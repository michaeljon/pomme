using CommandLine;
using Microsoft.Xna.Framework;

namespace InnoWerks.Emulators.AppleIIe
{
    public class CliOptions
    {
        [Option("disk1", HelpText = "Install this disk in slot 6 drive 1")]
        public string Disk1 { get; set; }

        [Option("disk2", HelpText = "Install this disk in slot 6 drive 2")]
        public string Disk2 { get; set; }

        [Option("harddisk1", HelpText = "Install ProDOS hard drive in slot 7 drive 1 with this file backing it.")]
        public string HardDisk1 { get; set; }

        [Option("harddisk2", HelpText = "Install ProDOS hard drive in slot 7 drive 2 with this file backing it.")]
        public string HardDisk2 { get; set; }

        [Option("monochrome", HelpText = "Display in monochrome: green, amber, or white. Omit for color mode.")]
        public string Monochrome { get; set; }

        [Option("mouse", HelpText = "Enable Apple Mouse Interface Card in slot 4.")]
        public bool Mouse { get; set; }
    }
}

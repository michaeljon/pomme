using CommandLine;
using Microsoft.Xna.Framework;

namespace InnoWerks.Emulators.AppleIIe
{
    public class CliOptions
    {
        [Option('1', "disk1", Default = "disks/dos33.dsk", HelpText = "Disk to boot in Drive 1")]
        public string Disk1 { get; set; }

        [Option('2', "disk2", HelpText = "Disk to boot in Drive 2")]
        public string Disk2 { get; set; }

        [Option('p', "profile", HelpText = "Install ProFile disk controller in slot 5 with this file backing it.")]
        public string Profile { get; set; }
    }
}

using CommandLine;

namespace InnoWerks.Emulators.AppleIIe
{
    public class CliOptions
    {
        [Option('c', "configuration", Default = "configurations/default.json", HelpText = "Use this configuration file")]
        public string Configuration { get; set; }

    }
}

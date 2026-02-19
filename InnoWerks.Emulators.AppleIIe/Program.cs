using System;
using System.Globalization;
using CommandLine;

namespace InnoWerks.Emulators.AppleIIe
{
    internal sealed class Program
    {
        public static void Main(string[] args)
        {
            using var parser = new Parser(with =>
            {
                with.AutoHelp = true;
                with.AutoVersion = true;
                with.HelpWriter = Console.Error;
                with.CaseInsensitiveEnumValues = true;
                with.CaseSensitive = false;
                with.ParsingCulture = CultureInfo.InvariantCulture;
            });

            var result = parser.ParseArguments<CliOptions>(args);

            result.MapResult(
                Run,

                errors =>
                {
                    foreach (var error in errors)
                    {
                        Console.Error.WriteLine(error.ToString());
                    }

                    return 1;
                }
            );
        }

        private static int Run(CliOptions options)
        {
            Console.TreatControlCAsInput = true;

            using var game = new Emulator(options);
            game.Run();

            return 0;
        }
    }
}

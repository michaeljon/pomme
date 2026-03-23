using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLine;

namespace InnoWerks.Emulators.AppleIIe
{
    internal sealed class Program
    {
        private static readonly JsonSerializerOptions serializerOptions = new()
        {
            AllowTrailingCommas = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new ConfiguredSlotDeviceConverter()
            },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IndentSize = 2,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true
        };

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

            using var file = File.OpenRead(options.Configuration);
            var configuration = JsonSerializer.Deserialize<EmulatorConfiguration>(file, serializerOptions);

            using var game = new Emulator(configuration);
            game.Run();

            return 0;
        }
    }
}

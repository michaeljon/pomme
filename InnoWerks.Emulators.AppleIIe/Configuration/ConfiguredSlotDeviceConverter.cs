using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InnoWerks.Emulators.AppleIIe
{
    public class ConfiguredSlotDeviceConverter : JsonConverter<ConfiguredSlotDevice>
    {
        public override ConfiguredSlotDevice Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var deviceType = root.GetProperty("deviceType").GetString();
            ConfiguredSlotDevice device = deviceType switch
            {
                "mouse" => JsonSerializer.Deserialize<ConfiguredMouse>(root.GetRawText(), options),
                "hardDisk" => JsonSerializer.Deserialize<ConfiguredHardDisk>(root.GetRawText(), options),
                "diskii" => JsonSerializer.Deserialize<ConfiguredDiskIIDevice>(root.GetRawText(), options),
                "thunderClock" => JsonSerializer.Deserialize<ConfiguredThunderClock>(root.GetRawText(), options),

                // Alternatively throw an Exception
                _ => JsonSerializer.Deserialize<ConfiguredSlotDevice>(root.GetRawText(), options)
            };

            return device!;
        }

        public override void Write(Utf8JsonWriter writer, ConfiguredSlotDevice value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}

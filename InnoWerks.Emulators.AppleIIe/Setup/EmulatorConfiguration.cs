using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using InnoWerks.Computers.Apple;
using Microsoft.Xna.Framework;

namespace InnoWerks.Emulators.AppleIIe
{
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CA2227 // Collection properties should be read only

    public enum DeviceType
    {
        None,

        Mouse,

        HardDisk,

        DiskII,
    }

    public enum MonochromeColor
    {
        White,

        Green,

        Amber
    }

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

    public class ConfiguredMouse : ConfiguredSlotDevice;

    public class EmulatorConfiguration
    {
        public AppleModel AppleModel { get; set; } = AppleModel.AppleIIeEnhanced;

        // todo: add memory configuration
        // todo: add way to override system roms

        /// <summary>
        /// If true shows the CPU trace and soft switches.
        /// </summary>
        public bool ShowInternals { get; set; } = true;

        /// <summary>
        /// Initial set of breakpoints to install.
        /// </summary>
        public List<ushort> Breakpoints { get; set; }

        /// <summary>
        /// The slot configuration details in slot order.
        /// </summary>
        public List<ConfiguredSlotDevice> Slots { get; set; }

        /// <summary>
        /// Set to true to turn on monochrome mode. Recommended
        /// for A2Desktop and some other applications. Uses the
        /// <see cref="Color"/> for the apple display area.
        /// </summary>
        public bool Monochrome { get; set; }

        /// <summary>
        /// Sets the monochrome color value.
        /// </summary>
        public MonochromeColor Color { get; set; } = MonochromeColor.Green;

        public Color? ResolveMonochromeColor()
        {
            if (Monochrome == false)
            {
                return null;
            }

            return Color switch
            {
                MonochromeColor.Green => DisplayCharacteristics.GreenText,
                MonochromeColor.Amber => DisplayCharacteristics.AmberText,
                MonochromeColor.White => DisplayCharacteristics.WhiteText,

                _ => null
            };
        }
    }

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

                // Alternatively throw an Exception
                _ => JsonSerializer.Deserialize<ConfiguredSlotDevice>(root.GetRawText(), options)
            };

            return device!;
        }

        public override void Write(Utf8JsonWriter writer, ConfiguredSlotDevice value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(value);

            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}

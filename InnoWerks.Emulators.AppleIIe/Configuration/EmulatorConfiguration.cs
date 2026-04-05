using System.Collections.Generic;
using InnoWerks.Computers.Apple;
using Microsoft.Xna.Framework;

namespace InnoWerks.Emulators.AppleIIe
{
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA2227 // Collection properties should be read only

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
        /// If true, installs a No-Slot-Clock (DS1215) in the ROM socket.
        /// </summary>
        public bool NoSlotClock { get; set; }

        public bool JoystickInverted { get; set; } = true;

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
}

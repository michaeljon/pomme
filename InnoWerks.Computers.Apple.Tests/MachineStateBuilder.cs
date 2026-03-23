namespace InnoWerks.Computers.Apple.Tests
{
    /// <summary>
    /// Fluent builder for <see cref="MachineState"/>. Construct a pre-configured
    /// state for unit tests without manually touching the State dictionary.
    /// </summary>
    /// <example>
    /// <code>
    /// var state = MachineStateBuilder.Default()
    ///     .WithAuxRead()
    ///     .WithLcBank2()
    ///     .WithLcReadEnabled()
    ///     .Build();
    /// </code>
    /// </example>
    public sealed class MachineStateBuilder
    {
        private readonly MachineState machineState = new();

        private MachineStateBuilder() { }

        /// <summary>Returns a builder with all soft-switches in their default (false) state.</summary>
        public static MachineStateBuilder Default() => new();

        // ------------------------------------------------------------------ //
        // Generic toggle
        // ------------------------------------------------------------------ //

        /// <summary>Sets <paramref name="softSwitch"/> to <paramref name="value"/> (default: true).</summary>
        public MachineStateBuilder With(SoftSwitch softSwitch, bool value = true)
        {
            machineState.State[softSwitch] = value;
            return this;
        }

        // ------------------------------------------------------------------ //
        // Memory-mapping switches
        // ------------------------------------------------------------------ //

        /// <summary>Enables 80-column store (text-page writes go to auxiliary RAM).</summary>
        public MachineStateBuilder WithStore80() => With(SoftSwitch.Store80);

        /// <summary>Routes reads from $0200–$BFFF to auxiliary RAM.</summary>
        public MachineStateBuilder WithAuxRead() => With(SoftSwitch.AuxRead);

        /// <summary>Routes writes from $0200–$BFFF to auxiliary RAM.</summary>
        public MachineStateBuilder WithAuxWrite() => With(SoftSwitch.AuxWrite);

        /// <summary>Routes zero page ($00–$01) to auxiliary RAM.</summary>
        public MachineStateBuilder WithZpAux() => With(SoftSwitch.ZpAux);

        // ------------------------------------------------------------------ //
        // ROM / slot control switches
        // ------------------------------------------------------------------ //

        /// <summary>Maps the internal CX ROM over the entire $C000–$CFFF range.</summary>
        public MachineStateBuilder WithIntCxRomEnabled() => With(SoftSwitch.IntCxRomEnabled);

        /// <summary>Routes $C300–$C3FF to the slot 3 ROM rather than internal ROM.</summary>
        public MachineStateBuilder WithSlotC3RomEnabled() => With(SoftSwitch.SlotC3RomEnabled);

        /// <summary>Maps the internal C8 ROM over $C800–$CFFF.</summary>
        public MachineStateBuilder WithIntC8RomEnabled() => With(SoftSwitch.IntC8RomEnabled);

        // ------------------------------------------------------------------ //
        // Language Card switches
        // ------------------------------------------------------------------ //

        /// <summary>Selects Language Card Bank 2 ($D000–$DFFF).</summary>
        public MachineStateBuilder WithLcBank2() => With(SoftSwitch.LcBank2);

        /// <summary>Enables reads from Language Card RAM in the $D000–$FFFF range.</summary>
        public MachineStateBuilder WithLcReadEnabled() => With(SoftSwitch.LcReadEnabled);

        /// <summary>Enables writes to Language Card RAM in the $D000–$FFFF range.</summary>
        public MachineStateBuilder WithLcWriteEnabled() => With(SoftSwitch.LcWriteEnabled);

        // ------------------------------------------------------------------ //
        // Video switches
        // ------------------------------------------------------------------ //

        /// <summary>Selects display page 2.</summary>
        public MachineStateBuilder WithPage2() => With(SoftSwitch.Page2);

        /// <summary>Enables high-resolution graphics mode.</summary>
        public MachineStateBuilder WithHiRes() => With(SoftSwitch.HiRes);

        /// <summary>Enables text mode.</summary>
        public MachineStateBuilder WithTextMode() => With(SoftSwitch.TextMode);

        /// <summary>Enables mixed (text + graphics) mode.</summary>
        public MachineStateBuilder WithMixedMode() => With(SoftSwitch.MixedMode);

        // ------------------------------------------------------------------ //
        // Keyboard helpers
        // ------------------------------------------------------------------ //

        /// <summary>Enqueues <paramref name="ascii"/> as a pending keypress.</summary>
        public MachineStateBuilder WithKey(byte ascii)
        {
            machineState.EnqueueKey(ascii);
            return this;
        }

        // ------------------------------------------------------------------ //
        // Terminal
        // ------------------------------------------------------------------ //

        /// <summary>Returns the fully configured <see cref="MachineState"/>.</summary>
        public MachineState Build() => machineState;
    }
}

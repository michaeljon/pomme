using System;

namespace InnoWerks.Computers.Apple.Tests
{
    /// <summary>
    /// Factory for creating pre-configured <see cref="Memory128k"/> instances in
    /// unit tests. Each factory method returns both the memory subsystem and the
    /// <see cref="MachineState"/> it is bound to so that tests can mutate switches
    /// and call <see cref="Memory128k.Remap"/> as needed.
    /// </summary>
    public static class Memory128kFactory
    {
        // ------------------------------------------------------------------ //
        // Bare / default
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates a <see cref="Memory128k"/> with all soft-switches in their
        /// default (false) state. All of $0000–$BFFF maps to zeroed main RAM;
        /// $D000–$FFFF maps to ROM (uninitialised, all zeros).
        /// </summary>
        public static (Memory128k Memory, MachineState State) CreateDefault()
        {
            var state = new MachineState();
            var memory = new Memory128k(state);
            return (memory, state);
        }

        /// <summary>
        /// Creates a <see cref="Memory128k"/> bound to an already-constructed
        /// <see cref="MachineState"/>, then calls <see cref="Memory128k.Remap"/>
        /// so the active maps reflect the supplied switch state.
        /// </summary>
        public static Memory128k CreateWithState(MachineState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            var memory = new Memory128k(state);
            memory.Remap();
            return memory;
        }

        // ------------------------------------------------------------------ //
        // ROM variants
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates a <see cref="Memory128k"/> with a zeroed 16 KB ROM placeholder loaded.
        /// The image is distributed across <c>intCxRom</c> ($C000–$CFFF),
        /// <c>intDxRom</c> ($D000–$DFFF), and <c>intEFRom</c> ($E000–$FFFF).
        /// </summary>
        public static (Memory128k Memory, MachineState State) CreateWith16kRom()
        {
            var state = new MachineState();
            var memory = new Memory128k(state);
            memory.LoadProgramToRom(new byte[16 * 1024]);
            return (memory, state);
        }

        /// <summary>
        /// Creates a <see cref="Memory128k"/> with a specific 16 KB ROM image loaded.
        /// </summary>
        public static (Memory128k Memory, MachineState State) CreateWith16kRom(byte[] rom)
        {
            ArgumentNullException.ThrowIfNull(rom);
            var state = new MachineState();
            var memory = new Memory128k(state);
            memory.LoadProgramToRom(rom);
            return (memory, state);
        }

        /// <summary>
        /// Creates a <see cref="Memory128k"/> with a zeroed 32 KB ROM placeholder loaded.
        /// </summary>
        public static (Memory128k Memory, MachineState State) CreateWith32kRom()
        {
            var state = new MachineState();
            var memory = new Memory128k(state);
            memory.LoadProgramToRom(new byte[32 * 1024]);
            return (memory, state);
        }

        /// <summary>
        /// Creates a <see cref="Memory128k"/> with a specific 32 KB ROM image loaded.
        /// </summary>
        public static (Memory128k Memory, MachineState State) CreateWith32kRom(byte[] rom)
        {
            ArgumentNullException.ThrowIfNull(rom);
            var state = new MachineState();
            var memory = new Memory128k(state);
            memory.LoadProgramToRom(rom);
            return (memory, state);
        }

        // ------------------------------------------------------------------ //
        // Pre-configured banking states
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates a <see cref="Memory128k"/> with auxiliary read and write
        /// both enabled. Reads and writes in $0200–$BFFF go to auxiliary RAM.
        /// </summary>
        public static (Memory128k Memory, MachineState State) CreateWithAuxMemory()
        {
            var state = MachineStateBuilder.Default()
                .WithAuxRead()
                .WithAuxWrite()
                .Build();
            return (CreateWithState(state), state);
        }

        /// <summary>
        /// Creates a <see cref="Memory128k"/> with Language Card RAM fully
        /// enabled: Bank 2 selected, read enabled, and write enabled.
        /// </summary>
        public static (Memory128k Memory, MachineState State) CreateWithLanguageCard()
        {
            var state = MachineStateBuilder.Default()
                .WithLcBank2()
                .WithLcReadEnabled()
                .WithLcWriteEnabled()
                .Build();
            return (CreateWithState(state), state);
        }

        /// <summary>
        /// Creates a <see cref="Memory128k"/> with the internal CX ROM mapped
        /// over the entire $C000–$CFFF range.
        /// </summary>
        public static (Memory128k Memory, MachineState State) CreateWithIntCxRom()
        {
            var state = MachineStateBuilder.Default()
                .WithIntCxRomEnabled()
                .Build();
            return (CreateWithState(state), state);
        }

        // ------------------------------------------------------------------ //
        // Slot ROM helpers
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates a <see cref="Memory128k"/> and loads a 256-byte slot CX ROM
        /// for <paramref name="slot"/> (1–7). The slot CX ROM appears at
        /// $C100 + (slot * $100) when <c>IntCxRomEnabled</c> is false.
        /// </summary>
        public static (Memory128k Memory, MachineState State) CreateWithSlotCxRom(int slot, byte[] rom)
        {
            ArgumentNullException.ThrowIfNull(rom);
            var state = new MachineState();
            var memory = new Memory128k(state);
            memory.LoadSlotCxRom(slot, rom);
            return (memory, state);
        }

        /// <summary>
        /// Creates a <see cref="Memory128k"/> and loads a zeroed 256-byte slot CX ROM
        /// for <paramref name="slot"/> (1–7).
        /// </summary>
        public static (Memory128k Memory, MachineState State) CreateWithSlotCxRom(int slot)
        {
            var state = new MachineState();
            var memory = new Memory128k(state);
            memory.LoadSlotCxRom(slot, new byte[MemoryPage.PageSize]);
            return (memory, state);
        }

        /// <summary>
        /// Creates a <see cref="Memory128k"/> and loads a 2 KB slot C8 ROM
        /// for <paramref name="slot"/> (1–7). The C8 ROM appears at
        /// $C800–$CFFF when the slot is the current slot and
        /// <c>IntC8RomEnabled</c> is false.
        /// </summary>
        public static (Memory128k Memory, MachineState State) CreateWithSlotC8Rom(int slot, byte[] rom)
        {
            ArgumentNullException.ThrowIfNull(rom);
            var state = new MachineState();
            var memory = new Memory128k(state);
            memory.LoadSlotC8Rom(slot, rom);
            return (memory, state);
        }

        /// <summary>
        /// Creates a <see cref="Memory128k"/> and loads a zeroed 2 KB slot C8 ROM
        /// for <paramref name="slot"/> (1–7).
        /// </summary>
        public static (Memory128k Memory, MachineState State) CreateWithSlotC8Rom(int slot)
        {
            var state = new MachineState();
            var memory = new Memory128k(state);
            memory.LoadSlotC8Rom(slot, new byte[2 * 1024]);
            return (memory, state);
        }
    }
}

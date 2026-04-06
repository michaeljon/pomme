// #define DEBUG_READ
// #define DEBUG_WRITE

using System;
using System.Collections.Generic;

namespace InnoWerks.Computers.Apple
{
    public class KeylatchHandler : IAddressInterceptDevice
    {
        private readonly IAppleBus bus;

        private readonly Memory128k memoryBlocks;

        private readonly MachineState machineState;

        public string Name => $"DummyIoHandler";

        public InterceptPriority InterceptPriority => InterceptPriority.SoftSwitch;

        public bool ReportKeyboardLatchAll { get; set; } = true;

        public KeylatchHandler(Memory128k memoryBlocks, MachineState machineState, IAppleBus bus)
        {
            ArgumentNullException.ThrowIfNull(machineState, nameof(machineState));
            ArgumentNullException.ThrowIfNull(memoryBlocks, nameof(memoryBlocks));
            ArgumentNullException.ThrowIfNull(bus, nameof(bus));

            this.machineState = machineState;
            this.memoryBlocks = memoryBlocks;
            this.bus = bus;

            AddressRanges =
            [
                new (0xC000, 0xC08F, MemoryAccessType.Any),
            ];
        }

        public bool DoRead(ushort address, out byte value)
        {
            value = 0;

            return false;
        }

        public bool DoWrite(ushort address, byte value)
        {
            CheckKeyboardLatch(address);

            return false;
        }

        public IReadOnlyList<AddressRange> AddressRanges { get; init; }

        public void Tick() { /* NO-OP */ }

        public void Reset() { /* NO-OP */ }

        private byte CheckKeyboardLatch(ushort address)
        {
            if (ReportKeyboardLatchAll == false)
            {
                return 0x00;
            }

            // all these addresses return the KSTRB and ASCII value
            if (address >= 0xC001 && address <= 0xC00F)
            {
                return machineState.PeekKeyboard();
            }

            // 0xC010 is handled directly by the keyboard as the "owning" device

            // if the IOU is disabled then we only handle the MMU soft switch
            if (machineState.State[SoftSwitch.IOUDisabled] == true)
            {
                return 0x00;
            }

            if (address >= 0xC001 && address <= 0xC01F)
            {
                return machineState.KeyLatch;
            }

            return 0x00;
        }
    }
}

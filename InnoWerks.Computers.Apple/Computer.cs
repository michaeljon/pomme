using System;
using System.Collections.Generic;
using InnoWerks.Processors;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CA1859 // Use concrete types when possible for improved performance

    public class Computer
    {
        public const double CyclesPerSecond = 1020484.0;

        public const int AppleClockSpeed = 1020484;

        public const float FramesPerSecond = 59.94f;

        public const int FrameCycles = 17030;     // NTSC Apple II approx

        public const int VblStart = 12480;     // Start of vertical blank

        public ICpu Processor { get; private set; }

        public IAppleBus Bus { get; private set; }

        public Memory128k Memory { get; private set; }

        public MachineState MachineState { get; private set; }

        public ISlotDevice[] SlotDevices { get; } = new ISlotDevice[8];

        public readonly List<IAddressInterceptDevice> InterceptDevices = [];

        public IntC8Handler IntC8Handler { get; private set; }

        public KeylatchHandler KeylatchHandler { get; private set; }

        public SlotHandler SlotHandler { get; private set; }

        public MMU MMU { get; private set; }

        public IOU IOU { get; private set; }

        public ulong CycleCount => Bus.CycleCount;

        public Computer(AppleModel appleModel, byte[] rom)
        {
            ArgumentNullException.ThrowIfNull(rom);

            var config = new AppleConfiguration(appleModel)
            {
                CpuClass = CpuClass.WDC65C02,
                HasAuxMemory = true,
                Has80Column = true,
                HasLowercase = true,
                RamSize = 128
            };

            MachineState = new MachineState();
            Memory = new Memory128k(MachineState);
            Bus = new AppleBus(Memory, InterceptDevices);

            Processor = Cpu6502Factory.Construct<Cpu65C02>(CpuClass.WDC65C02, Bus);

            IOU = new IOU(Memory, MachineState, Bus);
            MMU = new MMU(Memory, MachineState, Bus);
            IntC8Handler = new IntC8Handler(Memory, MachineState, Bus);
            KeylatchHandler = new KeylatchHandler(Memory, MachineState, Bus);
            SlotHandler = new SlotHandler(Memory, MachineState, Bus, SlotDevices);

            AddDevice(IOU);
            AddDevice(MMU);
            AddDevice(IntC8Handler);
            AddDevice(KeylatchHandler);
            AddDevice(SlotHandler);

            LoadProgramToRom(rom);
        }

        public MouseSlotDevice AddMouse(int slot)
        {
            var mouseDevice = new MouseSlotDevice(slot, this);
            AddDevice(mouseDevice);
            return mouseDevice;
        }

        public ProDOSSlotDevice AddGenericBlockDevice(int slot)
        {
            var blockDevice = new ProDOSSlotDevice(slot, false, this);
            AddDevice(blockDevice);
            return blockDevice;
        }

        public ProDOSSlotDevice AddSmartportDevice(int slot)
        {
            var blockDevice = new ProDOSSlotDevice(slot, true, this);
            AddDevice(blockDevice);
            return blockDevice;
        }

        public DiskIISlotDevice AddDiskIIController(int slot)
        {
            var diskiiController = new DiskIISlotDevice(slot, this);
            AddDevice(diskiiController);
            return diskiiController;
        }

        public ThunderClockSlotDevice AddThunderclock(int slot)
        {
            var thunderclock = new ThunderClockSlotDevice(slot, this);
            AddDevice(thunderclock);
            return thunderclock;
        }

        public MockingboardSlotDevice AddMockingboard(int slot)
        {
            var mockingboard = new MockingboardSlotDevice(slot, this);
            AddDevice(mockingboard);
            return mockingboard;
        }

        public NoSlotClockDevice AddNoSlotClock()
        {
            var noSlotClock = new NoSlotClockDevice(this);
            AddDevice(noSlotClock);
            return noSlotClock;
        }

        public void Build()
        {
            for (var slot = 1; slot < SlotDevices.Length; slot++)
            {
                if (SlotDevices[slot] == null)
                {
                    var emptySlotDevice = new EmptySlotDevice(slot);

                    if (emptySlotDevice.Rom?.Length == MemoryPage.PageSize)
                    {
                        Memory.LoadSlotCxRom(emptySlotDevice.Slot, emptySlotDevice.Rom);
                    }

                    SlotDevices[slot] = emptySlotDevice;
                }

                if (SlotDevices[slot] is SlotRomDevice slotRomDevice)
                {
                    if (slotRomDevice.HasRom)
                    {
                        Memory.LoadSlotCxRom(SlotDevices[slot].Slot, slotRomDevice.Rom);
                    }

                    if (slotRomDevice.ExpansionRom != null)
                    {
                        Memory.LoadSlotC8Rom(SlotDevices[slot].Slot, slotRomDevice.ExpansionRom);
                    }
                }
            }
        }

        public void LoadProgramToRom(byte[] objectCode)
        {
            ArgumentNullException.ThrowIfNull(objectCode);
            Memory.LoadProgramToRom(objectCode);
        }

        public void LoadProgramToRam(byte[] objectCode, ushort origin)
        {
            ArgumentNullException.ThrowIfNull(objectCode);
            Memory.LoadProgramToRam(objectCode, origin);
        }

        public void Reset()
        {
            foreach (var (softSwitch, value) in MachineState.State)
            {
                MachineState.State[softSwitch] = false;
            }

            if (SlotDevices[1] != null)
            {
                KeylatchHandler.ReportKeyboardLatchAll = false;
            }

            foreach (var interceptDevice in InterceptDevices)
            {
                interceptDevice?.Reset();
            }

            foreach (var slotDevice in SlotDevices)
            {
                slotDevice?.Reset();
            }

            Bus.Reset();

            Memory.Remap();

            Processor.Reset();
        }

        private void AddDevice(ISlotDevice slotDevice)
        {
            ArgumentNullException.ThrowIfNull(slotDevice, nameof(slotDevice));

            if (SlotDevices?[slotDevice.Slot] != null)
            {
                throw new ArgumentException($"There is already a device {SlotDevices[slotDevice.Slot].Name} in slot {slotDevice.Slot}");
            }

            SlotDevices[slotDevice.Slot] = slotDevice;
        }

        private void AddDevice(IAddressInterceptDevice interceptDevice)
        {
            ArgumentNullException.ThrowIfNull(interceptDevice, nameof(interceptDevice));

            interceptDevice.Reset();
            InterceptDevices.Add(interceptDevice);

            Bus.AddDevice(interceptDevice);
        }
    }
}

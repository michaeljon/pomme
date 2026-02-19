using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Transactions;
using InnoWerks.Processors;
using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
    public class AppleBus : IBus
    {
        private readonly AppleConfiguration configuration;

        private int transactionCycles;

        private readonly Memory128k memoryBlocks;

        // there are 8 slots, 0 - 7, most of the time, but slot 0 is not used
        // we keep the numbering for convenience
        private readonly ISlotDevice[] slotDevices = new SlotRomDevice[8];

        private readonly List<ISoftSwitchDevice> softSwitchDevices = [];

        private readonly IntC8Handler intC8Handler;

        private readonly MachineState machineState;

        private bool reportKeyboardLatchAll = true;

        public AppleBus(AppleConfiguration configuration, Memory128k memoryBlocks, MachineState machineState)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(memoryBlocks);
            ArgumentNullException.ThrowIfNull(machineState);

            this.configuration = configuration;
            this.memoryBlocks = memoryBlocks;
            this.machineState = machineState;

            intC8Handler = new IntC8Handler(memoryBlocks, machineState, this);
        }

        public void AddDevice(ISoftSwitchDevice device)
        {
            ArgumentNullException.ThrowIfNull(device, nameof(device));
            device.Reset();
            softSwitchDevices.Add(device);
        }

        public void AddDevice(ISlotDevice device)
        {
            ArgumentNullException.ThrowIfNull(device, nameof(device));
            device.Reset();

            if (slotDevices[device.Slot] != null)
            {
                throw new ArgumentException($"There is already a device {slotDevices[device.Slot].Name} in slot {device.Slot}");
            }

            var slotDevice = (SlotRomDevice)device;
            slotDevices[device.Slot] = slotDevice;

            memoryBlocks.LoadSlotCxRom(device.Slot, slotDevice.Rom);
            if (slotDevice.ExpansionRom != null)
            {
                memoryBlocks.LoadSlotC8Rom(device.Slot, slotDevice.ExpansionRom);
            }

            if (device.Slot == 1)
            {
                reportKeyboardLatchAll = false;
            }
        }

        public void BeginTransaction()
        {
            transactionCycles = 0;
        }

        public int EndTransaction()
        {
            return transactionCycles;
        }

        public ulong CycleCount { get; private set; }

        public void SetCpu(ICpu cpu)
        {
            ArgumentNullException.ThrowIfNull(cpu, nameof(cpu));
        }

        public byte Read(ushort address)
        {
            Tick(1);

            if (address >= 0xC000 && address <= 0xC08F)
            {
                foreach (var softSwitchDevice in softSwitchDevices)
                {
                    if (softSwitchDevice.HandlesRead(address))
                    {
                        var value = softSwitchDevice.Read(address);
                        value |= CheckKeyboardLatch(address);

                        return value;
                    }
                }

                return 0x00;
            }
            else if (address >= 0xC090 && address < 0xC0FF)
            {
                var slot = (address >> 4) & 7;
                var slotDevice = slotDevices[slot];

                if (slotDevice?.HandlesRead(address) == true)
                {
                    return slotDevice.Read(address);
                }

                return 0xFF;
            }
            else if (address >= 0xC300 && address <= 0xC3FF || address == 0xCFFF)
            {
                if (intC8Handler.HandlesRead(address))
                {
                    return intC8Handler.Read(address);
                }
            }

            return memoryBlocks.Read(address);
        }

        public void Write(ushort address, byte value)
        {
            Tick(1);

            if (address >= 0xC000 && address <= 0xC08F)
            {
                CheckClearKeystrobe(address);

                foreach (var softSwitchDevice in softSwitchDevices)
                {
                    if (softSwitchDevice.HandlesWrite(address))
                    {
                        softSwitchDevice.Write(address, value);
                    }
                }

                return;
            }
            else if (address >= 0xC090 && address < 0xC0FF)
            {
                var slot = (address >> 4) & 7;
                var slotDevice = slotDevices[slot];

                if (slotDevice?.HandlesWrite(address) == true)
                {
                    SimDebugger.Info("Write IO {0} {1:X4}\n", slot, address);
                    slotDevice.Write(address, value);
                }

                return;
            }
            else if (address >= 0xC300 && address <= 0xC3FF || address == 0xCFFF)
            {
                if (intC8Handler.HandlesWrite(address))
                {
                    intC8Handler.Write(address, value);
                    return;
                }
            }

            memoryBlocks.Write(address, value);
        }

        public byte Peek(ushort address)
        {
            return memoryBlocks.Read(address);
        }

        public void Poke(ushort address, byte value)
        {
            memoryBlocks.Write(address, value);
        }

        public void LoadProgramToRom(byte[] objectCode)
        {
            ArgumentNullException.ThrowIfNull(objectCode);
            memoryBlocks.LoadProgramToRom(objectCode);
        }

        public void LoadProgramToRam(byte[] objectCode, ushort origin)
        {
            ArgumentNullException.ThrowIfNull(objectCode);
            memoryBlocks.LoadProgramToRam(objectCode, origin);
        }

        public void Reset()
        {
            foreach (var (softSwitch, value) in machineState.State)
            {
                machineState.State[softSwitch] = false;
            }

            foreach (var device in softSwitchDevices)
            {
                device.Reset();
            }

            for (var slot = 0; slot < slotDevices.Length; slot++)
            {
                slotDevices[slot]?.Reset();
            }

            memoryBlocks.Remap();

            transactionCycles = 0;
            CycleCount = 0;
        }

        private void Tick(int cycles)
        {
            CycleCount += (ulong)cycles;

            foreach (var device in softSwitchDevices)
            {
                device.Tick(cycles);
            }

            for (var slot = 0; slot < slotDevices.Length; slot++)
            {
                slotDevices[slot]?.Tick(cycles);
            }

            transactionCycles += cycles;
        }

        private byte CheckKeyboardLatch(ushort address)
        {
            if (reportKeyboardLatchAll == false)
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

        private void CheckClearKeystrobe(ushort address)
        {
            if (reportKeyboardLatchAll == false)
            {
                return;
            }

            if (address >= 0xC010 && address <= 0xC01F)
            {
                machineState.ClearKeyboardStrobe();
            }
        }
    }
}

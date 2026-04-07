using System;
using InnoWerks.Processors;
using InnoWerks.Simulators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    // ------------------------------------------------------------------ //
    // Minimal test doubles
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Minimal ICpu test double — not used by SlotRomDevice beyond storage.
    /// </summary>
    internal sealed class CpuTestDouble : ICpu
    {
        public CpuClass CpuClass => CpuClass.WDC65C02;
        public Registers Registers { get; } = new Registers();
        public void Reset() { }
        public int Step(bool returnPriorToBreak = false) => 0;
        public CpuTraceEntry PeekInstruction() => default;
        public void AddIntercept(ushort address, Func<ICpu, IBus, bool> handler) { }
        public void ClearIntercept(ushort address) { }
        public void ClearIntercepts() { }
        public void StackPush(byte b) { }
        public byte StackPop() => 0;
        public void InjectInterrupt(bool nmi) { }
    }

    /// <summary>
    /// Concrete subclass used to exercise SlotRomDevice dispatch logic.
    /// Records which dispatch method was called and with what arguments.
    /// </summary>
    internal sealed class StubSlotDevice : SlotRomDevice
    {
        public MemoryAccessType? lastIoType;
        public byte? lastIoRegister;
        public byte? lastIoValue;

        public MemoryAccessType? lastCxType;
        public ushort? lastCxAddress;

        public MemoryAccessType? lastC8Type;
        public ushort? lastC8Address;

        public byte IoReturn { get; set; } = 0x55;
        public byte CxReturn { get; set; } = 0xAA;
        public byte C8Return { get; set; } = 0xBB;

        public int ResetCount { get; private set; }
        public int TickCount { get; private set; }

        public StubSlotDevice(int slot, Computer computer)
            : base(slot, $"StubSlot{slot}", computer) { }

        public StubSlotDevice(int slot, Computer computer, byte[] cxRom)
            : base(slot, $"StubSlot{slot}", computer, cxRom) { }

        public StubSlotDevice(int slot, Computer computer, byte[] cxRom, byte[] c8Rom)
            : base(slot, $"StubSlot{slot}", computer, cxRom, c8Rom) { }

        public override bool HandlesRead(ushort address) => true;
        public override bool HandlesWrite(ushort address) => true;

        protected override byte DoIo(MemoryAccessType ioType, ushort address, byte value)
        {
            lastIoType = ioType;
            lastIoRegister = (byte)(address & 0x0F);
            lastIoValue = value;
            return IoReturn;
        }

        protected override byte DoCx(MemoryAccessType ioType, ushort address, byte value)
        {
            lastCxType = ioType;
            lastCxAddress = address;
            return CxReturn;
        }

        protected override byte DoC8(MemoryAccessType ioType, ushort address, byte value)
        {
            lastC8Type = ioType;
            lastC8Address = address;
            return C8Return;
        }

        public override void Tick() => TickCount++;

        public override void Reset() => ResetCount++;
    }

    // ------------------------------------------------------------------ //
    // Helper factory
    // ------------------------------------------------------------------ //

    [TestClass]
    public class SlotRomDeviceTests
    {
        private static Computer CreateComputer() =>
            new(AppleModel.AppleIIeEnhanced, new byte[16 * 1024]);

        private static (StubSlotDevice Device, MachineState State) CreateForSlot(int slot)
        {
            var computer = CreateComputer();
            var device = new StubSlotDevice(slot, computer);
            return (device, computer.MachineState);
        }

        private static (StubSlotDevice Device, MachineState State) CreateForSlot(
            int slot, byte[] cxRom, byte[] c8Rom = null)
        {
            var computer = CreateComputer();
            StubSlotDevice device = c8Rom != null
                ? new StubSlotDevice(slot, computer, cxRom, c8Rom)
                : new StubSlotDevice(slot, computer, cxRom);
            return (device, computer.MachineState);
        }

        // ------------------------------------------------------------------ //
        // Identity / address ranges
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void SlotPropertyReflectsConstructorArgument()
        {
            var (device, _) = CreateForSlot(3);
            Assert.AreEqual(3, device.Slot);
        }

        [TestMethod]
        public void NameReflectsConstructorArgument()
        {
            var (device, _) = CreateForSlot(2);
            Assert.AreEqual("StubSlot2", device.Name);
        }

        [TestMethod]
        public void HasRomIsFalseWhenNoCxRomProvided()
        {
            var (device, _) = CreateForSlot(1);
            Assert.IsFalse(device.HasRom);
        }

        [TestMethod]
        public void HasRomIsTrueWhenCxRomProvided()
        {
            var cxRom = new byte[256];
            var (device, _) = CreateForSlot(1, cxRom);
            Assert.IsTrue(device.HasRom);
        }

        [TestMethod]
        public void HasAuxRomIsFalseWhenNoC8RomProvided()
        {
            var (device, _) = CreateForSlot(1);
            Assert.IsFalse(device.HasAuxRom);
        }

        [TestMethod]
        public void HasAuxRomIsTrueWhenC8RomProvided()
        {
            var cxRom = new byte[256];
            var c8Rom = new byte[2048];
            var (device, _) = CreateForSlot(1, cxRom, c8Rom);
            Assert.IsTrue(device.HasAuxRom);
        }

        // ------------------------------------------------------------------ //
        // IO dispatch ($C080+slot*$10 … +$0F)
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadAtIoBaseAddressDispatchesToDoIo()
        {
            var (device, _) = CreateForSlot(1);
            // slot 1 IO base = $C090
            device.Read(0xC090);
            Assert.AreEqual(MemoryAccessType.Read, device.lastIoType);
            Assert.AreEqual((byte)0x00, device.lastIoRegister);
        }

        [TestMethod]
        public void ReadAtIoHighAddressDispatchesToDoIo()
        {
            var (device, _) = CreateForSlot(1);
            // slot 1 IO high = $C09F; register = 0x0F
            device.Read(0xC09F);
            Assert.AreEqual(MemoryAccessType.Read, device.lastIoType);
            Assert.AreEqual((byte)0x0F, device.lastIoRegister);
        }

        [TestMethod]
        public void ReadAtIoAddressReturnsDoIoReturnValue()
        {
            var (device, _) = CreateForSlot(2);
            device.IoReturn = 0x77;
            var result = device.Read(0xC0A0); // slot 2 IO base
            Assert.AreEqual((byte)0x77, result);
        }

        [TestMethod]
        public void WriteAtIoBaseAddressDispatchesToDoIo()
        {
            var (device, _) = CreateForSlot(1);
            device.Write(0xC090, 0x42);
            Assert.AreEqual(MemoryAccessType.Write, device.lastIoType);
            Assert.AreEqual((byte)0x00, device.lastIoRegister);
            Assert.AreEqual((byte)0x42, device.lastIoValue);
        }

        [TestMethod]
        public void ReadAtIoAddressForSlot3UsesCorrectOffset()
        {
            var (device, _) = CreateForSlot(3);
            // slot 3 IO base = $C080 + 3*$10 = $C0B0
            device.Read(0xC0B5);
            Assert.AreEqual((byte)0x05, device.lastIoRegister);
        }

        // ------------------------------------------------------------------ //
        // CX ROM dispatch ($C000+slot*$100 … +$FF) when IntCxRomEnabled=false
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadAtCxRomAddressDispatchesToDoCxWhenIntCxRomDisabled()
        {
            var cxRom = new byte[256];
            var (device, state) = CreateForSlot(2, cxRom);
            state.State[SoftSwitch.IntCxRomEnabled] = false;

            device.Read(0xC200); // slot 2 CX ROM base
            Assert.AreEqual(MemoryAccessType.Read, device.lastCxType);
            Assert.AreEqual((ushort)0xC200, device.lastCxAddress);
        }

        [TestMethod]
        public void ReadAtCxRomAddressReturnsDoCxValueWhenIntCxRomDisabled()
        {
            var cxRom = new byte[256];
            var (device, state) = CreateForSlot(2, cxRom);
            state.State[SoftSwitch.IntCxRomEnabled] = false;
            device.CxReturn = 0xAA;

            var result = device.Read(0xC200);
            Assert.AreEqual((byte)0xAA, result);
        }

        [TestMethod]
        public void ReadAtCxRomAlwaysDispatchesToDoCx()
        {
            // SlotRomDevice no longer gates on IntCxRomEnabled — AppleBus handles
            // that before routing to the device.
            var cxRom = new byte[256];
            var (device, state) = CreateForSlot(2, cxRom);
            state.State[SoftSwitch.IntCxRomEnabled] = true;

            device.Read(0xC200);
            Assert.AreEqual(MemoryAccessType.Read, device.lastCxType);
        }

        [TestMethod]
        public void WriteAtCxRomAddressDispatchesToDoCxWhenIntCxRomDisabled()
        {
            var cxRom = new byte[256];
            var (device, state) = CreateForSlot(2, cxRom);
            state.State[SoftSwitch.IntCxRomEnabled] = false;

            device.Write(0xC200, 0x55);
            Assert.AreEqual(MemoryAccessType.Write, device.lastCxType);
        }

        // ------------------------------------------------------------------ //
        // C8 expansion ROM dispatch ($C800–$CFFF)
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadAtC8RomAddressDispatchesToDoC8WhenIntCxRomDisabled()
        {
            var cxRom = new byte[256];
            var c8Rom = new byte[2048];
            var (device, state) = CreateForSlot(1, cxRom, c8Rom);
            state.State[SoftSwitch.IntCxRomEnabled] = false;

            device.Read(0xC800);
            Assert.AreEqual(MemoryAccessType.Read, device.lastC8Type);
            Assert.AreEqual((ushort)0xC800, device.lastC8Address);
        }

        [TestMethod]
        public void ReadAtC8RomHighAddressDispatchesToDoC8()
        {
            var cxRom = new byte[256];
            var c8Rom = new byte[2048];
            var (device, state) = CreateForSlot(1, cxRom, c8Rom);
            state.State[SoftSwitch.IntCxRomEnabled] = false;

            device.Read(0xCFFF);
            Assert.AreEqual(MemoryAccessType.Read, device.lastC8Type);
        }

        [TestMethod]
        public void ReadAtC8RomAlwaysDispatchesToDoC8()
        {
            // SlotRomDevice no longer gates on soft switches — AppleBus handles
            // that before routing to the device. When the device receives a
            // $C800 read, it always dispatches to DoC8.
            var cxRom = new byte[256];
            var c8Rom = new byte[2048];
            var (device, state) = CreateForSlot(1, cxRom, c8Rom);
            state.State[SoftSwitch.IntCxRomEnabled] = true;
            state.State[SoftSwitch.IntC8RomEnabled] = true;

            device.Read(0xC800);
            Assert.AreEqual(MemoryAccessType.Read, device.lastC8Type);
        }

        [TestMethod]
        public void WriteAtC8RomAddressDispatchesToDoC8WhenIntCxRomDisabled()
        {
            var cxRom = new byte[256];
            var c8Rom = new byte[2048];
            var (device, state) = CreateForSlot(1, cxRom, c8Rom);
            state.State[SoftSwitch.IntCxRomEnabled] = false;

            device.Write(0xC800, 0x99);
            Assert.AreEqual(MemoryAccessType.Write, device.lastC8Type);
        }

        // ------------------------------------------------------------------ //
        // Slot validation
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void SlotZeroIsValid()
        {
            var (device, _) = CreateForSlot(0);
            Assert.AreEqual(0, device.Slot);
        }

        [TestMethod]
        public void SlotSevenIsValid()
        {
            var (device, _) = CreateForSlot(7);
            Assert.AreEqual(7, device.Slot);
        }

        [TestMethod]
        public void SlotGreaterThanSevenThrows()
        {
            var computer = CreateComputer();
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                new StubSlotDevice(8, computer));
        }

        // ------------------------------------------------------------------ //
        // Tick / Reset delegation
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void TickDelegatesToConcreteImplementation()
        {
            var (device, _) = CreateForSlot(1);
            device.Tick();
            device.Tick();
            device.Tick();
            device.Tick();
            device.Tick();
            Assert.AreEqual(5, device.TickCount);
        }

        [TestMethod]
        public void ResetDelegatesToConcreteImplementation()
        {
            var (device, _) = CreateForSlot(1);
            device.Reset();
            device.Reset();
            Assert.AreEqual(2, device.ResetCount);
        }
    }
}

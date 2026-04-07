using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class ComputerTests
    {
        private static Computer CreateComputer() =>
            new(AppleModel.AppleIIeEnhanced, new byte[16 * 1024]);

        // ------------------------------------------------------------------ //
        // Construction
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ConstructorCreatesProcessor()
        {
            var computer = CreateComputer();
            Assert.IsNotNull(computer.Processor);
        }

        [TestMethod]
        public void ConstructorCreatesBus()
        {
            var computer = CreateComputer();
            Assert.IsNotNull(computer.Bus);
        }

        [TestMethod]
        public void ConstructorCreatesMemory()
        {
            var computer = CreateComputer();
            Assert.IsNotNull(computer.Memory);
        }

        [TestMethod]
        public void ConstructorCreatesMachineState()
        {
            var computer = CreateComputer();
            Assert.IsNotNull(computer.MachineState);
        }

        [TestMethod]
        public void ConstructorCreatesIou()
        {
            var computer = CreateComputer();
            Assert.IsNotNull(computer.IOU);
        }

        [TestMethod]
        public void ConstructorCreatesMmu()
        {
            var computer = CreateComputer();
            Assert.IsNotNull(computer.MMU);
        }

        [TestMethod]
        public void ConstructorCreatesSlotHandler()
        {
            var computer = CreateComputer();
            Assert.IsNotNull(computer.SlotHandler);
        }

        // ------------------------------------------------------------------ //
        // Build — fills empty slots
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void BuildFillsEmptySlots()
        {
            var computer = CreateComputer();
            computer.Build();

            for (var slot = 1; slot <= 7; slot++)
            {
                Assert.IsNotNull(computer.SlotDevices[slot],
                    $"Slot {slot} should have a device after Build()");
            }
        }

        [TestMethod]
        public void BuildDoesNotOverwriteExistingDevice()
        {
            var computer = CreateComputer();
            var disk = computer.AddDiskIIController(6);
            computer.Build();

            Assert.AreSame(disk, computer.SlotDevices[6]);
        }

        [TestMethod]
        public void BuildFillsUnoccupiedSlotsWithEmptySlotDevice()
        {
            var computer = CreateComputer();
            computer.Build();

            Assert.IsInstanceOfType<EmptySlotDevice>(computer.SlotDevices[1]);
        }

        // ------------------------------------------------------------------ //
        // Reset
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ResetClearsSoftSwitches()
        {
            var computer = CreateComputer();
            computer.Build();
            computer.MachineState.State[SoftSwitch.AuxRead] = true;
            computer.Reset();
            Assert.IsFalse(computer.MachineState.State[SoftSwitch.AuxRead]);
        }

        [TestMethod]
        public void ResetAppliesIouDefaults()
        {
            var computer = CreateComputer();
            computer.Build();
            computer.MachineState.State[SoftSwitch.TextMode] = false;
            computer.Reset();
            Assert.IsTrue(computer.MachineState.State[SoftSwitch.TextMode]);
        }

        [TestMethod]
        public void ResetAppliesMmuDefaults()
        {
            var computer = CreateComputer();
            computer.Build();
            computer.MachineState.State[SoftSwitch.LcBank2] = false;
            computer.Reset();
            Assert.IsTrue(computer.MachineState.State[SoftSwitch.LcBank2]);
        }

        [TestMethod]
        public void ResetZeroesCycleCount()
        {
            var computer = CreateComputer();
            computer.Build();
            computer.Bus.Read(0x0100); // increment cycle count
            computer.Reset();
            Assert.AreEqual(0UL, computer.CycleCount);
        }

        // ------------------------------------------------------------------ //
        // Add devices
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void AddDiskIIControllerReturnsDevice()
        {
            var computer = CreateComputer();
            var disk = computer.AddDiskIIController(6);
            Assert.IsNotNull(disk);
            Assert.AreEqual(6, disk.Slot);
        }

        [TestMethod]
        public void AddMockingboardReturnsDevice()
        {
            var computer = CreateComputer();
            var mb = computer.AddMockingboard(4);
            Assert.IsNotNull(mb);
            Assert.AreEqual(4, mb.Slot);
        }

        [TestMethod]
        public void AddMouseReturnsDevice()
        {
            var computer = CreateComputer();
            var mouse = computer.AddMouse(2);
            Assert.IsNotNull(mouse);
            Assert.AreEqual(2, mouse.Slot);
        }

        [TestMethod]
        public void AddThunderclockReturnsDevice()
        {
            var computer = CreateComputer();
            var tc = computer.AddThunderclock(1);
            Assert.IsNotNull(tc);
            Assert.AreEqual(1, tc.Slot);
        }

        [TestMethod]
        public void AddNoSlotClockReturnsDevice()
        {
            var computer = CreateComputer();
            var nsc = computer.AddNoSlotClock();
            Assert.IsNotNull(nsc);
        }

        [TestMethod]
        public void AddGenericBlockDeviceReturnsDevice()
        {
            var computer = CreateComputer();
            var hd = computer.AddGenericBlockDevice(7);
            Assert.IsNotNull(hd);
            Assert.AreEqual(7, hd.Slot);
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void AddingTwoDevicesToSameSlotThrows()
        {
            var computer = CreateComputer();
            computer.AddDiskIIController(6);
            computer.AddDiskIIController(6);
        }

        // ------------------------------------------------------------------ //
        // Slot devices array
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void SlotDevicesArrayHasEightEntries()
        {
            var computer = CreateComputer();
            Assert.AreEqual(8, computer.SlotDevices.Length);
        }

        [TestMethod]
        public void Slot0IsNullByDefault()
        {
            var computer = CreateComputer();
            computer.Build();
            Assert.IsNull(computer.SlotDevices[0]);
        }
    }
}

using InnoWerks.Processors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CA1707, CA1822

namespace InnoWerks.Simulators.Tests
{
    /// <summary>
    /// Tests for IRQ and NMI interrupt dispatch, masking, priority, and RTI.
    /// </summary>
    [TestClass]
    public class InterruptTests : TestBase
    {
        private const ushort MainStart = 0x0000;
        private const ushort NmiIsrAddress = 0x0200;
        private const ushort IrqIsrAddress = 0x0300;

        /// <summary>
        /// Sets up a bus with reset, NMI, and IRQ vectors pointing at known ISR locations,
        /// and places NOP instructions in main memory starting at MainStart.
        /// </summary>
        private static AccessCountingBus CreateBus()
        {
            var bus = new AccessCountingBus();

            // Reset vector -> MainStart ($0000)
            bus.Poke(Cpu6502Core.RstVectorL, (byte)(MainStart & 0xFF));
            bus.Poke(Cpu6502Core.RstVectorH, (byte)(MainStart >> 8));

            // NMI vector -> NmiIsrAddress ($0200)
            bus.Poke(Cpu6502Core.NmiVectorL, (byte)(NmiIsrAddress & 0xFF));
            bus.Poke(Cpu6502Core.NmiVectorH, (byte)(NmiIsrAddress >> 8));

            // IRQ vector -> IrqIsrAddress ($0300)
            bus.Poke(Cpu6502Core.IrqVectorL, (byte)(IrqIsrAddress & 0xFF));
            bus.Poke(Cpu6502Core.IrqVectorH, (byte)(IrqIsrAddress >> 8));

            // Main program: a handful of NOPs
            bus.Poke(0x0000, 0xEA); // NOP
            bus.Poke(0x0001, 0xEA); // NOP
            bus.Poke(0x0002, 0xEA); // NOP

            return bus;
        }

        private static ICpu CreateCpu(AccessCountingBus bus, CpuClass cpuClass)
        {
            var cpu = Cpu6502Factory.Construct(
                cpuClass, bus, null, null
            );

            cpu.Reset();

            return cpu;
        }

        // -------------------------------------------------------------------------
        // Dispatch tests
        // Place a BRK ($00) at the ISR address as a sentinel. Step with
        // returnPriorToBreak: true so execution stops at the vector address,
        // letting us inspect state immediately after dispatch.
        // -------------------------------------------------------------------------

        [TestMethod]
        public void NmiDispatchesToNmiVector()
        {
            var bus = CreateBus();
            bus.Poke(NmiIsrAddress, 0x00); // BRK sentinel

            var cpu = CreateCpu(bus, CpuClass.WDC6502);
            cpu.InjectInterrupt(nmi: true);
            cpu.Step(returnPriorToBreak: true);

            Assert.AreEqual(NmiIsrAddress, cpu.Registers.ProgramCounter, "PC should be at NMI ISR");
            Assert.AreEqual(0xFA, cpu.Registers.StackPointer, "3 bytes pushed: SP $FD -> $FA");
            Assert.IsTrue(cpu.Registers.Interrupt, "I flag must be set after NMI");
        }

        [TestMethod]
        public void IrqDispatchesToIrqVector()
        {
            var bus = CreateBus();
            bus.Poke(IrqIsrAddress, 0x00); // BRK sentinel

            var cpu = CreateCpu(bus, CpuClass.WDC6502);
            cpu.InjectInterrupt(nmi: false);
            cpu.Step(returnPriorToBreak: true);

            Assert.AreEqual(IrqIsrAddress, cpu.Registers.ProgramCounter, "PC should be at IRQ ISR");
            Assert.AreEqual(0xFA, cpu.Registers.StackPointer, "3 bytes pushed: SP $FD -> $FA");
            Assert.IsTrue(cpu.Registers.Interrupt, "I flag must be set after IRQ");
        }

        // -------------------------------------------------------------------------
        // Stack frame contents
        // -------------------------------------------------------------------------

        [TestMethod]
        public void NmiStackFrameIsCorrect()
        {
            var bus = CreateBus();
            bus.Poke(NmiIsrAddress, 0x00); // BRK sentinel

            var cpu = CreateCpu(bus, CpuClass.WDC6502);
            // Initial state: PC=$0000, PS=$20 (Unused bit only)
            var psBeforeInterrupt = cpu.Registers.ProcessorStatus;

            cpu.InjectInterrupt(nmi: true);
            cpu.Step(returnPriorToBreak: true);

            // StackPushWord pushes PCH then PCL; with SP starting at $FD:
            //   $01FD = PCH, $01FC = PCL, $01FB = PS
            Assert.AreEqual(0x00, bus.Peek(0x01FD), "Stacked PCH should be $00");
            Assert.AreEqual(0x00, bus.Peek(0x01FC), "Stacked PCL should be $00");

            var stackedPs = bus.Peek(0x01FB);
            Assert.AreEqual(0, stackedPs & 0x10, "B flag must be clear (0) in stacked PS");
            Assert.AreNotEqual(0, stackedPs & 0x20, "Unused flag must be set in stacked PS");
            Assert.AreEqual(psBeforeInterrupt & ~0x10, stackedPs & ~0x10,
                "Stacked PS should match pre-interrupt PS with B forced clear");
        }

        [TestMethod]
        public void IrqStackFrameIsCorrect()
        {
            var bus = CreateBus();
            bus.Poke(IrqIsrAddress, 0x00); // BRK sentinel

            var cpu = CreateCpu(bus, CpuClass.WDC6502);
            cpu.InjectInterrupt(nmi: false);
            cpu.Step(returnPriorToBreak: true);

            Assert.AreEqual(0x00, bus.Peek(0x01FD), "Stacked PCH should be $00");
            Assert.AreEqual(0x00, bus.Peek(0x01FC), "Stacked PCL should be $00");
            var stackedPs = bus.Peek(0x01FB);
            Assert.AreEqual(0, stackedPs & 0x10, "B flag must be clear in stacked PS for IRQ");
        }

        // -------------------------------------------------------------------------
        // Masking: IRQ respects I flag; NMI ignores it
        // -------------------------------------------------------------------------

        [TestMethod]
        public void IrqMaskedWhenInterruptFlagSet()
        {
            var bus = CreateBus();
            bus.Poke(IrqIsrAddress, 0xEA); // NOP at ISR — should not be reached

            var cpu = CreateCpu(bus, CpuClass.WDC6502);
            cpu.Registers.Interrupt = true; // mask IRQ

            cpu.InjectInterrupt(nmi: false);
            cpu.Step(); // should execute NOP at $0000, not dispatch

            Assert.AreEqual(0x0001, cpu.Registers.ProgramCounter, "IRQ masked: NOP at $0000 should run");
            Assert.AreEqual(0xFD, cpu.Registers.StackPointer, "Stack must be untouched when IRQ is masked");
        }

        [TestMethod]
        public void IrqRemainsDeferredThenFiresAfterCli()
        {
            var bus = CreateBus();
            bus.Poke(IrqIsrAddress, 0x00); // BRK sentinel at ISR

            // Place CLI ($58) at $0001 so after the masked NOP, CLI executes next
            bus.Poke(0x0001, 0x58); // CLI

            var cpu = CreateCpu(bus, CpuClass.WDC6502);
            cpu.Registers.Interrupt = true; // mask IRQ

            cpu.InjectInterrupt(nmi: false);

            // Step 1: IRQ masked, NOP runs at $0000
            cpu.Step();
            Assert.AreEqual(0x0001, cpu.Registers.ProgramCounter, "Step 1: NOP executed, IRQ deferred");
            Assert.AreEqual(0xFD, cpu.Registers.StackPointer, "Stack untouched after masked step");

            // Step 2: CLI clears I flag; IRQ still pending
            cpu.Step();
            Assert.AreEqual(0x0002, cpu.Registers.ProgramCounter, "Step 2: CLI executed");
            Assert.IsFalse(cpu.Registers.Interrupt, "I flag clear after CLI");

            // Step 3: IRQ fires now that I flag is clear
            cpu.Step(returnPriorToBreak: true);
            Assert.AreEqual(IrqIsrAddress, cpu.Registers.ProgramCounter, "IRQ dispatched after CLI");
            Assert.AreEqual(0xFA, cpu.Registers.StackPointer, "3 bytes pushed by IRQ dispatch");
        }

        [TestMethod]
        public void NmiFiresEvenWhenInterruptFlagSet()
        {
            var bus = CreateBus();
            bus.Poke(NmiIsrAddress, 0x00); // BRK sentinel

            var cpu = CreateCpu(bus, CpuClass.WDC6502);
            cpu.Registers.Interrupt = true; // would mask IRQ, but not NMI

            cpu.InjectInterrupt(nmi: true);
            cpu.Step(returnPriorToBreak: true);

            Assert.AreEqual(NmiIsrAddress, cpu.Registers.ProgramCounter, "NMI fires regardless of I flag");
        }

        // -------------------------------------------------------------------------
        // Priority: NMI beats IRQ
        // -------------------------------------------------------------------------

        [TestMethod]
        public void NmiTakesPriorityOverIrq()
        {
            var bus = CreateBus();
            bus.Poke(NmiIsrAddress, 0x00); // BRK sentinel
            bus.Poke(IrqIsrAddress, 0x00); // BRK sentinel

            var cpu = CreateCpu(bus, CpuClass.WDC6502);
            cpu.InjectInterrupt(nmi: true);
            cpu.InjectInterrupt(nmi: false);

            cpu.Step(returnPriorToBreak: true);

            Assert.AreEqual(NmiIsrAddress, cpu.Registers.ProgramCounter, "NMI should take priority over IRQ");
        }

        // -------------------------------------------------------------------------
        // RTI restores state
        // -------------------------------------------------------------------------

        [TestMethod]
        public void RtiRestoresPcAndPsAfterNmi()
        {
            var bus = CreateBus();
            // ISR: NOP then RTI
            bus.Poke(NmiIsrAddress, 0xEA);      // NOP
            bus.Poke(NmiIsrAddress + 1, 0x40);  // RTI

            var cpu = CreateCpu(bus, CpuClass.WDC6502);
            var initialPs = cpu.Registers.ProcessorStatus;

            cpu.InjectInterrupt(nmi: true);

            // Step 1: dispatch NMI + execute NOP at ISR; PC = $0201
            cpu.Step();
            Assert.AreEqual((ushort)(NmiIsrAddress + 1), cpu.Registers.ProgramCounter);

            // Step 2: RTI restores PC=$0000 and PS
            cpu.Step();
            Assert.AreEqual(0x0000, cpu.Registers.ProgramCounter, "RTI should restore PC to $0000");
            Assert.AreEqual(initialPs, cpu.Registers.ProcessorStatus, "RTI should restore pre-interrupt PS");
        }

        [TestMethod]
        public void RtiRestoresPcAndPsAfterIrq()
        {
            var bus = CreateBus();
            // ISR: NOP then RTI
            bus.Poke(IrqIsrAddress, 0xEA);      // NOP
            bus.Poke(IrqIsrAddress + 1, 0x40);  // RTI

            var cpu = CreateCpu(bus, CpuClass.WDC6502);
            var initialPs = cpu.Registers.ProcessorStatus;

            cpu.InjectInterrupt(nmi: false);

            // Step 1: dispatch IRQ + execute NOP at ISR; PC = $0301
            cpu.Step();
            Assert.AreEqual((ushort)(IrqIsrAddress + 1), cpu.Registers.ProgramCounter);

            // Step 2: RTI restores PC=$0000 and PS
            cpu.Step();
            Assert.AreEqual(0x0000, cpu.Registers.ProgramCounter, "RTI should restore PC to $0000");
            Assert.AreEqual(initialPs, cpu.Registers.ProcessorStatus, "RTI should restore pre-interrupt PS");
        }

        [TestMethod]
        public void NmiClearsDecimalFlagOn65C02()
        {
            var bus = CreateBus();
            bus.Poke(NmiIsrAddress, 0x00); // BRK sentinel

            var cpu = CreateCpu(bus, CpuClass.WDC65C02);
            cpu.Registers.Decimal = true;

            cpu.InjectInterrupt(nmi: true);
            cpu.Step(returnPriorToBreak: true);

            Assert.IsFalse(cpu.Registers.Decimal, "65C02 NMI must clear Decimal flag");
        }

        [TestMethod]
        public void IrqClearsDecimalFlagOn65C02()
        {
            var bus = CreateBus();
            bus.Poke(IrqIsrAddress, 0x00); // BRK sentinel

            var cpu = CreateCpu(bus, CpuClass.WDC65C02);
            cpu.Registers.Decimal = true;

            cpu.InjectInterrupt(nmi: false);
            cpu.Step(returnPriorToBreak: true);

            Assert.IsFalse(cpu.Registers.Decimal, "65C02 IRQ must clear Decimal flag");
        }

        [TestMethod]
        public void NmiDoesNotClearDecimalFlagOnNmos6502()
        {
            var bus = CreateBus();
            bus.Poke(NmiIsrAddress, 0x00); // BRK sentinel

            var cpu = CreateCpu(bus, CpuClass.WDC6502);
            cpu.Registers.Decimal = true;

            cpu.InjectInterrupt(nmi: true);
            cpu.Step(returnPriorToBreak: true);

            Assert.IsTrue(cpu.Registers.Decimal, "NMOS 6502 NMI must leave Decimal flag unchanged");
        }
    }
}

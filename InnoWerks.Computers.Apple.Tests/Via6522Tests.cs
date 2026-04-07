using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class Via6522Tests
    {
        // ------------------------------------------------------------------ //
        // Reset
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ResetClearsAllRegisters()
        {
            var via = new Via6522();
            via.Write(0x02, 0xFF); // DDRB
            via.Write(0x03, 0xFF); // DDRA
            via.Write(0x00, 0xAB); // ORB
            via.Write(0x01, 0xCD); // ORA
            via.Reset();

            Assert.AreEqual((byte)0x00, via.Read(0x02)); // DDRB
            Assert.AreEqual((byte)0x00, via.Read(0x03)); // DDRA
            Assert.AreEqual((byte)0x00, via.Read(0x00)); // ORB
        }

        [TestMethod]
        public void ResetClearsIrq()
        {
            var via = new Via6522();
            Assert.IsFalse(via.IrqActive);
        }

        // ------------------------------------------------------------------ //
        // Data Direction Registers
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void WriteDdrbAndReadBack()
        {
            var via = new Via6522();
            via.Write(0x02, 0xAA);
            Assert.AreEqual((byte)0xAA, via.Read(0x02));
        }

        [TestMethod]
        public void WriteDdraAndReadBack()
        {
            var via = new Via6522();
            via.Write(0x03, 0x55);
            Assert.AreEqual((byte)0x55, via.Read(0x03));
        }

        // ------------------------------------------------------------------ //
        // Output Registers — masked by DDR
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ReadOrbReturnsMaskedByDdrb()
        {
            var via = new Via6522();
            via.Write(0x02, 0xF0); // upper 4 bits as output
            via.Write(0x00, 0xAB); // write ORB
            // read: output bits from ORB (0xA0), input bits = 0
            Assert.AreEqual((byte)0xA0, via.Read(0x00));
        }

        [TestMethod]
        public void ReadOraReturnsMaskedByDdra()
        {
            var via = new Via6522();
            via.Write(0x03, 0xFF); // all output
            via.Write(0x01, 0x42); // write ORA
            Assert.AreEqual((byte)0x42, via.Read(0x01));
        }

        [TestMethod]
        public void ReadOraNoHandshakeReturnsSameAsOra()
        {
            var via = new Via6522();
            via.Write(0x03, 0xFF);
            via.Write(0x0F, 0x99); // write ORA no handshake
            Assert.AreEqual((byte)0x99, via.Read(0x0F));
        }

        // ------------------------------------------------------------------ //
        // Timer 1 — free-running countdown
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void Timer1DecrementsOnTick()
        {
            var via = new Via6522();
            // write latch low then counter high to set counter = $0100
            via.Write(0x04, 0x00); // T1L-L
            via.Write(0x05, 0x01); // T1C-H → starts timer at $0100

            via.Tick();

            Assert.AreEqual((byte)0xFF, via.Read(0x04)); // T1C-L
            Assert.AreEqual((byte)0x00, via.Read(0x05)); // T1C-H
        }

        [TestMethod]
        public void Timer1FreeRunsAfterReset()
        {
            var via = new Via6522();
            // after reset, counter is 0, first tick should wrap to $FFFF
            var initial = via.Read(0x04); // T1C-L before tick

            for (var i = 0; i < 5; i++) via.Tick();

            var after = via.Read(0x04);
            // counter should have changed (decremented from initial value)
            Assert.AreNotEqual(initial, after);
        }

        [TestMethod]
        public void Timer1ReadingT1CLClearsInterruptFlag()
        {
            var via = new Via6522();
            // enable T1 interrupt
            via.Write(0x0E, 0xC0); // IER: set bit 6 (T1)
            // set timer to 1 so it fires on next tick
            via.Write(0x04, 0x01);
            via.Write(0x05, 0x00);
            via.Tick(); // counter reaches 0 → interrupt fires

            Assert.IsTrue(via.IrqActive);

            // reading T1C-L should clear the T1 flag
            via.Read(0x04);
            Assert.IsFalse(via.IrqActive);
        }

        // ------------------------------------------------------------------ //
        // Timer 1 — one-shot vs free-running
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void Timer1OneShotFiresInterruptOnce()
        {
            var via = new Via6522();
            var irqCount = 0;
            via.OnIrqChanged = (active) => { if (active) irqCount++; };

            // ACR bit 6 = 0 → one-shot mode (default)
            via.Write(0x0E, 0xC0); // enable T1 interrupt
            via.Write(0x04, 0x02); // T1L-L = 2
            via.Write(0x05, 0x00); // T1C-H = 0 → counter = $0002

            for (var i = 0; i < 10; i++)
            {
                via.Tick();
                if (via.IrqActive) via.Read(0x04); // acknowledge
            }

            Assert.AreEqual(1, irqCount);
        }

        [TestMethod]
        public void Timer1FreeRunningReloadsAndFiresRepeatedly()
        {
            var via = new Via6522();
            var irqCount = 0;
            via.OnIrqChanged = (active) => { if (active) irqCount++; };

            via.Write(0x0B, 0x40); // ACR bit 6 = 1 → free-running
            via.Write(0x0E, 0xC0); // enable T1 interrupt
            via.Write(0x04, 0x03); // T1L-L = 3
            via.Write(0x05, 0x00); // T1C-H = 0 → counter = $0003, latch = $0003

            for (var i = 0; i < 20; i++)
            {
                via.Tick();
                if (via.IrqActive) via.Read(0x04); // acknowledge
            }

            Assert.IsTrue(irqCount > 1, $"Expected multiple interrupts, got {irqCount}");
        }

        // ------------------------------------------------------------------ //
        // Timer 2
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void Timer2DecrementsAfterStarted()
        {
            var via = new Via6522();
            via.Write(0x08, 0x10); // T2L-L
            via.Write(0x09, 0x00); // T2C-H → starts timer at $0010

            for (var i = 0; i < 5; i++) via.Tick();

            Assert.AreEqual((byte)0x0B, via.Read(0x08)); // T2C-L should be 0x10 - 5 = 0x0B
        }

        [TestMethod]
        public void Timer2FiresInterruptOnUnderflow()
        {
            var via = new Via6522();
            via.Write(0x0E, 0xA0); // IER: set bit 5 (T2)
            via.Write(0x08, 0x02); // T2L-L
            via.Write(0x09, 0x00); // T2C-H → counter = $0002

            for (var i = 0; i < 5; i++) via.Tick();

            Assert.IsTrue(via.IrqActive);
        }

        // ------------------------------------------------------------------ //
        // Interrupt Enable Register (IER)
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void IerSetBitsWhenBit7IsHigh()
        {
            var via = new Via6522();
            via.Write(0x0E, 0xC0); // set bit 6 (T1 enable)
            // read IER: bit 7 always 1, bit 6 should be set
            Assert.AreEqual((byte)0xC0, via.Read(0x0E));
        }

        [TestMethod]
        public void IerClearBitsWhenBit7IsLow()
        {
            var via = new Via6522();
            via.Write(0x0E, 0xC0); // set T1
            via.Write(0x0E, 0x40); // clear T1 (bit 7 = 0)
            Assert.AreEqual((byte)0x80, via.Read(0x0E)); // only bit 7 remains
        }

        // ------------------------------------------------------------------ //
        // Interrupt Flag Register (IFR)
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void WritingToIfrClearsFlags()
        {
            var via = new Via6522();
            via.Write(0x0E, 0xC0); // enable T1
            via.Write(0x04, 0x01);
            via.Write(0x05, 0x00);
            via.Tick(); // T1 fires

            // write to IFR to clear T1 flag
            via.Write(0x0D, 0x40);
            Assert.IsFalse(via.IrqActive);
        }

        // ------------------------------------------------------------------ //
        // Control Registers
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void AcrWriteAndReadBack()
        {
            var via = new Via6522();
            via.Write(0x0B, 0x55);
            Assert.AreEqual((byte)0x55, via.Read(0x0B));
        }

        [TestMethod]
        public void PcrWriteAndReadBack()
        {
            var via = new Via6522();
            via.Write(0x0C, 0xAA);
            Assert.AreEqual((byte)0xAA, via.Read(0x0C));
        }

        // ------------------------------------------------------------------ //
        // Shift Register
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void ShiftRegisterWriteAndReadBack()
        {
            var via = new Via6522();
            via.Write(0x0A, 0x42);
            Assert.AreEqual((byte)0x42, via.Read(0x0A));
        }

        // ------------------------------------------------------------------ //
        // Port B Write Callback
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void WriteOrbInvokesCallback()
        {
            var via = new Via6522();
            byte capturedPortB = 0;
            byte capturedPortA = 0;
            via.OnPortBWrite = (b, a) => { capturedPortB = b; capturedPortA = a; };

            via.Write(0x02, 0xFF); // DDRB all output
            via.Write(0x03, 0xFF); // DDRA all output
            via.Write(0x01, 0xAA); // ORA
            via.Write(0x00, 0x55); // ORB → triggers callback

            Assert.AreEqual((byte)0x55, capturedPortB);
            Assert.AreEqual((byte)0xAA, capturedPortA);
        }

        // ------------------------------------------------------------------ //
        // IRQ callback
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void IrqCallbackFiredOnInterrupt()
        {
            var via = new Via6522();
            var irqFired = false;
            via.OnIrqChanged = (active) => { if (active) irqFired = true; };

            via.Write(0x0E, 0xC0); // enable T1
            via.Write(0x04, 0x01);
            via.Write(0x05, 0x00);
            via.Tick(); // underflow

            Assert.IsTrue(irqFired);
        }

        // ------------------------------------------------------------------ //
        // Timer 1 latch
        // ------------------------------------------------------------------ //

        [TestMethod]
        public void Timer1LatchReadBack()
        {
            var via = new Via6522();
            via.Write(0x06, 0x34); // T1L-L
            via.Write(0x07, 0x12); // T1L-H

            Assert.AreEqual((byte)0x34, via.Read(0x06));
            Assert.AreEqual((byte)0x12, via.Read(0x07));
        }

        [TestMethod]
        public void WritingT1LHClearsT1InterruptFlag()
        {
            var via = new Via6522();
            via.Write(0x0E, 0xC0); // enable T1
            via.Write(0x04, 0x01);
            via.Write(0x05, 0x00);
            via.Tick(); // T1 fires

            Assert.IsTrue(via.IrqActive);

            via.Write(0x07, 0x00); // write T1L-H clears T1 flag
            Assert.IsFalse(via.IrqActive);
        }
    }
}

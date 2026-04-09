using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InnoWerks.Computers.Apple.Tests
{
    [TestClass]
    public class AY38910Tests
    {
        // Bus control portB values (~RESET must be high = bit 2 set)
        private const byte Latch = 0x07;    // BDIR=1, BC1=1, ~RESET=inactive
        private const byte WriteData = 0x06; // BDIR=1, BC1=0, ~RESET=inactive
        private const byte Inactive = 0x04;  // ~RESET=inactive only
        private const byte BusReset = 0x00;  // ~RESET=active

        private static void LatchRegister(AY38910 psg, byte register)
        {
            psg.SetBusControl(Latch, register);
            psg.SetBusControl(Inactive, 0);
        }

        private static void WriteRegister(AY38910 psg, byte register, byte value)
        {
            LatchRegister(psg, register);
            psg.SetBusControl(WriteData, value);
            psg.SetBusControl(Inactive, 0);
        }

        private static byte ReadRegister(AY38910 psg, byte register)
        {
            LatchRegister(psg, register);
            return psg.ReadData();
        }

        //
        // Reset
        //

        [TestMethod]
        public void ResetClearsAllRegisters()
        {
            var psg = new AY38910();
            WriteRegister(psg, 0x07, 0x3F);
            psg.Reset();
            Assert.AreEqual((byte)0x00, ReadRegister(psg, 0x07));
        }

        //
        // Latch and write
        //

        [TestMethod]
        public void LatchAndWriteRegister()
        {
            var psg = new AY38910();
            WriteRegister(psg, 0x08, 0x0A); // channel A volume = 10
            Assert.AreEqual((byte)0x0A, ReadRegister(psg, 0x08));
        }

        [TestMethod]
        public void TonePeriodHighIsMaskedTo4Bits()
        {
            var psg = new AY38910();
            WriteRegister(psg, 0x01, 0xFF); // R1 tone A high
            Assert.AreEqual((byte)0x0F, ReadRegister(psg, 0x01));
        }

        [TestMethod]
        public void NoisePeriodIsMaskedTo5Bits()
        {
            var psg = new AY38910();
            WriteRegister(psg, 0x06, 0xFF); // R6 noise
            Assert.AreEqual((byte)0x1F, ReadRegister(psg, 0x06));
        }

        [TestMethod]
        public void VolumeIsMaskedTo5Bits()
        {
            var psg = new AY38910();
            WriteRegister(psg, 0x08, 0xFF); // R8 vol A
            Assert.AreEqual((byte)0x1F, ReadRegister(psg, 0x08));
        }

        [TestMethod]
        public void EnvelopeShapeIsMaskedTo4Bits()
        {
            var psg = new AY38910();
            WriteRegister(psg, 0x0D, 0xFF); // R13 envelope
            Assert.AreEqual((byte)0x0F, ReadRegister(psg, 0x0D));
        }

        //
        // Bus control — reset
        //

        [TestMethod]
        public void ResetViaBusControl()
        {
            var psg = new AY38910();
            WriteRegister(psg, 0x07, 0x3F);
            psg.SetBusControl(BusReset, 0); // ~RESET active
            Assert.AreEqual((byte)0x00, ReadRegister(psg, 0x07));
        }

        //
        // Clock — output
        //

        [TestMethod]
        public void ClockReturnsValueInRange()
        {
            var psg = new AY38910();
            WriteRegister(psg, 0x00, 0x01); // tone A period low = 1
            WriteRegister(psg, 0x08, 0x0F); // vol A = max
            WriteRegister(psg, 0x07, 0x38); // enable tone A

            var sample = psg.Clock();
            Assert.IsTrue(sample >= 0.0f && sample <= 1.0f,
                $"Sample {sample} out of range [0,1]");
        }

        [TestMethod]
        public void ClockWithSilenceReturnsNearZero()
        {
            var psg = new AY38910();
            WriteRegister(psg, 0x07, 0x3F); // disable all
            WriteRegister(psg, 0x08, 0x00); // vol A = 0
            WriteRegister(psg, 0x09, 0x00); // vol B = 0
            WriteRegister(psg, 0x0A, 0x00); // vol C = 0

            var sample = psg.Clock();
            Assert.IsLessThan(0.01f, sample, $"Expected near-silence, got {sample}");
        }

        [TestMethod]
        public void MultipleClockCallsProduceNonZeroOutput()
        {
            var psg = new AY38910();
            WriteRegister(psg, 0x00, 0x01); // tone A period = 1
            WriteRegister(psg, 0x08, 0x0F); // vol A = max
            WriteRegister(psg, 0x07, 0x38); // enable tone A

            var hasNonZero = false;
            for (var i = 0; i < 100; i++)
            {
                var sample = psg.Clock();
                if (sample > 0.01f) hasNonZero = true;
            }

            Assert.IsTrue(hasNonZero, "Expected some non-zero output");
        }

        //
        // Mixer register
        //

        [TestMethod]
        public void MixerRegisterFullByteWritten()
        {
            var psg = new AY38910();
            WriteRegister(psg, 0x07, 0xAB);
            Assert.AreEqual((byte)0xAB, ReadRegister(psg, 0x07));
        }

        //
        // Tone period low — full byte
        //

        [TestMethod]
        public void TonePeriodLowIsFullByte()
        {
            var psg = new AY38910();
            WriteRegister(psg, 0x00, 0xCD);
            Assert.AreEqual((byte)0xCD, ReadRegister(psg, 0x00));
        }
    }
}

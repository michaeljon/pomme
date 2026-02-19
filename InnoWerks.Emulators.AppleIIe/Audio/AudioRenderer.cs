using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace InnoWerks.Emulators.AppleIIe
{
    public sealed class AudioRenderer : IDisposable
    {
        private readonly DynamicSoundEffectInstance instance;

        private const double CyclesPerSecond = 1020484.0;
        private const int SampleRate = 44100;

        // We render one frame worth of audio at a time (e.g. 1/60th sec)
        private float[] floatBuffer = new float[2048];
        private byte[] byteBuffer = new byte[4096];

        // State tracking
        private List<SpeakerToggle> pendingToggles = new();
        private ulong totalSamplesGenerated;
        private float currentSpeakerLevel = -1.0f; // The level at the end of the last frame

        // DSP State
        private float smoothedValue;
        private float capacitorCharge;

        public AudioRenderer()
        {
            instance = new DynamicSoundEffectInstance(SampleRate, AudioChannels.Mono);

            instance.Play();
        }

        public void Clear()
        {
            currentSpeakerLevel = -1.0f;

            floatBuffer = new float[2048];
            byteBuffer = new byte[2048];

            smoothedValue = 0.0f;
            capacitorCharge = 0.0f;
        }

        public void UpdateAudio(ulong currentCpuCycle, AppleIIAudioSource source)
        {
            ArgumentNullException.ThrowIfNull(source);

            // --- SAFETY CHECK: EMULATOR RESET ---
            // If the CPU time jumped backwards, the emulator was likely reset.
            // We must reset our audio counters to match, or we will crash/screech.
            double expectedTime = (double)totalSamplesGenerated * CyclesPerSecond / SampleRate;
            if (currentCpuCycle < expectedTime - 1000) // 1000 cycle tolerance
            {
                totalSamplesGenerated = 0;
                pendingToggles.Clear();
                // Recalculate samples from t=0
            }

            // 1. Calculate needed samples
            double totalAudioSeconds = currentCpuCycle / CyclesPerSecond;
            ulong targetSampleCount = (ulong)(totalAudioSeconds * SampleRate);
            ulong samplesToGenerate = targetSampleCount - totalSamplesGenerated;

            if (samplesToGenerate <= 0) return;

            // Resize buffer if needed
            if (samplesToGenerate > (ulong)floatBuffer.Length) floatBuffer = new float[samplesToGenerate + 100];

            // 2. Harvest Toggles
            while (source.TryDequeue(out var t))
            {
                if (t.CycleTimestamp <= currentCpuCycle)
                {
                    pendingToggles.Add(t);
                }
            }

            // 3. RENDER LOOP
            int processedToggleCount = 0;

            for (ulong i = 0; i < samplesToGenerate; i++)
            {
                ulong sampleIndex = totalSamplesGenerated + i + 1UL;

                // High-Precision Timing
                double sampleEndCycle = (sampleIndex * CyclesPerSecond) / SampleRate;
                double sampleStartCycle = ((sampleIndex - 1) * CyclesPerSecond) / SampleRate;

                double currentCycle = sampleStartCycle;
                float energySum = 0.0f;

                // Process Toggles
                int scanIndex = processedToggleCount;
                while (scanIndex < pendingToggles.Count)
                {
                    var toggle = pendingToggles[scanIndex];

                    // Stop if toggle is in the future
                    if (toggle.CycleTimestamp >= sampleEndCycle) break;

                    // --- THE FIX: CLAMP DURATION ---
                    // If toggle is in the past (missed it last frame?), duration is 0.
                    // This prevents "negative energy" glitches.
                    double duration = Math.Max(0, toggle.CycleTimestamp - currentCycle);

                    energySum += (float)(duration * currentSpeakerLevel);

                    currentSpeakerLevel = toggle.State;

                    // Move current time forward, but never go backwards
                    currentCycle = Math.Max(currentCycle, toggle.CycleTimestamp);

                    processedToggleCount++;
                    scanIndex++;
                }

                // Fill remainder of sample
                double remaining = Math.Max(0, sampleEndCycle - currentCycle);
                energySum += (float)(remaining * currentSpeakerLevel);

                // Average
                float sampleValue = energySum / (float)(sampleEndCycle - sampleStartCycle);

                // --- DSP CHAIN ---
                // 1. Low Pass (Smoothing)
                smoothedValue += (sampleValue - smoothedValue) * 0.50f;

                // 2. High Pass (DC Blocker)
                capacitorCharge = (smoothedValue * 0.005f) + (capacitorCharge * 0.995f);
                float finalSample = (smoothedValue - capacitorCharge);

                // 3. Silence Snap
                // If the signal is extremely quiet, snap to 0 to prevent "floating bit" buzzing
                if (Math.Abs(finalSample) < 0.001f) finalSample = 0;

                // 4. Volume & Clamp
                finalSample *= 0.25f;
                floatBuffer[i] = Math.Clamp(finalSample, -1.0f, 1.0f);
            }

            // 4. Cleanup & Submit
            if (processedToggleCount > 0)
            {
                pendingToggles.RemoveRange(0, processedToggleCount);
            }

            totalSamplesGenerated += samplesToGenerate;
            SubmitToHardware((int)samplesToGenerate);
        }

        private void SubmitToHardware(int count)
        {
            // Resize byte buffer if necessary
            if (byteBuffer.Length < count * 2)
                byteBuffer = new byte[count * 2];

            int byteIndex = 0;
            for (int i = 0; i < count; i++)
            {
                short pcm = (short)(Math.Clamp(floatBuffer[i], -1.0f, 1.0f) * 32767);
                byteBuffer[byteIndex++] = (byte)(pcm & 0xFF);
                byteBuffer[byteIndex++] = (byte)((pcm >> 8) & 0xFF);
            }

            if (count > 0)
            {
                instance.SubmitBuffer(byteBuffer, 0, count * 2);
            }
        }

        public void Dispose()
        {
            instance?.Stop();
            instance?.Dispose();
        }
    }
}

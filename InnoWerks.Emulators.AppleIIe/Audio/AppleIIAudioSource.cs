using System.Collections.Concurrent;

namespace InnoWerks.Emulators.AppleIIe
{
    public struct SpeakerToggle : System.IEquatable<SpeakerToggle>
    {
        // The absolute CPU cycle when it happened
        public ulong CycleTimestamp { get; set; }

        // 1.0f or -1.0f
        public float State { get; set; }

        public SpeakerToggle(ulong cycleTimestamp, float state)
        {
            CycleTimestamp = cycleTimestamp;
            State = state;
        }

        public override readonly bool Equals(object obj)
        {
            return ((SpeakerToggle)obj).CycleTimestamp == CycleTimestamp &&
                   ((SpeakerToggle)obj).State == State;
        }

        public override readonly int GetHashCode()
        {
            return (int)CycleTimestamp ^ State.GetHashCode();
        }

        public static bool operator ==(SpeakerToggle left, SpeakerToggle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SpeakerToggle left, SpeakerToggle right)
        {
            return !(left == right);
        }

        public bool Equals(SpeakerToggle other)
        {
            return other.CycleTimestamp == CycleTimestamp &&
                   other.State == State;
        }
    }

    public class AppleIIAudioSource
    {
        // A queue of all toggles that happened this frame
        private readonly ConcurrentQueue<SpeakerToggle> toggles = new();

        private float currentState = -1.0f;

        public void TouchSpeaker(ulong currentTotalCpuCycles)
        {
            currentState *= -1.0f; // Flip state

            toggles.Enqueue(new SpeakerToggle
            {
                CycleTimestamp = currentTotalCpuCycles,
                State = currentState
            });
        }

        public bool TryDequeue(out SpeakerToggle toggle) => toggles.TryDequeue(out toggle);

        public void Clear() => toggles.Clear();
    }
}

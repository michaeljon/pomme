using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace InnoWerks.Simulators
{
    public sealed class CpuTraceBuffer
    {
        private readonly CpuTraceEntry[] buffer;
        private int current;
        private int count;

        public int Capacity => buffer.Length;
        public int Count => count;

        public CpuTraceBuffer(int capacity)
        {
            buffer = new CpuTraceEntry[capacity];
        }

        public void Add(CpuTraceEntry entry)
        {
            buffer[current] = entry;

            current = (current + 1) % buffer.Length;

            if (count < buffer.Length)
            {
                count++;
            }
        }

        public CpuTraceEntry this[int i]
        {
            get
            {
                int idx = (current - count + i + buffer.Length) % buffer.Length;
                return buffer[idx];
            }
        }

        public IEnumerable<CpuTraceEntry> Entries
        {
            get
            {
                for (int i = 0; i < count; i++)
                {
                    int idx = (current - count + i + buffer.Length) % buffer.Length;
                    yield return buffer[idx];
                }
            }
        }

        public void WriteStackTrace()
        {
            var entries = Entries.ToList();
            foreach (var entry in entries)
            {
                Debug.WriteLine(entry.ToString());
            }
        }
    }
}

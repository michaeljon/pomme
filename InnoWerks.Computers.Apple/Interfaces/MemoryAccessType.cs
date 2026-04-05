using System;

namespace InnoWerks.Computers.Apple
{
    [Flags]
    public enum MemoryAccessType
    {
        Read = 0x01,
        Write = 0x02,
        Any = Read | Write
    }
}

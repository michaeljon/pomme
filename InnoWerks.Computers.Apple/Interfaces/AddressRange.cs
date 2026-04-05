using System;

namespace InnoWerks.Computers.Apple
{
    /// <summary>
    /// Defines a contiguous range of addresses that a device is interested in.
    /// </summary>
    public readonly struct AddressRange : IEquatable<AddressRange>
    {
        public ushort Start { get; init; }
        public ushort End { get; init; }
        public MemoryAccessType MemoryAccessType { get; init; }

        public AddressRange(ushort start, ushort end, MemoryAccessType memoryAccessType)
        {
            Start = start;
            End = end;
            MemoryAccessType = memoryAccessType;
        }

        public bool Contains(ushort address, MemoryAccessType memoryAccessType) =>
            (MemoryAccessType & memoryAccessType) != 0 && address >= Start && address <= End;

        public override bool Equals(object obj) => obj is AddressRange other && Equals(other);

        public bool Equals(AddressRange other) => Start == other.Start && End == other.End;

        public override int GetHashCode() => HashCode.Combine(Start, End);

        public static bool operator ==(AddressRange left, AddressRange right) => left.Equals(right);

        public static bool operator !=(AddressRange left, AddressRange right) => !left.Equals(right);
    }
}

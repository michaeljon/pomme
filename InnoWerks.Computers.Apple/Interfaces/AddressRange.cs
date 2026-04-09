using System;
using System.Collections.Generic;

namespace InnoWerks.Computers.Apple
{
    /// <summary>
    /// Defines a set of addresses that a device is interested in.
    /// <para>
    /// Supports two modes:
    /// </para>
    /// <ul>
    /// <li>Contiguous range: start/end bounds checked with simple comparison</li>
    /// <li>Discrete set: a <see cref="HashSet{T}"/> of specific addresses</li>
    /// </ul>
    /// <para>
    /// Both modes also filter by <see cref="MemoryAccessType"/> (read, write, or both).
    /// </para>
    /// </summary>
    public class AddressRange
    {
        private readonly ushort start;
        private readonly ushort end;
        private readonly HashSet<ushort> discreteAddresses;
        private readonly MemoryAccessType memoryAccessType;
        private readonly bool isDiscrete;

        /// <summary>
        /// Creates an address range for a contiguous block of addresses.
        /// </summary>
        public AddressRange(ushort start, ushort end, MemoryAccessType memoryAccessType)
        {
            this.start = start;
            this.end = end;
            this.memoryAccessType = memoryAccessType;
        }

        /// <summary>
        /// Creates an address range from a discrete set of addresses.
        /// </summary>
        public AddressRange(HashSet<ushort> addresses, MemoryAccessType memoryAccessType)
        {
            ArgumentNullException.ThrowIfNull(addresses);

            discreteAddresses = addresses;
            this.memoryAccessType = memoryAccessType;
            isDiscrete = true;
        }

        /// <summary>
        /// Creates an address range from a single address.
        /// </summary>
        public AddressRange(ushort address, MemoryAccessType memoryAccessType)
        {
            discreteAddresses = [address];
            this.memoryAccessType = memoryAccessType;
            isDiscrete = true;
        }

        public bool InterestedIn(ushort address, MemoryAccessType accessType)
        {
            if ((memoryAccessType & accessType) == 0)
            {
                return false;
            }

            return isDiscrete
                ? discreteAddresses.Contains(address)
                : address >= start && address <= end;
        }

        /// <summary>
        /// Enumerates all addresses covered by this range for the given access type.
        /// Used by the bus to build dispatch tables at registration time.
        /// </summary>
        public IEnumerable<ushort> GetAddresses(MemoryAccessType accessType)
        {
            if ((memoryAccessType & accessType) == 0)
            {
                yield break;
            }

            if (isDiscrete)
            {
                foreach (var addr in discreteAddresses)
                {
                    yield return addr;
                }
            }
            else
            {
                for (int addr = start; addr <= end; addr++)
                {
                    yield return (ushort)addr;
                }
            }
        }
    }
}

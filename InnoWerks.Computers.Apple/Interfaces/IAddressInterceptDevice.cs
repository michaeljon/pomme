using System.Collections.Generic;

namespace InnoWerks.Computers.Apple
{
    public enum InterceptPriority
    {
        AddressIntercept = 0,

        SoftSwitch = 1,

        SlotDevice = 2,
    }

    /// <summary>
    /// <para>
    /// A device that monitors a range of bus addresses and can optionally
    /// intercept reads or writes. Unlike slot devices (which own specific
    /// I/O addresses) or soft switch devices (which own $C000-$C08F),
    /// address intercept devices observe bus traffic and conditionally
    /// handle accesses based on their internal state.
    /// </para>
    ///
    /// <para>
    /// Examples: No-Slot-Clock (watches $C300-$C3FF for an unlock sequence,
    /// then provides clock data on reads).
    /// </para>
    ///
    /// <para>
    /// The AppleBus queries registered intercept devices before falling
    /// through to normal memory routing. The device returns whether it
    /// handled the access; if not, normal routing continues.
    /// </para>
    /// </summary>
    public interface IAddressInterceptDevice
    {
        string Name { get; }

        InterceptPriority InterceptPriority { get; }

        /// <summary>
        /// Called on every bus read to an address in the device's registered range.
        /// Returns true if the device handled the read (value is set to the
        /// result); false if the bus should continue with normal routing.
        /// </summary>
        bool DoRead(ushort address, out byte value);

        /// <summary>
        /// Called on every bus write to an address in the device's registered range.
        /// Returns true if the device consumed the write; false if the bus
        /// should continue with normal routing.
        /// </summary>
        bool DoWrite(ushort address, byte value);

        /// <summary>
        /// The address ranges this device is interested in. The bus will only
        /// call DoRead/DoWrite for addresses within these ranges.
        /// </summary>
        IReadOnlyList<AddressRange> AddressRanges { get; }

        void Tick();

        void Reset();
    }
}

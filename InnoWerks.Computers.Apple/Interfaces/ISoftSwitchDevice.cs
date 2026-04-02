namespace InnoWerks.Simulators
{
#pragma warning disable CA1716
    public interface ISoftSwitchDevice
    {
        string Name { get; }

        bool HandlesRead(ushort address);

        bool HandlesWrite(ushort address);

        byte Read(ushort address);

        void Write(ushort address, byte value);

        void Tick(int cycles);

        void Reset();
    }
#pragma warning restore CA1716
}

namespace InnoWerks.Simulators
{
    public interface ISoftSwitchDevice
    {
        string Name { get; }

        bool HandlesRead(ushort address);

        bool HandlesWrite(ushort address);

        byte Read(ushort address);

        void Write(ushort address, byte value);

        void Tick();

        void Reset();
    }
}

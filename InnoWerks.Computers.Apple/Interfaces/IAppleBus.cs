using InnoWerks.Simulators;

namespace InnoWerks.Computers.Apple
{
    public interface IAppleBus : IBus
    {
        void AddDevice(ISoftSwitchDevice device);

        void AddDevice(ISlotDevice slotDevice);

        void AddDevice(IAddressInterceptDevice interceptDevice);
    }
}

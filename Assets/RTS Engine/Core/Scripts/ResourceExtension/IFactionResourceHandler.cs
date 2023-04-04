using RTSEngine.Event;

namespace RTSEngine.ResourceExtension
{
    public interface IFactionResourceHandler
    {
        ResourceTypeInfo Type { get; }

        int Amount { get; }
        int ReservedAmount { get; }

        int Capacity { get; }
        int ReservedCapacity { get; }
        int FreeAmount { get; }

        event CustomEventHandler<IFactionResourceHandler, ResourceUpdateEventArgs> FactionResourceAmountUpdated;

        void UpdateAmount(ResourceTypeValue updateValue, out int restAmount);
        void SetAmount(ResourceTypeValue setValue, out int restAmount);

        void ReserveAmount(ResourceTypeValue reserveValue);
        void SetReserveAmount(ResourceTypeValue setReserveValue);

        void ReleaseAmount(ResourceTypeValue reserveValue);
    }
}
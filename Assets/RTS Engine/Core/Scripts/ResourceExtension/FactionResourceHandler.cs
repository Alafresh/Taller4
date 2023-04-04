using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Logging;
using System.Collections.Generic;

namespace RTSEngine.ResourceExtension
{
    public class FactionResourceHandler : IFactionResourceHandler
    {
        #region Attributes
        private int factionID;
        public ResourceTypeInfo Type { private set; get; }

        public int Amount { private set; get; }
        public int ReservedAmount { private set; get; }

        public int Capacity { private set; get; }
        public int ReservedCapacity { private set; get; }
        public int FreeAmount => Capacity - Amount;

        public List<IResourceCollector> activeCollectors;

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IFactionResourceHandler, ResourceUpdateEventArgs> FactionResourceAmountUpdated;

        private void RaiseFactionResourceAmountUpdated(ResourceUpdateEventArgs args)
        {
            CustomEventHandler<IFactionResourceHandler, ResourceUpdateEventArgs> handler = FactionResourceAmountUpdated;

            handler?.Invoke(this, args);
        }
        #endregion

        #region Initializing/Terminating
        public FactionResourceHandler(
            IFactionSlot factionSlot,
            IGameManager gameMgr,
            ResourceTypeInfo data,
            ResourceTypeValue startingAmount)
        {
            this.factionID = factionSlot.ID;
            this.Type = data;

            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            Amount = startingAmount.amount;
            Capacity = startingAmount.capacity;

            activeCollectors = new List<IResourceCollector>();
        }
        #endregion

        #region Updating Amount
        public void UpdateAmount(ResourceTypeValue updateValue, out int restAmount)
        {
            restAmount = 0;

            if (Type.HasCapacity && Type.CapacityType == ResourceCapacityType.simpleLimit
                && Amount + updateValue.amount > Capacity + updateValue.capacity)
            {
                int allowedAmount = Capacity + updateValue.capacity - Amount;
                restAmount = updateValue.amount - allowedAmount;
                updateValue.amount = allowedAmount;
            }

            Capacity += updateValue.capacity;
            Amount += updateValue.amount;

            ResourceUpdateEventArgs eventArgs = new ResourceUpdateEventArgs(
                    Type,
                    updateValue);

            globalEvent.RaiseFactionSlotResourceAmountUpdatedGlobal(factionID.ToFactionSlot(), eventArgs);
            RaiseFactionResourceAmountUpdated(eventArgs);

            OnAmountUpdated();
        }

        public void SetAmount(ResourceTypeValue setValue, out int restAmount)
        {
            UpdateAmount(
                new ResourceTypeValue 
                {
                    amount = -Amount + setValue.amount,
                    capacity = -Capacity + setValue.capacity
                }, out restAmount);
        }

        private void OnAmountUpdated()
        {
            if (Amount < 0)
            {
                //logger.LogError($"[FactionResourceHandler - Faction ID: {factionID} - Resource Type: {Type.Key}] Property 'Amount' has been updated to a negative value. This is not allowed. Follow error trace to see how we got here.");
                Amount = 0;
            }
            if (Capacity < 0)
            {
                //logger.LogError($"[FactionResourceHandler - Faction ID: {factionID} - Resource Type: {Type.Key}] Property 'Capacity' has been updated to a negative value. This is not allowed. Follow error trace to see how we got here.");
                Capacity = 0;
            }
        }
        #endregion

        #region Reserving Amount
        public void SetReserveAmount(ResourceTypeValue setReserveValue)
        {
            ReserveAmount(new ResourceTypeValue
            {
                amount = -ReservedAmount + setReserveValue.amount,
                capacity = -ReservedCapacity + setReserveValue.capacity
            });
        }

        public void ReserveAmount(ResourceTypeValue reserveValue)
        {
            ReservedCapacity += reserveValue.capacity;
            ReservedAmount += reserveValue.amount;

            OnReservedUpdated();
        }
        #endregion

        #region Releasing Amount
        public void ReleaseAmount (ResourceTypeValue reserveValue)
        {
            ReservedCapacity -= reserveValue.capacity;
            ReservedAmount -= reserveValue.amount;

            OnReservedUpdated();
        }

        private void OnReservedUpdated()
        {
            if (ReservedAmount < 0)
            {
                //logger.LogError($"[FactionResourceHandler - Faction ID: {factionID} - Resource Type: {Type.Key}] Property 'ReservedAmount' has been updated to a negative value. This is not allowed. Follow error trace to see how we got here.");
                ReservedAmount = 0;
            }
            if (ReservedCapacity < 0)
            {
                //logger.LogError($"[FactionResourceHandler - Faction ID: {factionID} - Resource Type: {Type.Key}] Property 'ReservedCapacity' has been updated to a negative value. This is not allowed. Follow error trace to see how we got here.");
                ReservedCapacity = 0;
            }
        }
        #endregion
    }
}

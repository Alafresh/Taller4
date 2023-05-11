using System.Linq;
using System;

using UnityEngine;

using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using RTSEngine.Save.Game.Entities;

namespace RTSEngine.Save.Game.Service
{
    [Serializable]
    public struct FactionResourceSaveData
    {
        public ResourceCountSaveData[] elements;
    }

    [Serializable]
    public struct ResourceCountSaveData
    {
        public string resourceKey;

        public int amount;
        public int reservedAmount;

        public int capacity;
        public int reservedCapacity;
    }

    [Serializable]
    public class ResourceManagerSaveData : PreRunGameServiceSaveDataBase<IResourceManager> 
    {
        [SerializeField]
        private FactionResourceSaveData[] factionSlotResources;
        [SerializeField]
        private ResourceSaveData[] resources;

        public ResourceManagerSaveData(IResourceManager component)
        {
            factionSlotResources = component.FactionResources
                .Keys
                .Select(factionID => new FactionResourceSaveData
                {
                    elements = component.FactionResources[factionID].ResourceHandlers.Values
                        .Select(handler => new ResourceCountSaveData
                        {
                            resourceKey = handler.Type.Key,

                            amount = handler.Amount,
                            reservedAmount = handler.ReservedAmount,

                            capacity = handler.Capacity,
                            reservedCapacity = handler.ReservedCapacity
                        }).ToArray()
                })
                .ToArray();

            resources = component.AllResources
                // Do not include resource buildings since they will be saved in the faction slot save data
                .Where(resource => resource.IsResourceOnly())
                .Select(resource => new ResourceSaveData(resource)).ToArray();
        }

        public override void OnPreEntitySpawnLoad(IGameManager gameMgr)
        {
            for (int factionID = 0; factionID < factionSlotResources.Length; factionID++)
            {
                foreach (var element in factionSlotResources[factionID].elements)
                {
                    component.TryGetResourceTypeWithKey(element.resourceKey, out ResourceTypeInfo nextResourceType);

                    component.SetResource(
                        factionID,
                        new ResourceInput
                        {
                            type = nextResourceType,
                            value = new ResourceTypeValue
                            {
                                amount = element.amount,
                                capacity = element.capacity
                            },
                            nonConsumable = false
                        });

                    component.SetReserveResources(
                        new ResourceInput
                        {
                            type = nextResourceType,
                            value = new ResourceTypeValue
                            {
                                amount = element.reservedAmount,
                                capacity = element.reservedCapacity
                            },
                            nonConsumable = false
                        },
                        factionID);
                }
            }
        }

        public override void OnEntitySpawnLoad(IGameManager gameMgr)
        {
            foreach (var resourceData in resources)
                resourceData.Spawn(gameMgr);
        }

        public override void OnPostEntitySpawnLoad(IGameManager gameMgr)
        {
            foreach (var resourceData in resources)
                resourceData.LoadComponents(gameMgr);
        }
    }
}

using System.Linq;
using System;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.UnitExtension;
using RTSEngine.ResourceExtension;
using UnityEngine;

namespace RTSEngine.Save.Game.Entities
{
    [Serializable]
    public struct CarriableUnitSaveData
    {
        public bool enabled;
        public int targetCarrierEntitKey;
        public int slotID;
    }

    [Serializable]
    public struct CollectedResourceSaveData
    {
        public string resourcekey;
        public int amount;
    }

    [Serializable]
    public class UnitSaveData : FactionEntitySaveData
    {
        [SerializeField]
        private InitUnitParametersInput initParamsInput;
        public override int Key => initParamsInput.key;

        [SerializeField]
        private CarriableUnitSaveData carriableUnit;
        [SerializeField]
        private CollectedResourceSaveData[] collectedResources;

        public UnitSaveData(IUnit unit)
            : base(unit)
        {
            initParamsInput = new InitUnitParametersInput
            {
                enforceKey = true,
                key = unit.Key,

                factionID = unit.FactionID,
                free = unit.IsFree,

                setInitialHealth = true,
                initialHealth = unit.Health.CurrHealth,

                giveInitResources = false,

                rallypointEntityKey = unit.SpawnRallypoint.IsValid() ? unit.SpawnRallypoint.Entity.Key : InputManager.INVALID_ENTITY_KEY,

                creatorEntityKey = unit.CreatorEntityComponent.IsValid() ? unit.CreatorEntityComponent.Entity.GetKey() : InputManager.INVALID_ENTITY_KEY,
                creatorEntityComponentCode = unit.CreatorEntityComponent?.Code,

                useGotoPosition = false,
                gotoPosition = unit.transform.position,

                playerCommand = false
            };

            if (unit.CarriableUnit.IsValid() && unit.CarriableUnit.CurrCarrier.IsValid())
            {
                carriableUnit = new CarriableUnitSaveData
                {
                    enabled = true,
                    targetCarrierEntitKey = unit.CarriableUnit.CurrCarrier.Entity.GetKey(),
                    slotID = unit.CarriableUnit.CurrSlotID
                };
            }

            if (unit.DropOffSource.IsValid())
            {
                collectedResources = unit.DropOffSource.CollectedResources
                   .Select(element => new CollectedResourceSaveData
                   {
                       resourcekey = element.Key.Key,
                       amount = element.Value
                   })
                   .ToArray();
            }

        }

        public override void Spawn(IGameManager gameMgr)
        {
            var unitMgr = gameMgr.GetService<IUnitManager>();
            var inputMgr = gameMgr.GetService<IInputManager>();

            inputMgr.TryGetEntityPrefabWithCode(Code, out IEntity entity);
            IUnit createdUnit = unitMgr.CreateUnitLocal(
                    entity as IUnit,
                    Position,
                    Rotation,
                    initParamsInput.ToParams(inputMgr)
                );

            createdUnit.transform.localScale = LocalScale;

            OnSpawned(createdUnit);
        }

        private IUnit unit;
        public override void OnSpawned(IEntity entity)
        {
            base.OnSpawned(entity);
            this.unit = entity as IUnit;
        }

        public override void LoadComponents(IGameManager gameMgr)
        {
            base.LoadComponents(gameMgr);

            var inputMgr = gameMgr.GetService<IInputManager>();
            var resourceMgr = gameMgr.GetService<IResourceManager>();

            if (carriableUnit.enabled && unit.CarriableUnit.IsValid())
            {
                inputMgr.TryGetEntityInstanceWithKey(carriableUnit.targetCarrierEntitKey, out IEntity carrierEntity);
                unit.CarriableUnit.SetTarget((carrierEntity as IFactionEntity).UnitCarrier,
                    new AddableUnitData
                    {
                        ignoreMvt = true,
                        forceSlot = true,
                        forcedSlotID = carriableUnit.slotID,
                        playerCommand = false,
                    });
            }

            if (unit.DropOffSource.IsValid())
            {
                foreach (var data in collectedResources)
                {
                    resourceMgr.TryGetResourceTypeWithKey(data.resourcekey, out ResourceTypeInfo resourceType);
                    unit.DropOffSource.UpdateCollectedResources(resourceType, data.amount);
                }
            }
        }

    }
}

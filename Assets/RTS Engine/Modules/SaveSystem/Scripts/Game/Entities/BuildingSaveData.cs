using System;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.BuildingExtension;

namespace RTSEngine.Save.Game.Entities
{
    [Serializable]
    public class BuildingSaveData : FactionEntitySaveData
    {
        [SerializeField]
        private InitBuildingParametersInput initParamsInput;
        public override int Key => initParamsInput.key;

        public BuildingSaveData(IBuilding building)
            : base(building)
        {
            initParamsInput = new InitBuildingParametersInput
            {
                enforceKey = true,
                key = building.Key,

                factionID = building.FactionID,
                free = building.IsFree,

                setInitialHealth = true,
                initialHealth = building.Health.CurrHealth,

                giveInitResources = false,

                buildingCenterKey = building.CurrentCenter.IsValid() ? building.CurrentCenter.Building.Key : InputManager.INVALID_ENTITY_KEY,

                isBuilt = building.IsBuilt,

                playerCommand = false
            };
        }

        public override void Spawn(IGameManager gameMgr)
        {
            var buildingMgr = gameMgr.GetService<IBuildingManager>();
            var inputMgr = gameMgr.GetService<IInputManager>();

            inputMgr.TryGetEntityPrefabWithCode(Code, out IEntity entity);
            IBuilding createdBuilding = buildingMgr.CreatePlacedBuildingLocal(
                entity as IBuilding,
                Position,
                Rotation,
                initParamsInput.ToParams(inputMgr)
                );

            createdBuilding.transform.localScale = LocalScale;

            OnSpawned(createdBuilding);
        }
    }
}

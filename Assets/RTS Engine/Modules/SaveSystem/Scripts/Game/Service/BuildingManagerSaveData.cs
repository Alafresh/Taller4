using System.Linq;
using System;

using UnityEngine;
using RTSEngine.Game;
using RTSEngine.BuildingExtension;
using RTSEngine.Save.Game.Entities;

namespace RTSEngine.Save.Game.Service
{
    [Serializable]
    public class BuildingManagerSaveData : PreRunGameServiceSaveDataBase<IBuildingManager> 
    {
        [SerializeField]
        private BuildingSaveData[] freeBuildings;

        public BuildingManagerSaveData(IBuildingManager component)
        {
            freeBuildings = component.FreeBuildings
                .Select(freeBuilding => new BuildingSaveData(freeBuilding))
                .ToArray();
        }

        public override void OnEntitySpawnLoad(IGameManager gameMgr)
        {
            foreach (var buildingData in freeBuildings)
                buildingData.Spawn(gameMgr);
        }

        public override void OnPostEntitySpawnLoad(IGameManager gameMgr)
        {
            foreach (var buildingData in freeBuildings)
                buildingData.LoadComponents(gameMgr);
        }
    }
}

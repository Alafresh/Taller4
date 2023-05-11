using System.Linq;
using System;

using UnityEngine;
using RTSEngine.Game;
using RTSEngine.UnitExtension;
using RTSEngine.Save.Game.Entities;

namespace RTSEngine.Save.Game.Service
{
    [Serializable]
    public class UnitManagerSaveData : PreRunGameServiceSaveDataBase<IUnitManager> 
    {
        [SerializeField]
        private UnitSaveData[] freeUnits;

        public UnitManagerSaveData(IUnitManager component)
        {
            freeUnits = component.FreeUnits
                .Select(freeUnit => new UnitSaveData(freeUnit))
                .ToArray();
        }

        public override void OnEntitySpawnLoad(IGameManager gameMgr)
        {
            foreach (var unitData in freeUnits)
                unitData.Spawn(gameMgr);
        }

        public override void OnPostEntitySpawnLoad(IGameManager gameMgr)
        {
            foreach (var unitData in freeUnits)
                unitData.LoadComponents(gameMgr);
        }
    }
}

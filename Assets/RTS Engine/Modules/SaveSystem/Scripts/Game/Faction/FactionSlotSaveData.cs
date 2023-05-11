using System.Collections.Generic;
using System.Linq;
using System;

using UnityEngine;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Save.Game.Entities;

namespace RTSEngine.Save.Game.Faction
{
    [Serializable]
    public struct FactionSlotDataSaveData
    {
        public FactionSlotRole role;

        public string name;
        public Color color;
        public string typeKey;

        public string npcTypeKey;

        public bool isLocalPlayer;
    }

    [Serializable]
    public class FactionSlotSaveData
    {
        [SerializeField]
        private FactionSlotState state;
        public FactionSlotState State => state;
        [SerializeField]
        private int index;
        public int ID => index;
        [SerializeField]
        private FactionSlotDataSaveData data;
        public FactionSlotDataSaveData Data => data;
        [SerializeField]
        private UnitSaveData[] units;
        [SerializeField]
        private BuildingSaveData[] buildings;

        public FactionSlotSaveData(FactionSlotState state, int ID, FactionSlotData data, IEnumerable<UnitSaveData> units, IEnumerable<BuildingSaveData> buildings)
        {
            this.state = state;
            this.index = ID;

            this.data = new FactionSlotDataSaveData
            {
                role = data.role,

                name = data.name,
                color = data.color,

                typeKey = data.type.IsValid() ? data.type.Key : "",
                npcTypeKey = data.npcType.IsValid() ? data.npcType.Key : "",

                isLocalPlayer = data.isLocalPlayer,
            };

            this.units = units.ToArray();
            this.buildings = buildings.ToArray();
        }

        public void OnPreEntitySpawnLoad(IGameManager gameMgr)
        {
            gameMgr.GetFactionSlot(index).UpdateState(state);
        }

        public void OnEntitySpawnLoad(IGameManager gameMgr)
        {
            foreach (var building in buildings)
                building.Spawn(gameMgr);
            foreach (var unit in units)
                unit.Spawn(gameMgr);
        }

        public void OnPostEntitySpawnLoad(IGameManager gameMgr)
        {
            foreach (var unit in units)
                unit.LoadComponents(gameMgr);
            foreach (var building in buildings)
                building.LoadComponents(gameMgr);
        }
    }
}

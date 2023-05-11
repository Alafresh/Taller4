using System.Collections.Generic;
using System.Linq;
using System;
using RTSEngine.Game;
using RTSEngine.Save.Game.Service;
using UnityEngine;
using RTSEngine.Save.Game.Faction;

namespace RTSEngine.Save.Game
{
    [Serializable]
    public class GameSaveData
    {
        [SerializeField]
        private string name;
        public string Name => name;
        [SerializeField]
        private string date;
        public string Date => date;

        [SerializeField]
        private string sceneName;
        public string SceneName => sceneName;
        [SerializeField]
        private string gameCode;
        public string GameCode => gameCode;

        [SerializeField]
        private float peaceTime;
        [SerializeField]
        private DefeatConditionType defeatCondition;
        public DefeatConditionType DefeatCondition => defeatCondition;

        [SerializeField]
        private FactionSlotSaveData[] factionSlots;
        public IEnumerable<FactionSlotSaveData> FactionSlots => factionSlots;

        [SerializeField]
        private GameServicesSaveData essentialGameServices;
        public GameServicesSaveData EssentialGameServices => essentialGameServices;

        public GameSaveData(string name,
                            string date,

                            string sceneName,
                            string gameCode,

                            float peaceTime,
                            DefeatConditionType defeatCondition,

                            IEnumerable<FactionSlotSaveData> factionSlots,

                            GameServicesSaveData essentialGameServices)
        {
            this.name = name;
            this.date = date;

            this.sceneName = sceneName;
            this.gameCode = gameCode;

            this.peaceTime = peaceTime;
            this.defeatCondition = defeatCondition;

            this.factionSlots = factionSlots.ToArray();

            this.essentialGameServices = essentialGameServices;
        }

        public void Load(IGameManager gameMgr)
        {
            essentialGameServices.OnPreLoad(gameMgr);

            gameMgr.SetPeaceTime(peaceTime);

            essentialGameServices.OnPreEntitySpawnLoad(gameMgr);
            foreach (FactionSlotSaveData nextSavedSlot in factionSlots)
                nextSavedSlot.OnPreEntitySpawnLoad(gameMgr);

            essentialGameServices.OnEntitySpawnLoad(gameMgr);
            foreach (FactionSlotSaveData nextSavedSlot in factionSlots)
                nextSavedSlot.OnEntitySpawnLoad(gameMgr);

            essentialGameServices.OnPostEntitySpawnLoad(gameMgr);
            foreach (FactionSlotSaveData nextSavedSlot in factionSlots)
                nextSavedSlot.OnPostEntitySpawnLoad(gameMgr);
        }
    }
}

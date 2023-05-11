using System.Collections.Generic;
using System.Linq;
using System;
using RTSEngine.Game;
using UnityEngine;

namespace RTSEngine.Save.Game.Service
{
    [Serializable]
    public struct CustomServiceSaveData
    {
        public string saveCode;
        public string value;
    }

    [Serializable]
    public class GameServicesSaveData
    {
        public GameServicesSaveData(IGameManager gameMgr,
                                            TimeModifierSaveData timeModifier,
                                             SelectionManagerSaveData selectionMgr,
                                             EntityUpgradeManagerSaveData entityUpgradeMgr,
                                             EntityComponentUpgradeManagerSaveData entityCompUpgradeMgr,
                                             MainCameraControllerSaveData mainCameraCtlr,
                                             UnitManagerSaveData unitMgr,
                                             BuildingManagerSaveData buildingMgr,
                                             ResourceManagerSaveData resourceMgr,
                                             AttackManagerSaveData attackMgr,
                                             TaskManagerSaveData taskMgr)
        {
            this.timeModifier = timeModifier;
            this.selectionMgr = selectionMgr;
            this.entityUpgradeMgr = entityUpgradeMgr;
            this.entityCompUpgradeMgr = entityCompUpgradeMgr;
            this.mainCameraCtlr = mainCameraCtlr;
            this.unitMgr = unitMgr;
            this.buildingMgr = buildingMgr;
            this.resourceMgr = resourceMgr;
            this.attackMgr = attackMgr;
            this.taskMgr = taskMgr;

            customServicesSaveData = gameMgr.gameObject.GetComponentsInChildren<ISavableGameService>()
                .Select(comp => new CustomServiceSaveData
                {
                    saveCode = comp.SaveCode,
                    value = comp.Save()
                })
                .ToArray();

            if (customServicesSaveData.Select(data => data.saveCode).Distinct().Count() != customServicesSaveData.Length)
            {
                RTSHelper.LoggingService.LogError($"[SaveManager] Some game services that implement the '{typeof(ISavableGameService).Name}' interface are using the same SaveCode property. Please make sure that each game service utilizes a unique string value for the SaveCode property. The state of the custom game services will not be saved!");
                customServicesSaveData = new CustomServiceSaveData[0];
            }
        }

        [SerializeField]
        private TimeModifierSaveData timeModifier;
        public IEnumerable<TimeModifierOptionSaveData> TimeModifierOptions => timeModifier.Options;
        [SerializeField]
        private SelectionManagerSaveData selectionMgr;
        [SerializeField]
        private EntityUpgradeManagerSaveData entityUpgradeMgr;
        [SerializeField]
        private EntityComponentUpgradeManagerSaveData entityCompUpgradeMgr;
        [SerializeField]
        private MainCameraControllerSaveData mainCameraCtlr;
        [SerializeField]
        private UnitManagerSaveData unitMgr;
        [SerializeField]
        private BuildingManagerSaveData buildingMgr;
        [SerializeField]
        private ResourceManagerSaveData resourceMgr;
        [SerializeField]
        private AttackManagerSaveData attackMgr;
        [SerializeField]
        private TaskManagerSaveData taskMgr;

        [SerializeField]
        private CustomServiceSaveData[] customServicesSaveData;

        private List<IGameServiceSaveData> loadedServicesSaveData;

        public void OnPreLoad(IGameManager gameMgr)
        {
            loadedServicesSaveData = new List<IGameServiceSaveData>();
            loadedServicesSaveData.Add(timeModifier);
            loadedServicesSaveData.Add(selectionMgr);
            loadedServicesSaveData.Add(entityUpgradeMgr);
            loadedServicesSaveData.Add(entityCompUpgradeMgr);
            loadedServicesSaveData.Add(mainCameraCtlr);
            loadedServicesSaveData.Add(unitMgr);
            loadedServicesSaveData.Add(resourceMgr);
            loadedServicesSaveData.Add(attackMgr);
            loadedServicesSaveData.Add(taskMgr);

            foreach(var serviceSD in loadedServicesSaveData)
                serviceSD.OnPreLoad(gameMgr);
        }

        public void OnPreEntitySpawnLoad(IGameManager gameMgr)
        {
            foreach(var serviceSD in loadedServicesSaveData)
                serviceSD.OnPreEntitySpawnLoad(gameMgr);
        }

        public void OnEntitySpawnLoad(IGameManager gameMgr)
        {
            foreach(var serviceSD in loadedServicesSaveData)
                serviceSD.OnEntitySpawnLoad(gameMgr);
        }

        public void OnPostEntitySpawnLoad(IGameManager gameMgr)
        {
            foreach(var serviceSD in loadedServicesSaveData)
                serviceSD.OnPostEntitySpawnLoad(gameMgr);

            var customServices = gameMgr.gameObject.GetComponentsInChildren<ISavableGameService>()
                .ToDictionary(comp => comp.SaveCode, comp => comp);

            foreach(var data in customServicesSaveData)
                customServices[data.saveCode].Load(data.value);
        }
    }
}

using System.Linq;
using System;

using UnityEngine;
using UnityEngine.SceneManagement;

using RTSEngine.Determinism;
using RTSEngine.Game;
using RTSEngine.Selection;
using RTSEngine.Upgrades;
using RTSEngine.Cameras;
using RTSEngine.UnitExtension;
using RTSEngine.BuildingExtension;
using RTSEngine.ResourceExtension;
using RTSEngine.Save.IO;
using RTSEngine.Task;
using RTSEngine.Attack;
using RTSEngine.Save.Game.Service;
using RTSEngine.Save.Game.Entities;
using RTSEngine.Save.Game.Faction;
using System.Globalization;

namespace RTSEngine.Save.Game
{

    public class SaveManager : MonoBehaviour, ISaveManager 
    {
        [SerializeField, Tooltip("What to allow or not allow when saving games?")]
        private IOSaveOptions saveOptions = new IOSaveOptions
        {
            replaceDuplicate = true,
        };

        protected IGameManager gameMgr { private set; get; }
        protected ISaveIOHandler io { private set; get; }

        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;
            io = gameMgr.GetService<ISaveIOHandler>();
        }

        public IOSaveErrorMessage OnSave(string saveName)
        {
            var gameSaveData = new GameSaveData(
                    name: saveName,
                    date: DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    sceneName: SceneManager.GetActiveScene().name,
                    gameCode: gameMgr.GameCode,
                    defeatCondition: gameMgr.DefeatCondition,
                    factionSlots: gameMgr.FactionSlots.Select(slot => new FactionSlotSaveData(
                        state: slot.State,
                        ID: slot.ID,
                        data: slot.Data,
                        units: slot.FactionMgr.Units.Select(unit => new UnitSaveData(unit)),
                        buildings: slot.FactionMgr.Buildings.Select(building => new BuildingSaveData(building)))),
                    peaceTime: gameMgr.PeaceTimer.CurrValue,
                    essentialGameServices: new GameServicesSaveData(
                        gameMgr: gameMgr,
                        timeModifier: new TimeModifierSaveData(gameMgr.GetService<ITimeModifier>()),
                        selectionMgr: new SelectionManagerSaveData(gameMgr.GetService<ISelectionManager>()),
                        entityUpgradeMgr: new EntityUpgradeManagerSaveData(gameMgr.GetService<IEntityUpgradeManager>()),
                        entityCompUpgradeMgr: new EntityComponentUpgradeManagerSaveData(gameMgr.GetService<IEntityComponentUpgradeManager>()),
                        mainCameraCtlr: new MainCameraControllerSaveData(gameMgr.GetService<IMainCameraController>()),
                        unitMgr: new UnitManagerSaveData(gameMgr.GetService<IUnitManager>()),
                        buildingMgr: new BuildingManagerSaveData(gameMgr.GetService<IBuildingManager>()),
                        resourceMgr: new ResourceManagerSaveData(gameMgr.GetService<IResourceManager>()),
                        attackMgr: new AttackManagerSaveData(gameMgr.GetService<IAttackManager>()),
                        taskMgr: new TaskManagerSaveData(gameMgr.GetService<ITaskManager>())
                    )
                );

            return io.Save(gameSaveData, saveOptions);
        }
    }
}

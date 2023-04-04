using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Utilities;
using RTSEngine.Logging;

namespace RTSEngine.BuildingExtension
{
    /// <summary>
    /// health: Buildings are constructed and repaired by builders adding health points to it until it reaches maximum health
    /// time: Buildings are constructed and repaired by builders actively constructing them for a certain amount of time until it reaches maximum health.
    /// </summary>
    public enum ConstructionType
    {
        health,
        time
    }

    public class BuildingManager : ObjectPool<IBorderObject, BorderObjectSpawnInput>, IBuildingManager
    {
        #region Attributes
        [SerializeField, EnforceType(typeof(IBuilding), sameScene: true), Tooltip("Prespawned free buildings in the current map scene.")]
        private GameObject[] preSpawnedFreeBuildings = new GameObject[0];

        private List<IBuilding> freeBuildings = new List<IBuilding>();
        public IEnumerable<IBuilding> FreeBuildings => freeBuildings;

        [SerializeField, Tooltip("Selection and minimap color that all free buildings use.")]
        private Color freeBuildingColor = Color.black; 
        public Color FreeBuildingColor => freeBuildingColor;

        // Borders
        // In order to draw borders and show which order has been set before the other, their objects have different sorting orders.
        public int LastBorderSortingOrder { private set; get; }
        private List<IBorder> allBorders = new List<IBorder>();
        public IEnumerable<IBorder> AllBorders => allBorders;

        [Header("Building Construction")]
        [SerializeField, Tooltip("Pick how building construction is handled. Either by builders adding health points to the building or builders actively constructing for a certain amount of time until the building reaches maximum health.")]
        private ConstructionType constructionType = ConstructionType.health;
        public ConstructionType ConstructionType => constructionType;

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        protected IBuildingPlacement placementMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; } 
        #endregion

        #region Initializing/Terminating
        protected sealed override void OnObjectPoolInit() 
        { 
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.inputMgr = gameMgr.GetService<IInputManager>();
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>();
            this.logger = gameMgr.GetService<IGameLoggingService>();

            freeBuildings = new List<IBuilding>();

            if(gameMgr.ClearDefaultEntities)
            {
                foreach(GameObject freeBuildingObj in preSpawnedFreeBuildings)
                    UnityEngine.Object.DestroyImmediate(freeBuildingObj);

                preSpawnedFreeBuildings = new GameObject[0];
            }

            this.gameMgr.GameStartRunning += HandleGameStartRunning;

            globalEvent.BorderActivatedGlobal += HandleBorderActivatedGlobal;
            globalEvent.BorderDisabledGlobal += HandleBorderDisabledGlobal;

            globalEvent.EntityFactionUpdateStartGlobal += HandleEntityFactionUpdateStartGlobal;
        }

        private void OnDisable()
        {
            gameMgr.GameStartRunning -= HandleGameStartRunning;

            globalEvent.BorderActivatedGlobal -= HandleBorderActivatedGlobal;
            globalEvent.BorderDisabledGlobal -= HandleBorderDisabledGlobal;

            globalEvent.EntityFactionUpdateStartGlobal -= HandleEntityFactionUpdateStartGlobal;
        }

        public void HandleGameStartRunning(IGameManager source, EventArgs args)
        {
            // Activate free buildings after all faction slots are initialized.
            foreach (IBuilding building in preSpawnedFreeBuildings
                .Select(building => 
                {
                    IBuilding nextBuilding = building.IsValid() ? building.GetComponent<IBuilding>() : null;
                    if (!building.IsValid())
                        logger.LogError($"[{GetType().Name}] The 'Pre Spawned Free Buildings' list has some invalid/unassigned. elements!");

                    return nextBuilding; 
                })
                .ToList())
            {
                building.Init(
                    gameMgr,
                    new InitBuildingParameters
                    {
                        free = true,
                        factionID = -1,

                        setInitialHealth = false,

                        buildingCenter = null,
                    });
            }

            gameMgr.GameStartRunning -= HandleGameStartRunning;
        }
        #endregion

        #region Handling Events: Monitoring Borders
        private void HandleBorderActivatedGlobal(IBorder border, EventArgs e)
        {
            allBorders.Add(border);
            LastBorderSortingOrder--;
        }

        private void HandleBorderDisabledGlobal(IBorder border, EventArgs e) => allBorders.Remove(border);
        #endregion

        #region Handling Events: Monitoring Free Buildings
        private void HandleEntityFactionUpdateStartGlobal(IEntity updatedInstance, FactionUpdateArgs args)
        {
            if (updatedInstance.IsBuilding() && updatedInstance.IsFree)
                freeBuildings.Remove(updatedInstance as IBuilding);
        }
        #endregion

        #region Creating Buildings
        public ErrorMessage CreatePlacedBuilding(IBuilding buildingPrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitBuildingParameters initParams)
        {
            return inputMgr.SendInput(
                new CommandInput()
                {
                    isSourcePrefab = true,

                    sourceMode = (byte)InputMode.create,
                    targetMode = (byte)InputMode.building,

                    sourcePosition = spawnPosition,
                    opPosition = spawnRotation.eulerAngles,

                    code = JsonUtility.ToJson(initParams.ToInput()),

                    playerCommand = initParams.playerCommand
                },
                source: buildingPrefab,
                target: null);
        }

        public IBuilding CreatePlacedBuildingLocal(IBuilding buildingPrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitBuildingParameters initParams)
        {
            IBuilding newBuilding = Instantiate(buildingPrefab.gameObject, spawnPosition, spawnRotation).GetComponent<IBuilding>();

            newBuilding.gameObject.SetActive(true);
            newBuilding.Init(gameMgr, initParams);

            return newBuilding;
        }

        public IBuilding CreatePlacementBuilding(IBuilding building, Quaternion spawnRotation, InitBuildingParameters initParams)
        {
            IEntity prefab = null;
            if (!building.IsValid() 
                || !inputMgr.TryGetEntityPrefabWithCode(building.Code, out prefab))
                logger.LogError($"[{GetType().Name}] Unable to create placement instance for building with code '{building?.Code}'!");

            IBuilding placementInstance = Instantiate(
                prefab.gameObject,
                new Vector3(0.0f, placementMgr.BuildingPositionYOffset, 0.0f),
                spawnRotation).GetComponent<IBuilding>();
            placementInstance.gameObject.SetActive(true);

            placementInstance.InitPlacementInstance(
                gameMgr,
                initParams);

            return placementInstance;
        }
        #endregion

        #region IBorderObject Pooling
        public IBorderObject SpawnBorderObject(IBorderObject prefab, BorderObjectSpawnInput input)
        {
            IBorderObject nextBorderObj = base.Spawn(prefab);
            if (!nextBorderObj.IsValid())
                return null;

            nextBorderObj.OnSpawn(input);

            return nextBorderObj;
        }
        #endregion
    }
}

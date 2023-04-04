using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Serialization;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Terrain;
using RTSEngine.BuildingExtension;
using RTSEngine.Event;
using RTSEngine.NPC.ResourceExtension;
using RTSEngine.ResourceExtension;

namespace RTSEngine.NPC.BuildingExtension
{
    public class NPCBuildingPlacer : NPCComponentBase, INPCBuildingPlacer
    {
        #region Attributes
        [SerializeField, FormerlySerializedAs("placementHandler")]
        private NPCFactionPlacementHandler defaultPlacementHandler = new NPCFactionPlacementHandler();
        private IBuildingPlacementHandler currPlacementHandler = null;

        // NPC components
        protected INPCResourceManager npcResourceMgr { private set; get; }

        // Other components
        protected ITerrainManager terrainMgr { private set; get; }
        protected IBuildingPlacement placementMgr { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; }
        protected IResourceManager resourceMgr { private set; get; } 
        protected IBuildingPlacement placerMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnPreInit()
        {
            defaultPlacementHandler.EventLogger = LogEvent;

            npcResourceMgr = npcMgr.GetNPCComponent<INPCResourceManager>();

            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>();
            this.resourceMgr = gameMgr.GetService<IResourceManager>();
            this.placerMgr = gameMgr.GetService<IBuildingPlacement>(); 
        }

        protected override void OnPostInit()
        {
            globalEvent.BuildingUpgradedGlobal += HandleBuildingUpgradedGlobal;

            globalEvent.BuildingDeadGlobal += HandleBuildingDeadGlobal;

            // In case a component that implements the required interface is attached to the same game object
            // Use that component instead of the defaultPlacementHandler field.
            currPlacementHandler = GetComponent<IBuildingPlacementHandler>();
            if (!currPlacementHandler.IsValid())
            {
                currPlacementHandler = defaultPlacementHandler;
                defaultPlacementHandler.EventLogger = LogEvent;
            }
            placerMgr.RegisterFactionPlacementHandler(factionSlot.ID, currPlacementHandler);

            currPlacementHandler.PlacementAdded += HandlePlacementAdded;
            currPlacementHandler.PlacementStopped += HandlePlacementStopped;
        }

        protected override void OnDestroyed()
        {
            globalEvent.BuildingUpgradedGlobal -= HandleBuildingUpgradedGlobal;

            globalEvent.BuildingDeadGlobal -= HandleBuildingDeadGlobal;

            currPlacementHandler.PlacementAdded -= HandlePlacementAdded;
            currPlacementHandler.PlacementStopped -= HandlePlacementStopped;
        }
        #endregion

        #region Handling Events: Placement Added/Stopped
        private void HandlePlacementStopped(IBuildingPlacementHandler sender, IPendingPlacementData args)
        {
            if (currPlacementHandler.Count == 0)
                IsActive = false;
        }

        private void HandlePlacementAdded(IBuildingPlacementHandler sender, IPendingPlacementData args)
        {
            IsActive = true;
        }
        #endregion

        #region Handling Event: Building Dead
        private void HandleBuildingDeadGlobal(IBuilding building, DeadEventArgs args)
        {
            if (!factionMgr.IsSameFaction(building)
                || !building.BorderComponent.IsValid())
                return;

            // Consider building centers that are destroyed and remove all pending buildings that are supposed to be placed around them
            defaultPlacementHandler.StopOnCondition(placementData =>
                placementData.options.initialCenter.IsValid() && placementData.options.initialCenter == building);
        }
        #endregion

        #region Handling Event: Building Upgrades
        private void HandleBuildingUpgradedGlobal(IBuilding building, UpgradeEventArgs<IEntity> args)
        {
            if (!factionMgr.IsSameFaction(args.FactionID))
                return;

            // In case a building type that is scheduled to be placed has been upraded then destroy it and allow the NPC faction to place the upgraded instance
            defaultPlacementHandler.StopOnCondition(placementData =>
                placementData.options.initialCenter.IsValid() && placementData.instance.Code == building.Code);
        }
        #endregion

        #region Requesting Building Placement
        public bool OnBuildingPlacementRequest(BuildingCreationTask task, IBuilding buildingCenter, IReadOnlyList<BuildingPlaceAroundData> placeAroundDataSet, bool canRotate)
        {
            ErrorMessage errorMsg = defaultPlacementHandler.Add(task,
                new BuildingPlacementOptions
                {
                    initialCenter = buildingCenter?.BorderComponent,
                    placeAroundData = placeAroundDataSet,
                    disableRotation = !canRotate
                });

            if (errorMsg != ErrorMessage.none)
            {
                switch (errorMsg)
                {
                    case ErrorMessage.placementBuildingCenterMissing:
                        LogEvent($"{task.Prefab.Code}: Building Center for building prefab hasn't been specified in the Building Placement Request!");
                        break;

                    case ErrorMessage.taskMissingResourceRequirements:
                        LogEvent($"{task.Prefab.Code}: Building creation task resource requirements missing!");
                        npcResourceMgr.OnIncreaseMissingResourceRequest(task.RequiredResources);
                        break;
                    default:
                        LogEvent($"'{task.Prefab.Code}': Placement Request Failure - Creation Tasks Requirements Not Met - Erorr: {errorMsg}");
                        break;
                }

                return false;
            }

            LogEvent($"'{task.Prefab.Code}': Placement Request Success");

            return true;
        }
        #endregion

        #region Logging
        [SerializeField, ReadOnly]
        private GameObject[] placementQueue = new GameObject[0];

        protected override void UpdateActiveLogs()
        {
            if (!defaultPlacementHandler.IsValid())
            {
                placementQueue = new GameObject[0];
                return;
            }

            placementQueue = defaultPlacementHandler
                .Queue
                .Select(elem => elem.instance.gameObject)
                .ToArray();
        }
        #endregion
    }
}

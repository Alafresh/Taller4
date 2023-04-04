using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.BuildingExtension;
using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.NPC.ResourceExtension;
using RTSEngine.NPC.UnitExtension;
using RTSEngine.NPC.Event;
using RTSEngine.Logging.NPC;

namespace RTSEngine.NPC.BuildingExtension
{
    public class NPCBuildingCreator : NPCComponentBase, INPCBuildingCreator
    {
        #region Attributes 
        [SerializeField, Tooltip("In case this component or any other NPC component attempts to create a building that does not have valid regulator data defined and this field is assigned, the regulator defined here will be used for that building.")]
        private NPCBuildingRegulatorData defaultRegulatorData = null;

        [SerializeField, EnforceType(typeof(IBuilding), prefabOnly: true), Tooltip("Buildings that this component is able to create idenpendently. If other NPC components include fields for building prefabs then those prefabs should not be included here. Make sure to not include upgrade targets of building prefabs in the same list as these will be added when the upgrade occurs.")]
        private GameObject[] independentBuildings = new GameObject[0];

        // Holds the building prefabs for which the NPC faction has active regulators
        private List<IBuilding> regulatedBuildingPrefabs;

        // Holds building centers and their corresponding active building regulators
        private Dictionary<IBuilding, NPCBuildingCenterRegulatorData> buildingCenterRegulators;

        // Has the first building center of the NPC faction (a building with an IBorder component) been initialized?
        public bool IsFirstBuildingCenterInitialized { private set; get; }
        public IBuilding FirstBuildingCenter => buildingCenterRegulators.Keys.FirstOrDefault();

        // NPC Components
        protected INPCUnitCreator npcUnitCreator { private set; get; }

        protected INPCTerritoryManager npcTerritoryMgr { private set; get; }

        protected INPCBuildingConstructor npcBuildingConstructor { private set; get; }
        protected INPCBuildingPlacer npcBuildingPlacer { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<INPCBuildingCreator, EventArgs> FirstBuildingCenterInitialized;
        private void RaiseFirstBuildingCenterInitialized()
        {
            var handler = FirstBuildingCenterInitialized;
            handler?.Invoke(this, EventArgs.Empty);

            IsFirstBuildingCenterInitialized = true;
        }
        #endregion

        #region Initiliazing/Terminating
        protected override void OnPreInit()
        {
            this.npcUnitCreator = npcMgr.GetNPCComponent<INPCUnitCreator>();

            this.npcTerritoryMgr = npcMgr.GetNPCComponent<INPCTerritoryManager>();

            this.npcBuildingConstructor = npcMgr.GetNPCComponent<INPCBuildingConstructor>();
            this.npcBuildingPlacer = npcMgr.GetNPCComponent<INPCBuildingPlacer>();

            regulatedBuildingPrefabs = new List<IBuilding>();
            buildingCenterRegulators = new Dictionary<IBuilding, NPCBuildingCenterRegulatorData>();

            IsFirstBuildingCenterInitialized = false;
        }

        protected override void OnPostInit()
        {
            foreach (IBuilding buildingCenter in factionMgr.BuildingCenters)
                AddBuildingCenterRegulator(buildingCenter);

            globalEvent.BorderActivatedGlobal += HandleBorderActivatedGlobal;
            globalEvent.BorderDisabledGlobal += HandleBorderDisabledGlobal;

            globalEvent.BuildingUpgradedGlobal += HandleBuildingUpgradeGlobal;
        }

        protected override void OnDestroyed()
        {
            globalEvent.BorderActivatedGlobal -= HandleBorderActivatedGlobal;
            globalEvent.BorderDisabledGlobal -= HandleBorderDisabledGlobal;

            globalEvent.BuildingUpgradedGlobal -= HandleBuildingUpgradeGlobal;

            DestroyAllActiveRegulators();
        }
        #endregion

        #region Handling Building Center Regulators
        private void AddBuildingCenterRegulator(IBuilding buildingCenter)
        {
            NPCBuildingCenterRegulatorData newBuildingCenterRegulator = new NPCBuildingCenterRegulatorData
            {
                buildingCenter = buildingCenter,
                activeBuildingRegulators = new Dictionary<string, NPCActiveBuildingRegulatorData>()
            };

            buildingCenterRegulators.Add(buildingCenter, newBuildingCenterRegulator);

            LogEvent($"{buildingCenter.Code}: Activated building center regulator. First building center? {!IsFirstBuildingCenterInitialized}");

            // Activate the independent building regulators for this new center regulator if it's the first building center
            // Else add all of the buildings that have been used by the NPC faction from the 'regulatedBuildingPrefabs' list.
            IEnumerable<IBuilding> nextBuildingList = !IsFirstBuildingCenterInitialized
                ? independentBuildings.Select(obj => obj.IsValid() ? obj.GetComponent<IBuilding>() : null)
                : regulatedBuildingPrefabs;

            foreach (IBuilding building in nextBuildingList)
                ActivateBuildingRegulator(building, buildingCenter);

            if (!IsFirstBuildingCenterInitialized)
                RaiseFirstBuildingCenterInitialized();
        }

        private void DestroyBuildingCenterRegulator(IBuilding buildingCenter)
        {
            if (buildingCenterRegulators.TryGetValue(buildingCenter, out NPCBuildingCenterRegulatorData nextBuildingCenterRegulator))
            {
                foreach (NPCActiveBuildingRegulatorData nextBuildingRegulator in nextBuildingCenterRegulator.activeBuildingRegulators.Values)
                    nextBuildingRegulator.instance.Disable();

                buildingCenterRegulators.Remove(buildingCenter);

                LogEvent($"{buildingCenter.Code}: Destroyed building center regulator.");
            }
        }
        #endregion

        #region Handling Building Regulators
        private void ActivateBuildingRegulator(IBuilding buildingPrefab)
        {
            foreach (IBuilding nextBuildingCenter in buildingCenterRegulators.Keys)
                ActivateBuildingRegulator(buildingPrefab, nextBuildingCenter);
        }

        public NPCBuildingRegulator ActivateBuildingRegulator(IBuilding buildingPrefab, IBuilding buildingCenter)
        {
            if (!logger.RequireValid(buildingPrefab,
                $"[{GetType().Name} - {factionMgr.FactionID}] Can not activate a regulator for an invalid building prefab! Check the 'Independent Buildings' list for unassigned elements or any other building input field in other NPC components or provide a valid building if you are calling the activation method from an external component.")
                || !logger.RequireTrue(buildingCenterRegulators.ContainsKey(buildingCenter),
                $"[{GetType().Name} - {factionMgr.FactionID}] The provided building center has not been registered as a valid building center for this NPC faction!"))
                return null;

            // If no valid regulator data for the building is returned then do not continue
            NPCBuildingRegulatorData regulatorData = buildingPrefab.GetComponent<NPCBuildingRegulatorDataInput>()?.GetFiltered(factionType: factionSlot.Data.type, npcType: npcMgr.Type);
            if (!regulatorData.IsValid())
            {
                if (defaultRegulatorData.IsValid())
                    regulatorData = defaultRegulatorData;
                else
                {
                    LogEvent($"{buildingPrefab.Code}: Unable to find a valid regulator for building.");
                    return null;
                }
            }

            // If there is a regulator for the provided building type that is already active on the provided building center then return it directly.
            NPCBuildingRegulator activeInstance = GetBuildingRegulator(buildingPrefab.Code, buildingCenter);
            if (activeInstance.IsValid())
            {
                LogEvent($"{buildingPrefab.Code}: A valid active regulator already exists!");
                return activeInstance;
            }

            // At this stage, we create a new regulator for the building to be added to the provided building center.
            NPCActiveBuildingRegulatorData newBuildingRegulator = new NPCActiveBuildingRegulatorData()
            {
                instance = new NPCBuildingRegulator(regulatorData, buildingPrefab, gameMgr, npcMgr, buildingCenter),

                // Initial spawning timer: regular spawn reload + start creating after delay
                spawnTimer = new TimeModifiedTimer(regulatorData.SpawnReload + regulatorData.CreationDelayTime)
            };

            newBuildingRegulator.instance.AmountUpdated += HandleBuildingRegulatorAmountUpdated;

            buildingCenterRegulators[buildingCenter].activeBuildingRegulators.Add(buildingPrefab.Code, newBuildingRegulator);

            if (!regulatedBuildingPrefabs.Contains(buildingPrefab))
                regulatedBuildingPrefabs.Add(buildingPrefab);

            // Whenever a new regulator is added to the active regulators list, then move the building creator into the active state
            IsActive = true;

            LogEvent($"{buildingPrefab.Code}: Created and activated a regulator for building. Active Border: {buildingCenter}. Using default regulator data? {regulatorData == defaultRegulatorData}");
            return newBuildingRegulator.instance;
        }

        public NPCBuildingRegulator GetBuildingRegulator(string buildingCode, IBuilding buildingCenter)
        {
            if (buildingCenterRegulators.TryGetValue(buildingCenter, out NPCBuildingCenterRegulatorData nextBuildingCenterRegulator))
                if (nextBuildingCenterRegulator.activeBuildingRegulators.TryGetValue(buildingCode, out NPCActiveBuildingRegulatorData nextBuildingRegulator))
                    return nextBuildingRegulator.instance;

            return null;
        }

        public NPCBuildingRegulator GetCreatableBuildingRegulatorFirst(string buildingCode, bool creatableOnDemandOnly)
        {
            // The first creatable building regulator is the one that can be created on demand (if it is a requirement) and whose max amount has not been reached yet.
            // Therefore, we go through all building center regulators and get the first building regulator that satisfies the above two conditions

            foreach (NPCBuildingCenterRegulatorData nextBuildingCenterRegulator in buildingCenterRegulators.Values)
                if (nextBuildingCenterRegulator.activeBuildingRegulators.TryGetValue(buildingCode, out NPCActiveBuildingRegulatorData nextBuildingRegulator))
                    if ((!creatableOnDemandOnly || nextBuildingRegulator.instance.Data.CanCreateOnDemand) && !nextBuildingRegulator.instance.HasReachedMaxAmount)
                        return nextBuildingRegulator.instance;

            return null;
        }

        private void DestroyAllActiveRegulators()
        {
            foreach (NPCBuildingCenterRegulatorData nextBuildingCenterRegulator in buildingCenterRegulators.Values)
                foreach (NPCActiveBuildingRegulatorData nextBuildingRegulator in nextBuildingCenterRegulator.activeBuildingRegulators.Values)
                {
                    nextBuildingRegulator.instance.AmountUpdated -= HandleBuildingRegulatorAmountUpdated;

                    nextBuildingRegulator.instance.Disable();
                }

            buildingCenterRegulators.Clear();

            LogEvent($"All active building regulators destroyed!");
        }

        private void DestroyActiveRegulator(string buildingCode)
        {
            regulatedBuildingPrefabs.RemoveAll(prefab => prefab.Code == buildingCode);

            foreach (IBuilding nextBuildingCenter in buildingCenterRegulators.Keys)
            {
                NPCBuildingRegulator nextBuildingRegulator = GetBuildingRegulator(buildingCode, nextBuildingCenter);
                if (nextBuildingRegulator.IsValid())
                {
                    nextBuildingRegulator.AmountUpdated -= HandleBuildingRegulatorAmountUpdated;

                    nextBuildingRegulator.Disable();
                }

                buildingCenterRegulators[nextBuildingCenter].activeBuildingRegulators.Remove(buildingCode);

                LogEvent($"{buildingCode}: Building regulator destroyed!");
            }
        }
        #endregion

        #region Handling Events: Building Regulator
        private void HandleBuildingRegulatorAmountUpdated(NPCRegulator<IBuilding> buildingRegulator, NPCRegulatorUpdateEventArgs args)
        {
            // In case this component is inactive and one of the existing (not pending) buildings is removed then reactivate it
            if (!IsActive
                && !buildingRegulator.HasTargetCount)
                IsActive = true;
        }
        #endregion

        #region Handling Events: Border Activated/Deactivated
        private void HandleBorderActivatedGlobal(IBorder border, EventArgs args)
        {
            if (factionMgr.IsSameFaction(border.Building))
                AddBuildingCenterRegulator(border.Building);
        }

        private void HandleBorderDisabledGlobal(IBorder border, EventArgs args)
        {
            if (factionMgr.IsSameFaction(border.Building))
                DestroyBuildingCenterRegulator(border.Building);
        }
        #endregion

        #region Handling Events: Building Upgrade 
        private void HandleBuildingUpgradeGlobal(IBuilding building, UpgradeEventArgs<IEntity> args)
        {
            if (!factionMgr.IsSameFaction(args.FactionID))
                return;

            // Remove the old building regulator
            if(building.IsValid())
                DestroyActiveRegulator(building.Code);
            // And replace it with the upgraded building regulator
            ActivateBuildingRegulator(args.UpgradeElement.target as IBuilding);
        }
        #endregion

        #region Handling Building Creation
        protected override void OnActiveUpdate()
        {
            // Assume that the building creator has finished its job with the current active building regulators.
            IsActive = false;

            foreach (NPCBuildingCenterRegulatorData nextBuildingCenterRegulator in buildingCenterRegulators.Values.ToArray())
                foreach (NPCActiveBuildingRegulatorData nextBuildingRegulator in nextBuildingCenterRegulator.activeBuildingRegulators.Values.ToArray())
                {
                    // Buildings are only automatically created if they haven't reached their min amount and still haven't reached their max amount
                    if (nextBuildingRegulator.instance.Data.CanAutoCreate 
                        && !nextBuildingRegulator.instance.HasTargetCount)
                    {
                        // To keep this component monitoring the creation of the next buildings
                        IsActive = true;

                        if (nextBuildingRegulator.spawnTimer.ModifiedDecrease())
                        {
                            nextBuildingRegulator.spawnTimer.Reload(nextBuildingRegulator.instance.Data.SpawnReload);

                            OnCreateBuildingRequestInternal(nextBuildingRegulator.instance, nextBuildingCenterRegulator.buildingCenter);
                        }
                    }
                }
        }

        public bool OnCreateBuildingRequest(string buildingCode, IBuilding buildingCenter = null)
        {
            NPCBuildingRegulator nextBuildingRegulator = GetCreatableBuildingRegulatorFirst(buildingCode, creatableOnDemandOnly: true);
            if(!nextBuildingRegulator.IsValid())
            {
                LogEvent($"{buildingCode}: Unable to create instance. No valid regulator found!");
            }
            if (nextBuildingRegulator?.Data.CanCreateOnDemand == false)
            {
                LogEvent($"{buildingCode}: Unable to create instance. Creation on demand is disabled!");
                return false;
            }

            return OnCreateBuildingRequestInternal(nextBuildingRegulator, buildingCenter);
        }

        private bool OnCreateBuildingRequestInternal(NPCBuildingRegulator instance, IBuilding buildingCenter = null)
        {
            if (!instance.IsValid())
                return false;
            if (instance.HasReachedMaxAmount)
            {
                LogEvent($"{instance.Prefab.Code}: Unable to create instance. Has reached max amount? {instance.HasReachedMaxAmount}");
                return false;
            }

            if (!npcBuildingConstructor.BuilderPlacableBuildingMapper.TryGetValue(instance.Prefab.Code, out string[] builderCodes))
            {
                LogEvent($"{instance.Prefab.Code}: Can not find valid builder types for the building prefab to start placing it!");
                return false;
            }
            else if (!RTSHelper.TestFactionEntityRequirements(instance.Data.FactionEntityRequirements, factionMgr))
            {
                LogEvent($"{instance.Prefab.Code}: Unable to fullfill faction entity requirements!");
                return false;
            }

            // Try to get a valid building creation task from the builder units to create the provided building type
            BuildingCreationTask nextBuildingCreationTask = null;
            foreach (string nextBuilderCode in builderCodes)
            {
                NPCUnitRegulator nextBuilderRegulator;
                if ((nextBuilderRegulator = npcUnitCreator.GetActiveUnitRegulator(nextBuilderCode)).IsValid())
                {
                    IBuilder nextBuilder = nextBuilderRegulator.Instances.FirstOrDefault(unitInstance => unitInstance.BuilderComponent.IsValid())?.BuilderComponent;

                    if (nextBuilder.IsValid())
                        foreach (BuildingCreationTask task in nextBuilder.CreationTasks)
                            if (task.Prefab.Code == instance.Prefab.Code)
                            {
                                nextBuildingCreationTask = task;
                                break;
                            }
                }
            }

            if (!nextBuildingCreationTask.IsValid())
            {
                LogEvent($"{instance.Prefab.Code}: No valid building creation task has been found!");
                return false;
            }

            // In case the building center where the building will be placed has not been specified then attempt to pick one
            if (!buildingCenter.IsValid() 
                && !factionMgr.BuildingCenters.GetEntityFirst(out buildingCenter, building => building.BorderComponent.IsBuildingAllowedInBorder(instance.Prefab as IBuilding)))
            {
                //FUTURE FEATURE -> no building center is found -> request to place a building center.
                LogEvent($"{instance.Prefab.Code}: Attempting to start placement but no valid building center is found!");
                return false;
            }

            if (instance.Data.ForcePlaceAround && instance.Data.AllPlaceAroundData.Count == 0)
            {
                LogEvent($"{instance.Prefab.Code}: Attempting to start placement with place around parameters forced but no place around data has been defined in the regulator!");
                return false;
            }

            // Either take the pre defined place around data or if none exists (and we have made sure that force place around is not set), we define the place around data around the building center
            IReadOnlyList<BuildingPlaceAroundData> nextPlaceAroundDataSet = instance.Data.AllPlaceAroundData
                .Concat(instance.Data.ForcePlaceAround
                    ? Enumerable.Empty<BuildingPlaceAroundData>()
                    : Enumerable.Repeat(new BuildingPlaceAroundData
                    {
                        entityType = new CodeCategoryField { codes = new string[] { buildingCenter.Code } },
                        range = new FloatRange(0.0f, buildingCenter.BorderComponent.Size)
                    }, 1))
                .ToArray();

            LogEvent($"{instance.Prefab.Code}: Sending building placement request");
            npcBuildingPlacer.OnBuildingPlacementRequest(
                nextBuildingCreationTask,
                buildingCenter,
                nextPlaceAroundDataSet,
                instance.Data.CanRotate);

            return true;
        }
        #endregion

        #region Logging
        [System.Serializable]
        private struct NPCBuildingCenterActiveBuildingsLogData
        {
            public GameObject center;
            public NPCActiveFactionEntityRegulatorLogData[] buildings;
        }

        [SerializeField, ReadOnly]
        private List<NPCBuildingCenterActiveBuildingsLogData> activeBuildingRegulatorLogs = new List<NPCBuildingCenterActiveBuildingsLogData>();

        protected override void UpdateActiveLogs()
        {
            activeBuildingRegulatorLogs = buildingCenterRegulators.Values
                .Select(centerRegulator => new NPCBuildingCenterActiveBuildingsLogData 
                { 
                    center = centerRegulator.buildingCenter.gameObject,

                    buildings = centerRegulator.activeBuildingRegulators.Values
                    .Select(regulator => new NPCActiveFactionEntityRegulatorLogData(
                        regulator.instance,
                        spawnTimer: regulator.spawnTimer.CurrValue,
                        creators: npcBuildingConstructor.BuilderPlacableBuildingMapper.ContainsKey(regulator.instance.Prefab.Code) ? npcBuildingConstructor.BuilderPlacableBuildingMapper[regulator.instance.Prefab.Code].ToArray() : new string[0]))
                    .ToArray()
                })
                .ToList();
        }
        #endregion
    }
}

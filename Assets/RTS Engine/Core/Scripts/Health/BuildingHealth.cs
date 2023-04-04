using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using System;
using RTSEngine.Game;
using RTSEngine.Determinism;
using System.Linq;
using RTSEngine.BuildingExtension;
using RTSEngine.EntityComponent;
using RTSEngine.ResourceExtension;
using RTSEngine.Logging;

namespace RTSEngine.Health
{
    public class BuildingHealth : FactionEntityHealth, IBuildingHealth, IEntityPostInitializable
    {
        #region Attributes
        public IBuilding Building { private set; get; }
        public override EntityType EntityType => EntityType.building;

        public override float DestroyObjectDelay => Building.IsPlacementInstance ? 0.0f : base.DestroyObjectDelay;

        [SerializeField, Tooltip("Possible health states that the building can have while it is being constructed. Make sure that the states are defined in an ascending health range.")]
        private List<EntityHealthState> constructionStates = new List<EntityHealthState>();  

        [SerializeField, Tooltip("State to activate when the building completes construction, a transition state from construction states to regular building states.")]
        private EntityHealthState constructionCompleteState = new EntityHealthState();

        [SerializeField, Tooltip("Building repair resources costs that apply when the building is repaired from 1 health points to maximum health points after the initial complete construction post placement. Leave array empty for no repair costs.")]
        private ResourceInput[] repairCosts = new ResourceInput[0];
        private ResourceInput[] adjustedCosts;

        // TIME CONSTRUCTION FIELDS
        [SerializeField, Tooltip("In case the construction type is set to 'Time' in the building manager, this field represents the amount of time needed to construct this building from 1 health points to maximum health points when only one builder is actively constructing it with a time multiplier of 0.")]
        private float buildTime = 50.0f;
        public float BuildTime => buildTime;
        // Whenever a new worker starts or stops actively constructing the building, the time required to construct the building to its full health is updated based on the amount of workers and their accumulative mutlipliers
        private float currBuildTime;
        // The initial health that the building starts with when the currBuildTime is updated
        private int currBuildTimeStartingHealth;
        private TimeModifiedTimer buildTimer;
        private bool hasBuildersInProgress = false;

        protected IBuildingManager buildingMgr { private set; get; } 
        #endregion

        #region Initializing/Terminating
        protected override void OnFactionEntityHealthInit()
        {
            Building = Entity as IBuilding;

            this.buildingMgr = gameMgr.GetService<IBuildingManager>();

            adjustedCosts = new ResourceInput[repairCosts.Length];
            Array.Copy(repairCosts, adjustedCosts, repairCosts.Length);

            hasBuildersInProgress = false;
            Building.WorkerMgr.WorkerAdded += HandleWorkerAdded;
            Building.WorkerMgr.WorkerRemoved += HandleWorkerRemoved;
        }

        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            // Show the construction state only if this is not the placement instance
            // We also check for whether the building has been built or not because in case of a faction conversion, components are re-initiated and this would cause the construction states to appear.
            if(!Building.IsPlacementInstance && !Building.IsBuilt) 
                stateHandler.Reset(constructionStates, CurrHealth);
        }

        protected override void OnDisabled()
        {
            Building.WorkerMgr.WorkerAdded -= HandleWorkerAdded;
            Building.WorkerMgr.WorkerRemoved -= HandleWorkerRemoved;
        }

        #endregion

        #region Time Based Construction Handling
        private void HandleWorkerAdded(IEntity sender, EntityEventArgs<IUnit> args)
        {
            args.Entity.BuilderComponent.ProgressStart += HandleWorkerProgressStart;
            args.Entity.BuilderComponent.ProgressStop += HandleWorkerProgressStop;
        }

        private void HandleWorkerRemoved(IEntity sender, EntityEventArgs<IUnit> args)
        {
            args.Entity.BuilderComponent.ProgressStart -= HandleWorkerProgressStart;
            args.Entity.BuilderComponent.ProgressStop -= HandleWorkerProgressStop;
        }

        private void HandleWorkerProgressStart(IEntityTargetProgressComponent sender, EventArgs args)
        {
            UpdateBuildTime();
        }

        private void HandleWorkerProgressStop(IEntityTargetProgressComponent sender, EventArgs args)
        {
            UpdateBuildTime();
        }

        private void UpdateBuildTime()
        {
            hasBuildersInProgress = false;
            float multiplierTotal = 0;
            for (int i = 0; i < Building.WorkerMgr.Workers.Count; i++)
            {
                IUnit worker = Building.WorkerMgr.Workers[i];
                if (worker.BuilderComponent.InProgress)
                {
                    hasBuildersInProgress = true;
                    multiplierTotal += worker.BuilderComponent.TimeMultiplier;
                }
            }

            currBuildTimeStartingHealth = CurrHealth;
            currBuildTime = ((MaxHealth - CurrHealth) * buildTime) / (MaxHealth - 1);
            currBuildTime /= (1+multiplierTotal);
            
            buildTimer = new TimeModifiedTimer(currBuildTime);
        }

        private void Update()
        {
            if (!RTSHelper.IsMasterInstance()
                || Building.WorkerMgr.Amount == 0
                || !hasBuildersInProgress
                || buildingMgr.ConstructionType != ConstructionType.time)
                return;

            if (buildTimer.ModifiedDecrease())
                Add(new HealthUpdateArgs(value: MaxHealth, source: Building.WorkerMgr.Workers[0]));
            else
            {
                int nextCurrHealth = currBuildTimeStartingHealth + Mathf.CeilToInt(MaxHealth - (1 + ((MaxHealth - 1) / currBuildTime) * buildTimer.CurrValue));
                if (nextCurrHealth < CurrHealth)
                    return;

                // Unable to afford repair costs
                if (Building.IsBuilt && !CanRepair(nextCurrHealth - CurrHealth))
                {
                    if (Building.IsLocalPlayerFaction())
                        playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                        {
                            message = ErrorMessage.constructionResourcesMissing,

                            source = Building.WorkerMgr.Workers[0],
                            target = Building 
                        });

                    // stop all builders
                    while(Building.WorkerMgr.Amount > 0)
                        Building.WorkerMgr.Workers[0].BuilderComponent.Stop();
                    return;
                }


                Add(new HealthUpdateArgs(value: nextCurrHealth - CurrHealth, source: Building.WorkerMgr.Workers[0]));
            }
        }
        #endregion

        #region Handling Repair Costs
        public bool CanRepair(int healthToAdd, bool takeResources = true)
        {
            for (int i = 0; i < adjustedCosts.Length; i++)
            {
                adjustedCosts[i].value = new ResourceTypeValue
                {
                    amount = Mathf.FloorToInt((healthToAdd * repairCosts[i].value.amount) / (MaxHealth - 1)),
                    capacity = Mathf.FloorToInt((healthToAdd * repairCosts[i].value.capacity) / (MaxHealth - 1)),
                };

                if(!resourceMgr.HasResources(adjustedCosts[i], Building.FactionID))
                    return false;
            }

            if(takeResources)
                resourceMgr.UpdateResource(Building.FactionID, adjustedCosts, add: false);
            return true;
        }
        #endregion

        #region Updating Health
        protected override void OnHealthUpdated(HealthUpdateArgs args)
        {
            base.OnHealthUpdated(args);

            globalEvent.RaiseBuildingHealthUpdatedGlobal(Building, args);
        }

        protected override void OnMaxHealthReached(HealthUpdateArgs args)
        {
            if(Building.IsBuilt)
            {
                stateHandler.Activate(constructionCompleteState);

                stateHandler.Reset(States, CurrHealth);
            }

            base.OnMaxHealthReached(args);
        }
        #endregion

        #region Destroying Building
        protected override void OnDestroyed(bool upgrade, IEntity source)
        {
            base.OnDestroyed(upgrade, source);

            globalEvent.RaiseBuildingDeadGlobal(Building, new DeadEventArgs(upgrade, source, DestroyObjectDelay));
        }
        #endregion
    }
}

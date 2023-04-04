using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.UI;
using RTSEngine.Upgrades;
using RTSEngine.BuildingExtension;
using RTSEngine.Audio;
using RTSEngine.UnitExtension;
using UnityEngine.Serialization;
using RTSEngine.Logging;
using RTSEngine.Determinism;

namespace RTSEngine.EntityComponent
{
	public class Builder : FactionEntityTargetProgressComponent<IBuilding>, IBuilder
    {
        #region Attributes 
        private IUnit unit;

        [SerializeField, Tooltip("When the construction type in the Building Manager is set to 'Health': This is the Health amount to add to the constructed building every progress round."), Min(0)]
        private int healthPerProgress = 5;
        [SerializeField, Tooltip("When the construction type in the Building Manager is set to 'Time': This represents how much the builder can speed up the required construction time. When a building is being constructed, the time required to complete the building is equal to the max total building Build Time (assigned in the BuildingHealth) divided by (1 + the sum of the multipliers of all active builders)."), Min(0)]
        private float timeMultiplier = 0.2f;
        public float TimeMultiplier => timeMultiplier;

        [SerializeField, Tooltip("Can the builder construct and repair buildings?")]
        private bool canConstruct = true;
        [SerializeField, Tooltip("Define the buildings that can be constructed/repaired by this builder."), FormerlySerializedAs("targetPicker")]
        private FactionEntityTargetPicker constructionTargetPicker = new FactionEntityTargetPicker();

        [SerializeField, Tooltip("Can the builder place buildings? i.e. have building placement tasks in the task when it is selected?")]
        private bool canPlaceBuildings = true;
        [SerializeField, Tooltip("List of building creation tasks that can be launched through this component.")]
        private List<BuildingCreationTask> creationTasks = new List<BuildingCreationTask>();
        public IReadOnlyList<BuildingCreationTask> CreationTasks => creationTasks;

        [SerializeField, Tooltip("List of building creation tasks that can be launched through this component after the building upgrades are unlocked.")]
        private BuildingCreationTask[] upgradeTargetCreationTasks = new BuildingCreationTask[0];
        public IReadOnlyList<BuildingCreationTask> UpgradeTargetCreationTasks => upgradeTargetCreationTasks.ToList();

        [SerializeField, Tooltip("When enabled, only one audio clip from the 'Construction Audio' will be fetched and looped the entire time the builder is constructing the building. When disabled, an audio clip is fetched and played every time the builder adds health points to the target building it is constructing.")]
        private bool fetchConstructionAudioOnce = false;
        [SerializeField, Tooltip("What audio clip to play when constructing a building?")]
		private AudioClipFetcher constructionAudio = new AudioClipFetcher();

        // Game services
        protected IEntityUpgradeManager entityUpgradeMgr { private set; get; }
        protected IBuildingPlacement placementMgr { private set; get; } 
        protected IGameUITextDisplayManager textDisplayer { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; } 
        #endregion

        #region Initializing/Terminating
        protected override void OnProgressInit()
        {
            unit = Entity as IUnit;

            if (!logger.RequireTrue(healthPerProgress >= 0,
                $"[{GetType().Name} - {unit.Code}] The 'Health Per Progress' must have a positive value.")

                || !logger.RequireTrue(timeMultiplier >= 0,
                $"[{GetType().Name} - {unit.Code}] The 'Time Multiplier' must have a positive value.")

                || !logger.RequireTrue(creationTasks.All(task => task.PrefabObject != null),
                $"[{GetType().Name} - {unit.Code}] Some elements in the 'Creation Tasks' array have the 'Prefab Object' field unassigned!")

                || !logger.RequireTrue(upgradeTargetCreationTasks.All(task => task.PrefabObject != null),
                $"[{GetType().Name} - {Entity.Code}] Some elements in the 'Upgrade Target Creation Tasks' array have the 'Prefab Object' field unassigned!"))
                return;

            this.entityUpgradeMgr = gameMgr.GetService<IEntityUpgradeManager>();
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>(); 
            this.textDisplayer = gameMgr.GetService<IGameUITextDisplayManager>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>(); 

            // Initialize creation tasks
            int taskID = 0;
            for (taskID = 0; taskID < creationTasks.Count; taskID++)
            {
                creationTasks[taskID].Init(this, taskID, gameMgr);
                creationTasks[taskID].Enable();
            }
            foreach(var nextTask in upgradeTargetCreationTasks)
            {
                nextTask.Init(this, taskID, gameMgr);
                taskID++;
            }

            // Check for building upgrades:
            if (!Entity.IsFree
                && entityUpgradeMgr.TryGet (Entity.FactionID, out UpgradeElement<IEntity>[] upgradeElements))
            {
                foreach(var nextElement in upgradeElements)
                {
                    DisableTasksWithPrefabCode(nextElement.sourceCode);
                    EnableUpgradeTargetTasksWithPrefab(nextElement.target);
                }
            }

            globalEvent.BuildingUpgradedGlobal += HandleBuildingUpgradedGlobal;
        }

        protected override void OnTargetDisabled()
        {
            globalEvent.BuildingUpgradedGlobal -= HandleBuildingUpgradedGlobal;
        }

        private void DisableTasksWithPrefabCode (string prefabCode)
        {
            foreach (var task in creationTasks)
                if (task.Prefab.Code == prefabCode)
                    task.Disable();
        }

        private void EnableUpgradeTargetTasksWithPrefab(IEntity prefab)
        {
            foreach (var upgradeTargetTask in upgradeTargetCreationTasks)
                if (upgradeTargetTask.Prefab.Code == prefab.Code)
                {
                    creationTasks.Add(upgradeTargetTask);
                    upgradeTargetTask.Enable();
                }
        }
        #endregion

        #region Handling Events: Building Upgrade
        private void HandleBuildingUpgradedGlobal(IBuilding building, UpgradeEventArgs<IEntity> args)
        {
            if (!Entity.IsSameFaction(args.FactionID))
                return;

            // Disable the upgraded tasks
            DisableTasksWithPrefabCode(args.UpgradeElement.sourceCode);
            EnableUpgradeTargetTasksWithPrefab(args.UpgradeElement.target);

            // Remove the building creation task of the unit that has been upgraded

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(
                this,
                new TaskUIReloadEventArgs(reloadAll: true));
        }
        #endregion

        #region Updating Component State
        protected override bool MustStopProgress()
        {
            return Target.instance.Health.IsDead
                || Target.instance.Health.CurrHealth >= Target.instance.Health.MaxHealth
                || Target.instance.FactionID != factionEntity.FactionID
                || (InProgress && !IsTargetInRange(transform.position, Target));
        }

        protected override bool CanEnableProgress()
        {
            if (!IsTargetInRange(transform.position, Target))
            {
                // Builder unit is not moving yet it is not able to start progress?
                // Reset target to renable movement to a location where progress can be started.
                // TO BE REVISED
                if (!unit.MovementComponent.HasTarget)
                {
                    SetTarget(TargetInputData);
                }
                return false;
            }

            return true;
        }

        protected override bool CanProgress() => true;

        protected override bool MustDisableProgress() => false;

        protected override void OnProgressStop()
        {
            if (LastTarget.instance.IsValid())
                LastTarget.instance.WorkerMgr.Remove(unit);
        }
        #endregion

        #region Handling Progress
        protected override void OnInProgressEnabled()
        {
            if(fetchConstructionAudioOnce)
                audioMgr.PlaySFX(unit.AudioSourceComponent, constructionAudio.Fetch(), true);

            globalEvent.RaiseEntityComponentTargetStartGlobal(this, new TargetDataEventArgs(Target));

            unit.MovementComponent.UpdateRotationTarget(Target.instance, Target.instance.transform.position);
        }

        protected override void OnProgress()
        {
            switch(buildingMgr.ConstructionType)
            {
                case ConstructionType.health:

                    // Unable to afford repair costs
                    if(Target.instance.IsBuilt && !Target.instance.Health.CanRepair(healthPerProgress))
                    {
                        if(unit.IsLocalPlayerFaction())
                            playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                            {
                                message = ErrorMessage.constructionResourcesMissing,

                                source = Entity,
                                target = Target.instance
                            });
                            
                        Stop();
                        return;
                    }

                    Target.instance.Health.Add(new HealthUpdateArgs(healthPerProgress, unit));
                    break;

                case ConstructionType.time:
                    // construction is handled in the BuildingHealth component
                    break;
            }

            if(!fetchConstructionAudioOnce)
                audioMgr.PlaySFX(unit.AudioSourceComponent, constructionAudio.Fetch());
        }
        #endregion

        #region Searching/Updating Target
        public override bool CanSearch => true;

        public override float GetProgressRange()
            => Target.instance.WorkerMgr.GetOccupiedPosition(unit, out _)
            ? mvtMgr.StoppingDistance
            : base.GetProgressRange();
        public override Vector3 GetProgressCenter() 
            => Target.instance.WorkerMgr.GetOccupiedPosition(unit, out Vector3 workerPosition)
            ? workerPosition
            : base.GetProgressCenter();

        public override bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target)
        {
            return (target.instance as IBuilding).WorkerMgr.GetOccupiedPosition(unit, out Vector3 workerPosition)
                ? Vector3.Distance(sourcePosition, workerPosition) <= mvtMgr.StoppingDistance
                : base.IsTargetInRange(sourcePosition, target);
        }

        public override ErrorMessage IsTargetValid (TargetData<IEntity> testTarget, bool playerCommand)
        {
            TargetData<IBuilding> potentialTarget = testTarget;

            if (!IsActive || !potentialTarget.instance.IsValid())
                return ErrorMessage.invalid;
            else if (!potentialTarget.instance.IsInteractable)
                return ErrorMessage.uninteractable;
            else if (!RTSHelper.IsSameFaction(potentialTarget.instance, factionEntity))
                return ErrorMessage.factionMismatch;
            else if (!canConstruct || !constructionTargetPicker.IsValidTarget(potentialTarget.instance))
                return ErrorMessage.targetPickerUndefined;
            else if (potentialTarget.instance.Health.IsDead)
                return ErrorMessage.targetDead;
            else if (potentialTarget.instance.Health.HasMaxHealth)
                return ErrorMessage.targetHealthtMaxReached;
            else if (!factionEntity.CanMove() && !IsTargetInRange(transform.position, potentialTarget))
                return ErrorMessage.targetOutOfRange;
            else if (Target.instance != potentialTarget.instance && potentialTarget.instance.WorkerMgr.HasMaxAmount)
                return ErrorMessage.workersMaxAmountReached;

            // Repair Costs check
            if (potentialTarget.instance.IsBuilt)
            {
                switch (buildingMgr.ConstructionType)
                {
                    case ConstructionType.health:

                        // Unable to afford repair costs
                        if (!potentialTarget.instance.Health.CanRepair(healthPerProgress, takeResources: false))
                            return ErrorMessage.constructionResourcesMissing;
                        break;

                    case ConstructionType.time:
                        int maxHealth = potentialTarget.instance.Health.MaxHealth;
                        int currHealth = potentialTarget.instance.Health.CurrHealth;
                        float expectedBuildTime = ((maxHealth - currHealth) * potentialTarget.instance.Health.BuildTime) / (maxHealth - 1);
                        int expectedNextHealthToAdd = Mathf.CeilToInt(maxHealth - (1 + ((maxHealth - 1) / expectedBuildTime) * (expectedBuildTime - Time.deltaTime * TimeModifier.CurrentModifier)));
                        if (!potentialTarget.instance.Health.CanRepair(expectedNextHealthToAdd, takeResources: false))
                            return ErrorMessage.constructionResourcesMissing;
                        break;
                }
            }

            return potentialTarget.instance.WorkerMgr.CanMove(unit);
        }

        protected override void OnTargetPostLocked(SetTargetInputData input, bool sameTarget)
        {
            // For the worker component manager, make sure that enough worker positions is available even in the local method.
            // Since they are only updated in the local method, meaning that the SetTarget method would always relay the input in case a lot of consecuive calls are made...
            //... on the same resource from multiple collectors.
            if(Target.instance.WorkerMgr.Move(
                unit,
                new AddableUnitData(sourceTargetComponent: this, input)) != ErrorMessage.none)
            {
                Stop();
                return;
            }

            // If the movement component is unable to calculate the path towards the target, it will set the unit back to idle
            // And in this case, we do not continue
            if (unit.IsIdle)
                return;

            globalEvent.RaiseEntityComponentTargetLockedGlobal(this, new TargetDataEventArgs(Target));
        }
        #endregion

        #region Task UI
        public override bool OnTaskUIRequest(
            out IEnumerable<EntityComponentTaskUIAttributes> taskUIAttributes,
            out IEnumerable<string> disabledTaskCodes)
        {
            if (base.OnTaskUIRequest(out taskUIAttributes, out disabledTaskCodes) == false)
                return false;

            if (canPlaceBuildings)
            {
                // For building creation tasks, we show building creation tasks that do not have required conditions to launch but make them locked.
                taskUIAttributes = taskUIAttributes
                    .Concat(creationTasks
                        .Where(task => task.IsFactionTypeAllowed(unit.Slot.Data.type))
                        .Select(task => new EntityComponentTaskUIAttributes
                        {
                            data = task.Data,

                            // Wa want the building placement process to start once, this avoids having each builder component instance launch a placement
                            launchOnce = true,

                            locked = task.CanStart() != ErrorMessage.none,
                            lockedData = task.MissingRequirementData,

                            tooltipText = GetTooltipText(task)
                        }));
            }

            return true;
        }

        public override bool OnTaskUIClick(EntityComponentTaskUIAttributes taskAttributes)
        {
            if (base.OnTaskUIClick(taskAttributes))
                return true;

            // If it's not the launch task (task that makes the builder construct a building) then it is a building placement task.
            foreach (BuildingCreationTask creationTask in creationTasks)
                if (creationTask.Data.code == taskAttributes.data.code)
                {
                    placementMgr.Add(unit.FactionID, creationTask);
                    return true;
                }

            return false;
        }

        private string GetTooltipText(BuildingCreationTask nextTask)
        {
            textDisplayer.BuildingCreationTaskToString(
                nextTask,
                out string tooltipText);

            return tooltipText;
        }
        #endregion
    }
}
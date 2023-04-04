using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using RTSEngine.Upgrades;
using RTSEngine.Logging;
using RTSEngine.BuildingExtension;
using RTSEngine.Faction;

namespace RTSEngine.UI
{
    public class GameUITextDisplayManager : MonoBehaviour, IGameUITextDisplayManager
    {
        #region Attributes
        protected IGameManager gameMgr { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.resourceMgr = gameMgr.GetService<IResourceManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
        }
        #endregion

        #region Faction Entity Requirements
        public virtual bool FactionEntityRequirementsToString(IEnumerable<FactionEntityRequirement> reqs, out string text)
        {
            text = String.Empty;

            if (!reqs.IsValid() || !reqs.Any())
                return false;

            List<FactionEntityRequirement> requirementList = reqs.ToList();

            StringBuilder builder = new StringBuilder();

            for(int i = 0; i < requirementList.Count; i++)
            {
                FactionEntityRequirement req = requirementList[i];

                string reqAmountColored = req.TestFactionEntityRequirement(gameMgr.LocalFactionSlot.FactionMgr)
                    ? $"<color=green>{req.amount}</color>"
                    : $"<color=red>{req.amount}</color>";

                builder.Append($"<b>{req.name}</b>: {reqAmountColored}");

                if (i < requirementList.Count - 1)
                    builder.Append(" -");
            }

            text = builder.ToString();

            return true;
        }
        #endregion

        #region Resources
        public virtual bool ResourceTypeValueToString (ResourceTypeValue value, out string text)
        {
            text = value.amount != 0
                ? value.amount.ToString()
                : value.capacity.ToString();

            return true;
        }

        public virtual bool ResourceInputToString(IEnumerable<ResourceInput> resourceInputs, out string text)
        {
            text = String.Empty;

            if (!resourceInputs.IsValid() || !resourceInputs.Any())
                return false;

            List<ResourceInput> resourceInputList = resourceInputs.ToList();

            StringBuilder builder = new StringBuilder();

            for(int i = 0; i < resourceInputList.Count; i++)
            {
                ResourceInput input = resourceInputList[i];

                if (!input.type.IsValid())
                    continue;

                ResourceTypeValueToString(input.value, out string inputAmountText);

                string inputAmountTextColored = resourceMgr.HasResources(input, gameMgr.LocalFactionSlot.ID)
                    ? $"<color=green>{inputAmountText}</color>"
                    : $"<color=red>{inputAmountText}</color>";

                builder.Append($"<b>{input.type.DisplayName}</b>: {inputAmountTextColored}");

                if (i < resourceInputList.Count - 1)
                    builder.Append(" -");
            }

            text = builder.ToString();

            return true;
        }

        public virtual bool ResourceTooltipToString(ResourceTypeInfo resourceType, out string text)
        {
            text = String.Empty;

            if (!resourceType.IsValid())
                return false;

            text = $"<b>{resourceType.DisplayName}</b>:\n{resourceType.Description}";
            return true;
        }
        #endregion

        #region Tasks
        public virtual bool BuildingCreationTaskToString(BuildingCreationTask creationTask, out string text)
            => EntityComponentTaskInputToText(creationTask, creationTask?.Prefab, out text);

        public virtual bool UnitCreationTaskToString(UnitCreationTask creationTask, out string text)
            => EntityComponentTaskInputToText(creationTask, creationTask?.Prefab, out text);

        public virtual bool UpgradeTaskToString(UpgradeTask upgradeTask, out string text)
        {
            if (upgradeTask.Prefab is EntityUpgrade)
                return EntityComponentTaskInputToText(
                    upgradeTask,
                    (upgradeTask.Prefab as EntityUpgrade).GetUpgrade(upgradeTask.UpgradeIndex).UpgradeTarget,
                    out text);
            else if (upgradeTask.Prefab is EntityComponentUpgrade)
            {
                /*return EntityComponentTaskInputToText(
                    upgradeTask,
                    (upgradeTask.Prefab as EntityComponentUpgrade).GetUpgrade(upgradeTask.UpgradeIndex).UpgradeTarget,
                    out text);*/
                text = "";
                return false;
            }
            else
            {
                logger.LogError("[GameUITextDisplayManager] Unable to determine the upgrade type!", source: upgradeTask.Prefab.SourceEntity);
                text = "";
                return false;
            }
        }

        public virtual bool EntityComponentTaskInputToText(IEntityComponentTaskInput taskInput, IEntity targetPrefab, out string text)
        {
            text = String.Empty;

            if(!taskInput.IsValid())
                return false;

            StringBuilder builder = new StringBuilder();

            if (EntityComponentTaskToText(taskInput.Data, out string taskDescription))
            {
                builder.AppendLine(taskDescription);
                builder.AppendLine();
            }
            if (EntityDescriptionToText(targetPrefab, out string targetPrefabDescription))
            {
                builder.AppendLine(targetPrefabDescription);
                builder.AppendLine();
            }

            if (ResourceInputToString(taskInput.RequiredResources, out string resourceInputText))
                builder.AppendLine(resourceInputText);
            if (FactionEntityRequirementsToString(taskInput.FactionEntityRequirements, out string factionReqsText))
                builder.AppendLine(factionReqsText);

            text = builder.ToString();

            return true;
        }

        public virtual bool EntityComponentTaskToText(EntityComponentTaskUIData taskData, out string text)
        {
            text = taskData.description;

            return true;
        }
        #endregion

        #region Entities
        public virtual bool EntityDescriptionToText (IEntity entity, out string text)
        {
            text = String.Empty;

            if(!entity.IsValid())
                return false;

            text = entity.Description;

            return true;
        }

        public virtual bool EntityNameToText (IEntity entity, out string text)
        {
            text = String.Empty;

            if(!entity.IsValid())
                return false;

            text = entity.Name;

            return true;
        }
        #endregion

        #region PlayerErrorMessage
        public virtual bool PlayerErrorMessageToString(PlayerErrorMessageWrapper msgWrapper, out string text)
        {
            text = "";

            switch(msgWrapper.message)
            {
                #region Health
                case ErrorMessage.healthtMaxReached:
                    text = "Maximum health reached!";
                    break;
                case ErrorMessage.healthLow:
                    text = "Health is low!";
                    break;
                case ErrorMessage.healthDead:
                    text = "Entity is dead/destroyed!";
                    break;
                case ErrorMessage.healthNoDecrease:
                    text = "Health can not be decreased";
                    break;
                case ErrorMessage.healthNoIncrease:
                    text = "Health can not be increased";
                    break;
                #endregion

                #region Upgrade / UpgradeLauncher
                case ErrorMessage.upgradeLaunched:
                    text = "Upgrade has been already launched";
                    break;
                case ErrorMessage.upgradeTypeMismatch:
                    text = "Upgrade source and target entity must be of the same type!";
                    break;
                #endregion

                #region Unit Creator / Unit Creation Tasks
                case ErrorMessage.unitCreatorMaxLaunchTimesReached:
                    text = "Unit creator reached maximum launch times!";
                    break;
                case ErrorMessage.unitCreatorMaxActiveInstancesReached:
                    text = "Unit creator reached maximum active created units!";
                    break;
                #endregion

                #region Building Placement
                case ErrorMessage.placementSegmentAmountNotMet:
                    text = $"Minimum amount of segments required for placement: {msgWrapper.amount}";
                    break;
                case ErrorMessage.placementBuildingCenterMissing:
                    text = $"No building center in range!";
                    break;
                case ErrorMessage.alreadyPlaced:
                    text = $"Building already placed!";
                    break;
                case ErrorMessage.placementNotInBorder:
                    text = $"Building not inside border!";
                    break;
                case ErrorMessage.placementNotOnMap:
                    text = $"Building not on map!";
                    break;
                case ErrorMessage.placementOverlapColliders:
                    text = $"Building overlaps with obstacles!";
                    break;
                case ErrorMessage.placementAroundInvalid:
                    text = $"Building place around condition not met!";
                    break;
                case ErrorMessage.placementInFog:
                    text = $"Building can not be placed in fog area!";
                    break;
                case ErrorMessage.placementGridOccupied:
                    text = $"Grid cells are occupied!";
                    break;
                #endregion

                #region Building Placement
                case ErrorMessage.constructionResourcesMissing:
                    text = $"'{msgWrapper.target.Name}' Building repair costs are missing!";
                    break;
                #endregion

                #region Game
                case ErrorMessage.gameFrozen:
                    text = "Game is frozen";
                    break;
                case ErrorMessage.gamePeaceTimeActive:
                    text = "Game is still in peace time!";
                    break;
                #endregion

                #region Faction
                case ErrorMessage.factionLocked:
                    text = "Faction locked!";
                    break;
                case ErrorMessage.factionMismatch:
                    text = "Factions do not match!";
                    break;
                case ErrorMessage.factionLimitReached:
                    text = "Factions limit for the entity has been reached!";
                    break;
                case ErrorMessage.factionIsFriendly:
                    text = "Target faction is a friendly faction!";
                    break;
                #endregion

                #region Tasks
                case ErrorMessage.taskMissingFactionEntityRequirements:
                    text = "Task faction entity requirements are missing!";
                    break;
                case ErrorMessage.taskMissingResourceRequirements:
                    text = "Task required resources are missing!";
                    break;
                case ErrorMessage.taskSourceCanNotLaunch:
                    text = "Launching tasks is disabled!";
                    break;
                #endregion

                #region IEntityTargetComponent
                case ErrorMessage.targetPickerUndefined:
                    text = "Target not allowed!";
                    break;
                case ErrorMessage.targetDead:
                    text = "Target is dead/destroyed!";
                    break;
                case ErrorMessage.targetHealthtMaxReached:
                    text = "Target reached maximum health!";
                    break;
                case ErrorMessage.targetOutOfRange:
                    text = "Target out of range!";
                    break;
                #endregion

                #region IWorkerManager
                case ErrorMessage.workersMaxAmountReached:
                    text = "Maximum workers amount reached!";
                    break;
                #endregion

                #region  Builder
                #endregion

                #region Resource / ResourceCollector
                case ErrorMessage.resourceNotCollectable:
                    text = "Resource not collectable!";
                    break;
                case ErrorMessage.resourceOutsideTerritory:
                    text = "Resource outside faction territory!";
                    break;
                case ErrorMessage.resourceTypeLimitCapacityReached:
                    text = string.IsNullOrEmpty(msgWrapper.text)
                        ? $"Resource has reached maximum capacity!"
                        : $"Resource '{msgWrapper.text}' has reached maximum capacity!";
                    break;
                #endregion

                #region Dropoff
                case ErrorMessage.dropOffTargetMissing:
                    text = "Target is not a drop off point!";
                    break;
                case ErrorMessage.dropOffComponentMissing:
                    text = "Unit does not have drop off component!";
                    break;
                case ErrorMessage.dropOffMaxCapacityReached:
                    text = "Maximum dropoff capacity reached!";
                    break;
                #endregion

                #region Rallypoint
                case ErrorMessage.rallypointTargetNotInRange:
                    text = "Rallypoint target out of range!";
                    break;
                case ErrorMessage.rallypointTerrainAreaMismatch:
                    text = "Rallypoint target area not allowed!";
                    break;
                #endregion

                #region Healer
                #endregion

                #region Converter 
                #endregion

                #region Attack
                case ErrorMessage.attackTargetDisabled:
                    text = "Target can not be attacked!";
                    break;
                case ErrorMessage.attackSourceDisabled:
                    text = "Entity unable to attack!";
                    break;
                case ErrorMessage.attackTypeActive:
                    text = "Attack is already active!";
                    break;
                case ErrorMessage.attackTargetRequired:
                    text = "Attack target is required!";
                    break;
                case ErrorMessage.attackTypeLocked:
                    text = "Attack is locked!";
                    break;
                case ErrorMessage.attackTypeInCooldown:
                    text = "Attack is in cooldown!";
                    break;
                case ErrorMessage.attackTerrainDisabled:
                    text = "Unable to launch terrain attack!";
                    break;
                case ErrorMessage.attackPositionOutOfRange:
                case ErrorMessage.attackTargetOutOfRange:
                    text = "Attack target is out of range!";
                    break;
                case ErrorMessage.attackMoveToTargetOnly:
                case ErrorMessage.attackPositionNotFound:
                    text = "Unable to find attack position!";
                    break;
                #endregion

                #region Movement
                case ErrorMessage.mvtDisabled:
                    text = "Unit can not move!";
                    break;
                case ErrorMessage.mvtTargetPositionNotFound:
                case ErrorMessage.mvtPositionMarkerReserved:
                case ErrorMessage.mvtPositionNavigationOccupied:
                case ErrorMessage.mvtPositionObstacleReserved:
                    text = "Unable to find movement target position!";
                    break;
                #endregion

                #region UnitCarrier / CarriableUnit
                case ErrorMessage.carrierCapacityReached:
                    text = "Unit carrier reached maximum capacity!";
                    break;
                case ErrorMessage.carriableComponentMissing:
                    text = "Unit can not be carried!";
                    break;
                case ErrorMessage.carrierForceSlotOccupied:
                    text = "Forced carrier slot already occupied!";
                    break;
                case ErrorMessage.carrierIdleOnlyAllowed:
                    text = "Only idle units can be called to carrier!";
                    break;
                case ErrorMessage.carrierAttackerNotAllowed:
                    text = "Attack units not allowed on carrier!";
                    break;
                case ErrorMessage.carrierMissing:
                    text = "Target can not carry units!";
                    break;
                #endregion

                default:
                    return false;
            }

            return true;
        }
        #endregion

        #region Faction State
        public virtual bool FactionDefeatToText (FactionSlot factionSlot, out string text)
        {
            text = String.Empty;

            if(!factionSlot.IsValid())
                return false;

            text = $"Faction '{factionSlot.Data.name}' (ID: {factionSlot.ID}) has been defeated!";

            return true;
        }
        #endregion
    }
}

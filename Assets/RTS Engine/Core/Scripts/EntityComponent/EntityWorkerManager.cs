using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Model;
using RTSEngine.Movement;
using RTSEngine.Terrain;
using RTSEngine.UnitExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    public abstract class EntityWorkerManager : MonoBehaviour, IEntityWorkerManager, IEntityPreInitializable
    {
        #region Attributes
        public IEntity Entity {private set; get;}

        [SerializeField, Tooltip("Code to identify this component, unique within the entity")]
        private string code = "unique_code";
        public string Code => code;

        [SerializeField, Tooltip("Size of this array is the max. amount of workers. Worker positions can be static if the array's elements are assigned. If left unassigned, then the working position will be chosen as the closest position on this entity's radius to the worker position.")]
        private ModelCacheAwareTransformInput[] workerPositions = new ModelCacheAwareTransformInput[0];

        [SerializeField, Tooltip("For static worker position, populate to define the types of terrain areas where the fixed worker positions can be placed at. Defining a terrain area for the worker position ensures that their height is at the same level of the chosen terrain area.")]
        private TerrainAreaType[] forcedTerrainAreas = new TerrainAreaType[0];

        private List<IUnit> workers = null;
        private Dictionary<IUnit, int> workerToPositionIndex;
        private List<int> freePositionIndexes;

        public IReadOnlyList<IUnit> Workers => workers;

        public int Amount => workers.Count;
        [SerializeField, Tooltip("Enable this option to allow to define the maximum amount of workers independent of the 'Worker Positions' field.")]
        private bool defineMaxAmount = false;
        [SerializeField, Tooltip("In case the above option is enabled, this is the maximum amount of workers. The amount of this field must be lower than the size of the 'Worker Positions' array.")]
        private int maxWorkerAmount = 4;

        public int MaxAmount => defineMaxAmount ? Mathf.Min(maxWorkerAmount, workerPositions.Length) : workerPositions.Length;
        public bool HasMaxAmount => Amount >= MaxAmount;

        // Game services
        protected IMovementManager mvtMgr { private set; get; } 
        protected IPlayerMessageHandler playerMsgHandler { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; } 
        #endregion

        #region Raising Events
        public event CustomEventHandler<IEntity, EntityEventArgs<IUnit>> WorkerAdded;
        public event CustomEventHandler<IEntity, EntityEventArgs<IUnit>> WorkerRemoved;
        public void RaiseWorkerAdded (IEntity sender, EntityEventArgs<IUnit> args)
        {
            var handler = WorkerAdded;
            handler?.Invoke(sender, args);
        }
        public void RaiseWorkerRemoved (IEntity sender, EntityEventArgs<IUnit> args)
        {
            var handler = WorkerRemoved;

            handler?.Invoke(sender, args);
        }

        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.mvtMgr = gameMgr.GetService<IMovementManager>(); 
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();

            this.Entity = entity;

            workers = new List<IUnit>(workerPositions.Length);
            workerToPositionIndex = new Dictionary<IUnit, int>();
            freePositionIndexes = new List<int>(workerPositions.Length);
            for (int i = 0; i < workerPositions.Length; i++)
                freePositionIndexes.Add(i);

            // In case of a building placement instance, IsDummy will be set to true and in that case, we do not want to trigger checking for terrain areas for the worker positions
            if (entity.IsDummy)
                return;

            foreach (ModelCacheAwareTransformInput workerPosTransform in workerPositions)
            {
                if (!workerPosTransform.IsValid())
                    return;

                if (!terrainMgr.GetTerrainAreaPosition(workerPosTransform.Position, forcedTerrainAreas, out Vector3 nextWorkerPosition))
                {
                    logger.LogError("[EntityWorkerManager] Unable to update the worker position transform as its initial position does not comply with the forced terrain areas!", source: this);
                    return;
                }

                workerPosTransform.Position = nextWorkerPosition;
            }
        }

        public void Disable() { }
        #endregion

        #region Adding Workers
        public Vector3 GetAddablePosition(IUnit worker)
        {
            GetOccupiedPosition(worker, out Vector3 workerPosition);
            return workerPosition;
        }

        // Returns the source entity's position if the worker is not registed as worker in this component or it is registered but no static positions are provided.
        public bool GetOccupiedPosition(IUnit requestedWorker, out Vector3 workerPosition)
        {
            ModelCacheAwareTransformInput positionTransform = null;

            for (int i = 0; i < workers.Count; i++)
                if (workers[i] == requestedWorker)
                    positionTransform = workerPositions[workerToPositionIndex[workers[i]]];

            if (positionTransform.IsValid())
            {
                workerPosition = positionTransform.Position;
                return true;
            }
            else
            {
                workerPosition = Entity.transform.position;
                return false;
            }
        }

        public ErrorMessage CanMove(IUnit worker, AddableUnitData addableData = default)
        {
            if (!worker.IsValid())
                return ErrorMessage.invalid;
            else if (!worker.IsInteractable)
                return ErrorMessage.uninteractable;
            else if (worker.Health.IsDead)
                return ErrorMessage.healthDead;

            else if (!addableData.allowDifferentFaction && !RTSHelper.IsSameFaction(worker, Entity))
                return ErrorMessage.factionMismatch;

            // Already reached maxmimum amount and this is a new worker attempting to be added
            else if (HasMaxAmount && !workers.Contains(worker))
            {
                // If no possible destination is available then stop all entity target components of the worker
                worker.SetIdle(); 
                return ErrorMessage.workersMaxAmountReached;
            }

            return ErrorMessage.none;
        }

        public ErrorMessage Move(IUnit worker, AddableUnitData addableData)
        {
            ErrorMessage errorMsg;
            if((errorMsg = CanMove(worker, addableData)) != ErrorMessage.none)
            {
                if (addableData.playerCommand && RTSHelper.IsLocalPlayerFaction(worker))
                    playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                    {
                        message = errorMsg,

                        source = Entity,
                        target = worker
                    });

                return errorMsg;
            }

            // Getting the addable position is handled here and not in the GetAddablePosition() method
            // This is because when an addable position is determined, the worker entity gets added to the workers list of this worker manager
            // The reason behind that is that we want the worker to reserve a spot in this worker manager before they get to their addable position

            if(!workerToPositionIndex.TryGetValue(worker, out int positionIndex))
            {
                // If no specific transforms are assigned to the worker positions then get the first free slot
                if (!workerPositions[freePositionIndexes[0]].IsValid())
                {
                    positionIndex = 0;
                }
                // Else look for the closest worker position to the next worker
                else
                {
                    float closestDistance = Mathf.Infinity;
                    float nextDistance;

                    for (int i = 0; i < freePositionIndexes.Count; i++)
                    {
                        nextDistance = Vector3.Distance(workerPositions[freePositionIndexes[i]].Position, worker.transform.position);
                        if (nextDistance < closestDistance)
                        {
                            positionIndex = freePositionIndexes[i];
                            closestDistance = nextDistance;
                        }
                    }
                }

                freePositionIndexes.Remove(positionIndex);

                workers.Add(worker);
                workerToPositionIndex.Add(worker, positionIndex);

                RaiseWorkerAdded(Entity, new EntityEventArgs<IUnit>(worker));
            }

            Vector3 destination = workerPositions[positionIndex].IsValid() ? workerPositions[positionIndex].Position : Entity.transform.position;
            float radius = workerPositions[positionIndex].IsValid() ? 0.0f : Entity.Radius;

            errorMsg = mvtMgr.SetPathDestinationLocal(
                worker,
                destination,
                radius,
                Entity,
                new MovementSource
                {
                    playerCommand = addableData.playerCommand,

                    sourceTargetComponent = addableData.sourceTargetComponent,

                    targetAddableUnit = this,
                    targetAddableUnitPosition = destination,

                    isMoveAttackRequest = addableData.isMoveAttackRequest
                });

            if (errorMsg != ErrorMessage.none 
                && addableData.playerCommand && RTSHelper.IsLocalPlayerFaction(worker))
                playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                {
                    message = errorMsg,

                    source = Entity,
                    target = worker
                });

            return errorMsg;
        }

        // Registering the workers occurs at the Move() methods instead of the Add() methods here
        // It is because we want the worker to reserve a working slot before they get into the working position.
        public ErrorMessage CanAdd(IUnit worker, AddableUnitData addableData = default) => ErrorMessage.undefined;
        public ErrorMessage Add(IUnit worker, AddableUnitData addable = default) => ErrorMessage.undefined;
        #endregion

        #region Removing Workers
        public void Remove(IUnit worker)
        {
            if (!workers.Contains(worker))
                return;

            int positionIndex = workerToPositionIndex[worker];

            workers.Remove(worker);
            workerToPositionIndex.Remove(worker);

            freePositionIndexes.Add(positionIndex);

            RaiseWorkerRemoved(Entity, new EntityEventArgs<IUnit>(worker));
        }
        #endregion
    }
}

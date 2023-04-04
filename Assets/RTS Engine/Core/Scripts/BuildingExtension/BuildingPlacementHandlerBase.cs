using RTSEngine.Audio;
using RTSEngine.Cameras;
using RTSEngine.Controls;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.ResourceExtension;
using RTSEngine.Selection;
using RTSEngine.Terrain;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.BuildingExtension
{
    #region Data Types

    public interface IPendingPlacementData
    {
        bool IsActive { get; }
    }

    public struct CompletedPlacementData
    {
        public string code;
        public IBuildingPlacementTask task;
        public Vector3 position;
        public Quaternion rotation;
    }

    public struct PendingPlacementData : IPendingPlacementData
    {
        public IBuildingPlacementTask task;
        public BuildingPlacementOptions options;

        public IBuilding instance;

        public bool IsActive => instance.IsValid();
    }
    #endregion

    public abstract class BuildingPlacementHandlerBase : IBuildingPlacementHandler
    {

        #region Attributes
        protected PendingPlacementData current => queue.Count > 0 ? queue[0] : default;
        private List<PendingPlacementData> queue;
        public IReadOnlyList<PendingPlacementData> Queue => queue;
        public int Count => queue.Count;

        public bool IsActive => current.IsActive;

        [SerializeField, Tooltip("Reserve resources that will be used to place the placement buildings so that the player faction does not consume them before the placement is completed.")]
        private bool reservePlacementResources = true;

        public IFactionSlot FactionSlot { private set; get; }

        protected IGameManager gameMgr { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected IMainCameraController mainCameraController { private set; get; }
        protected IPlayerMessageHandler playerMsgHandler { private set; get; }
        protected IGameControlsManager controls { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IBuildingPlacement placerMgr { private set; get; }
        protected IMouseSelector mouseSelector { private set; get; } 
        #endregion

        #region Raising Events
        public event CustomEventHandler<IBuildingPlacementHandler, IPendingPlacementData> PlacementAdded;
        private void RaisePlacementAdded(PendingPlacementData addedPlacement)
        {
            var handler = PlacementAdded;
            handler?.Invoke(this, addedPlacement);
        }

        public event CustomEventHandler<IBuildingPlacementHandler, IPendingPlacementData> PlacementStopped;
        private void RaisePlacementStopped(PendingPlacementData stoppedPlacement)
        {
            var handler = PlacementStopped;
            handler?.Invoke(this, stoppedPlacement);
        }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, IFactionSlot factionSlot)
        {
            this.gameMgr = gameMgr;

            this.resourceMgr = gameMgr.GetService<IResourceManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.mainCameraController = gameMgr.GetService<IMainCameraController>();
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();
            this.controls = gameMgr.GetService<IGameControlsManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.placerMgr = gameMgr.GetService<IBuildingPlacement>();
            this.mouseSelector = gameMgr.GetService<IMouseSelector>(); 

            this.FactionSlot = factionSlot;

            queue = new List<PendingPlacementData>();

            ResetProperties();

            OnInit();
        }

        protected virtual void OnInit() { }
        #endregion

        #region Update
        public void OnUpdate()
        {
            if (!IsActive)
            {
                OnInactiveUpdate();
                return;
            }

            OnActiveUpdate();
        }

        protected virtual void OnActiveUpdate()
        {
        }

        protected virtual void OnInactiveUpdate()
        {

        }
        #endregion

        #region Adding
        public virtual ErrorMessage CanAdd(IBuildingPlacementTask task, BuildingPlacementOptions options)
        {
            ErrorMessage errorMsg;
            if ((errorMsg = task.CanStart()) != ErrorMessage.none)
                return errorMsg;
            else if (!task.FactionID.IsSameFaction(FactionSlot.ID))
                return ErrorMessage.factionMismatch;

            return ErrorMessage.none;
        }

        public ErrorMessage Add(IBuildingPlacementTask task, BuildingPlacementOptions options)
        { 
            ErrorMessage errorMsg;
            if ((errorMsg = CanAdd(task, options)) != ErrorMessage.none)
            {
                if (FactionSlot.IsLocalPlayerFaction())
                {
                    playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                    {
                        message = errorMsg,

                        source = task.Prefab
                    });
                }

                OnAdded(errorMsg, null, task, options);
                return errorMsg;
            }

            task.OnStart();

            IBuilding placementInstance = buildingMgr.CreatePlacementBuilding(
                task.Prefab,
                options.setInitialRotation ? options.initialRotation : task.Prefab.transform.rotation,
                new InitBuildingParameters
                {
                    factionID = FactionSlot.ID,
                    free = false,

                    setInitialHealth = false,

                    buildingCenter = options.initialCenter,
                });

            if(reservePlacementResources)
                gameMgr.GetService<IResourceManager>().UpdateReserveResources(
                    task.RequiredResources,
                    FactionSlot.ID
                );

            PendingPlacementData addedPlacemnent = new PendingPlacementData
            {
                task = task,
                options = options,

                instance = placementInstance
            };
            queue.Add(addedPlacemnent);

            OnAdded(ErrorMessage.none, placementInstance, task, options);
            RaisePlacementAdded(addedPlacemnent);
            if(FactionSlot.IsLocalPlayerFaction())
                mouseSelector.Lock(buildingMgr);

            addedPlacemnent.instance.PlacerComponent.OnPlacementStart(new BuildingPlacementUpdateData
            {
                targetSegmentIndex = 0,
                sourceSegmentIndex = 0,
                segmentCount = 1
            });
            globalEvent.RaiseBuildingPlacementStartGlobal(addedPlacemnent.instance);

            if (Count == 1)
                OnStart();

            return ErrorMessage.none;
        }

        protected virtual void OnAdded(ErrorMessage errorMsg, IBuilding placementInstance, IBuildingPlacementTask task, BuildingPlacementOptions options) { }
        #endregion

        #region Starting
        protected virtual void OnStart() { }
        #endregion

        #region Stopping
        public bool Stop() => Stop(queueIndex: 0);

        public bool StopAll()
        {
            while (queue.Count > 0)
                Stop(queueIndex: 0);

            return true;
        }

        public void StopOnCondition(Func<PendingPlacementData, bool> condition)
        {
            int index = 0;
            while(index < Count)
            {
                if(condition(Queue[index]))
                {
                    Stop(index);
                    continue;
                }
                index++;
            }
        }

        public virtual bool Stop(int queueIndex)
        {
            if (!IsActive
                || !queueIndex.IsValidIndex(queue))
                return false;

            PendingPlacementData removedPlacement = queue[queueIndex];

            if (reservePlacementResources)
                gameMgr.GetService<IResourceManager>().ReleaseResources(
                    removedPlacement.task.RequiredResources,
                    FactionSlot.ID
                );

            removedPlacement.task.OnCancel();

            if (removedPlacement.instance.IsValid())
                removedPlacement.instance.Health.DestroyLocal(false, null);

            globalEvent.RaiseBuildingPlacementStopGlobal(removedPlacement.instance);

            removedPlacement.instance.PlacerComponent.OnPlacementStop(new BuildingPlacementUpdateData
            {
                targetSegmentIndex = 0,
                sourceSegmentIndex = 0,
                segmentCount = 1
            });

            queue.RemoveAt(queueIndex);
            OnStop();
            RaisePlacementStopped(removedPlacement);

            ResetProperties();


            return true;
        }

        protected virtual void OnStop() { }

        private void ResetProperties()
        {
            if (Count == 0)
            {
                globalEvent.RaiseBuildingPlacementResetGlobal(this);

                if(FactionSlot.IsLocalPlayerFaction())
                    mouseSelector.Unlock(buildingMgr);
            }
            else
                OnStart();
        }
        #endregion

        #region Complete
        public ErrorMessage CanPlace(IBuildingPlacer buildingPlacer) => ErrorMessage.none;

        protected ErrorMessage CanComplete()
        {
            IBuilding instance = current.instance;

            if (!instance.PlacerComponent.CanPlace)
                return instance.PlacerComponent.CanPlaceError;

            if(reservePlacementResources)
                gameMgr.GetService<IResourceManager>().ReleaseResources(
                    current.task.RequiredResources,
                    FactionSlot.ID
                );

            ErrorMessage errorMsg = current.task.CanComplete();

            // Reserve resources again after releasing them for task completion check
            // In case this is not called prior to an actual placement command
            if(reservePlacementResources)
                gameMgr.GetService<IResourceManager>().UpdateReserveResources(
                    current.task.RequiredResources,
                    FactionSlot.ID
                );

            return errorMsg;
        }

        protected bool Complete()
        {
            ErrorMessage errorMsg;
            if ((errorMsg = CanComplete()) != ErrorMessage.none)
            {
                OnComplete(errorMsg, default);
                return false;
            }

            current.task.OnComplete();

            buildingMgr.CreatePlacedBuilding(
                current.instance,
                current.instance.transform.position,
                current.instance.transform.rotation,
                new InitBuildingParameters
                {
                    factionID = FactionSlot.ID,
                    free = false,

                    setInitialHealth = false,

                    giveInitResources = true,

                    buildingCenter = current.instance.PlacerComponent.PlacementCenter,

                    playerCommand = FactionSlot.IsLocalPlayerFaction() 
                });

            CompletedPlacementData completedPlacement = new CompletedPlacementData
            {
                code = current.instance.Code,
                task = current.task,
                position = current.instance.transform.position,
                rotation = current.instance.transform.rotation,
            };

            // To reset the building placement state
            Stop();

            OnComplete(ErrorMessage.none, completedPlacement);

            return true;
        }

        protected virtual void OnComplete(ErrorMessage errorMsg, CompletedPlacementData completedPlacement) { }
        #endregion
    }
}
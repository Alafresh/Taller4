using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.BuildingExtension;
using RTSEngine.NPC.ResourceExtension;

namespace RTSEngine.NPC.BuildingExtension
{
    [System.Serializable]
    public class NPCFactionPlacementHandler : BuildingPlacementHandlerBase
    {
        #region Attributes
        [Space()]
        [SerializeField, Tooltip("NPC faction will only consider placing each new building after a delay sampled from this range has passed. This allows to introduce randomness into the building placement process of NPC factions.")]
        private FloatRange placementDelayRange = new FloatRange(7.0f, 20.0f);
        private float placementDelayTimer;

        [Space()]
        [SerializeField, Tooltip("NPC pending building placement is a process where the building rotates around a build around position and gradually moves away from it until an appropriate placement position is found. This field represents how fast will the building rotation speed be?")]
        private float rotationSpeed = 50.0f;

        [SerializeField, Tooltip("Time before the NPC faction decides to try another position to place the building at. Currently, this component just moves the to be placed building from its build around position a distance that can keep specified in the 'Move Distance' field.")]
        private FloatRange placementMoveReload = new FloatRange(8.0f, 12.0f);
        private float placementMoveTimer;
        [SerializeField, Tooltip("Every time the 'Placement Move Reload' time is through, the pending building will be moved away from its build around position by a distance sampled from this field.")]
        private FloatRange moveDistance = new FloatRange(0.5f, 1.5f);

        [SerializeField, Tooltip("Each time the NPC faction attempts another position to place a building, this value is added to the 'Placement Mvt Reload' field.")]
        private FloatRange placementMoveReloadInc = new FloatRange(1.5f, 2.5f);
        //this will be added to the move timer each time the building moves.
        private int placementMoveReloadIncCount = 0;

        [Space()]
        [SerializeField, Range(0.0f, 1.0f), Tooltip("How often is the height of a building sampled from the terrain's height per second? We do not need to do this every frame as it may become an expensive computation but we can do it often enough to get good results. This depends on the height variations in your map, so the more height variations you have, the more often you want to sample the height of the map when placing buildings.")]
        private float heightCheckReload = 0.2f;
        // This coroutine is running as long as there's a building to be placed and it allows NPC factions to place buildings on different heights
        private IEnumerator heightCheckCoroutine;
        private float rotationMultiplier;

        BuildingPlaceAroundHandler currentPlaceAroundHandler = null;
        private Vector3 currentPlaceAroundPosition;
        public Vector3 CurrentPlaceAroundPosition => currentPlaceAroundPosition;
        public float CurrentMaxPlacementRange => currentPlaceAroundHandler.CurrData.range.max;

        public bool IsGridPlacementActive { private set; get; }

        // NPC components
        protected INPCManager npcMgr { private set; get; }
        protected INPCBuildingPlacer npcBuildingPlacer { private set; get; }
        protected INPCResourceManager npcResourceMgr { private set; get; }

        // Debug Logger
        public Action<string> EventLogger { set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            npcMgr = FactionSlot.CurrentNPCMgr;

            npcResourceMgr = npcMgr.GetNPCComponent<INPCResourceManager>();
            npcBuildingPlacer = npcMgr.GetNPCComponent<INPCBuildingPlacer>();

            IsGridPlacementActive = placerMgr.GridHandler.IsValid() && placerMgr.GridHandler.IsEnabled;
        }
        #endregion

        #region Adding
        public override ErrorMessage CanAdd(IBuildingPlacementTask task, BuildingPlacementOptions options)
        {
            // If the building center hasn't been specified, do not proceed.
            if (!options.initialCenter.IsValid())
                return ErrorMessage.placementBuildingCenterMissing;

            return base.CanAdd(task, options);
        }

        protected override void OnAdded(ErrorMessage errorMsg, IBuilding placementInstance, IBuildingPlacementTask task, BuildingPlacementOptions options)
        {
            if (errorMsg != ErrorMessage.none)
                return;

            // Only one placement instance in the queue, this is the first one that the NPC faction is placing next 
            placementInstance.gameObject.SetActive(true);
        }
        #endregion

        #region Starting 
        protected override void OnStart()
        {
            currentPlaceAroundHandler = new BuildingPlaceAroundHandler(gameMgr,current.instance, current.options.placeAroundData);

            // If we are unable to set the place around data for the next pending building then we discard it and move to the next one.
            // And make sure that the building is allowed inside its currently assigned border
            if (!TrySetNextPlaceAroundData()
                || !current.instance.PlacerComponent.IsBuildingInBorder())
            {
                EventLogger?.Invoke($"'{current.instance.Code}': Active Placement Stop - Unable to set place around data.");
                Stop();
                return;
            }

            current.instance.gameObject.SetActive(true);
            ResetMovementFields();

            // Start the height check coroutine to keep the building always on top of the terrain.
            // This coroutine is only active when this component is placing a pending building.
            if (heightCheckCoroutine.IsValid())
                return;

            heightCheckCoroutine = HeightCheck(heightCheckReload);
            npcBuildingPlacer.StartCoroutine(heightCheckCoroutine);
        }

        public bool TrySetNextPlaceAroundData ()
        {
            while(currentPlaceAroundHandler.TrySetNextData())
            {
                IEnumerable<IEntity> nextPlaceAroundEntities = current.options.initialCenter.EntitiesInRange
                    .Where(entity => currentPlaceAroundHandler.CurrData.IsValidType(entity.ToTargetData(), playerCommand: false) == ErrorMessage.none);

                IEntity nextPlaceAroundEntity = nextPlaceAroundEntities
                    .ElementAtOrDefault(UnityEngine.Random.Range(0, nextPlaceAroundEntities.Count()));

                if(nextPlaceAroundEntity.IsValid())
                {
                    // Get a suitable position for the new building on the build around position
                    terrainMgr.GetTerrainAreaPosition(
                        nextPlaceAroundEntity.transform.position,
                        current.instance.PlacerComponent.PlacableTerrainAreas,
                        out currentPlaceAroundPosition
                    );
                    currentPlaceAroundPosition.y += placerMgr.BuildingPositionYOffset;

                    // Offset the building by the place around data minimum distance and the build around entity radius
                    Vector3 nextBuildingPosition = currentPlaceAroundPosition;
                    nextBuildingPosition.x += nextPlaceAroundEntity.Radius + currentPlaceAroundHandler.CurrData.range.min;

                    current.instance.transform.position = nextBuildingPosition;

                    // Pick a random starting position for building by randomly rotating it around its build around positio
                    current.instance.transform.RotateAround(currentPlaceAroundPosition, Vector3.up, UnityEngine.Random.Range(0.0f, 360.0f));
                    // Keep initial rotation (because the RotateAround method will change the building's rotation as well which we do not want)
                    current.instance.transform.rotation = current.task.Prefab.transform.rotation;

                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Stopping
        protected override void OnStop()
        {
            if (!IsActive)
            {
                // This component is no longer handling placing pending buildings so there is no need to have a height check coroutine
                if(heightCheckCoroutine.IsValid())
                    npcBuildingPlacer.StopCoroutine(heightCheckCoroutine);

                heightCheckCoroutine = null;
                return;
            }
        }
        #endregion

        #region Handling Movement/Rotation
        private void ResetMovementFields ()
        {
            placementDelayTimer = placementDelayRange.RandomValue;
            placementDelayTimer = 0.0f;

            placementMoveTimer = placementMoveReload.RandomValue;
            placementMoveReloadIncCount = 0;

            rotationMultiplier = UnityEngine.Random.value > 0.5f ? 1 : -1;
        }

        private IEnumerator HeightCheck(float waitTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(waitTime);

                if (current.instance.IsValid())
                {
                    terrainMgr.SampleHeight(current.instance.transform.position, placerMgr.PlacableTerrainAreas, out float height);

                    current.instance.transform.position = new Vector3(
                            current.instance.transform.position.x,
                            height + placerMgr.BuildingPositionYOffset,
                            current.instance.transform.position.z);

                }
            }
        }
        #endregion

        #region Handling Movement
        protected override void OnActiveUpdate()
        {
            placementDelayTimer -= Time.deltaTime;
            placementMoveTimer -= Time.deltaTime;

            // Invalid pending building instance
            if (!current.instance.IsValid())
            {
                EventLogger?.Invoke($"'{current.instance.Code}': Active Placement Stop - Invalid Pending Building Instance");
                Stop();
                return;
            }

            // If the pending building leaves the allowed maximum range to the build around position, then try if we can get another place around data to be active for this pending building
            // If none is found then we can discard this pending building and move to the next one.
            if(Vector3.Distance(current.instance.transform.position, CurrentPlaceAroundPosition) >= CurrentMaxPlacementRange)
            {
                if (TrySetNextPlaceAroundData())
                {
                    ResetMovementFields();

                    EventLogger?.Invoke($"'{current.instance.Code}': Active Placement Update - Next Place Around Set");
                }
                else
                {
                    EventLogger?.Invoke($"'{current.instance.Code}': Active Placement Stop - Place Around Data No Longer Met");
                    Stop();
                }

                return;
            }

            // Keep rotating the building around its build around position
            // Make sure to cache the building's local rotation as the 'RotateAround' method will affect that while we need it unchanged
            Quaternion nextBuildingRotation = current.options.disableRotation
                ? current.instance.transform.rotation
                : RTSHelper.GetLookRotation(current.instance.transform, CurrentPlaceAroundPosition, true);

            current.instance.transform.RotateAround(CurrentPlaceAroundPosition, Vector3.up, rotationMultiplier * rotationSpeed * Time.deltaTime);

            current.instance.transform.rotation = nextBuildingRotation;

            if (placementMoveTimer <= 0.0f)
            {
                // Reset timer
                placementMoveTimer = placementMoveReload.RandomValue + (placementMoveReloadInc.RandomValue * placementMoveReloadIncCount);
                placementMoveReloadIncCount++;

                // Move building away from build around position by the defined movement distance
                Vector3 mvtDir = (current.instance.transform.position - CurrentPlaceAroundPosition).normalized;
                mvtDir.y = 0.0f;
                if (mvtDir == Vector3.zero)
                    mvtDir = new Vector3(1.0f, 0.0f, 0.0f);
                Vector3 nextPlacementPos = current.instance.transform.position + mvtDir * moveDistance.RandomValue;

                if (IsGridPlacementActive)
                {
                    placerMgr.GridHandler.TryGetCellPosition(nextPlacementPos, out nextPlacementPos);
                }

                current.instance.PlacerComponent.OnPlacementUpdate(nextPlacementPos);
            }


            if (placementDelayTimer <= 0.0f
                && current.instance.PlacerComponent.CanPlace
                && currentPlaceAroundHandler.IsPlaceAroundValid())
                Complete();
        }
        #endregion

        #region Completion
        protected override void OnComplete(ErrorMessage errorMsg, CompletedPlacementData completedPlacement)
        {
            if(errorMsg != ErrorMessage.none)
            {
                switch(errorMsg)
                {
                    case ErrorMessage.taskMissingResourceRequirements:
                        npcResourceMgr.OnIncreaseMissingResourceRequest(current.task.RequiredResources);
                        break;
                }

                EventLogger?.Invoke($"'{completedPlacement.code}': Placement Completion Failure - Creation Task Conditions Not Met - Error: {errorMsg}");

                Stop();
            }

            EventLogger?.Invoke($"'{completedPlacement.code}': Placement Completion Success.");
        }
        #endregion
    }
}

using RTSEngine.Audio;
using RTSEngine.Controls;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Logging;
using RTSEngine.Selection;
using RTSEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RTSEngine.BuildingExtension
{
    [System.Serializable]
    public class LocalFactionPlacementHandler : BuildingPlacementHandlerBase 
    {
        #region Attributes
        //[Header("General")]
        [SerializeField, Tooltip("Audio clip to play when the player places a building.")]
        private AudioClipFetcher placeBuildingAudio = new AudioClipFetcher();

        //[Header("Rotation")]
        [SerializeField, Tooltip("Enable to allow the player to rotate buildings while placing them.")]
        private bool canRotate = true;
        [SerializeField, Tooltip("Key used to increment the building's euler rotation on the y axis.")]
        private ControlType positiveRotationKey = null;
        [SerializeField, Tooltip("Key used to decrement the building's euler rotation on the y axis.")]
        private ControlType negativeRotationKey = null;
        [SerializeField, Tooltip("How fast would the building rotate?")]
        private float rotationSpeed = 1f;

        //[Header("Hold And Spawn")]
        [SerializeField, Tooltip("Enable to allow the player to hold a key to keep placing the same building type multiple times.")]
        private bool holdAndSpawnEnabled = false;
        private bool holdAndSpawnActive = false;
        private IBuildingPlacementTask holdAndSpawnTask;
        private BuildingPlacementOptions holdAndSpawnOptions;

        [SerializeField, Tooltip("Key used to keep placing the same building type multiple times when the option to do so is enabled")]
        private ControlType holdAndSpawnKey = null;
        [SerializeField, Tooltip("Preserve last building placement rotation when holding and spawning buildings?")]
        private bool preserveBuildingRotation = true;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
        }
        #endregion

        #region Update
        protected override void OnInactiveUpdate()
        {
            if (holdAndSpawnActive)
            {
                Add(holdAndSpawnTask, holdAndSpawnOptions);

                holdAndSpawnActive = false;
            }
        }

        protected override void OnActiveUpdate()
        {

            // Right mouse button stops building placement
            if (Input.GetMouseButtonUp(1))
            {
                Stop();
                return;
            }

            MoveBuilding();

            RotateBuilding();

            // Left mouse button allows to place the building
            if (Input.GetMouseButtonUp(0)
                && !EventSystem.current.IsPointerOverGameObject())
            {
                Complete();
            }
        }
        #endregion

        #region Handling Movement
        // Keep moving the building by following the player's mouse
        private void MoveBuilding()
        {
            // Using a raycheck, we will make the current building follow the mouse position and stay on top of the terrain.
            if (Physics.Raycast(
                mainCameraController.MainCamera.ScreenPointToRay(Input.mousePosition),
                out RaycastHit hit,
                Mathf.Infinity,
                placerMgr.PlacableLayerMask))
            {
                // Depending on the height of the terrain, we will place the building on it
                Vector3 nextBuildingPos = hit.point;

                // Make sure that the building position on the y axis stays inside the min and max height interval
                nextBuildingPos.y += placerMgr.BuildingPositionYOffset;

                if (current.instance.transform.position != nextBuildingPos)
                {
                    // Check if the building can be placed in this new position
                    current.instance.PlacerComponent.OnPlacementUpdate(nextBuildingPos);
                }

            }
        }
        #endregion

        #region Handling Rotation
        private void RotateBuilding()
        {
            if (!canRotate
                || current.options.disableRotation)
                return;

            Vector3 nextEulerAngles = current.instance.transform.rotation.eulerAngles;
            // Only rotate if one of the keys is pressed down (check for direction) and rotate on the y axis only.
            nextEulerAngles.y += rotationSpeed * (controls.Get(positiveRotationKey) ? 1.0f : (controls.Get(negativeRotationKey) ? -1.0f : 0.0f));

            current.instance.transform.rotation = Quaternion.Euler(nextEulerAngles);
        }
        #endregion

        #region Adding
        protected override void OnAdded(ErrorMessage errorMsg, IBuilding placementInstance, IBuildingPlacementTask task, BuildingPlacementOptions options)
        {
            if (errorMsg != ErrorMessage.none)
                return;

            placementInstance.gameObject.SetActive(false);
        }
        #endregion

        #region Starting
        protected override void OnStart()
        {
            // Set the position of the new building (and make sure it's on the terrain)
            if (Physics.Raycast(mainCameraController.MainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, placerMgr.PlacableLayerMask))
            {
                Vector3 nextBuildingPos = hit.point;
                nextBuildingPos.y += placerMgr.BuildingPositionYOffset;
                current.instance.transform.position = nextBuildingPos;
            }

            current.instance.gameObject.SetActive(true);
            // Enable marker since this is the local player
            current.instance.SelectionMarker.Enable();
        }
        #endregion

        #region Stopping
        protected override void OnStop()
        {
            holdAndSpawnActive = false;
        }
        #endregion

        #region Complete
        protected override void OnComplete(ErrorMessage errorMsg, CompletedPlacementData completedPlacement)
        {
            if (errorMsg != ErrorMessage.none)
            {
                playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                {
                    message = errorMsg,

                    source = current.instance
                });

                return;
            }

            audioMgr.PlaySFX(placeBuildingAudio.Fetch(), false);

            if (holdAndSpawnEnabled && controls.Get(holdAndSpawnKey))
            {
                holdAndSpawnActive = true;
                holdAndSpawnTask = completedPlacement.task;
                holdAndSpawnOptions = new BuildingPlacementOptions
                {
                    setInitialRotation = preserveBuildingRotation,
                    initialRotation = completedPlacement.rotation
                };
            }
        }
        #endregion
    }
}
using UnityEngine;

using RTSEngine.Game;
using RTSEngine.BuildingExtension;
using RTSEngine.Controls;
using RTSEngine.Terrain;
using RTSEngine.Logging;
using RTSEngine.Utilities;

namespace RTSEngine.Cameras
{
    public abstract class MainCameraZoomHandlerBase : MonoBehaviour, IMainCameraZoomHandler
    {
        #region Attributes
        [SerializeField, Tooltip("How fast can the main camera zoom?")]
        private SmoothSpeed zoomSpeed = new SmoothSpeed { value = 1.0f, smoothFactor = 0.1f };
        public SmoothSpeed ZoomSpeed => zoomSpeed;

        [Space(), SerializeField, Tooltip("Enable to zoom using the camera's field of view instead of the height of the camera.")]
        private bool useFOV = false;
        public bool UseFOV => useFOV;
        // Gets either incremented or decremented depending on the zoom inputs.
        // Zoom value is treated / updated differently depending if FOV zoom or Transform based zoom is enabled.
        protected float zoomValue = 0.0f;
        protected float lastZoomValue = 0.0f;

        [Space(), SerializeField, Tooltip("The height that the main camera starts with.")]
        private float initialHeight = 15.0f;
        public float InitialHeight => initialHeight;
        [SerializeField, Tooltip("The minimum height the main camera is allowed to have.")]
        protected float minHeight = 5.0f;
        [SerializeField, Tooltip("The maximum height the main camera is allowed to have.")]
        protected float maxHeight = 18.0f;

        [Space(), SerializeField, Tooltip("Allow the player to zoom the camera when they are placing a building?")]
        protected bool allowBuildingPlaceZoom = true;

        [Space(), SerializeField, Tooltip("Populate this array by one or more terrain area types to offset the height of the camera by the height of the terrain area objects when the camera is facing them.")]
        private TerrainAreaType[] heightOffsetTerrainAreas = new TerrainAreaType[0];
        [SerializeField, Tooltip("Minimum height of the terrain area objects that the camera is looking at in order for an offset to be triggered.")]
        private float offsetMinTerrainHeight = 0.0f;
        /// <summary>
        /// When true, the main camera is looking at a terrain position with a height over the minimum offset. When false, the camera is looking at an even terrain position.
        /// </summary>
        private bool isAddingOffset;
        /// <summary>
        /// Value that decrease over time and has the currOffset float as its initial value and used to actually update the height of the camera.
        /// </summary>
        private float lastOffset;
        /// <summary>
        /// The total offset values stored to be used later when the camera is not looking at an even terrain anymore to restore the height of the camera again.
        /// </summary>
        private float accumOffset;

        [Space(), SerializeField, Tooltip("Enable to allow the main camera to pivot upwards when reaching the minimum zoom height. This only works when zooming is enabled without adjusting the FOV of the camera and when the main camera is in its initial rotation values before pivoting starts!")]
        private bool pivotNearMinHeight = true;
        [SerializeField, Tooltip("Range before the mininum allowed height of the main camera in which pivoting upwards occurs if enabled.")]
        private float pivotMinHeightRange = 5.0f;
        // Enabled when the camera is pivoting towards near minimum height (camera height entered the pivotMinHeightRange)
        private bool isPivoting;
        [SerializeField, Tooltip("When pivoting upwards towards the minimum height is enabled, the camera will pivot smoothly towards this angle.")]
        private float pivotTargetAngle = 25.0f;
        [SerializeField, Tooltip("When pivoting upwards towards the minimum height is enabled, the camera will slightly move backwards smoothly until it reaches the values in this field when the minimum height is reached."), Min(0.0f)]
        private float pivotMoveBackwardsDistance = 5.0f;
        // Holds the target position of the camera (x and z axis only) when the camera must reach when it reaches the minimum height with pivoting enabled.
        // When the player moves the main camear during pivoting, this reference position is updated accordingly
        private Vector3 pivotCameraPositionTargetRef;

        public float LookAtTargetMinHeight => (useFOV || !pivotNearMinHeight) ? 0.0f : minHeight + pivotMinHeightRange;

        protected IGameControlsManager controls { private set; get; }
        protected IMainCameraController cameraController { private set; get; }
        protected IBuildingPlacement placementMgr { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.controls = gameMgr.GetService<IGameControlsManager>();
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.cameraController = gameMgr.GetService<IMainCameraController>();
            this.logger = gameMgr.GetService<IGameLoggingService>();

            if (!logger.RequireTrue(initialHeight >= minHeight && initialHeight <= maxHeight,
                $"[{GetType().Name}] The 'Initial Height' value must be between the minimum and maximum allowed height values."))
                return;

            if (!heightOffsetTerrainAreas.IsValid())
            {
                logger.LogError($"[{GetType().Name}] The field 'Height Offset Terrain Areas' must be empty or include valid elements!");
                return;
            }

            // Initial FOV or camera position height configurations
            if (useFOV)
            {
                zoomValue = (initialHeight - minHeight) / (maxHeight - minHeight);
                SetCameraFOV(initialHeight);
            }
            else
            {
                cameraController.MainCamera.transform.position = new Vector3(cameraController.MainCamera.transform.position.x, initialHeight, cameraController.MainCamera.transform.position.y);
            }

            cameraController.RaiseCameraTransformUpdated();

            lastOffset = 0.0f;
            accumOffset = 0.0f;
            isAddingOffset = false;

            OnInit();
        }

        protected virtual void OnInit() { }
        #endregion

        #region Update/Apply Input
        public abstract void UpdateInput();

        public void Apply()
        {
            // Get terrain height point
            terrainMgr.ScreenPointToTerrainPoint(RTSHelper.MiddleScreenPoint, heightOffsetTerrainAreas, out Vector3 middleScreenTerrainPoint); 

            // FOV zooming
            if (useFOV)
            {
                ApplyFOVZoom(middleScreenTerrainPoint);
            }
            // Camera Transform Height zooming
            else
            {
                HandleNearMinHeightPivot();

                if (cameraController.RotationHandler.HasInitialRotation
                    || cameraController.PanningHandler.IsFollowingTarget)
                    isPivoting = false;

                // By default the zoom direction is the middle of the screen, i.e the forward direction of the main camera transform 
                Vector3 zoomDirection = cameraController.MainCamera.transform.forward;
                // Target direction used to zoom in / out the main camera
                Vector3 targetDirection = Vector3.zero;
                ApplyTransformZoom(middleScreenTerrainPoint, zoomDirection, targetDirection);
            }
        }

        protected virtual void ApplyTransformZoom(Vector3 middleScreenTerrainPoint, Vector3 zoomDirection, Vector3 targetDirection)
        {
            // Hold the last camera position so that we restore it later if the height leaves the allowed boundaries
            Vector3 lastCamPos = cameraController.MainCamera.transform.position;

            // Handling terrain height offset elevation
            if (middleScreenTerrainPoint.y >= offsetMinTerrainHeight)
            {
                // Only enable adding a new offset if there is not one active at the moment...
                // ... or if there is one already active and it reached the target offset elevation (lastOffset <= 0.0f) and the new one is higher than the last accumulated ones
                if (!isAddingOffset || (lastOffset <= 0.0f && (middleScreenTerrainPoint.y - offsetMinTerrainHeight - accumOffset) > 0.5f))
                {
                    // The new target height elevation (lastOffset) is the new terrain height difference minus the last 
                    lastOffset = (middleScreenTerrainPoint.y - offsetMinTerrainHeight - accumOffset);
                    accumOffset += lastOffset;
                    isAddingOffset = true;
                }
            }
            else if (isAddingOffset)
            {
                lastOffset = accumOffset - lastOffset;
                // When activating the height offset elevation then reset the accumulated offsets values so that we can substract later when deactivating height offset elevation
                accumOffset = 0;
                isAddingOffset = false;
            }
            if (lastOffset > 0.0f)
            {
                float change = Time.deltaTime * zoomSpeed.value;
                lastOffset -= change;
                targetDirection += (isAddingOffset ? -1.0f : 1.0f) * change * cameraController.MainCamera.transform.forward;
            }

            // Handling actual zooming in/out
            lastZoomValue = Mathf.Lerp(lastZoomValue, zoomValue, zoomSpeed.smoothFactor);
            if ((lastCamPos.y > minHeight && lastZoomValue > 0.0f) || (lastCamPos.y < maxHeight && lastZoomValue < 0.0f))
                targetDirection += zoomSpeed.value * lastZoomValue * zoomDirection;

            // Updating the camera height by adding the target movement direction
            cameraController.MainCamera.transform.position += targetDirection;

            // Apply zooming limit
            if (cameraController.MainCamera.transform.position.y < minHeight || cameraController.MainCamera.transform.position.y > maxHeight)
            {
                lastCamPos.y = Mathf.Clamp(lastCamPos.y, minHeight, maxHeight);
                cameraController.MainCamera.transform.position = lastCamPos;
            }

            if (lastZoomValue != zoomValue)
                cameraController.RaiseCameraTransformUpdated();

            zoomValue = 0.0f;
        }

        private void ApplyFOVZoom(Vector3 middleScreenTerrainPoint)
        {
            zoomValue = Mathf.Clamp01(zoomValue);

            // Only if there is change in the zooming related inputs
            float targetHeight = Mathf.Lerp(minHeight, maxHeight, zoomValue);

            // Terrain height offset
            if (middleScreenTerrainPoint.y >= offsetMinTerrainHeight)
                targetHeight += middleScreenTerrainPoint.y - offsetMinTerrainHeight;

            SetCameraFOV(Mathf.Lerp(cameraController.MainCamera.fieldOfView, targetHeight, Time.deltaTime * zoomSpeed.value));
        }

        public void SetCameraFOV(float value)
        {
            if (cameraController.MainCamera.fieldOfView == value)
                return;

            cameraController.MainCamera.fieldOfView = value;
            if (cameraController.MainCameraUI.IsValid())
                cameraController.MainCameraUI.fieldOfView = value;

            cameraController.RaiseCameraTransformUpdated();
        }

        private void HandleNearMinHeightPivot()
        {
            if (!pivotNearMinHeight)
                return;

            float currHeight = cameraController.MainCamera.transform.position.y;
            float heightDiff = currHeight - minHeight;

            // Can only enabling pivoting near min height when the camera is in initial rotation values
            // So if the player manipulates the rotation beforehand, no pivoting towards min height can occur
            if (!isPivoting && heightDiff < minHeight && cameraController.RotationHandler.HasInitialRotation)
            {
                isPivoting = true;
                // Calculate the target camera position to be set when min height is reached in case no camera movement occurs
                pivotCameraPositionTargetRef = new Vector3(
                    cameraController.MainCamera.transform.position.x - pivotMoveBackwardsDistance,
                    0.0f,
                    cameraController.MainCamera.transform.position.z - pivotMoveBackwardsDistance);

                // Stop following any target entity
                cameraController.PanningHandler.SetFollowTarget(null);
            }

            if (isPivoting && heightDiff >= minHeight)
            {
                // Disabling pivoting to min height resets the rotation back to the initial rotation values since it can only start from initial rotation values
                cameraController.RotationHandler.ResetRotation(smooth: true);
                isPivoting = false;
            }

            if (isPivoting)
            {
                float heightPercentage = (currHeight - minHeight) / (pivotMinHeightRange);

                // In case the player moves the camera while pivoting towards min height, recalculate the reference position to be set when min height is reached
                pivotCameraPositionTargetRef += Quaternion.Euler(new Vector3(0f, cameraController.MainCamera.transform.eulerAngles.y, 0f)) * cameraController.PanningHandler.LastPanDirection * cameraController.PanningHandler.PanningSpeed.value * Time.deltaTime;
                Vector3 nextCameraPosition = new Vector3(
                    pivotCameraPositionTargetRef.x + (pivotMoveBackwardsDistance * heightPercentage),
                    cameraController.MainCamera.transform.position.y,
                    pivotCameraPositionTargetRef.z + (pivotMoveBackwardsDistance * heightPercentage)
                    );
                nextCameraPosition.x = Mathf.Clamp(nextCameraPosition.x, pivotCameraPositionTargetRef.x, pivotCameraPositionTargetRef.x + pivotMoveBackwardsDistance);
                nextCameraPosition.z = Mathf.Clamp(nextCameraPosition.z, pivotCameraPositionTargetRef.z, pivotCameraPositionTargetRef.z + pivotMoveBackwardsDistance);
                cameraController.MainCamera.transform.position = Vector3.Lerp(cameraController.MainCamera.transform.position, nextCameraPosition, cameraController.PanningHandler.PanningSpeed.value * Time.deltaTime);

                Vector3 nextEulerAngles = cameraController.MainCamera.transform.rotation.eulerAngles;
                float nextAngle = pivotTargetAngle + (cameraController.RotationHandler.InitialEulerAngles.x - pivotTargetAngle) * heightPercentage;
                nextEulerAngles.x = Mathf.Lerp(nextEulerAngles.x, nextAngle, cameraController.RotationHandler.RotationSpeed.value * Time.deltaTime);
                nextEulerAngles.x = Mathf.Clamp(nextEulerAngles.x, pivotTargetAngle, cameraController.RotationHandler.InitialEulerAngles.x);
                cameraController.MainCamera.transform.rotation = Quaternion.Euler(nextEulerAngles);

                if (cameraController.RotationHandler.IsRotating)
                    isPivoting = false;
            }
        }
        #endregion
    }
}
 
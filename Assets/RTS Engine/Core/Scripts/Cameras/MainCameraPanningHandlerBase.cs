using System;

using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Controls;
using RTSEngine.Utilities;

namespace RTSEngine.Cameras
{
    public abstract class MainCameraPanningHandlerBase : MonoBehaviour, IMainCameraPanningHandler
    {
        #region Attributes
        [SerializeField, Tooltip("X-axis camera position offset value.")]
        // Camera look at offset values, when the camera is looking at a position, this value is the offset that the camera position will have on the x and z axis
        // The value of the offset depends on the camera's rotation on the x-axis
        private float offsetX = -15;
        [SerializeField, Tooltip("Z-axis camera position offset value.")]
        private float offsetZ = -15;

        // The above two floats are the initial offset values used for the initial height of the main camera, the curr offsets are updated when the player changes the height
        public float CurrOffsetX { private set; get; }
        public float CurrOffsetZ { private set; get; }

        [Space(), SerializeField, Tooltip("How fast does the camera pan?")]
        private SmoothSpeed panningSpeed = new SmoothSpeed { value = 20.0f, smoothFactor = 0.1f };
        public SmoothSpeed PanningSpeed => panningSpeed;

        // Limit the pan of the camera on the x and z axis? 
        [System.Serializable]
        public struct PanningLimit
        {
            public bool enabled;
            [Tooltip("The minimum allowed position values on the x and z axis.")]
            public Vector2 minPosition;
            [Tooltip("The maximum allowed position values on the x and z axis.")]
            public Vector2 maxPosition;
        }
        [Space(), SerializeField, Tooltip("Limit the position that the camera can pan to.")]
        private PanningLimit panLimit = new PanningLimit { enabled = true, minPosition = new Vector2(-20.0f, -20.0f), maxPosition = new Vector2(120.0f, 120.0f) };

        protected Vector3 currPanDirection;
        public Vector3 LastPanDirection { protected set; get; }
        public bool IsPanning => currPanDirection != Vector3.zero;

        [Header("Follow Target")]
        [Space(), SerializeField, Tooltip("Does the camera follow its target smoothly?")]
        private bool smoothFollow = true;
        [SerializeField, Tooltip("How smooth does the camera follow its target?")]
        private float smoothFollowFactor = 0.1f;
        [SerializeField, Tooltip("Does the camera stop following its target when it moves?")]
        private bool stopFollowingOnMovement = true;
        protected Transform followTarget = null;
        public bool IsFollowingTarget => followTarget.IsValid();

        protected IGameControlsManager controls { private set; get; }
        protected IMainCameraController cameraController { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.controls = gameMgr.GetService<IGameControlsManager>();
            this.cameraController = gameMgr.GetService<IMainCameraController>();

            cameraController.CameraTransformUpdated += HandleMainCameraTransformUpdated;

            LastPanDirection = Vector3.zero;

            OnInit();
        }

        protected virtual void OnInit() { }
        #endregion

        #region Handling Event: Main Camera Tranform Update
        private void HandleMainCameraTransformUpdated(IMainCameraController sender, EventArgs args)
        {
            // Update the current camera offset in case the camera zoom/height has been updated
            CurrOffsetX = (offsetX * cameraController.MainCamera.transform.position.y) / cameraController.ZoomHandler.InitialHeight;
            CurrOffsetZ = (offsetZ * cameraController.MainCamera.transform.position.y) / cameraController.ZoomHandler.InitialHeight;
        }
        #endregion

        #region Update/Apply Input
        public virtual void UpdateInput() { }

        public void Apply()
        {
            // Prioritize following target
            if (followTarget.IsValid())
            {
                OnFollowTargetPanning();
                return;
            }
            else
            {
                // Smoothly update the last panning direction towards the current one
                LastPanDirection = Vector3.Lerp(LastPanDirection, currPanDirection, panningSpeed.smoothFactor);

                OnNonFolloTargetPanning();

                if (LastPanDirection != Vector3.zero)
                    cameraController.RaiseCameraTransformUpdated();
            }
        }

        protected virtual void OnNonFolloTargetPanning() { }

        private void OnFollowTargetPanning()
        {
            LookAt(followTarget.position, smoothFollow, smoothFollowFactor);

            if (currPanDirection != Vector3.zero && stopFollowingOnMovement)
                SetFollowTarget(null);
        }
        #endregion

        #region Handle Follow Target
        /// <summary>
        /// Updates the target that the camera will be following
        /// </summary>
        public void SetFollowTarget(Transform transform, bool lockMovementUntilCentered = true)
        {
            followTarget = transform;

            // Reset movement inputs
            currPanDirection = Vector3.zero;
            LastPanDirection = Vector3.zero;
        }

        /// <summary>
        /// Make the camera look at a target position and return the final position while considering the offset values
        /// </summary>
        public void LookAt(Vector3 targetPosition, bool smooth, float smoothFactor = 0.1f)
        {
            cameraController.RotationHandler.ResetRotation(smooth: true);

            // Adding half of the target position height to the X and Z axis of the camera's target position allows it to center over the target position for any variying height
            targetPosition = new Vector3(
                targetPosition.x + CurrOffsetX + targetPosition.y / 2.0f,
                // Enforce minimum look at camera height in case FOV is not used, which is the height before which pivoting to reach minimum height is enabled
                cameraController.ZoomHandler.UseFOV
                    ? cameraController.MainCamera.transform.position.y
                    : Mathf.Max(cameraController.MainCamera.transform.position.y, cameraController.ZoomHandler.LookAtTargetMinHeight),
                targetPosition.z + CurrOffsetZ + targetPosition.y / 2.0f);

            SetPosition(
                smooth
                ? Vector3.Lerp(cameraController.MainCamera.transform.position, targetPosition, smoothFactor)
                : targetPosition);

            cameraController.RaiseCameraTransformUpdated();
        }
        #endregion

        #region Helper Functions
        public void SetPosition(Vector3 position)
        {
            cameraController.MainCamera.transform.position = panLimit.enabled
                ? new Vector3(
                    Mathf.Clamp(position.x, panLimit.minPosition.x, panLimit.maxPosition.x),
                    position.y,
                    Mathf.Clamp(position.z, panLimit.minPosition.y, panLimit.maxPosition.y))
                : position;
        }
        #endregion

    }
}
 
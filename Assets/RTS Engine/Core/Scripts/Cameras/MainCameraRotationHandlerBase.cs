
using RTSEngine.BuildingExtension;
using RTSEngine.Controls;
using RTSEngine.Game;
using RTSEngine.Terrain;
using RTSEngine.Utilities;
using UnityEngine;

namespace RTSEngine.Cameras
{
    public abstract class MainCameraRotationHandlerBase : MonoBehaviour, IMainCameraRotationHandler
    {
        #region Attributes
        [SerializeField, Tooltip("Defines the initial rotation of the main camera.")]
        private Vector3 initialEulerAngles = new Vector3(45.0f, 45.0f, 0.0f);
        public Vector3 InitialEulerAngles => initialEulerAngles;
        private Quaternion initialRotation;
        /// <summary>
        /// True when the current rotation is different than the initially assigned rotation.
        /// </summary>
        public bool HasInitialRotation => Vector3.Distance(cameraController.MainCamera.transform.eulerAngles, initialEulerAngles) < 0.1f;

        [Space(), SerializeField, Tooltip("Have a fixed rotation when the camera is panning? When enabled, the camera rotation will be reset when the camera pans.")]
        private bool fixPanRotation = true;
        [SerializeField, Min(0), Tooltip("How far can the camera move before reverting to the initial rotation (if above field is enabled).")]
        private float allowedRotationPanSize = 0.2f;

        [Space(), SerializeField, Tooltip("How fast can the camera rotate?")]
        private SmoothSpeed rotationSpeed = new SmoothSpeed { value = 40.0f, smoothFactor = 0.1f };
        public SmoothSpeed RotationSpeed => rotationSpeed;
        [SerializeField, Tooltip("Minimum rotation input value for an actual rotation to be executed. This allows to avoid small mouse movements of the right mouse button or the mouse wheel to trigger rotation. Values must be in [0,1) for both axis.")]
        private Vector2 minRotationTriggerValues = new Vector2(0.8f, 0.8f);

        [System.Serializable]
        public struct RotationLimit
        {
            [Space()]
            public bool enableXLimit;
            public FloatRange xLimit;

            [Space()]
            public bool enableYLimit;
            public FloatRange yLimit;
        }
        [Space(), SerializeField, Tooltip("Limit the rotation of the main camera.")]
        private RotationLimit rotationLimit = new RotationLimit { enableYLimit = false, yLimit = new FloatRange(-360f, 360f), enableXLimit = true, xLimit = new FloatRange(25.0f, 90.0f) };

        [Space(), SerializeField, Tooltip("Reset camera rotation to initial values when placing a building? When enabled, player will not be allowed to rotate the camera as long as they are placing a building")]
        private bool resetRotationOnPlacement = true;

        [Space(), SerializeField, Tooltip("When this field is enabled, rotating the main camera will always occur by orbiting/rotating around the position that the camera is looking at.")]
        private bool rotateAroundAlways = false;
        [SerializeField, Tooltip("When rotating around the position the camera is looking at is not always enabled, you can define a key that when down, will force the rotation to orbit around the position the camera is looking at.")]
        private ControlType rotateAroundKey = null;
        private bool isRotatingAround;
        private Vector3 rotateAroundCenter;

        // The current and last rotation value that is determined using the different rotation inputs.
        protected Vector2 currRotationValue;
        private Vector2 lastRotationValue;
        public bool IsRotating => currRotationValue != Vector2.zero;

        // When set to true, the main camera ignores all input until the rotation is reset back to the initial euler angles
        private bool isResettingRotation;

        protected IGameControlsManager controls { private set; get; }
        protected IMainCameraController cameraController { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IBuildingPlacement placementMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.controls = gameMgr.GetService<IGameControlsManager>();
            this.cameraController = gameMgr.GetService<IMainCameraController>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>(); 

            initialRotation = Quaternion.Euler(initialEulerAngles);
            ResetRotation(smooth: false);

            minRotationTriggerValues.x = Mathf.Clamp(minRotationTriggerValues.x, 0.0f, 0.99f);
            minRotationTriggerValues.y = Mathf.Clamp(minRotationTriggerValues.x, 0.0f, 0.99f);

            OnInit();
        }

        protected virtual void OnInit() { }
        #endregion

        #region Updating/Applying Input 
        public abstract void UpdateInput();
        
        public void Apply()
        {
            if (Mathf.Abs(currRotationValue.x) < minRotationTriggerValues.x)
                currRotationValue.x = 0.0f;
            if (Mathf.Abs(currRotationValue.y) < minRotationTriggerValues.y)
                currRotationValue.y = 0.0f;

            if (resetRotationOnPlacement && placementMgr.IsLocalPlayerPlacingBuilding)
            {
                if (!HasInitialRotation && !isResettingRotation)
                    ResetRotation(smooth: true);
                currRotationValue = Vector2.zero;
            }

            isRotatingAround = rotateAroundAlways || controls.Get(rotateAroundKey);

            if (currRotationValue != Vector2.zero && cameraController.PanningHandler.IsFollowingTarget)
                cameraController.PanningHandler.SetFollowTarget(null);

            // If the player is moving the camera and the camera's rotation must be fixed during movement...
            //... or if the camera is following a target, lock camera rotation to default value
            if (isResettingRotation
                || ((fixPanRotation && cameraController.PanningHandler.LastPanDirection.magnitude > allowedRotationPanSize) || cameraController.PanningHandler.IsFollowingTarget))
            {
                cameraController.MainCamera.transform.rotation = Quaternion.Lerp(cameraController.MainCamera.transform.rotation, initialRotation, rotationSpeed.smoothFactor);

                if (HasInitialRotation)
                    isResettingRotation = false;
                return;
            }

            // Smoothly update the last rotation value towards the current one
            lastRotationValue = Vector2.Lerp(lastRotationValue, currRotationValue, rotationSpeed.smoothFactor);

            Vector3 nextEulerAngles = cameraController.MainCamera.transform.rotation.eulerAngles;

            if (isRotatingAround)
            {
                // The position that the camera will be rotating around is the world position of the middle of the screen.
                // Only update it when the player is not actively rotating the camera..
                if(currRotationValue == Vector2.zero)
                    rotateAroundCenter = cameraController.ScreenToWorldPoint(RTSHelper.MiddleScreenPoint, applyOffset: false);

                // Find the rotations to be added both vertically and horizontally
                Quaternion targetRotation = Quaternion.AngleAxis(
                    -lastRotationValue.x * rotationSpeed.value * Time.deltaTime,
                    cameraController.MainCamera.transform.up);
                targetRotation *= Quaternion.AngleAxis(
                    -lastRotationValue.y * rotationSpeed.value * Time.deltaTime,
                    cameraController.MainCamera.transform.right);

                // Update camera position accordingly
                Vector3 targetDirection = targetRotation * (cameraController.MainCamera.transform.position - rotateAroundCenter);
                cameraController.PanningHandler.SetPosition(rotateAroundCenter + targetDirection);

                // Get the next camera rotation angles for the rotate around
                Quaternion nextRotation = cameraController.MainCamera.transform.rotation;
                nextRotation *= Quaternion.Inverse(nextRotation) * targetRotation * nextRotation;
                nextEulerAngles = nextRotation.eulerAngles;
            }
            else
            {
                nextEulerAngles.y -= rotationSpeed.value * Time.deltaTime * lastRotationValue.x;
                nextEulerAngles.x -= rotationSpeed.value * Time.deltaTime * lastRotationValue.y;
            }

            // Limit the y/x euler angless if that's enabled
            if (rotationLimit.enableXLimit)
                nextEulerAngles.x = Mathf.Clamp(nextEulerAngles.x, rotationLimit.xLimit.min, rotationLimit.xLimit.max);
            if (rotationLimit.enableYLimit)
                nextEulerAngles.y = Mathf.Clamp(nextEulerAngles.y, rotationLimit.yLimit.min, rotationLimit.yLimit.max);

            cameraController.MainCamera.transform.rotation = Quaternion.Euler(nextEulerAngles);

            if (lastRotationValue != Vector2.zero)
                cameraController.RaiseCameraTransformUpdated();
        }

        public void ResetRotation(bool smooth)
        {
            if (HasInitialRotation)
                return;

            if (smooth)
            {
                // This will allow the Apply() method to smoothly move the rotation back to the initial values.
                isResettingRotation = true;
            }
            else
            {
                cameraController.MainCamera.transform.rotation = initialRotation;
                isResettingRotation = false;
            }
        }
        #endregion

    }
}
 
using System;

using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Event;
using RTSEngine.Terrain;

namespace RTSEngine.Cameras
{
    public class MainCameraController : MonoBehaviour, IMainCameraController
    {
        #region Attributes
        public int Priority => 50;

        [SerializeField, Tooltip("The main camera in the scene.")]
        private Camera mainCamera = null;
        /// <summary>
        /// Gets the main camera in the game.
        /// </summary>
        public Camera MainCamera => mainCamera;

        [SerializeField, Tooltip("Child of the main camera object, used to display UI elements only. This UI camear is optional but it is always recommended to separate rendering UI elements and the rest of the game elements.")]
        private Camera mainCameraUI = null;
        public Camera MainCameraUI => mainCameraUI;

        private IMainCameraControlHandler[] controlHandlers;

        public IMainCameraPanningHandler PanningHandler { private set; get; }
        public IMainCameraRotationHandler RotationHandler { private set; get; }
        public IMainCameraZoomHandler ZoomHandler { private set; get; }

        public bool CanUpdateCameraTransform => gameMgr.State != GameStateType.pause;

        // Keeps track of the mouse position in the last frame to determine delta and use it in control components
        private Vector3 lastMousePosition;
        public Vector3 MousePositionDelta => Input.mousePosition - lastMousePosition;

        // Game services
        protected ITerrainManager terrainMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IGameManager gameMgr { private set; get; } 
        #endregion

        #region Raising Events
        public event CustomEventHandler<IMainCameraController, EventArgs> CameraTransformUpdated;

        public void RaiseCameraTransformUpdated()
        {
            var handler = CameraTransformUpdated;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();

            if (!mainCamera.IsValid())
            {
                logger.LogError($"[{GetType().Name}] The field 'Main Camera' must be assigned!");
                return;
            }

            controlHandlers = gameObject.GetComponentsInChildren<IMainCameraControlHandler>();
            foreach (var handler in controlHandlers)
            {
                if (handler is IMainCameraPanningHandler)
                    PanningHandler = handler as IMainCameraPanningHandler;
                else if (handler is IMainCameraRotationHandler)
                    RotationHandler = handler as IMainCameraRotationHandler;
                else if (handler is IMainCameraZoomHandler)
                    ZoomHandler = handler as IMainCameraZoomHandler;
            }

            if(!PanningHandler.IsValid())
            {
                logger.LogError($"[{GetType().Name}] A component that handles camera panning that implements the interface '{typeof(IMainCameraPanningHandler).Name}' must be attached  to the same game object!");
                return;
            }
            else if(!ZoomHandler.IsValid())
            {
                logger.LogError($"[{GetType().Name}] A component that handles camera zooming that implements the interface '{typeof(IMainCameraZoomHandler).Name}' must be attached  to the same game object!");
                return;
            }
            else if(!RotationHandler.IsValid())
            {
                logger.LogError($"[{GetType().Name}] A component that handles camera rotation that implements the interface '{typeof(IMainCameraRotationHandler).Name}' must be attached  to the same game object!");
                return;
            }

            foreach (var handler in controlHandlers)
                handler.Init(gameMgr);
        }
        #endregion

        #region Updating/Applying Input
        private void Update()
        {
            if (!CanUpdateCameraTransform)
                return;

            foreach (var handler in controlHandlers)
                handler.UpdateInput();

            lastMousePosition = Input.mousePosition;
        }

        private void LateUpdate()
        {
            if (!CanUpdateCameraTransform)
                return;

            foreach (var handler in controlHandlers)
                handler.Apply();
        }
        #endregion

        #region Main Camera Helper Methods
        public Vector3 ScreenToViewportPoint(Vector3 position) => mainCamera.ScreenToViewportPoint(position);

        public Ray ScreenPointToRay(Vector3 position) => mainCamera.ScreenPointToRay(position);

        public Vector3 ScreenToWorldPoint(Vector3 position, bool applyOffset = true)
        {
            position.z = mainCamera.transform.position.y;
            Vector3 worldPosition;
            // First try with a raycast to hit the base terrain bottom collider and if that is not successful then use  the ScreenToWorldPoint
            // The raycast would guarantee getting a position that is on level with the base map terrain level
            // While ScreenToWorldPoint will often not return that and therefore and is used as a backup solution for this method
            if (Physics.Raycast(ScreenPointToRay(position), out RaycastHit hit, Mathf.Infinity, terrainMgr.BaseTerrainLayerMask))
                worldPosition = hit.point;
            else
                worldPosition = mainCamera.ScreenToWorldPoint(position);

            return applyOffset
                ? new Vector3(worldPosition.x - PanningHandler.CurrOffsetX, worldPosition.y, worldPosition.z - PanningHandler.CurrOffsetZ)
                : worldPosition;
        }
        #endregion
    }
}
 
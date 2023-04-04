
using UnityEngine;

using RTSEngine.Game;
using RTSEngine.BuildingExtension;
using RTSEngine.Controls;
using RTSEngine.Terrain;
using RTSEngine.Logging;

namespace RTSEngine.Cameras
{
    public class MainCameraKeyboardZoomHandler : MainCameraZoomHandlerBase
    {
        #region Zoom Attributes
        [System.Serializable]
        public struct MouseWheelZoom
        {
            public bool enabled;
            public bool invert;
            public string name;
            public float sensitivity;
        }
        [SerializeField, Tooltip("Use the mouse wheel to zoom.")]
        private MouseWheelZoom mouseWheelZoom = new MouseWheelZoom { enabled = true, invert = false, name = "Mouse ScrollWheel", sensitivity = 20.0f };

        [System.Serializable]
        public struct KeyZoom
        {
            public bool enabled;
            public ControlType inKey;
            public ControlType outKey;
        }
        [SerializeField, Tooltip("Zoom using keys.")]
        private KeyZoom keyZoom = new KeyZoom { enabled = false };
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
        }
        #endregion

        #region Handling Camera Zoom
        public override void UpdateInput()
        {
            zoomValue = 0.0f;

            if (placementMgr.IsLocalPlayerPlacingBuilding && !allowBuildingPlaceZoom)
                return;

            // Camera zoom on keys
            if (keyZoom.enabled)
            {
                if (controls.Get(keyZoom.inKey))
                    zoomValue -= Time.deltaTime;
                else if (controls.Get(keyZoom.outKey))
                    zoomValue += Time.deltaTime;
            }

            // Camera zoom when the player is moving the mouse scroll wheel
            if (mouseWheelZoom.enabled)
                zoomValue += Input.GetAxis("Mouse ScrollWheel") * mouseWheelZoom.sensitivity
                    * (mouseWheelZoom.invert ? -1.0f : 1.0f) * Time.deltaTime;
        }
        #endregion
    }
}
 
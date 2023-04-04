using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Controls;
using RTSEngine.Terrain;
using RTSEngine.Utilities;
using RTSEngine.BuildingExtension;
using RTSEngine.Event;
using RTSEngine.Entities;
using System;

namespace RTSEngine.Cameras
{
    public class MainCameraKeyboardRotationHandler : MainCameraRotationHandlerBase 
    {
        #region Attributes
        [System.Serializable]
        public struct KeyRotation
        {
            public bool enabled;
            public ControlType positive;
            public ControlType negative;
        }
        [Space(), SerializeField, Tooltip("Rotate the camera with keys")]
        protected KeyRotation keyRotation = new KeyRotation { enabled = false };

        [Space(), SerializeField, Tooltip("Rotate the camera with the mouse wheel.")]
        private ToggableSmoothFactor mouseWheelRotation = new ToggableSmoothFactor { enabled = true, smoothFactor = 0.1f };

        [Space(), SerializeField, Tooltip("Rotate the camera while holding down right mouse button.")]
        private ToggableSmoothFactor rightMouseButtonRotation = new ToggableSmoothFactor { enabled = true, smoothFactor = 0.1f };
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
        }
        #endregion

        #region Updating/Applying Input 
        public override void UpdateInput()
        {
            currRotationValue = Vector2.zero;

            // If the keyboard keys rotation is enabled, check for the positive and negative rotation keys and update the current rotation value accordinly
            if (keyRotation.enabled)
            {
                if(controls.Get(keyRotation.positive))
                    currRotationValue.x = 1.0f;
                else if(controls.Get(keyRotation.negative))
                    currRotationValue.x = -1.0f;
            }

            // If the mouse wheel rotation is enabled and the player is holding the mouse wheel button, update the rotation value accordinly
            if (mouseWheelRotation.enabled && Input.GetMouseButton(2))
                currRotationValue = cameraController.MousePositionDelta * mouseWheelRotation.smoothFactor;

            // Rotation using holding down the right mouse button
            if (rightMouseButtonRotation.enabled && Input.GetMouseButton(1))
                currRotationValue = cameraController.MousePositionDelta * rightMouseButtonRotation.smoothFactor;
        }
        #endregion
    }
}
 
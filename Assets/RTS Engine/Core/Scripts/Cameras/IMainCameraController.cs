using System;

using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Game;

namespace RTSEngine.Cameras
{
    public interface IMainCameraController : IPreRunGamePriorityService 
    {
        Camera MainCamera { get; }
        Camera MainCameraUI { get; }

        Vector3 MousePositionDelta { get; }

        IMainCameraPanningHandler PanningHandler { get; }
        IMainCameraRotationHandler RotationHandler { get; }
        IMainCameraZoomHandler ZoomHandler { get; }

        event CustomEventHandler<IMainCameraController, EventArgs> CameraTransformUpdated;
        void RaiseCameraTransformUpdated();

        Vector3 ScreenToViewportPoint(Vector3 position);
        Vector3 ScreenToWorldPoint(Vector3 position, bool applyOffset = true);
        Ray ScreenPointToRay(Vector3 position);
    }
}
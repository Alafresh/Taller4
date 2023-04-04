using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Utilities;

namespace RTSEngine.Cameras
{
    public interface IMainCameraControlHandler
    {
        void Init(IGameManager gameMgr);

        void UpdateInput();
        void Apply();
    }

    public interface IMainCameraPanningHandler : IMainCameraControlHandler
    {
        bool IsPanning { get; }
        Vector3 LastPanDirection { get; }
        SmoothSpeed PanningSpeed { get; }

        bool IsFollowingTarget { get; }

        float CurrOffsetX { get; }
        float CurrOffsetZ { get; }

        void LookAt(Vector3 targetPosition, bool smooth, float smoothFactor = 0.1F);
        void SetFollowTarget(Transform transform, bool lockMovementUntilCentered = true);

        void SetPosition(Vector3 position);
    }

    public interface IMainCameraZoomHandler : IMainCameraControlHandler
    {
        bool UseFOV { get; }

        float InitialHeight { get; }
        float LookAtTargetMinHeight { get; }

        SmoothSpeed ZoomSpeed { get; }

        void SetCameraFOV(float value);
    }

    public interface IMainCameraRotationHandler : IMainCameraControlHandler
    {
        bool HasInitialRotation { get; }
        Vector3 InitialEulerAngles { get; }
        SmoothSpeed RotationSpeed { get; }
        bool IsRotating { get; }

        void ResetRotation(bool smooth);
    }
}
 
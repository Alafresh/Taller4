using UnityEngine;

namespace RTSEngine.Selection
{
    public interface IEntitySelectionMarker
    {
        bool IsLocked { get; set; }

        void Enable(Color color);
        void Enable();
        void Disable();

        void StartFlash(float totalDuration, float cycleDuration, Color flashColor);
        void StopFlash();
    }
}

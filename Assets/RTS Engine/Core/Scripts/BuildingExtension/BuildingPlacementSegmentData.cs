using UnityEngine;

namespace RTSEngine.BuildingExtension
{
    [System.Serializable]
    public struct BuildingPlacementSegmentData
    {
        [Tooltip("Enable placing multiple instances of the building type by dragging and dropping the mouse in a row that starts from the position the mouse was dragged from until the position where the mouse is released at.")]
        public bool enabled;

        [Tooltip("This field's value is the distance that will be used to separate two individual consecutive instances of the building when segmentation mode is active.")]
        public float segmentLength;

        [Tooltip("Minimum amount of segments required for a valid segmentation placement.")]
        public int minAmount;
    }
}
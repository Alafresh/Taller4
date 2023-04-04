using UnityEngine;

namespace RTSEngine.BuildingExtension
{
    [System.Serializable]
    public struct BuildingPlacedData
    {
        /// <summary>
        /// Is the placed building instance part of a segment placement?
        /// </summary>
        public bool isSegment;
        /// <summary>
        /// In case the building instance is part of a segment placement, this is the direction of the segments it belongs to.
        /// </summary>
        public Vector3 segmentDirection;
    }
}

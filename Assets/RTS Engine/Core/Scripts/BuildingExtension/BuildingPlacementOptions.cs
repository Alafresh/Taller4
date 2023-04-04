using RTSEngine.Entities;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.BuildingExtension
{
    public struct BuildingPlacementOptions
    {
        /// <summary>
        /// Can the building be rotated during placement?
        /// </summary>
        public bool disableRotation;

        public bool setInitialRotation;
        public Quaternion initialRotation;

        public IReadOnlyList<BuildingPlaceAroundData> placeAroundData;

        public IBorder initialCenter; 

        // Segment Placement
        /// <summary>
        /// Holds the segment index to handle in a placement operation.
        /// </summary>
        public int targetIndex;

        /// <summary>
        /// True when adding a new segment to the currently processed pending placement. 
        /// False when the placement addition is to be added to the queue.
        /// </summary>
        public bool isAddingSegment;

        /// <summary>
        /// True when replacing existing segment with a different segment type.
        /// </summary>
        public bool isReplacingExisting;

        /// <summary>
        /// True when snapping a segment to an already placed building.
        /// </summary>
        public bool isSnappingSegment;
        /// <summary>
        /// Target placed building that an existing placement segment would snap to.
        /// </summary>
        public IBuilding snapTarget;
    }
}
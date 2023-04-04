namespace RTSEngine.BuildingExtension
{
    public struct BuildingPlacementUpdateData
    {
        /// <summary>
        /// Index of the segment whose placement is being updated.
        /// </summary>
        public int targetSegmentIndex;
        /// <summary>
        /// Index of the segment whose part of the pending placement but is not the one that is being updated.
        /// </summary>
        public int sourceSegmentIndex;
        /// <summary>
        /// Amount of segments in the current pending placement.
        /// </summary>
        public int segmentCount;
    }
}

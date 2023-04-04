using RTSEngine.Entities;
using RTSEngine.Utilities;
using UnityEngine;

namespace RTSEngine.BuildingExtension
{
    public enum GridAreaPivotPoint
    {
        center = 0,

        topLeft = 1,
        bottomLeft = 2,
        topRight = 3,
        bottomRight = 4
    }

    [System.Serializable]
    public struct BuildingPlacementGridData
    {
        [Tooltip("Size of the grid to be occupied by the building.")]
        public Int2D area;
        [Tooltip("Segment position pivot point in the grid.")]
        public GridAreaPivotPoint pivotPoint;

        private IBuilding sourceBuilding;

        public void Init(IBuilding building)
        {
            this.sourceBuilding = building;
        }

        /// <summary>
        /// False if the absolute value of the y euler angle of the building is 90 or 270. True otherwise.
        /// </summary>
        public bool IsHorizontal
        {
            get
            {
                int angleAbs = Mathf.Abs((int)sourceBuilding.transform.eulerAngles.y);
                return angleAbs != 90 && angleAbs != 270; 
            }
        }

        private float xOffset => (IsHorizontal ? area.x : area.y) / 2.0f;
        private float yOffset => (IsHorizontal ? area.y : area.x) / 2.0f;

        public Vector3 ApplyPivotPoint(Vector3 position) => ApplyPivotPoint(position, pivotPoint);
        public Vector3 ApplyPivotPoint(Vector3 position, GridAreaPivotPoint pivotPoint)
        {
            switch(pivotPoint)
            {
                case GridAreaPivotPoint.bottomLeft:
                    return new Vector3(
                        position.x + xOffset,
                        position.y,
                        position.z + yOffset);
                case GridAreaPivotPoint.topLeft:
                    return new Vector3(
                        position.x + xOffset,
                        position.y,
                        position.z - yOffset);

                case GridAreaPivotPoint.bottomRight:
                    return new Vector3(
                        position.x - xOffset,
                        position.y,
                        position.z + yOffset);
                case GridAreaPivotPoint.topRight:
                    return new Vector3(
                        position.x - xOffset,
                        position.y,
                        position.z - yOffset);

                default:
                    return position;

            }
        }

        public Vector3 ApplyPivotPointReverse(Vector3 position) => ApplyPivotPointReverse(position, pivotPoint);
        public Vector3 ApplyPivotPointReverse(Vector3 position, GridAreaPivotPoint pivotPoint)
        {
            switch(pivotPoint)
            {
                case GridAreaPivotPoint.bottomLeft:
                    return new Vector3(
                        position.x - xOffset,
                        position.y,
                        position.z - yOffset);
                case GridAreaPivotPoint.topLeft:
                    return new Vector3(
                        position.x - xOffset,
                        position.y,
                        position.z + yOffset);

                case GridAreaPivotPoint.bottomRight:
                    return new Vector3(
                        position.x + xOffset,
                        position.y,
                        position.z - yOffset);
                case GridAreaPivotPoint.topRight:
                    return new Vector3(
                        position.x + xOffset,
                        position.y,
                        position.z + yOffset);

                default:
                    return position;
            }
        }

    }
}
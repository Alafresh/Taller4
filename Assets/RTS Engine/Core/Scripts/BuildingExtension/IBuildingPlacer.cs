using System;
using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Terrain;
using UnityEngine;

namespace RTSEngine.BuildingExtension
{
    public interface IBuildingPlacer : IMonoBehaviour
    {
        IBuilding Building { get; }

        IReadOnlyList<TerrainAreaType> PlacableTerrainAreas { get; }

        bool CanPlace { get; }
        bool CanPlaceOutsideBorder { get; }
        bool Placed { get; }

        IBorder PlacementCenter { get; }
        BuildingPlacedData PlacedData { get; }

        BuildingPlacementSegmentData SegmentData { get; }
        BuildingPlacementGridData GridOptions { get; }
        ErrorMessage CanPlaceError { get; }

        event CustomEventHandler<IBuilding, EventArgs> BuildingPlacementStatusUpdated;
        event CustomEventHandler<IBuilding, EventArgs> BuildingPlacementTransformUpdated;

        /// <summary>
        /// Called on the building instance when it is placed.
        /// Called after the EntityPreInit method.
        /// </summary>
        void InitPlaced(BuildingPlacedData placedData);

        void OnPlacementStart(BuildingPlacementUpdateData data);
        void OnPlacementStop(BuildingPlacementUpdateData data);

        void OnPlacementUpdate();
        void OnPlacementUpdate(Vector3 position);

        bool IsBuildingInBorder();
    }
}

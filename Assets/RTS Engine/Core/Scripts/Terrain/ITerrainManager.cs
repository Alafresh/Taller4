using System.Collections.Generic;

using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Model;
using RTSEngine.Utilities;

namespace RTSEngine.Terrain
{
    public interface ITerrainManager : IPreRunGamePriorityService
    {
        float MapSize { get; }
        IEnumerable<TerrainAreaType> Areas { get; }
        LayerMask BaseTerrainLayerMask { get; }
        CameraBoundariesToTerrainPositions BaseTerrainCameraBounds { get; }

        Int2D HeightCacheLowerLeftCorner { get; }
        Int2D HeightCacheUpperRightCorner { get; }

        //float SampleHeight(Vector3 position, IReadOnlyList<TerrainAreaType> areaTypes = null);
        bool SampleHeight(Vector3 position, IReadOnlyList<TerrainAreaType> areaTypes, out float height);
        bool SampleHeight(Vector3 position, IMovementComponent refMvtComp, out float height);

        ErrorMessage TryGetCachedHeight(Vector3 position, IReadOnlyList<TerrainAreaType> areaTypes, out float height, bool logWarning = false);

        bool IsTerrainArea(GameObject obj);
        bool IsTerrainArea(GameObject obj, string areaKey);
        bool IsTerrainArea(GameObject obj, TerrainAreaType areaType);
        bool IsTerrainArea(GameObject obj, IReadOnlyList<TerrainAreaType> areaTypes);

        TerrainAreaMask TerrainAreasToMask(IReadOnlyList<TerrainAreaType> areaTypes);

        bool GetTerrainAreaPosition(Vector3 inPosition, string areaKey, out Vector3 outPosition);
        bool GetTerrainAreaPosition(Vector3 inPosition, TerrainAreaType areaType, out Vector3 outPosition);
        bool GetTerrainAreaPosition(Vector3 inPosition, IReadOnlyList<TerrainAreaType> areaTypes, out Vector3 outPosition);

        bool ScreenPointToTerrainPoint(Vector3 screenPoint, IReadOnlyList<TerrainAreaType> areaTypes, out Vector3 terrainPoint);
    }
}
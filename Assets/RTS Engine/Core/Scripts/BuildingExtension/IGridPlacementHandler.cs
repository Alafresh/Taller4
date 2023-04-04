
using UnityEngine;
using RTSEngine.Game;
using RTSEngine.Utilities;

namespace RTSEngine.BuildingExtension
{
    public interface IGridPlacementHandler : IPreRunGameService
    {
        bool IsEnabled { get; }
        int CellSize { get; }

        bool IsOccupied(Vector3 worldPosition);
        bool IsOccupied(Int2D cellPosition);
        bool IsOccupied(Int2D area, Int2D bottomLeftCellPosition, bool horizontal);
        bool IsOccupied(Int2D area, Vector3 bottomLeftWorldPosition, bool horizontal);

        bool TryGetCellPosition(Vector3 position, out Int2D cellPosition);
        bool TryGetCellPosition(Vector3 position, out Vector3 worldPosition);
        bool TryGetCellPosition(Int2D position, out Vector3 cellPosition);
    }
}

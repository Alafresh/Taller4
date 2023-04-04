using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using System.Collections.Generic;

namespace RTSEngine.BuildingExtension
{
    public interface IBuildingPlacementHandler
    {
        bool IsActive { get; }
        int Count { get; }
        IFactionSlot FactionSlot { get; }

        event CustomEventHandler<IBuildingPlacementHandler, IPendingPlacementData> PlacementAdded;
        event CustomEventHandler<IBuildingPlacementHandler, IPendingPlacementData> PlacementStopped;

        void Init(IGameManager gameMgr, IFactionSlot factionSlot);

        ErrorMessage CanAdd(IBuildingPlacementTask task, BuildingPlacementOptions options);
        ErrorMessage Add(IBuildingPlacementTask task, BuildingPlacementOptions options);

        bool Stop();

        void OnUpdate();

        ErrorMessage CanPlace(IBuildingPlacer buildingPlacer);
    }
}
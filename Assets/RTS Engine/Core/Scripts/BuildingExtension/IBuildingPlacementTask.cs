using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.ResourceExtension;

namespace RTSEngine.BuildingExtension
{
    public interface IBuildingPlacementTask
    {
        IBuilding Prefab { get; }
        IEnumerable<ResourceInput> RequiredResources { get; }
        IEnumerable<FactionEntityRequirement> FactionEntityRequirements { get; }
        int FactionID { get; }

        ErrorMessage CanStart();
        void OnStart();

        ErrorMessage CanComplete();
        void OnComplete();

        void OnCancel();
    }
}

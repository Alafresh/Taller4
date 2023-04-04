using RTSEngine.Entities;

namespace RTSEngine.BuildingExtension
{
    public interface IBuildingPlacerCondition 
    {
        ErrorMessage CanPlaceBuilding(IBuilding building);
    }
}

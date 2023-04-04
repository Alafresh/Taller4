using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.ResourceExtension;
using System.Collections.Generic;

namespace RTSEngine.Health
{
    public interface IBuildingHealth : IFactionEntityHealth
    {
        IBuilding Building { get; }

        float BuildTime { get; }

        bool CanRepair(int healthToAdd, bool takeResources = true);
    }
}

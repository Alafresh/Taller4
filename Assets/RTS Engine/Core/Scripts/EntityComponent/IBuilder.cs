using RTSEngine.Entities;
using System.Collections.Generic;

namespace RTSEngine.EntityComponent
{
    public interface IBuilder : IEntityTargetProgressComponent
    {
        TargetData<IBuilding> Target { get; }

        IReadOnlyList<BuildingCreationTask> CreationTasks { get; }
        IReadOnlyList<BuildingCreationTask> UpgradeTargetCreationTasks { get; }

        float TimeMultiplier { get; }
    }
}

using System;

using RTSEngine.Entities;
using RTSEngine.Event;

namespace RTSEngine.EntityComponent
{
    [Serializable]
    public struct EntityTargetComponentProgressData
    {
        public float progressTime;
    }

    public interface IEntityTargetProgressComponent : IEntityTargetComponent
    {
        bool InProgress { get; }
        EntityTargetComponentProgressData ProgressData { get; }

        event CustomEventHandler<IEntityTargetProgressComponent, EventArgs> ProgressStart;
        event CustomEventHandler<IEntityTargetProgressComponent, EventArgs> ProgressStop;
    }
}

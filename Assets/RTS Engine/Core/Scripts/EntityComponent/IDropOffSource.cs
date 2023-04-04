using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.ResourceExtension;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    public interface IDropOffSource : IEntityTargetComponent
    {
        DropOffState State { get; }
        IReadOnlyDictionary<ResourceTypeInfo, int> CollectedResources { get; }
        int CollectedResourcesSum { get; }

        event CustomEventHandler<IDropOffSource, EventArgs> CollectedResourcesUpdated;
        event CustomEventHandler<IDropOffSource, EventArgs> DropOffStateUpdated;
        event CustomEventHandler<IDropOffSource, EventArgs> DropOffUnloaded;

        void UpdateCollectedResources(ResourceTypeInfo resourceType, int value);

        //ErrorMessage SendToTarget(bool playerCommand);

        bool HasReachedMaxCapacity(ResourceTypeInfo resourceType = null);
        int GetMaxCapacity(ResourceTypeInfo resourceType);

        bool AttemptStartDropOff(bool force = false, ResourceTypeInfo resourceType = null);

        void Unload();
        void Cancel();
    }
}
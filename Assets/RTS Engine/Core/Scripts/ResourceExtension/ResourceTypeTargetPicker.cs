using System.Collections.Generic;
using System;

using RTSEngine.Entities;

namespace RTSEngine.ResourceExtension
{
    [Serializable]
    public class ResourceTypeTargetPicker : TargetPicker<ResourceTypeInfo, List<ResourceTypeInfo>>
    {
        protected override bool IsInList(ResourceTypeInfo resourceType)
            => options.Contains(resourceType);
    }

}
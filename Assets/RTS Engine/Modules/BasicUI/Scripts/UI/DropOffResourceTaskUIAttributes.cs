using RTSEngine.EntityComponent;
using RTSEngine.ResourceExtension;
using UnityEngine;

namespace RTSEngine.UI
{
    public struct DropOffResourceTaskUIAttributes : ITaskUIAttributes
    {
        public ResourceTypeInfo resourceType;
        public IDropOffSource dropOffSource; 

        public Color maxCapacityColor;
    }
}

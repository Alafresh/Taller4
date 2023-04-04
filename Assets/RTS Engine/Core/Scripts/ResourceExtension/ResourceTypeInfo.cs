using UnityEngine;

using RTSEngine.Audio;
using UnityEngine.Serialization;

namespace RTSEngine.ResourceExtension
{
    public enum ResourceCapacityType { simpleLimit = 0, slotBased = 1 }

    [CreateAssetMenu(fileName = "NewResourceType", menuName = "RTS Engine/Resource Type", order = 2)]
    public class ResourceTypeInfo : RTSEngineScriptableObject
    {
        [SerializeField, Tooltip("A unique name used to identify the resource type."), FormerlySerializedAs("_name")]
        private string key = "new_resource";
        public override string Key => key;

        [SerializeField, Tooltip("Name used to display the resource in UI elements.")]
        private string displayName = "Name";
        public string DisplayName => displayName;

        [SerializeField, TextArea, Tooltip("Short description used to display the resource in UI elements.")]
        private string description = "Name";
        public string Description => description;

        [Space(), SerializeField, Tooltip("Enable to make the resource type a capacity one where it will have a maximum amount (capacity) property in addition.")]
        private bool hasCapacity = false;
        public bool HasCapacity => hasCapacity;
        [SerializeField, Tooltip("If 'Has Capacity' is enabled, pick the type of the capacity resource in this field. 'Simple Limit' means that the resource amount will be capped/limited by a maximum capacity amount. 'Slot Based' means that the free amount (difference between the capacity and the current amount) is the value that is available to be filled/used, the 'Slot Based' capacity allows for having population slots as a resource type for example.")]
        private ResourceCapacityType capacityType = ResourceCapacityType.slotBased;
        public ResourceCapacityType CapacityType => capacityType;

        [SerializeField, Tooltip("Default starting amount/capacity of the resource for each faction in a game.")]
        private ResourceTypeValue startingAmount = new ResourceTypeValue();
        public ResourceTypeValue StartingAmount => startingAmount;

        [SerializeField, Tooltip("UI icon of the resource.")]
        private Sprite icon = null;
        public Sprite Icon => icon;
    }
}

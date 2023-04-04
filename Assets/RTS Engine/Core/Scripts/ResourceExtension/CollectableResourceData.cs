using RTSEngine.Animation;
using RTSEngine.Audio;
using RTSEngine.Effect;
using RTSEngine.Model;
using RTSEngine.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace RTSEngine.ResourceExtension
{
    [System.Serializable]
    public struct CollectableResourceData
    {
        [Tooltip("Type of the resource to collect.")]
        public ResourceTypeInfo type;

        [SerializeField, Tooltip("What audio clip to play when the unit is ordered by the local player to start collecting/dropping off a resource of the above type?")]
        public AudioClipFetcher orderAudio;

        [Space(), Tooltip("Amount of resources to be collected per progress OR maximum capacity of the collected resource.")]
        public int amount;

        [Space(), Tooltip("Allows to have a custom resource collection/drop off animaiton for the above resource type.")]
        public AnimatorOverrideControllerFetcher animatorOverrideController;

        [Space(), Tooltip("Child object of the collector that gets activated when the above resource type is being actively collected/dropped off."), Space(), FormerlySerializedAs("obj")]
        public ModelCacheAwareTransformInput enableObject;
        [SerializeField, Tooltip("What audio clip to play when the unit starts collecting/dropping off the resource?"), FormerlySerializedAs("audio")]
        public AudioClipFetcher enableAudio;

        [SerializeField, EnforceType(typeof(IEffectObject)), Tooltip("Triggered on the source faction entity when the component is in progress.")]
        public GameObjectToEffectObjectInput sourceEffect; 

        [SerializeField, EnforceType(typeof(IEffectObject)), Tooltip("Triggered on the target when the component is in progress.")]
        public GameObjectToEffectObjectInput targetEffect; 

        [Space(), SerializeField, Tooltip("What audio clip to play when the unit is actively collecting/dropping the resource?")]
        public AudioClipFetcher progressAudio;
        [SerializeField, Tooltip("Enable this option to allow the progress audio to loop during the duration where the unit is collecting and have it disabled to play the audio clip every time the collector gathers resource units.")]
        public bool loopProgressAudio;
    }
}

using UnityEngine;
using UnityEngine.Events;

using RTSEngine.Model;
using RTSEngine.Entities;

namespace RTSEngine.Health
{
    [System.Serializable]
    public class EntityHealthState
    {
        [SerializeField, Tooltip("When this option is enabled, the values entered in the 'Health Range' field below will be regarded as percentages with '0 -> 0% -> 0 Health Points' and '100 -> 100% -> Max Health Points'.")]
        private bool usePercentage = false;
        [SerializeField, Tooltip("The entity is considered in this state only if its health is inside this range.")]
        private IntRange healthRange = new IntRange(0, 100);

        public int UpperLimit => healthRange.max * (usePercentage ? healthComp.MaxHealth / 100 : 1);
        public int LowerLimit => healthRange.min * (usePercentage ? healthComp.MaxHealth / 100 : 1);

        // When 'upperBoundState' is set to true, it means that there is no other health state which has a higher health interval
        // In this case, we do not consider the upper bound of the interval
        public bool IsInRange(int value, bool upperBoundState = false) => value >= LowerLimit && (upperBoundState || value < UpperLimit);

        [SerializeField, Tooltip("Gameobjects to show when the entity is in this health state.")]
        private ModelCacheAwareTransformInput[] showChildObjects = new ModelCacheAwareTransformInput[0];

        [SerializeField, Tooltip("Gameobjects to hide when the entity is in this health state.")]
        private ModelCacheAwareTransformInput[] hideChildObjects = new ModelCacheAwareTransformInput[0];

        [SerializeField, Tooltip("Event(s) triggered when the entity enters this health state. Do not assign objects that are the entity model or any of its child objects. Due to the model caching system, the event invoke will not function properly. Insead please use custom scripts that access the entity model and its children objects using RTS Engine model cache aware field types such ModelCacheAwareTransformInput instead of Tranform.")]
        private UnityEvent triggerEvent = new UnityEvent();

        private IEntityHealth healthComp;

        public void Init(IEntity entity)
        {
            healthComp = entity.Health;

            foreach (ModelCacheAwareTransformInput obj in showChildObjects)
                if (!obj.IsValid())
                    RTSHelper.LoggingService.LogError($"[EntityHealthState - {entity.Code}] One of the entity health states assigned elements is either unassigned or assigned to an invalid child transform object!", source: entity);

            foreach (ModelCacheAwareTransformInput obj in hideChildObjects)
                if (!obj.IsValid())
                    RTSHelper.LoggingService.LogError($"[EntityHealthState - {entity.Code}] One of the entity health states assigned elements is either unassigned or assigned to an invalid child transform object!", source: entity);
        }

        public bool Toggle(bool enable)
        {
            foreach (ModelCacheAwareTransformInput obj in showChildObjects)
                if (obj.IsValid())
                    obj.IsActive = enable;

            foreach (ModelCacheAwareTransformInput obj in hideChildObjects)
                if (obj.IsValid())
                    obj.IsActive = !enable;

            if (enable)
                triggerEvent.Invoke();

            return true;
        }
    }
}

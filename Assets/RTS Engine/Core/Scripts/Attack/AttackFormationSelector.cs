using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Movement;
using RTSEngine.EntityComponent;
using UnityEngine.Assertions;
using RTSEngine.Search;
using RTSEngine.Game;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public class AttackFormationSelector
    {
        [SerializeField, Tooltip("Minimum and maximum stopping distance when targeting a unit.")]
        public FloatRange unitStoppingDistance = new FloatRange(2.0f, 6.0f);
        [SerializeField, Tooltip("Distance at which the attack gets enabled when the target is a unit entity, this value must fit in the min and max bounds of the unit stopping distance.")]
        public float unitEnableDistance = 2.0f;

        [Space(), SerializeField, Tooltip("Minimum and maximum stopping distance when targeting a building.")]
        private FloatRange buildingStoppingDistance = new FloatRange(5.0f, 10.0f); 

        [Space(), SerializeField, Tooltip("Minimum and maximum stopping distance when not targeting a specific target.")]
        private FloatRange noTargetStoppingDistance = new FloatRange(5.0f, 10.0f);

        [Space(), SerializeField, Tooltip("Enforce minimum stopping distance when engaging with a target.")]
        private bool enforceMinStoppingDistance = false;
        [SerializeField, Tooltip("The minimum stopping distance to enforce when engaging a target.")]
        private float minStoppingDistance = 0.5f;
        [SerializeField, Tooltip("Stop movement when the attack unit enters the valid range for engaging a target.")]
        private bool stopMovementOnTargetInRange = true;
        public bool StopMovementOnTargetInRange => stopMovementOnTargetInRange;

        [SerializeField, Tooltip("How far does the attack target need to move in order to recalculate the attacker's unit movement."), Min(0), Space()]
        private float updateMvtDistance = 2.0f; 

        [Space(), SerializeField, Tooltip("Attack movement formation for this unit type.")]
        private MovementFormationSelector movementFormation = new MovementFormationSelector { };
        public MovementFormationSelector MovementFormation => movementFormation;

        protected IGridSearchHandler gridSearch { private set; get; } 

        private IAttackComponent source;

        public void Init(IGameManager gameMgr, IAttackComponent source)
        {
            this.source = source;

            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();

            unitEnableDistance = Mathf.Clamp(unitEnableDistance, unitStoppingDistance.min, unitStoppingDistance.max);

            Assert.IsNotNull(MovementFormation.type,
                $"[UnitAttack - '{source.Entity.Code}'] The attack movement formation type must be assigned!");

            movementFormation.Init();
        }

        public bool MustUpdateAttackPosition (Vector3 lastTargetPosition, Vector3 currTargetPosition, Vector3 currAttackPosition, IFactionEntity target)
        {
            return Vector3.Distance(lastTargetPosition, currTargetPosition) > updateMvtDistance
                && Vector3.Distance(currTargetPosition, currAttackPosition) > GetStoppingDistance(target, min:false);
        }

        /// <summary>
        /// Get the appropriate stopping distance for an attack depending on the target type.
        /// </summary>
        /// <param name="target">FactionEntity instance that represents the potential target for the unit.</param>
        /// <param name="min">True to get the minimum value of the stopping range and false to get the maximum value of the stopping range.</param>
        /// <returns>Stopping distance for the unit's movement to launch an attack.</returns>
        public float GetStoppingDistance (IEntity target, bool min = true, bool onProgressEnableTest = false)
        {
            float stoppingDistance;

            EntityType targetType = target.IsValid() ? target.Type : EntityType.all;
            if(target.IsValid())
            {
                if (target.IsUnit())
                    stoppingDistance = onProgressEnableTest
                        ? unitEnableDistance
                        : (min ? unitStoppingDistance.min : unitStoppingDistance.max);
                else if (target.IsBuilding())
                    stoppingDistance = min ? buildingStoppingDistance.min : buildingStoppingDistance.max;
                else
                    stoppingDistance = min ? noTargetStoppingDistance.min : noTargetStoppingDistance.max;
            }
            else
                stoppingDistance = min ? noTargetStoppingDistance.min : noTargetStoppingDistance.max;

            return stoppingDistance + (target.IsValid() ? target.Radius : 0.0f);
        }

        public bool IsTargetInRange (Vector3 attackPosition, TargetData<IFactionEntity> target, bool onProgressEnableTest = false, bool debug = false)
        {
            float distance = Vector3.Distance(attackPosition, RTSHelper.GetAttackTargetPosition(target));

            return distance <= GetStoppingDistance(target.instance, min: false, onProgressEnableTest)
                && (!enforceMinStoppingDistance || distance >= Mathf.Min(GetStoppingDistance(target.instance, min: true), minStoppingDistance))
                && gridSearch.IsPositionReserved(source.Entity.transform.position, source.Entity.Radius, source.Entity.MovementComponent.AreasMask, playerCommand: false, ignoreMarker: source.Entity.MovementComponent.TargetPositionMarker) == ErrorMessage.none;
        }
    }
}

using UnityEngine;
using UnityEngine.AI;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Event;
using System.Collections;
using System;

namespace RTSEngine.Movement
{
    public class NavMeshAgentController : MonoBehaviour, IMovementController
    {
        #region Attributes
        public bool Enabled
        {
            set { navAgent.enabled = value; }
            get => navAgent.enabled;
        }

        public bool IsActive
        {
            set
            {
                if (navAgent.isOnNavMesh)
                    navAgent.isStopped = !value;
            }
            get => !navAgent.isStopped;
        }

        private NavMeshAgent navAgent; 
        private NavMeshPath navPath;

        private MovementControllerData data;

        // Used to save the NavMeshAgent velocity to re-assign it when the game is paused and the unit resumes movement
        private Vector3 cachedVelocity;

        public MovementControllerData Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;

                navAgent.speed = data.speed;

                navAgent.acceleration = data.acceleration;

                navAgent.angularSpeed = data.angularSpeed;

                navAgent.stoppingDistance = data.stoppingDistance;

                if (!navAgent.isOnNavMesh)
                    return;

                // If the speed value is positive and the movement was stopped (due to a game pause for example) before this assignment
                if (navAgent.speed > 0)
                {
                    if (navAgent.isStopped)
                    {
                        navAgent.velocity = cachedVelocity; // Assign velocity before the isStopped is enabled.
                        navAgent.isStopped = false; // Enable movement
                    }
                }
                else
                {
                    if (!navAgent.isStopped)
                    {
                        cachedVelocity = navAgent.velocity; // Cache current velocity of unit. 
                        navAgent.isStopped = true; // Disable movement
                        navAgent.velocity = Vector3.zero; // Nullify velocity to stop any in progress movement
                    }
                }
            }
        }

        public LayerMask NavigationAreaMask => navAgent.areaMask;

        public float Radius => navAgent.radius;

        public Vector3 NextPathTarget => navAgent.steeringTarget;

        public MovementSource LastSource { get; private set; }

        public Vector3 Destination => navAgent.destination;

        // Game services
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; } 

        // Other components
        protected IGameManager gameMgr { private set; get; }
        protected IMovementComponent mvtComponent { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, IMovementComponent mvtComponent, MovementControllerData data)
        {
            this.gameMgr = gameMgr;
            this.mvtComponent = mvtComponent;

            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            IEntity entity = mvtComponent?.Entity;
            if (!logger.RequireValid(entity
                , $"[{GetType().Name}] Can not initialize without a valid Unit instance."))
                return;

            navAgent = entity.gameObject.GetComponent<NavMeshAgent>();
            if (!logger.RequireValid(navAgent,
                $"[{GetType().Name} - '{entity.Code}'] '{typeof(NavMeshAgent).Name}' component must be attached to the unit."))
                return;
            navAgent.enabled = true;

            this.Data = data;

            navPath = new NavMeshPath();

            // Always set to none as Navmesh's obstacle avoidance desyncs multiplayer game since it is far from deterministic
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            // Make sure the NavMeshAgent component updates our unit's position when active.
            navAgent.updatePosition = true;

            mvtComponent.MovementStart += HandleMovementStart;
            mvtComponent.MovementStop += HandleMovementStop;
        }

        public void Disable()
        {
            mvtComponent.MovementStart -= HandleMovementStart;
            mvtComponent.MovementStop -= HandleMovementStop;
        }
        #endregion

        #region Preparing/Launching Movement
        public void Prepare(Vector3 destination, MovementSource source)
        {
            this.LastSource = source;

            navAgent.CalculatePath(destination, navPath);

            if (navPath != null && navPath.status == NavMeshPathStatus.PathComplete)
                mvtComponent.OnPathPrepared(LastSource);
            else
            {
                this.LastSource = new MovementSource();
                mvtComponent.OnPathFailure();
            }
        }

        public void Launch ()
        {
            IsActive = true;

            navAgent.SetPath(navPath);
        }
        #endregion

        #region Handling Movement Stop
        // When movement is stopped, it can stop and the velocity of the agent is still non-zero
        // When this happens, the unit will continue moving for a bit more before it fully stops
        // The logic below allows to launch a coroutine that keeps resetting the marker position until the velocity hits 0 and the unit fully stops
        private void HandleMovementStart(IMovementComponent sender, MovementEventArgs args)
        {
            if (markerResetPositionCoroutine != null)
                StopCoroutine(markerResetPositionCoroutine);
        }

        private void HandleMovementStop(IMovementComponent sender, EventArgs args)
        {
            markerResetPositionCoroutine = StartCoroutine(MarkerResetPositionCoroutine());
        }

        private Coroutine markerResetPositionCoroutine;
        private const float markerResetPositionDelay = 0.1f;
        private IEnumerator MarkerResetPositionCoroutine()
        {
            while(true)
            {
                if (navAgent.velocity == Vector3.zero)
                    yield break;

                yield return new WaitForSeconds(markerResetPositionDelay);

                mvtComponent.TargetPositionMarker.Toggle(true, mvtComponent.Entity.transform.position);
            }
        }
        #endregion
    }
}

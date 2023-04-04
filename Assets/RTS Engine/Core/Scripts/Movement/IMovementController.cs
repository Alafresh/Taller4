using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Game;

namespace RTSEngine.Movement
{
    public interface IMovementController : IMonoBehaviour
    {
        /// <summary>
        /// When set to false, the unit is not expected to handle movement path calculations and pathfinding.
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Used by the IMovementComponent instance to handle stopping or pausing pathfinding for the unit, in the case where the game is paused for example.
        /// If Enabled is set to false, enabling this would not have any effect.
        /// </summary>
        bool IsActive { set; get; }

        /// <summary>
        /// Get or set the defining properties of the movement controller such as speed, acceleration and stopping distance. 
        /// </summary>
        MovementControllerData Data { set;  get; }

        /// <summary>
        /// The navigation mesh area mask in which the unit can move.
        /// </summary>
        LayerMask NavigationAreaMask { get; }

        /// <summary>
        /// The size that the unit occupies in the navigation mesh while moving.
        /// </summary>
        float Radius { get; }

        /// <summary>
        /// The position of the next corner of the unit's active path.
        /// </summary>
        Vector3 NextPathTarget { get; }

        /// <summary>
        /// Holds the last movement source data of the last successful movement for which a successful path has been generated.
        /// </summary>
        MovementSource LastSource { get; }

        /// <summary>
        /// The current target position that the unit is moving towards. This is used to determine when the movement should stop when the unit approaches its destination.
        /// </summary>
        Vector3 Destination { get; } 

        /// <summary>
        /// Initialization method called by the IMovementComponent instance to set the initial properties of the movement controller.
        /// </summary>
        /// <param name="gameMgr">IGameManager instance of the currently active game.</param>
        /// <param name="mvtComponent">IMovementComponent instance of the unit that called the initialization method.</param>
        /// <param name="data">Initial movement controller data including speed, acceleration and stopping distance values.</param>
        void Init(IGameManager gameMgr, IMovementComponent mvtComponent, MovementControllerData data);

        /// <summary>
        /// Called by the IMovementComponent instance when the unit is disabled.
        /// </summary>
        void Disable();

        /// <summary>
        /// Attempts to calculate a valid path for the specified destination position.
        /// If the path calculation is successful, you need to call OnPathPrepared() on the IMovementComponent instance of the unit and provide the same MovementSource data as its sole parameter.
        /// While you have the option to modify the MovementSource struct when calling OnPathPrepared(), it is recommended to pass the same back.
        /// If the path calculation failed, you need to call OnPathFailure() on the IMovementComponent instance of the unit.
        /// You do not have to make either of those two calls directly through this method. Path calculation might take some time to finish so you can always call either of those two methods later.
        /// However, make sure that one of them is eventually called as the movement component will be expecting a response after it attempts to preapre a path for the movement.
        /// </summary>
        /// <param name="destination">Vector3 that represents the movement's target position.</param>
        void Prepare(Vector3 destination, MovementSource source);

        /// <summary>
        /// Starts the unit movement using the last calculated path from the Prepare() method.
        /// </summary>
        void Launch();
    }
}

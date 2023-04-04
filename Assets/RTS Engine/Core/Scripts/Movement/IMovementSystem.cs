using RTSEngine.Event;
using System;
using UnityEngine;

namespace RTSEngine.Movement
{
    public interface IMovementSystem : IMonoBehaviour
    {
        event CustomEventHandler<IMovementSystem, EventArgs> GraphUpdated;
        void RaiseGraphUpdated();

        /// <summary>
        /// Attempt to get a valid position that can be used as a movement path destination by sampling an area.
        /// </summary>
        /// <param name="center">Center of the area to sample.</param>
        /// <param name="radius">Radius of the area to sample.</param>
        /// <param name="areaMask">Area mask of the area to sample. This is the navigation area mask and not the terrain area mask!</param>
        /// <param name="validPosition">If a valid movement destination is found, it would be available in this parameter.</param>
        /// <returns>True if there is a valid movement path destination in the area to sample. Otherwise, false.</returns>
        bool TryGetValidPosition(Vector3 center, float radius, int areaMask, out Vector3 validPosition);
    }
}

using RTSEngine.Event;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace RTSEngine.Movement
{
    public class NavmeshMovementSystem : MonoBehaviour, IMovementSystem
    {
        #region Raising Event: Graph Updated
        public event CustomEventHandler<IMovementSystem, EventArgs> GraphUpdated;

        public void RaiseGraphUpdated()
        {
            var handler = GraphUpdated;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Helper Functions
        public bool TryGetValidPosition(Vector3 center, float radius, int areaMask, out Vector3 validPosition)
        {
            if (NavMesh.SamplePosition(center, out NavMeshHit hit, radius, areaMask))
            {
                validPosition = hit.position;
                return true;
            }

            validPosition = center;
            return false;
        }
        #endregion
    }
}

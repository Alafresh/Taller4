using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Movement;

namespace RTSEngine.Entities
{
    public class BaseEntityObstacle<T> : MonoBehaviour, IEntityFullInitializable, IEntityObstacleHandler where T : Behaviour
    {
        #region Attributes
        protected IEntity entity { private set; get; }
        private T[] obstacles = new T[0];
        public IEnumerable<T> Obstacles => obstacles;

        protected IGameLoggingService logger { private set; get; }
        protected IMovementSystem mvtSystem { private set; get; }
        protected IGameManager gameMgr { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.gameMgr = gameMgr;
            this.logger = gameMgr.GetService<IGameLoggingService>(); 
            mvtSystem = gameMgr.GetService<IMovementManager>().MvtSystem;

            this.entity = entity;
            if (!logger.RequireValid(this.entity,
                $"[{GetType().Name}] This component can only be attached to an object where a component that extends '{typeof(IBuilding).Name}' interface is attached!"))
                return;

            obstacles = this.entity.gameObject.GetComponentsInChildren<T>();

            if (!logger.RequireValid(obstacles,
                $"[{GetType().Name} - {this.entity.Code}] A component of type '{typeof(T).Name}' must be attached to the building!"))
                return;

            SetActive(false);

            OnPreInit();
        }

        protected virtual void OnPreInit() { }

        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            SetActive(true);

            OnPostInit();

            mvtSystem.RaiseGraphUpdated();
        }

        protected virtual void OnPostInit() { }

        public virtual void Disable() { 
            OnDead();
            mvtSystem.RaiseGraphUpdated();
        }
        protected virtual void OnDead()
        {
        }
        #endregion

        #region Handling Active Status
        public void SetActive(bool enable)
        {
            foreach (T obstacle in obstacles)
                obstacle.enabled = enable;
        }
        #endregion
    }
}

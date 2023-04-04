using System;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Utilities;
using RTSEngine.Health;
using System.Collections.Generic;

namespace RTSEngine.UI
{
    public class HoverHealthBarUIHandler : ObjectPool<HoverHealthBar, PoolableObjectSpawnInput>, IPreRunGameService
    {
        #region Attributes
        [SerializeField, Tooltip("Enable or disable showing health bars when the player hovers the mouse over an entity in the game.")]
        private bool isActive = true;

        [SerializeField, EnforceType(prefabOnly: true), Tooltip("Hover health bar prefab object that includes the 'HoverHelathBar' component")]
        private HoverHealthBar prefab = null;

        public enum HoverHealthBarMode { always, onMouseHover, onSelection, onMouseHoverOrSelection }
        [SerializeField, Tooltip("Choose whether health bars are shown when always, when the local player places their mouse over an entity, when the local player selects an entity or the last two together.")]
        private HoverHealthBarMode enableMode = HoverHealthBarMode.always;
        private Dictionary<IEntity, HoverHealthBar> activeHealthBars;

        [SerializeField, Tooltip("Enable to only display the hover health bar for the player faction's units and buildings.")]
        private bool playerFactionOnly = true;

        [SerializeField, Tooltip("What types of entites are allowed to have a hover health bar.")]
        private EntityType allowedEntityTypes = EntityType.all;

        // Game services
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnObjectPoolInit()
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

            if (!isActive)
                return;

            if (!prefab.IsValid())
                logger.LogError($"[{GetType().Name}] The 'Prefab' field must be assigned", source: this);

            activeHealthBars = new Dictionary<IEntity, HoverHealthBar>();

            if (enableMode == HoverHealthBarMode.onMouseHover
                || enableMode == HoverHealthBarMode.onMouseHoverOrSelection)
            {
                globalEvent.EntityMouseEnterGlobal += HandleEntityMouseEnterGlobal;
                globalEvent.EntityMouseExitGlobal += HandleEntityMouseExitGlobal;
            }
            
            if (enableMode == HoverHealthBarMode.onSelection
                || enableMode == HoverHealthBarMode.onMouseHoverOrSelection)
            {
                globalEvent.EntitySelectedGlobal += HandleEntitySelectedGlobal;
                globalEvent.EntityDeselectedGlobal += HandleEntityDeselectedGlobal;
            }

            if(enableMode == HoverHealthBarMode.always)
            {
                globalEvent.EntityInitiatedGlobal += HandleEntityInitiatedGlobal;
            }
        }

        private void OnDestroy ()
        {
            globalEvent.EntityMouseEnterGlobal -= HandleEntityMouseEnterGlobal;
            globalEvent.EntityMouseExitGlobal -= HandleEntityMouseExitGlobal;

            globalEvent.EntitySelectedGlobal -= HandleEntitySelectedGlobal;
            globalEvent.EntityDeselectedGlobal -= HandleEntityDeselectedGlobal;

            globalEvent.EntityInitiatedGlobal -= HandleEntityInitiatedGlobal;
        }
        #endregion

        #region Handling Events
        private void HandleEntitySelectedGlobal(IEntity entity, EntitySelectionEventArgs args)
        {
            Enable(entity);
        }

        private void HandleEntityDeselectedGlobal(IEntity entity, EventArgs args)
        {
            Hide(entity);
        }

        private void HandleEntityInitiatedGlobal(IEntity source, EventArgs args)
        {
            Enable(source);
        }

        private void HandleEntityMouseEnterGlobal(IEntity entity, EventArgs e) => Enable(entity);
        private void HandleEntityMouseExitGlobal(IEntity entity, EventArgs e)
        {
            if (enableMode == HoverHealthBarMode.onMouseHoverOrSelection
                && entity.Selection.IsSelected)
                return;

            Hide(entity);
        }
        #endregion

        #region Enabling/Disabling Hover Health Bar
        public bool CanEnable(IEntity source)
        {
            return isActive
                && source.IsValid()
                && !source.IsDummy
                && source.IsEntityTypeMatch(allowedEntityTypes)
                && (!playerFactionOnly || source.IsLocalPlayerFaction())
                && source.Health.HoverHealthBarData.enabled
                && !activeHealthBars.ContainsKey(source);
        }

        private void Enable(IEntity source)
        {
            if (!CanEnable(source))
                return;

            HoverHealthBarData nextData = source.Health.HoverHealthBarData;
            if (nextData.offset.y == -1.0f)
                nextData.offset = new Vector3(0.0f, source.Health.HoverHealthBarY, 0.0f);

            HoverHealthBar nextHealthBar = Spawn(prefab, new HoverHealthBarSpawnInput(source, nextData));

            activeHealthBars.Add(source, nextHealthBar);
        }

        private void Hide (IEntity source)
        {
            if (!activeHealthBars.TryGetValue(source, out HoverHealthBar nextHealthBar))
                return;

            Despawn(nextHealthBar);
            activeHealthBars.Remove(source);
        }

        public HoverHealthBar Spawn(HoverHealthBar prefab, HoverHealthBarSpawnInput input)
        {
            HoverHealthBar nextHealthBar = base.Spawn(prefab);
            if (!nextHealthBar.IsValid())
                return null;

            nextHealthBar.OnSpawn(input);

            return nextHealthBar;
        }
        #endregion
    }
}

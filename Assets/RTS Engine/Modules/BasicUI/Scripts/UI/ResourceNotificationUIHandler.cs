using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.ResourceExtension;
using RTSEngine.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.UI
{
    public class ResourceNotificationUIHandler : ObjectPool<ResourceNotification, ResourceNotificationSpawnInput>, IPreRunGameService
    {
        #region Attributes
        [SerializeField, EnforceType(prefabOnly: true), Tooltip("Prefab object that handles showing the resource UI notification.")]
        private ResourceNotification prefab = null;

        [SerializeField, Tooltip("Enable to only display the resource notifications for the player faction's entities.")]
        private bool playerFactionOnly = true;

        [SerializeField, Tooltip("Show resource UI notifications when a unit drops resources to indicate the type of resource added at the drop point.")]
        private bool trackResourceDropOff = true;
        [SerializeField, Tooltip("Show resource UI notifications when a resource generator adds resources to the faction it belongs to.")]
        private bool trackResourceGenerator = true;

        [SerializeField, Tooltip("Define what faction entities are allowed to have the resource notification (which resource collectors and resource generators).")]
        private EntityTargetPicker picker;

        // Game services
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnObjectPoolInit()
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

            if (!logger.RequireValid(prefab,
              $"[{GetType().Name}] The 'Prefab' field must be assigned"))
                return; 

            if (trackResourceDropOff)
                globalEvent.UnitResourceDropOffCompleteGlobal += HandleUnitResourceDropOffCompleteGlobal;

            if (trackResourceGenerator)
                globalEvent.ResourceGeneratorCollectedGlobal += HandleResourceGeneratorCollectedGlobal;
        }

        private void OnDestroy ()
        {
            globalEvent.UnitResourceDropOffCompleteGlobal -= HandleUnitResourceDropOffCompleteGlobal;

            globalEvent.ResourceGeneratorCollectedGlobal -= HandleResourceGeneratorCollectedGlobal;
        }

        private void HandleResourceGeneratorCollectedGlobal(IResourceGenerator generator, ResourceAmountEventArgs args)
        {
            if ((playerFactionOnly && !generator.FactionEntity.IsLocalPlayerFaction())
                || !picker.IsValidTarget(generator.FactionEntity))
                return;

            Spawn(prefab,
                new ResourceNotificationSpawnInput(
                    generator.FactionEntity,
                    args.ResourceInput,
                    generator.FactionEntity.transform.position + generator.FactionEntity.Health.HoverHealthBarData.offset)
                );
        }
        #endregion

        private void HandleUnitResourceDropOffCompleteGlobal(IEntity entity, ResourceAmountEventArgs args)
        {
            if ((playerFactionOnly && !entity.IsLocalPlayerFaction())
                || !picker.IsValidTarget(entity))
                return;


            Spawn(prefab,
                new ResourceNotificationSpawnInput(
                    entity,
                    args.ResourceInput,
                    entity.transform.position + entity.Health.HoverHealthBarData.offset)
                );
        }

        public ResourceNotification Spawn(ResourceNotification prefab, ResourceNotificationSpawnInput input)
        {
            ResourceNotification nextResourceNotif = base.Spawn(prefab);
            if (!nextResourceNotif.IsValid())
                return null;

            nextResourceNotif.OnSpawn(input);

            return nextResourceNotif;
        }
    }
}

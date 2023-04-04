using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Determinism;
using RTSEngine.Utilities;

namespace RTSEngine.NPC
{
    public abstract class NPCComponentBase : MonoBehaviour, INPCComponent
    {
        #region Attributes 
        // EDITOR ONLY
        [HideInInspector]
        public Int2D tabID = new Int2D { x = 0, y = 0 };

        protected INPCManager npcMgr { private set; get; }
        protected IFactionManager factionMgr { private set; get; }
        protected IFactionSlot factionSlot { private set; get; }
        protected IGameManager gameMgr { private set; get; }

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IInputManager inputMgr { private set; get; } 

        [SerializeField, ReadOnly, Space(), Tooltip("Activity status of the NPC component.")]
        private bool isActive = false;
        public bool IsActive { 
            protected set 
            {
                isActive = value;

                if (isActive)
                {
                    OnActivtated();
                }
                else
                {
                    OnDeactivated();
                }
            }
            get => isActive;
        }

        public virtual bool IsSingleInstance => true;
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, INPCManager npcMgr)
        {
            this.gameMgr = gameMgr;

            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.inputMgr = gameMgr.GetService<IInputManager>(); 

            this.npcMgr = npcMgr; 
            this.factionMgr = npcMgr.FactionMgr;
            this.factionSlot = factionMgr.Slot;

            OnPreInit();

            this.npcMgr.InitComplete += HandleNPCFactionInitComplete;
        }

        /// <summary>
        /// Called when the INPCManager instance first initializes the INPCComponent instance.
        /// </summary>
        protected virtual void OnPreInit() { }

        private void HandleNPCFactionInitComplete(INPCManager npcManager, EventArgs args)
        {
            OnPostInit();

            this.npcMgr.InitComplete -= HandleNPCFactionInitComplete;
        }

        /// <summary>
        /// Called after all INPCComponent instances have been cached and initialized by the INPCManager instance.
        /// </summary>
        protected virtual void OnPostInit() { }

        private void OnDestroy()
        {
            this.npcMgr.InitComplete -= HandleNPCFactionInitComplete;

            OnDestroyed();
        }

        protected virtual void OnDestroyed() { }
        #endregion

        #region Activating/Deactivating
        protected virtual void OnActivtated() { }

        protected virtual void OnDeactivated() { }
        #endregion

        #region Updating Component
        private void Update()
        {
            if(ShowActiveLogs)
                UpdateActiveLogs();

            if (!IsActive)
                return;

            OnActiveUpdate();
        }

        protected virtual void OnActiveUpdate () { }
        #endregion

        #region Logs
        // For NPC components, there is two types of logs:
        // Event Logs: Logs that show active entities/objects that the NPC is managing
        // Active Logs: Text logs that show the decisions that the NPC is taking with time stamps

        // Event Logs
        [SerializeField, Tooltip("Enable logging to record logs of the events taken by this NPC component which will be logged in the 'Event Logs' field in the inspector. You can also retrieve these event logs from the 'EventLogs' property.")]
        private bool logEvents = false;
        [SerializeField, ReadOnly]
        private List<string> eventLogs = new List<string>();
        public IReadOnlyList<string> EventLogs => eventLogs;
        public const int EVENT_LOGS_MAX_SIZE = 50;

        public void LogEvent(string newEvent)
        {
            if (!logEvents)
                    return;

            eventLogs.Add($"[{Time.time}] {newEvent}");
            if (eventLogs.Count > EVENT_LOGS_MAX_SIZE)
                eventLogs.RemoveAt(0);
        }

        // Active Logs
        [SerializeField, Tooltip("Enable to allow to update logs on the inspector of the NPC component.")]
        private bool showActiveLogs = false;
        protected bool ShowActiveLogs => showActiveLogs;

        protected virtual void UpdateActiveLogs()
        {
        }
        #endregion
    }
}

using System;

using UnityEngine;

using RTSEngine.Game;

namespace RTSEngine.Determinism
{
    [System.Serializable]
    public class GlobalTimeModifiedTimer : TimeModifiedTimer
    {
        [SerializeField, Tooltip("Enable/disable .")]
        private bool enabled = false;

        [SerializeField, Tooltip("Default timer duration fetched randomly from a range of values.")]
        private FloatRange defaultValueRange = new FloatRange(1.0f, 2.0f);
        // When the timer is initialized using the default value range above, it would reload its duration by sampling randomly from the range everytime.
        private bool reloadWithDefaultValueRange = false;

        private bool isActive = false;
        public bool IsActive
        {
            set
            {
                if (!enabled)
                    return;

                if (!IsInitialized)
                    RTSHelper.TryGameInitPostStart(Init);

                // If we are activating the timer again while it was already active then disable it first to reload it
                if (isActive && value == true)
                    timeModifier.RemoveTimer(this);

                isActive = value;

                if (isActive)
                {
                    // To set the CurrValue.
                    if (reloadWithDefaultValueRange)
                        Reload(defaultValueRange);
                    else
                        Reload();

                    // To run the timer
                    timeModifier.AddTimer(this, removalCallback);
                }
                else
                    timeModifier.RemoveTimer(this);
            }
            get
            {
                if (!enabled)
                    return false;

                if (!IsInitialized)
                    RTSHelper.TryGameInitPostStart(Init);

                return CurrValue > 0.0f;
            }
        }

        public bool IsInitialized { private set; get; } = false;

        // callback called when the timer is removed.
        private Action removalCallback;

        // Game services
        protected ITimeModifier timeModifier { private set; get; } 

        // We want to start the cooldown timer with a CurrValue of 0.0 so the timer is inactive by default
        public GlobalTimeModifiedTimer(bool enabled = false) : base() 
        {
            this.enabled = enabled;
        }

        public void Init(IGameManager gameMgr, Action timerRemovedCallback)
        {
            reloadWithDefaultValueRange = true;
            Init(gameMgr, timerRemovedCallback, defaultValueRange.RandomValue);
        }

        public void Init(IGameManager gameMgr, Action timerRemovedCallback, float defaultValue)
        {
            this.removalCallback = timerRemovedCallback;

            this.timeModifier = gameMgr.GetService<ITimeModifier>();
            this.DefaultValue = defaultValue;

            IsInitialized = true;
        }

        public void Init(IGameManager gameMgr) => Init(gameMgr, null);

        public void SetDefaultValueRange(FloatRange newDefaultValueRange)
        {
            defaultValueRange = newDefaultValueRange;
        }
    }
}

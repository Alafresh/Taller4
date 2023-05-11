using System.Collections.Generic;
using System.Linq;
using System;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Game;

namespace RTSEngine.Save.Game.Service
{
    [Serializable]
    public struct TimeModifierOptionSaveData
    {
        public string name;
        public float modifier;
    }

    [Serializable]
    public class TimeModifierSaveData : PreRunGameServiceSaveDataBase<ITimeModifier>
    {
        [SerializeField]
        private TimeModifierOptionSaveData[] options;
        public IEnumerable<TimeModifierOptionSaveData> Options => options;

        [SerializeField]
        private float currentModifier;

        public TimeModifierSaveData(ITimeModifier component)
        {
            currentModifier = TimeModifier.CurrentModifier > 0.0f 
                ? TimeModifier.CurrentModifier
                : component.Options.values[component.CurrOptionID].modifier;

            options = component.Options.values.Select(option => new TimeModifierOptionSaveData
            {
                name = option.name,
                modifier = option.modifier
            }).ToArray();
        }

        public override void OnPreEntitySpawnLoad(IGameManager gameMgr)
        {
            component.SetModifierLocal(currentModifier, playerCommand: false);
        }
    }
}

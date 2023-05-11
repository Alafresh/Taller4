using System.Collections.Generic;

using RTSEngine.Determinism;
using RTSEngine.Faction;
using RTSEngine.Game;

namespace RTSEngine.Save.Loader
{
    public struct SavedGameSlotData
    {
        public string filePath;

        public string name;
        public string date;

        public string sceneName;

        public DefeatConditionType defeatCondition;

        public IEnumerable<TimeModifierOption> timeModifierOptions;

        public IReadOnlyList<FactionSlotData> factionSlotData;
    }
}

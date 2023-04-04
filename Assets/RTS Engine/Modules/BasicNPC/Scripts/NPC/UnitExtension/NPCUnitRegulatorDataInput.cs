using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Faction;

namespace RTSEngine.NPC.UnitExtension
{
    [RequireComponent(typeof(IUnit))]
    public class NPCUnitRegulatorDataInput : MonoBehaviour
    {
        [SerializeField, Tooltip("When no matches are found in the Type Specific field, then this regulator data will be used by the NPC faction.")]
        private NPCUnitRegulatorData allTypes = null;

        [System.Serializable]
        public struct InputElement
        {
            [Tooltip("Regulator data asset file to be used with the faction type and NPC type configurations below.")]
            public NPCUnitRegulatorData regulatorData;

            [Tooltip("What faction types must the NPC faction have in order to consider using the assigned regulator data?")]
            public FactionTypeTargetPicker factionTypes;

            [Tooltip("What NPC types must the NPC faction have in order to consider using the assigned regulator data?")]
            public NPCTypeTargetPicker npcTypes;
        }
        [SerializeField, Tooltip("Define regulator data asset files to be used with specific faction and NPC types.")]
        private InputElement[] typeSpecific = new InputElement[0];

        public NPCUnitRegulatorData GetFiltered(FactionTypeInfo factionType, NPCType npcType)
        {
            NPCUnitRegulatorData filtered = allTypes;

            foreach (InputElement nextElement in typeSpecific)
                if(nextElement.factionTypes.IsValidTarget(factionType)
                    && nextElement.npcTypes.IsValidTarget(npcType))
                {
                    filtered = nextElement.regulatorData;
                    break;
                }

            return filtered;
        }
    }
}

using RTSEngine.Entities;
using System.Collections.Generic;

namespace RTSEngine.NPC
{
    [System.Serializable]
    public class NPCTypeTargetPicker : TargetPicker<NPCType, List<NPCType>>
    {
        /// <summary>
        /// Is the NPCType instance defined as a valid entry.
        /// </summary>
        /// <param name="npcType">NPCType instance to test.</param>
        /// <returns>True if the input NPCType instance is defined as valid entry in this target picker.</returns>
        protected override bool IsInList(NPCType npcType)
        {
            foreach (NPCType element in options)
                if (element == npcType)
                    return true;

            return false;
        }

    }
}

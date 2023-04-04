using RTSEngine.Game;
using System.Collections.Generic;

namespace RTSEngine.NPC
{
    public interface INPCComponent : IMonoBehaviour
    {
        // When true, the component implementing the interface would only be allowed to have one instance per NPC faction
        bool IsSingleInstance { get; }

        bool IsActive { get; }

        void Init(IGameManager gameMgr, INPCManager npcMgr);

        IReadOnlyList<string> EventLogs { get; }
    }
}

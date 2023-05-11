using RTSEngine.Game;

namespace RTSEngine.Save.Game.Service
{
    public interface IGameServiceSaveData
    {
        void OnPreLoad(IGameManager gameMgr);

        void OnPreEntitySpawnLoad(IGameManager gameMgr);
        void OnEntitySpawnLoad(IGameManager gameMgr);
        void OnPostEntitySpawnLoad(IGameManager gameMgr);
    }
}

using RTSEngine.Determinism;
using RTSEngine.Game;

namespace RTSEngine.Save.Game.Service
{
    public abstract class PostRunGameServiceSaveDataBase<T> : GameServiceSaveDataBase<T> where T : IPostRunGameService
    {
        protected T component { private set; get; }
        public sealed override void OnPreLoad(IGameManager gameMgr)
        {
            component = gameMgr.GetService<T>();
        }
    }

    public abstract class PreRunGameServiceSaveDataBase<T> : GameServiceSaveDataBase<T> where T : IPreRunGameService
    {
        protected T component { private set; get; }
        public sealed override void OnPreLoad(IGameManager gameMgr)
        {
            component = gameMgr.GetService<T>();
        }
    }

    public abstract class GameServiceSaveDataBase<T> : IGameServiceSaveData where T : IGameService
    {
        public virtual void OnPreLoad(IGameManager gameMgr)
        {
        }

        public virtual void OnPreEntitySpawnLoad(IGameManager gameMgr)
        {
        }

        public virtual void OnEntitySpawnLoad(IGameManager gameMgr)
        {
        }

        public virtual void OnPostEntitySpawnLoad(IGameManager gameMgr)
        {
        }
    }
}

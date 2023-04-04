using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.Selection
{
    public interface IMouseSelector : IPreRunGameService
    {
        bool MultipleSelectionKeyDown { get; }
        string EntitySelectionLayer { get; }


        void SelectEntitisInRange(IEntity source, bool playerCommand);

        void FlashSelection(IEntity entity, bool isFriendly);

        bool Unlock(IGameService service);
        bool Lock(IGameService service);
    }
}
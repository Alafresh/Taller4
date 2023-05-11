using RTSEngine.Game;
using RTSEngine.Save.IO;

namespace RTSEngine.Save.Game
{
    public interface ISaveManager : IPreRunGameService
    {
        IOSaveErrorMessage OnSave(string saveName);
    }
}

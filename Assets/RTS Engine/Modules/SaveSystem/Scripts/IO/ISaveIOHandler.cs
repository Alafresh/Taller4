using System;
using System.Collections.Generic;
using RTSEngine.Game;
using RTSEngine.Save.Game;
using RTSEngine.Save.Loader;

namespace RTSEngine.Save.IO
{

    public interface ISaveIOHandler : IPreRunGameService, IGameLoaderService
    {
        IOSaveErrorMessage Save(GameSaveData gameSaveData, IOSaveOptions saveOptions);

        GameSaveData Load(string saveName);

        IReadOnlyDictionary<string, GameSaveData> LoadAll();
        IReadOnlyDictionary<string, GameSaveData> LoadAll(Func<GameSaveData, bool> filter);

        bool Delete(string saveName);
    }
}
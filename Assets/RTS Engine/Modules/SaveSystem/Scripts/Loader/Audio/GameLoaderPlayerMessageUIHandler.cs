using RTSEngine.Save.Loader.Audio;
using RTSEngine.Save.Loader.Logging;
using RTSEngine.UI;
using RTSEngine.UI.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Save.Loader.UI
{
    public interface IGameLoaderPlayerMessageUIHandler : IGameLoaderService
    {
        ITextMessage Message { get; }
    }

    public class GameLoaderPlayerMessageUIHandler : PlayerMessageUIHandlerBase, IGameLoaderPlayerMessageUIHandler
    {
        #region Initializing/Terminating
        public void Init(IGameLoader gameLoader)
        {
            InitBase(logger: gameLoader.GetService<IGameLoaderLoggingService>(),
                audioMgr: gameLoader.GetService<IGameLoaderAudioManager>());
        }
        #endregion

    }
}

using RTSEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Save.Loader.Audio
{
    public interface IGameLoaderAudioManager : IGameLoaderService, IAudioManager
    {

    }

    public class GameLoaderAudioManager : AudioManagerBase, IGameLoaderAudioManager
    {
        protected IGameLoader loader { get; private set; }

        public void Init(IGameLoader loader)
        {
            this.loader = loader;
        }
    }
}

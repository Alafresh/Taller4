using RTSEngine.Logging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Save.Loader.Logging
{
    public class GameLoaderLogger : LoggerBase, IGameLoaderLoggingService
    {
        protected IGameLoader loader { get; private set; }

        public void Init(IGameLoader loader)
        {
            this.loader = loader;
        }
    }
}
    

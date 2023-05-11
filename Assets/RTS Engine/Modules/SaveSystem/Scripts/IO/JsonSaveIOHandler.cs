using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Save.Loader;
using RTSEngine.Save.Game;
using RTSEngine.Logging;
using RTSEngine.Save.Loader.Logging;

namespace RTSEngine.Save.IO
{
    public class JsonSaveIOHandler : MonoBehaviour, ISaveIOHandler
    {
        #region Attributes
        [SerializeField, Tooltip("Folder path where the saved games files will be located. This path will be appended to 'Application.dataPath'.")]
        private string saveFolder = "Saves";
        [SerializeField, Tooltip("Prefix that will be added to the save name of each save file.")]
        private string savePrefix = "saved_game_";
        [SerializeField, Tooltip("Format that the save game file will use.")]
        public string saveFormat = "txt";

        protected ILoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            Init();
        }

        public void Init(IGameLoader loader)
        {
            this.logger = loader.GetService<IGameLoaderLoggingService>(); 

            Init();
            LoadAll();
        }

        private void Init()
        {
            saveFolder.Trim('/');
            saveFolder = $"{Application.dataPath}/{saveFolder.Trim('/')}/";

            savePrefix = savePrefix.Trim('/');

            saveFormat = $".{saveFormat.Trim('.')}";

            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }
        }
        #endregion

        #region Saving
        public IOSaveErrorMessage Save(GameSaveData gameSaveData, IOSaveOptions options)
        {
            try
            {
                //Check if filename already exists.. etc..
                string gameSDJson = JsonUtility.ToJson(gameSaveData);
                string filePath = GetPath(gameSaveData);

                if (File.Exists(filePath))
                    return IOSaveErrorMessage.pathAlreadyExists;

                File.WriteAllText(filePath, gameSDJson);
                return IOSaveErrorMessage.none;
            }
            catch(Exception e)
            {
                logger.LogError($"[JsonSaveIOHandler] Exception raised when attempting to save game {e}");
                return IOSaveErrorMessage.error;
            }
        }
        #endregion

        #region Loading
        public GameSaveData Load(string saveName)
        {
            try
            {
                string gameSDJson = File.ReadAllText(GetPath(saveName));
                return JsonUtility.FromJson<GameSaveData>(gameSDJson);
            }
            catch(Exception e)
            {
                logger.LogError($"[JsonSaveIOHandler] Exception raised when attempting to load game of name {saveName}: {e}");
                return null;
            }
        }

        // Key: filePath 
        public IReadOnlyDictionary<string, GameSaveData> LoadAll()
        {
            try
            {
                string searchPattern = $"{savePrefix}*{saveFormat}";

                DirectoryInfo saveDirectory = new DirectoryInfo($@"{saveFolder}");
                FileInfo[] saveFiles = saveDirectory.GetFiles(searchPattern);

                return saveFiles
                    .ToDictionary(
                        file => file.FullName,
                        file => JsonUtility.FromJson<GameSaveData>(File.ReadAllText(file.FullName))
                    );
            }
            catch(Exception e)
            {
                logger.LogError($"[JsonSaveIOHandler] Exception raised when attempting to load all games in save folder: {e}");
                return new Dictionary<string, GameSaveData>();
            }
        }

        public IReadOnlyDictionary<string, GameSaveData> LoadAll(Func<GameSaveData, bool> filter)
        {
            return LoadAll()
                .Where(kvp => filter(kvp.Value))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        #endregion

        #region Deleting
        public bool Delete(string saveName)
        {
            string filePath = saveFolder + $"{savePrefix}{saveName}{saveFormat}";

            if (!File.Exists(filePath))
                return false;

            File.Delete(filePath);
            return true;
        }
        #endregion

        #region Helper Methods
        private string GetPath(GameSaveData gameSaveData)
        {
            return GetPath(gameSaveData.Name);
        }
        private string GetPath(string saveName)
        {
            return $@"{saveFolder}{savePrefix}{saveName}{saveFormat}";
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

using RTSEngine.Determinism;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.NPC;
using RTSEngine.Scene;
using RTSEngine.UI;
using RTSEngine.Utilities;
using RTSEngine.Save.Event;
using RTSEngine.Save.Loader.Logging;
using RTSEngine.Save.Loader.UI;
using RTSEngine.Save.IO;
using RTSEngine.Save.Game;
using System.Globalization;

namespace RTSEngine.Save.Loader
{
    public enum SavedGameSlotSortType { name, date};

    public partial class GameLoader : MonoBehaviour, IGameLoader, IGameBuilder
    {

        #region Attributes
        [SerializeField, Tooltip("Code that uniquely identifies the game version/type that this saved game loader handlee. Only save files that match the game code will be loaded.")]
        private string gameCode = "2022.0.0";
        public string GameCode => gameCode;

        [SerializeField, Tooltip("Scene loaded when leaving this lobby menu.")]
        private string prevScene = "main_menu";

        [SerializeField, EnforceType(typeof(ISavedGameSlot), prefabOnly: true), Tooltip("Prefab used to display the name and the date of each saved game.")]
        private GameObject savedGameSlotPrefab = null;

        [SerializeField, Tooltip("All possible faction types that can be found in the saved games.")]
        private FactionTypeInfo[] factionTypes = new FactionTypeInfo[0];
        private IReadOnlyDictionary<string, FactionTypeInfo> keyToFactionType;
        [SerializeField, Tooltip("All possible NPC types that can be found in the saved games.")]
        private NPCType[] npcTypes = new NPCType[0];
        private IReadOnlyDictionary<string, NPCType> keyToNPCType;

        public List<ISavedGameSlot> currentSaveSlots = null;
        public SavedGameSlotSortType CurrSavedGameSlotSortType { get; private set; }
        private ISavedGameSlot selectedSlot;
        // Holds the loaded game save data.
        private GameSaveData nextGameSaveData;

        [SerializeField, Tooltip("Define properties for loading target scenes from this scene.")]
        private SceneLoader sceneLoader = new SceneLoader();

        protected IGameLoaderLoggingService logger { private set; get; }
        protected IGameLoaderUIManager loaderUIMgr { get; private set; }
        protected IGameLoaderPlayerMessageUIHandler playerMessageUIHandler { private set; get; }
        protected ISaveIOHandler io { private set; get; }
        #endregion

        #region IGameBuilder
        public bool IsMaster => true;
        public bool CanFreezeTimeOnPause => true;

        public bool IsInputAdderReady => InputAdder.IsValid();
        public IInputAdder InputAdder { private set; get; }
        public event CustomEventHandler<IGameBuilder, EventArgs> InputAdderReady;
        public void OnInputAdderReady(IInputAdder inputAdder)
        {
            this.InputAdder = inputAdder;

            var handler = InputAdderReady;
            handler?.Invoke(this, EventArgs.Empty);
        }

        public GameData Data { private set; get; }

        public int FactionSlotCount => selectedSlot.Data.factionSlotData.Count;

        public IEnumerable<FactionSlotData> FactionSlotDataSet => selectedSlot.Data.factionSlotData;

        public bool ClearDefaultEntities => true;


        public void OnGameBuilt(IGameManager gameMgr)
        {
            ClearSavedGameSlots();
            loaderUIMgr.Toggle(false);

            OnInputAdderReady(new DirectInputAdder(gameMgr));

            gameMgr.GamePostBuilt += HandleGamePostBuilt;
        }

        private void HandleGamePostBuilt(IGameManager gameMgr, EventArgs args)
        {
            nextGameSaveData.Load(gameMgr);

            nextGameSaveData = null; // To release the loaded saved game data from memory
            gameMgr.GamePostBuilt -= HandleGamePostBuilt;
        }

        public void OnGameLeave()
        {
            LeaveGameLoader();
        }
        #endregion

        #region Services
        private IReadOnlyDictionary<System.Type, IGameLoaderService> services = null;

        public T GetService<T>() where T : IGameLoaderService
        {
            if (!services.ContainsKey(typeof(T)))
                Debug.LogError($"[SavedGameLoader] No service of type: '{typeof(T)}' has been registered!");

            if (services.TryGetValue(typeof(T), out IGameLoaderService value))
                return (T)value;

            return default;
        }

        private void RegisterServices()
        {
            // Only services that are attached to the same game object are recognized
            // Register the services when the game starts.
            services = GetComponents<IGameLoaderService>()
                .ToDictionary(service => service.GetType().GetSuperInterfaceType<IGameLoaderService>(), service => service);

            // Initialize services.
            foreach (IGameLoaderService service in services.Values)
                service.Init(this);
        }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IGameLoader, SavedGameSlotEventArgs> SavedGameSlotAdded;
        private void RaiseSavedGameSlotAdded(SavedGameSlotEventArgs args)
        {
            var handler = SavedGameSlotAdded;
            handler?.Invoke(this, args);
        }

        public event CustomEventHandler<IGameLoader, SavedGameSlotEventArgs> SavedGameSlotSelected;
        private void RaiseSavedGameSlotSelected(SavedGameSlotEventArgs args)
        {
            var handler = SavedGameSlotSelected;
            handler?.Invoke(this, args);
        }

        public event CustomEventHandler<IGameLoader, EventArgs> SavedGameSlotDeselected;
        private void RaiseSavedGameSlotDeselected()
        {
            var handler = SavedGameSlotDeselected;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        private void Awake()
        {
            // We need this component to pass to the map scene
            this.gameObject.DontDestroyOnLoad();

            this.logger = GetComponent<IGameLoaderLoggingService>();

            RTSHelper.Init(this);

            // Register the game loader related services.
            RegisterServices();

            loaderUIMgr = GetService<IGameLoaderUIManager>();

            if (!loaderUIMgr.IsValid())
            {
                logger.LogError($"[GameLoader] A component that extends the interface '{typeof(IGameLoaderUIManager).Name}' must be attached to the same game object to handle UI.", source: this);
                return;
            }
            playerMessageUIHandler = GetService<IGameLoaderPlayerMessageUIHandler>();

            io = GetService<ISaveIOHandler>();
            if (!io.IsValid())
            {
                logger.LogError($"[GameLoader] A component that extends the interface '{typeof(ISaveIOHandler).Name}' must be attached to the same game object to handle UI.", source: this);
                return;
            }

            if (!savedGameSlotPrefab.IsValid())
            {
                logger.LogError("[GameLoader] The field 'Saved Game Slot Prefab' must be assigned!", source: this);
                return;
            }

            keyToFactionType = factionTypes.ToDictionary(type => type.Key, type => type);
            keyToNPCType = npcTypes.ToDictionary(type => type.Key, type => type);

            currentSaveSlots = new List<ISavedGameSlot>();
            RefreshSavedGameSlots();

            DeselectSlot();

            OnInit();
        }

        protected virtual void OnInit() { }

        private void OnDestroy()
        {
            OnDestroyed();
        }

        protected virtual void OnDestroyed() { }
        #endregion

        #region Handling Saved Game Slots
        public void ClearSavedGameSlots()
        {
            foreach (ISavedGameSlot slot in currentSaveSlots)
                Destroy(slot.gameObject);

            DeselectSlot();
            currentSaveSlots.Clear();
        }

        public void RefreshSavedGameSlots(SavedGameSlotSortType sortBy = SavedGameSlotSortType.date)
        {
            ClearSavedGameSlots();

            IEnumerable<SavedGameSlotData> nextSavedSlots = LoadAllToSavedGameSlotData();

            switch (sortBy)
            {
                case SavedGameSlotSortType.name:
                    nextSavedSlots = nextSavedSlots.OrderBy(slot => slot.name);
                    break;
                default:
                    nextSavedSlots = nextSavedSlots.OrderBy(slot => DateTime.Parse(slot.date, CultureInfo.InvariantCulture));
                    break;
            }
            CurrSavedGameSlotSortType = sortBy;

            foreach (var savedGameSlotData in nextSavedSlots)
            {
                var nextSlot = Instantiate(savedGameSlotPrefab).GetComponent<ISavedGameSlot>();

                nextSlot.Init(this, savedGameSlotData);
                currentSaveSlots.Add(nextSlot);

                RaiseSavedGameSlotAdded(new SavedGameSlotEventArgs(nextSlot));
            }

            if (currentSaveSlots.Count == 0)
                playerMessageUIHandler.Message.Display(new MessageEventArgs(
                    MessageType.warning,
                    "No saved games found!"));
        }

        public IEnumerable<SavedGameSlotData> LoadAllToSavedGameSlotData()
        {
            // Only load the save game files that match the game code in this menu
            return io.LoadAll(gameSaveData => gameSaveData.GameCode == GameCode)
                .Select(gameSD => new SavedGameSlotData
                {
                    filePath = gameSD.Key,

                    name = gameSD.Value.Name,
                    date = gameSD.Value.Date,

                    sceneName = gameSD.Value.SceneName,

                    defeatCondition = gameSD.Value.DefeatCondition,

                    timeModifierOptions = gameSD.Value.EssentialGameServices.TimeModifierOptions
                        .Select(option => new TimeModifierOption
                        {
                            name = option.name,
                            modifier = option.modifier
                        }),

                    factionSlotData = gameSD.Value.FactionSlots
                        .Where(factionSlot => factionSlot.State == FactionSlotState.active)
                        .Select(factionSlot =>
                        {
                            if (!keyToFactionType.TryGetValue(factionSlot.Data.typeKey, out FactionTypeInfo factionType)
                                && !string.IsNullOrEmpty(factionSlot.Data.typeKey))
                                logger.LogError($"[GameLoader] Unable to find faction type of code '{factionSlot.Data.typeKey}'. Are you sure you added it to the Faction Types field?");
                            if(!keyToNPCType.TryGetValue(factionSlot.Data.npcTypeKey, out NPCType npcType)
                                && !string.IsNullOrEmpty(factionSlot.Data.npcTypeKey))
                                logger.LogError($"[GameLoader] Unable to find NPC type of code '{factionSlot.Data.typeKey}'. Are you sure you added it to the NPC Types field?");
                            return new FactionSlotData
                            {
                                role = factionSlot.Data.role,

                                name = factionSlot.Data.name,
                                color = factionSlot.Data.color,

                                // Get
                                type = factionType,
                                npcType = npcType,

                                isLocalPlayer = factionSlot.Data.isLocalPlayer,

                                forceID = true,
                                forcedID = factionSlot.ID
                            };
                        })
                        .ToList()
                });
        }

        public void SortByName ()
        {
            RefreshSavedGameSlots(sortBy: SavedGameSlotSortType.name);
        }

        public void SortByDate()
        {
            RefreshSavedGameSlots(sortBy: SavedGameSlotSortType.date);
        }
        #endregion

        #region Handling Slot Selection
        public void SelectSlot(ISavedGameSlot nextSlot)
        {
            if(nextSlot != selectedSlot)
                DeselectSlot();

            selectedSlot = nextSlot;
            selectedSlot.OnSelected();
            RaiseSavedGameSlotSelected(new SavedGameSlotEventArgs(selectedSlot));
        }

        public void DeselectSlot()
        {
            if(selectedSlot.IsValid())
            {
                selectedSlot.OnDeselected();
            }

            selectedSlot = null;
            RaiseSavedGameSlotDeselected();
        }

        public void LoadSelected()
        {
            if (!selectedSlot.IsValid())
                return;

            nextGameSaveData = io.Load(selectedSlot.Data.name);
            Data = new GameData
            {
                defeatCondition = selectedSlot.Data.defeatCondition,
                initialResources = null,
                timeModifierOptions = new TimeModifierOptions
                {
                    values = selectedSlot.Data.timeModifierOptions.ToArray(),
                    initialValueID = 0
                },
                factionSlotIndexSeed = null
            };

            sceneLoader.LoadScene(selectedSlot.Data.sceneName, source: this);
        }
        #endregion

        #region Navigation
        public void LeaveGameLoader()
        {
            SceneManager.LoadScene(prevScene);

            DontDestroyOnLoadManager.Destroy(gameObject);
        }
        #endregion
    }
}

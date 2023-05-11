using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Save.Event;
using RTSEngine.Save.Loader.Logging;

namespace RTSEngine.Save.Loader.UI
{
    public class GameLoaderUIManager : MonoBehaviour, IGameLoaderUIManager
    {
        #region Attributes
        [SerializeField, Tooltip("Main canvas that is the parent object of all UI elements.")]
        private Canvas canvas = null;

        [SerializeField, Tooltip("Parent object of the slot instances created for each saved game."), Space()]
        private Transform savedGameSlotsParent = null;

        [SerializeField, Tooltip("Parent object of UI elements that display data about the currently selected saved game slot. When assigned and no saved game slot is selected, this panel is hidden."), Space()]
        private GameObject savedGameDataPanel = null;

        [SerializeField, Tooltip("Text UI used to display the current selected saved game slot's name.")]
        private Text saveNameUI = null;

        [System.Serializable]
        public struct MapSceneToName
        {
            public string sceneName;
            public string name;
        }
        [SerializeField, Tooltip("Possible choices for the map scenes that a saved game can have and the label used for them in the UI elements.")]
        private MapSceneToName[] mapNames = new MapSceneToName[0];
        private IReadOnlyDictionary<string, string> mapToUIName;
        [SerializeField, Tooltip("Text UI used to display the current selected saved game slot's map name.")]
        private Text mapNameUI = null;

        [System.Serializable]
        public struct DefeatConditionToName
        {
            public DefeatConditionType condition;
            public string name;
        }
        [SerializeField, Tooltip("Possible choices for the defeat condition that a saved game can have and the label used for them in the UI elements.")]
        private DefeatConditionToName[] defeatConditionNames = new DefeatConditionToName[0];
        private IReadOnlyDictionary<DefeatConditionType, string> defeatConditionToUIName;
        [SerializeField, Tooltip("Text UI used to display the current selected saved game slot's defeat condition.")]
        private Text defeatConditionUI = null;

        [SerializeField, EnforceType(typeof(ISavedGameFactionSlot), prefabOnly: true), Tooltip("Prefab object used to display a selected saved game faction slots data.")]
        private GameObject savedGameFactionSlotPrefab = null;
        [SerializeField, Tooltip("Parent object of each saved game faction slot data displayer (above prefab instances).")]
        private Transform savedGameFactionSlotsParent = null;
        private List<ISavedGameFactionSlot> savedGameFactionSlotInstances = null;

        [SerializeField, Tooltip("UI Button that loads a selected saved game slot when clicked."), Space()]
        private Button loadGameButton = null;

        protected IGameLoader loader { get; private set; }
        protected IGameLoaderLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameLoader loader)
        {
            this.loader = loader;
            this.logger = loader.GetService<IGameLoaderLoggingService>();

            mapToUIName = mapNames
                .ToDictionary(element => element.sceneName, element => element.name);

            defeatConditionToUIName = defeatConditionNames
                .ToDictionary(element => element.condition, element => element.name);


            if (!canvas.IsValid()
                || !saveNameUI.IsValid()
                || !mapNameUI.IsValid()
                || !defeatConditionUI.IsValid()
                || !savedGameDataPanel.IsValid()
                || !savedGameFactionSlotPrefab.IsValid()
                || !savedGameFactionSlotsParent.IsValid()
                || !loadGameButton.IsValid()
                || !savedGameSlotsParent.IsValid()) 
            {
                logger.LogError("[GameLoaderUIManager] All fields must be assigned in the inspector!", source: this);
                return;
            }

            loadGameButton.onClick.AddListener(loader.LoadSelected);

            savedGameFactionSlotInstances = new List<ISavedGameFactionSlot>();

            loader.SavedGameSlotAdded += HandleSavedGameAdded;
            loader.SavedGameSlotSelected += HandleSavedGameSlotSelected;
            loader.SavedGameSlotDeselected += HandleSavedGameSlotDeselected;
        }

        private void HandleSavedGameAdded(IGameLoader sender, SavedGameSlotEventArgs args)
        {
            args.SavedGameSlot.transform.SetParent(savedGameSlotsParent, false);
            args.SavedGameSlot.transform.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            loader.SavedGameSlotSelected -= HandleSavedGameSlotSelected;
            loader.SavedGameSlotDeselected -= HandleSavedGameSlotDeselected;
        }
        #endregion

        #region Handling Events: Saved Game Slot Selection 
        private void HandleSavedGameSlotDeselected(IGameLoader sender, EventArgs args)
        {
            if(savedGameDataPanel.IsValid())
                savedGameDataPanel.gameObject.SetActive(false);
        }

        private void HandleSavedGameSlotSelected(IGameLoader sender, SavedGameSlotEventArgs args)
        {
            if(savedGameDataPanel.IsValid())
                savedGameDataPanel.gameObject.SetActive(true);

            // TryGetValue and check for valid values here.
            saveNameUI.text = args.SavedGameSlot.Data.name;
            mapNameUI.text = mapToUIName[args.SavedGameSlot.Data.sceneName];
            defeatConditionUI.text = defeatConditionToUIName[args.SavedGameSlot.Data.defeatCondition];

            int i = 0;
            IReadOnlyList<FactionSlotData> factionSlotDataSet = args.SavedGameSlot.Data.factionSlotData;
            while(i < factionSlotDataSet.Count)
            {
                if(!i.IsValidIndex(savedGameFactionSlotInstances))
                {
                    savedGameFactionSlotInstances.Add(Instantiate(savedGameFactionSlotPrefab).GetComponent<ISavedGameFactionSlot>());
                    savedGameFactionSlotInstances[i].transform.SetParent(savedGameFactionSlotsParent.transform, false);
                    savedGameFactionSlotInstances[i].transform.localScale = savedGameFactionSlotPrefab.transform.localScale;

                    savedGameFactionSlotInstances[i].Init(loader);
                }

                savedGameFactionSlotInstances[i].gameObject.SetActive(true);
                savedGameFactionSlotInstances[i].ResetData(factionSlotDataSet[i]);

                i++;
            }

            for(int j = i; i < factionSlotDataSet.Count; j++)
                savedGameFactionSlotInstances[i].gameObject.SetActive(false);
        }
        #endregion

        #region Other
        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
                loader.DeselectSlot();
        }

        public void Toggle(bool show)
        {
            canvas.gameObject.SetActive(show);
        }
        #endregion
    }
}

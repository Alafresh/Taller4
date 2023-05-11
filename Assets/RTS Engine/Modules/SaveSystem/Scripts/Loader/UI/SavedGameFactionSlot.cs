using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Faction;
using RTSEngine.Save.Loader.Logging;

namespace RTSEngine.Save.Loader.UI
{
    public class SavedGameFactionSlot : MonoBehaviour, ISavedGameFactionSlot
    {
        #region Attributes
        public FactionSlotData Data { private set; get; }

        [SerializeField, Tooltip("UI Image to display the faction's color.")]
        private Image factionColorImage = null;
        [SerializeField, Tooltip("UI Text used to display the faction's name.")]
        private Text factionNameText = null;
        [SerializeField, Tooltip("UI Text used to display the faction's type.")]
        private Text factionTypeText = null;
        [SerializeField, Tooltip("UI Text used to display the faction's NPC type.")]
        private Text npcTypeText = null;

        protected IGameLoader loader { private set; get; }
        protected IGameLoaderLoggingService logger { private set; get; }
        //protected IGameLoaderUIManager UIMgr { private set; get; }
        #endregion

        public void Init(IGameLoader loader)
        {
            this.loader = loader;
            this.logger = loader.GetService<IGameLoaderLoggingService>(); 

            if (!logger.RequireValid(factionColorImage, $"[SavedGameFactionSlot] The field 'Faction Color Image' is required!")
                || !logger.RequireValid(factionNameText, $"[SavedGameFactionSlot] The field 'Faction Type Menu' is required!")
                || !logger.RequireValid(factionTypeText, $"[SavedGameFactionSlot] The field 'NPC Type Menu' is required!")
                || !logger.RequireValid(npcTypeText, $"[SavedGameFactionSlot] The field 'Remove Button' is required!"))
                return;
        }

        public void ResetData(FactionSlotData data)
        {
            this.Data = data;

            factionColorImage.color = Data.color;
            factionNameText.text = Data.name;
            factionTypeText.text = Data.type.IsValid()
                ? Data.type.Name
                : "None";
            npcTypeText.text = Data.isLocalPlayer
                ? "Player"
                : (Data.npcType.IsValid() ? Data.npcType.Name : "None");
        }
    }
}

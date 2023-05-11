using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Save.IO;
using RTSEngine.Save.Loader.Logging;

namespace RTSEngine.Save.Loader.UI
{
    public class SavedGameSlot : MonoBehaviour, ISavedGameSlot
    {
        public SavedGameSlotData Data { get; private set; }

        [SerializeField, Tooltip("")]
        private Text nameUI = null;
        [SerializeField, Tooltip("")]
        private Text dateUI = null;
        private Button button;

        protected IGameLoader loader { get; private set; }
        protected IGameLoaderLoggingService logger { private set; get; }
        protected ISaveIOHandler io { private set; get; } 

        public void Init(IGameLoader loader, SavedGameSlotData data)
        {
            this.loader = loader;
            this.Data = data;

            this.logger = loader.GetService<IGameLoaderLoggingService>();
            this.io = loader.GetService<ISaveIOHandler>();

            button = GetComponent<Button>();
            if (!button.IsValid())
            {
                logger.LogError($"[SavedGameSlot] A component of type '{typeof(Button)}' must be attached to this gameobject!", source: this);
                return;
            }
            else if (!nameUI.IsValid())
            {
                logger.LogError("[SavedGameSlot] The field 'Name UI' must be assigned!", source: this);
                return;
            }
            else if (!dateUI.IsValid())
            {
                logger.LogError("[SavedGameSlot] The field 'Date UI' must be assigned!", source: this);
                return;
            }

            button.onClick.AddListener(Select);

            nameUI.text = Data.name;
            dateUI.text = Data.date;
        }

        private void Select()
        {
            loader.SelectSlot(this);
        }

        public void OnSelected()
        {
            button.interactable = false;
        }

        public void OnDeselected()
        {
            button.interactable = true;   
        }

        public void Delete()
        {
            if (io.Delete(Data.name))
                loader.RefreshSavedGameSlots(loader.CurrSavedGameSlotSortType);
        }
    }
}

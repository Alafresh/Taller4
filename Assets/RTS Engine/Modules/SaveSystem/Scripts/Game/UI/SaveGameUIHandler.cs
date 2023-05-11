using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Event;
using RTSEngine.UI;
using RTSEngine.Save.IO;

namespace RTSEngine.Save.Game.UI
{
    public class SaveGameUIHandler : MonoBehaviour, IPostRunGameService
    {
        [SerializeField, Tooltip("Input of the name of the next saved game file.")]
        private InputField saveNameInput = null;
        [SerializeField, Tooltip("Input of the name of the next saved game file.")]
        private Button saveButton = null;

        protected ISaveManager saveMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IPlayerMessageHandler playerMessage { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; } 

        public void Init(IGameManager gameMgr)
        {
            saveMgr = gameMgr.GetService<ISaveManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.playerMessage = gameMgr.GetService<IPlayerMessageHandler>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>(); 

            if (!saveNameInput.IsValid() || !saveButton.IsValid())
            {
                logger.LogError("[SaveGameUIHandler] All fields in the inspector must be assigned!", source: this);
                return;
            }

            saveButton.onClick.AddListener(OnSaveButtonClick);
            saveNameInput.onValueChanged.AddListener(OnSaveNameUpdated);

            saveNameInput.gameObject.SetActive(true);
            saveButton.gameObject.SetActive(true);

            OnSaveNameUpdated(saveNameInput.text);
        }

        protected virtual bool IsSaveNameValid()
        {
            return !string.IsNullOrEmpty(saveNameInput.text);
        }    

        private void OnSaveNameUpdated(string newValue)
        {
            saveButton.interactable = IsSaveNameValid();
        }

        private void OnSaveButtonClick()
        {
            string saveName = saveNameInput.text;
            IOSaveErrorMessage errorMessage = saveMgr.OnSave(saveName);

            MessageEventArgs nextMessage;
            switch (errorMessage)
            {
                case IOSaveErrorMessage.none:
                    nextMessage = new MessageEventArgs
                    (
                        type: MessageType.info,
                        message: $"Game '{saveName}' saved!"
                    );
                    break;

                case IOSaveErrorMessage.pathAlreadyExists:
                    nextMessage = new MessageEventArgs
                    (
                        type: MessageType.error,
                        message: $"Save name '{saveName}' already exists!"
                    );
                    break;

                default:
                    nextMessage = new MessageEventArgs
                    (
                        type: MessageType.error,
                        message: $"Unable to save game!"
                    );
                    break;
            }

            globalEvent.RaiseShowPlayerMessageGlobal(this, nextMessage);
        }
    }
}

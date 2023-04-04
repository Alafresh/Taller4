using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.UI
{
    [System.Serializable]
    public class ProgressBarUI
    {
        #region Attributes
        [SerializeField, Tooltip("Includes a UI Image component used to display the full progress bar. Make sure the bar's default size is the one used to represent 100% complete full bar.")]
        private RectTransform fullBar = null;

        private Image barImage;
        private float imageFullLength;
        private Vector3 imageFullLocalPosition;

        [SerializeField, Tooltip("Includes a UI Image component used to display the empty progress bar. This bar is only for visual purposes and can act as a background for the full bar.")]
        private RectTransform emptyBar = null;
        private Image emptyImage;

        // Game services
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            barImage = fullBar.GetComponent<Image>();
            imageFullLength = fullBar.sizeDelta.x;
            imageFullLocalPosition = fullBar.localPosition;

            if (emptyBar.IsValid())
                emptyImage = emptyBar.GetComponent<Image>();

            if(!logger.RequireValid(fullBar,
                $"[ProgressBarUI] The field 'Full Bar' field has not been assigned!")
                || !logger.RequireValid(barImage,
                $"[ProgressBarUI] The assigned 'Full Bar' field does not have a '{typeof(Image).Name}' component attached to it!"))
                return;
        }
        #endregion

        #region Handling Progress Bar
        public void Toggle(bool enable)
        {
            barImage.enabled = enable;
            if (emptyImage.IsValid())
                emptyImage.enabled = enable;
        }

        public void Update(float progress)
        {
            progress = Mathf.Clamp(progress, 0.0f, 1.0f);

            fullBar.sizeDelta = new Vector2(
                progress * imageFullLength,
                fullBar.sizeDelta.y);

            fullBar.localPosition = new Vector3(
                imageFullLocalPosition.x - (imageFullLength - fullBar.sizeDelta.x) / 2.0f,
                imageFullLocalPosition.y,
                imageFullLocalPosition.z);
        }
        #endregion
    }
}

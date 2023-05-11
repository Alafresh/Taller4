using System;

using RTSEngine.Save.Loader.UI;

namespace RTSEngine.Save.Event
{
    public class SavedGameSlotEventArgs : EventArgs
    {
        public ISavedGameSlot SavedGameSlot { get; }

        public SavedGameSlotEventArgs(ISavedGameSlot savedGameSlot)
        {
            SavedGameSlot = savedGameSlot;
        }
    }
}

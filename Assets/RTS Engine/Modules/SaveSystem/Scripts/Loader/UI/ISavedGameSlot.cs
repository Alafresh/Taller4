namespace RTSEngine.Save.Loader.UI
{
    public interface ISavedGameSlot : IMonoBehaviour
    {
        SavedGameSlotData Data { get; }

        void Init(IGameLoader loader, SavedGameSlotData data);
        void OnDeselected();
        void OnSelected();
    }
}

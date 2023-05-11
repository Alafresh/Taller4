using RTSEngine.Faction;

namespace RTSEngine.Save.Loader.UI
{
    public interface ISavedGameFactionSlot : IMonoBehaviour
    {
        FactionSlotData Data { get; }

        void Init(IGameLoader loader);
        void ResetData(FactionSlotData data);
    }
}
namespace RTSEngine.Save.Loader.UI
{
    public interface IGameLoaderUIManager : IGameLoaderService, IMonoBehaviour
    {
        void Toggle(bool show);
    }
}

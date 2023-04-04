using RTSEngine.Entities;

namespace RTSEngine.EntityComponent
{
    public interface IResourceGenerator : IEntityComponent
    {
        IFactionEntity FactionEntity { get; }
    }
}

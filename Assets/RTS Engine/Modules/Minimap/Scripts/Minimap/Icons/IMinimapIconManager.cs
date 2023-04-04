using RTSEngine.Game;
using UnityEngine;

namespace RTSEngine.Minimap.Icons
{
    public interface IMinimapIconManager : IPreRunGameService
    {
        Sprite DefaultIcon { get; }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Minimap.Icons
{
    public class EntityMinimapIconHandler : MonoBehaviour, IEntityMinimapIconHandler
    {
        [SerializeField, Tooltip("Data used to display the minimap icon of this entity.")]
        private IEntityMinimapIconData data = new IEntityMinimapIconData();
        public IEntityMinimapIconData Data => data;
    }
}

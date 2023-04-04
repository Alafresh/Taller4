using System;
using UnityEngine;

namespace RTSEngine.Minimap.Icons
{
    [Serializable]
    public struct IEntityMinimapIconData
    {
        [Tooltip("When assigned, the sprite in this field will be used instead of the default minimap's icon.")]
        public Sprite icon;

        [Tooltip("Manipulate color transparency?")]
        public bool changeTransparency;
        [Range(0.0f, 1.0f), Tooltip("How transparent would the icon's color? The higher this value, the more transparent the color would be.")]
        public float transparency;
        [Tooltip("Manipulate color darkness?")]
        public bool changeDarkness;
        [Range(0.0f, 1.0f), Tooltip("Adjust the darkness of the icon's color, the higher this value, the darker the color would be.")]
        public float darkness;
    }

    public interface IEntityMinimapIconHandler
    {
        IEntityMinimapIconData Data { get; }
    }
}

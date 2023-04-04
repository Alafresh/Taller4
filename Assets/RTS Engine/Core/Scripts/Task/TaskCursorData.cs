using UnityEngine;

namespace RTSEngine.Task
{
    [System.Serializable]
    public struct TaskCursorData
    {
        [Tooltip("Either fill the 'Icon' or the 'Texture' (Texture2D) field to use as the mouse cursor texture ('Texture' will be prioritized. Or set Leave unassigned to use the default mouse cursor.")]
        public Sprite icon;
        [Tooltip("Either fill the 'Icon' or the 'Texture' (Texture2D) field to use as the mouse cursor texture ('Texture' will be prioritized. Or set Leave unassigned to use the default mouse cursor.")]
        public Texture2D texture;

        [Space(), Tooltip("If the mouse cursor texture has a different hotspot, assign it here.")]
        public Vector2 hotspot;
    }
}

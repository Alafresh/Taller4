using System;
using UnityEngine;

namespace RTSEngine.Save.IO
{
    [Serializable]
    public struct IOSaveOptions
    {
        [Tooltip("When disabled, two saved games can not have the same name!")]
        public bool replaceDuplicate;
    }
}

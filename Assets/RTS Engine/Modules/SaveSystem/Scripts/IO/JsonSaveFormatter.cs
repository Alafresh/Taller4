using UnityEngine;

using RTSEngine.Game;

namespace RTSEngine.Save.IO
{
    public class JsonSaveFormatter : MonoBehaviour, ISaveFormatter
    {
        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
        }
        #endregion

        #region String To/From JSON
        public string ToSaveFormat<T> (T input)
        {
            return JsonUtility.ToJson(input);
        }

        public T FromSaveFormat<T> (string input)
        {
            return JsonUtility.FromJson<T>(input);
        }
        #endregion
    }
}

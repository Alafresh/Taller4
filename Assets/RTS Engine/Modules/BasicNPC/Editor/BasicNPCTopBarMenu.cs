using UnityEngine;
using UnityEditor;

namespace RTSEngine.EditorOnly.NPC
{
    public class BasicTopBarMenu
    {
        private const string NewNPCManagerPrefabName = "basic_npc_manager_prefab";

        [MenuItem("RTS Engine/Basic NPC/New NPC Manager", false, 501)]
        private static void ConfigNewMapOption()
        {
            var prefab = Resources.Load(NewNPCManagerPrefabName, typeof(GameObject));
            string targetPath = $"{RTSEditorHelper.CurrentProjectFolderPath}/new_npc_manager.prefab";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(prefab), targetPath);

            RTSEditorHelper.Log($"New Baic NPC Manager prefab created at path: {targetPath}");
        }
    }
}

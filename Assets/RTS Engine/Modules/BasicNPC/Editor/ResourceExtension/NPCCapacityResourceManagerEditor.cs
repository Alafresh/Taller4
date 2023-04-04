using UnityEditor;
using RTSEngine.NPC.ResourceExtension;

namespace RTSEngine.EditorOnly.NPC.ResourceExtension
{
    [CustomEditor(typeof(NPCCapacityResourceManager))]
    public class NPCCapacityResourceManagerEditor : NPCComponentEditor<NPCCapacityResourceManager>
    {
        private string[][] toolbars = new string[][] {
            new string [] { "Attributes", "Logs" },
        };

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(toolbars);
        }

        protected override void OnComponentSpecificTabSwitch(string tabName)
        {
            switch (tabName)
            {
                case "Attributes":
                    OnAttributesInspectorGUI ();
                    break;
            }
        }

        protected virtual void OnAttributesInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("capacityResource"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("factionEntities"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("autoCreate"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("reloadRange"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("targetCapacityRange"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("minFreeAmount"), true);

            EditorGUILayout.Space();
        }

        protected override void OnActiveLogsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("capacityResourceLogs"), true);
        }
    }
}

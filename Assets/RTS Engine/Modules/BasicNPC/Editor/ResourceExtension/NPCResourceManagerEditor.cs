using UnityEditor;
using RTSEngine.NPC.ResourceExtension;

namespace RTSEngine.EditorOnly.NPC.ResourceExtension
{
    [CustomEditor(typeof(NPCResourceManager))]
    public class NPCResourceManagerEditor : NPCComponentEditor<NPCResourceManager>
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
            EditorGUILayout.PropertyField(SO.FindProperty("resourceNeedRatioRange"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("resourceDefaultExploitRatioRange"), true);
        }

        protected override void OnActiveLogsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("targetResourcesLogs"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("factionResourcesLogs"), true);
        }
    }
}

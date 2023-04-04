using UnityEditor;
using RTSEngine.NPC.ResourceExtension;

namespace RTSEngine.EditorOnly.NPC.ResourceExtension
{
    [CustomEditor(typeof(NPCTerritoryManager))]
    public class NPCTerritoryManagerEditor : NPCComponentEditor<NPCTerritoryManager>
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
            EditorGUILayout.PropertyField(SO.FindProperty("buildingCenters"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("expandOnDemand"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("targetTerritoryRatio"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("expandDelayRange"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("expandReloadRange"), true);
        }

        protected override void OnActiveLogsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("territoryLogs"), true);
        }
    }
}

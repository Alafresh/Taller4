using UnityEditor;
using RTSEngine.NPC.ResourceExtension;

namespace RTSEngine.EditorOnly.NPC.ResourceExtension
{

    [CustomEditor(typeof(NPCResourceCollector))]
    public class NPCResourceCollectorEditor : NPCComponentEditor<NPCResourceCollector>
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
            EditorGUILayout.PropertyField(SO.FindProperty("collectors"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("collectionData"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("collectionTimerRange"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("enforceMinCollectorsTimerTicks"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("collectOnDemand"), true);
        }

        protected override void OnActiveLogsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("collectionTrackerLogs"), true);
        }
    }
}

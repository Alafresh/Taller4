using UnityEditor;

using RTSEngine.NPC.UnitExtension;

namespace RTSEngine.EditorOnly.NPC.UnitExtension
{
    [CustomEditor(typeof(NPCUnitCreator))]
    public class NPCUnitCreeatorEditor : NPCComponentEditor<NPCUnitCreator>
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
            EditorGUILayout.PropertyField(SO.FindProperty("defaultRegulatorData"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("independentUnits"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("populationResource"), true);
        }

        protected override void OnActiveLogsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("activeUnitRegulatorLogs"), true);
        }

    }
}

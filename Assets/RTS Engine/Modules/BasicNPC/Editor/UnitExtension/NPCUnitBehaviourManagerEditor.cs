using UnityEditor;

using RTSEngine.NPC.UnitExtension;

namespace RTSEngine.EditorOnly.NPC.UnitExtension
{
    [CustomEditor(typeof(NPCUnitBehaviourManager))]
    public class NPCUnitBehaviourManagerEditor : NPCComponentEditor<NPCUnitBehaviourManager>
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
            EditorGUILayout.PropertyField(SO.FindProperty("prefabs"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("forceCreation"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("attackEngageOrderBehaviour"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("territoryDefenseOrderBehaviour"), true);
        }

        protected override void OnActiveLogsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("currStateLog"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("trackedUnits"), true);
        }

    }
}

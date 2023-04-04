using UnityEditor;
using RTSEngine.NPC.BuildingExtension;

namespace RTSEngine.EditorOnly.NPC.BuildingExtension
{
    [CustomEditor(typeof(NPCBuildingPlacer))]
    public class NPCBuildingPlacerEditor : NPCComponentEditor<NPCBuildingPlacer>
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
            EditorGUILayout.PropertyField(SO.FindProperty("defaultPlacementHandler"), true);
        }

        protected override void OnActiveLogsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("placementQueue"), true);
        }
    }
}

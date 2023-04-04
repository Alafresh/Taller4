using UnityEditor;
using RTSEngine.NPC.BuildingExtension;
using System;

namespace RTSEngine.EditorOnly.NPC.BuildingExtension
{
    [CustomEditor(typeof(NPCBuildingConstructor))]
    public class NPCBuildingConstructorEditor : NPCComponentEditor<NPCBuildingConstructor>
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
            EditorGUILayout.PropertyField(SO.FindProperty("builders"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("constructionTimerRange"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("enforceBuildersTimerTicks"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("targetBuildersRatio"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("constructOnDemand"), true);
        }

        protected override void OnActiveLogsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("targetBuildings"), true);
        }
    }

    [CustomEditor(typeof(NPCBuildingCreator))]
    public class NPCBuildingCreeatorEditor : NPCComponentEditor<NPCBuildingCreator>
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

            EditorGUILayout.PropertyField(SO.FindProperty("independentBuildings"), true);
        }

        protected override void OnActiveLogsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("activeBuildingRegulatorLogs"), true);
        }
    }
}

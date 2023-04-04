using UnityEditor;
using RTSEngine.NPC.Upgrades;

namespace RTSEngine.EditorOnly.NPC.Upgrades
{
    [CustomEditor(typeof(NPCUpgradeManager))]
    public class NPCUpgradeManagerEditor : NPCComponentEditor<NPCUpgradeManager>
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
            EditorGUILayout.PropertyField(SO.FindProperty("autoUpgrade"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("upgradeOnDemand"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("upgradeReloadRange"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("acceptanceRange"), true);
        }

        protected override void OnActiveLogsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("upgradeLogs"), true);
        }
    }
}

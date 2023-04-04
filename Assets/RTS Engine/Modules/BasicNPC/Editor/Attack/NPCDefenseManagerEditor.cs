using UnityEditor;
using RTSEngine.NPC.Attack;

namespace RTSEngine.EditorOnly.NPC.Attack
{
    [CustomEditor(typeof(NPCDefenseManager))]
    public class NPCDefenseManagerEditor : NPCComponentEditor<NPCDefenseManager>
    {

        private string[][] toolbars = new string[][] {
            new string [] { "Territory Defense", "Unit Support", "Logs" }, 
        };

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(toolbars);
        }

        protected override void OnComponentSpecificTabSwitch(string tabName)
        {
            switch (tabName)
            {
                case "Territory Defense":
                    OnTerritoryDefenseInspectorGUI ();
                    break;
                case "Unit Support":
                    OnUnitSupportInspectorGUI ();
                    break;
            }
        }

        protected virtual void OnTerritoryDefenseInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("canDefendTerritory"), true);
            if (!SO.FindProperty("canDefendTerritory").boolValue)
                return;

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("cancelAttackOnTerritoryDefense"), true);
            if (!SO.FindProperty("cancelAttackOnTerritoryDefense").boolValue)
                return;
            EditorGUILayout.PropertyField(SO.FindProperty("cancelTerritoryDefenseReloadRange"), true);
        }

        protected virtual void OnUnitSupportInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("unitSupportEnabled"), true);
            if (!SO.FindProperty("unitSupportEnabled").boolValue)
                return;
            EditorGUILayout.PropertyField(SO.FindProperty("unitSupportRange"), true);
        }

        protected override void OnActiveLogsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("defenseLogs"), true);
        }
    }
}

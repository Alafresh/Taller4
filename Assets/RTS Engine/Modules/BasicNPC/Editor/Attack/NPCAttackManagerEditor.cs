using UnityEditor;
using RTSEngine.NPC.Attack;
using System;

namespace RTSEngine.EditorOnly.NPC.Attack
{
    [CustomEditor(typeof(NPCAttackManager))]
    public class NPCAttackManagerEditor : NPCComponentEditor<NPCAttackManager>
    {
        private string[][] inactivetoolbars = new string[][] {
            new string [] { "Picking Target", "Logs" }, 
        };

        private string[][] toolbars = new string[][] {
            new string [] { "Picking Target", "Launching Attack", "Handling Active Attack", "Logs" }, 
        };

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(SO.FindProperty("canAttack").boolValue ? toolbars : inactivetoolbars);
        }

        protected override void OnComponentSpecificTabSwitch(string tabName)
        {
            switch (tabName)
            {
                case "Picking Target":
                    OnPickingTargetInspectorGUI ();
                    break;
                case "Launching Attack":
                    OnLaunchAttackInspectorGUI ();
                    break;
                case "Handling Active Attack":
                    OnHandleAttackInspectorGUI ();
                    break;
            }
        }

        protected virtual void OnPickingTargetInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("canAttack"), true);
            if (!SO.FindProperty("canAttack").boolValue)
                return;

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("attackResources"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("targetFactionType"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("setTargetFactionDelay"), true);
        }

        protected virtual void OnLaunchAttackInspectorGUI()
        {
            if (!SO.FindProperty("canAttack").boolValue)
                return;

            EditorGUILayout.PropertyField(SO.FindProperty("launchAttackReloadRange"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("launchAttackResources"), true);
        }

        protected virtual void OnHandleAttackInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("attackOrderReloadRange"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("targetPicker"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("targetPickerOnly"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("targetPreference"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("cancelAttackResources"), true);
        }

        protected override void OnActiveLogsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("attackLogs"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("launchAttackResourcesLogs"), true);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("cancelAttackResourcesLogs"), true);
        }
    }
}

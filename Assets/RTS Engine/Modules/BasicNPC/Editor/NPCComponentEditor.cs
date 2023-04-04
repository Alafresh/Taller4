using UnityEditor;

using RTSEngine.Utilities;
using RTSEngine.NPC;

namespace RTSEngine.EditorOnly.NPC
{
    public abstract class NPCComponentEditor<T> : TabsEditorBase<T> where T : NPCComponentBase
    {
        protected override Int2D tabID
        {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        public override void OnInspectorGUI(string[][] toolbars)
        {
            EditorGUILayout.PropertyField(SO.FindProperty("isActive"));

            EditorGUILayout.Space();

            base.OnInspectorGUI(toolbars);
        }

        protected override void OnTabSwitch(string tabName)
        {
            switch (tabName)
            {
                case "Logs":
                    OnLogsInspectorGUI();
                    break;
                default:
                    OnComponentSpecificTabSwitch(tabName);
                    break;
            }
        }

        protected virtual void OnComponentSpecificTabSwitch(string tabName) { }

        protected virtual void OnLogsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("showActiveLogs"));
            if(SO.FindProperty("showActiveLogs").boolValue)
                OnActiveLogsInspectorGUI();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("logEvents"));
            if(SO.FindProperty("logEvents").boolValue)
                EditorGUILayout.PropertyField(SO.FindProperty("eventLogs"), true);
        }

        protected virtual void OnActiveLogsInspectorGUI() { }
    }
}

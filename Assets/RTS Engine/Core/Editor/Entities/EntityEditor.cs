using UnityEditor;
using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Utilities;
using RTSEngine.EntityComponent;
using RTSEngine.Movement;
using System.Linq;
using RTSEngine.ResourceExtension;
using System.Collections.Generic;

namespace RTSEngine.EditorOnly.Entities
{
    [CustomEditor(typeof(Unit))]
    public class UnitEditor : EntityEditor<Unit>
    {
        private string[][] toolbars = new string[][] {
            new string[] { "Entity", "Faction Entity", "Unit" }
        };

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }
    }

    [CustomEditor(typeof(Building))]
    public class BuildingEditor : EntityEditor<Building>
    {
        private string[][] toolbars = new string[][] {
            new string[] { "Entity", "Faction Entity", "Building" }
        };

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }
    }

    [CustomEditor(typeof(Resource))]
    public class ResourceEditor : EntityEditor<Resource>
    {
        private string[][] toolbars = new string[][] {
            new string[] { "Entity",  "Resource" }
        };

        #region TEMP SECTION
        private bool CanCollect(string unitCode)
            => CanCollect(unitCode, null);
        private bool CanCollect(string unitCode, ResourceTypeInfo resourceType = null)
        {
            if (RTSEditorHelper.GetEntities().TryGetValue(unitCode, out IEntity unit))
                return CanCollect(unit as Unit, resourceType);

            return false;
        }
        private bool CanCollect(Unit unit, ResourceTypeInfo resourceType = null)
        {
            if (!unit.IsValid())
                return false;

            ResourceCollector resourceCollector = unit.GetComponentInChildren<ResourceCollector>();
            if (!resourceCollector.IsValid())
                return false;
            else if (!resourceType.IsValid())
                return true;

            SerializedObject resourceCollector_SO = new SerializedObject(resourceCollector);
            if (!resourceCollector_SO.IsValid())
                return false;

            var collectableResources = resourceCollector_SO.FindProperty("collectableResources");
            for (int i = 0; i < collectableResources.arraySize; i++)
            {
                var testResourceType = collectableResources.GetArrayElementAtIndex(i).FindPropertyRelative("type").objectReferenceValue as ResourceTypeInfo;
                if (testResourceType.Key == resourceType.Key)
                    return true;
            }

            return false;
        }
        private bool AddCollector(string unitCode, ResourceTypeInfo resourceType)
        {
            if (RTSEditorHelper.GetEntities().TryGetValue(unitCode, out IEntity unit))
                return AddCollector(unit as Unit, resourceType);

            return false;
        }
        private bool AddCollector(Unit unit, ResourceTypeInfo resourceType)
        {
            if (!unit.IsValid() || !resourceType.IsValid())
                return false;

            ResourceCollector resourceCollector = unit.GetComponentInChildren<ResourceCollector>();
            if (!resourceCollector.IsValid())
                return false;

            SerializedObject resourceCollector_SO = new SerializedObject(resourceCollector);
            if (!resourceCollector_SO.IsValid())
                return false;

            resourceCollector_SO.Update();
            var collectableResources = resourceCollector_SO.FindProperty("collectableResources");
            collectableResources.InsertArrayElementAtIndex(collectableResources.arraySize);
            collectableResources.GetArrayElementAtIndex(collectableResources.arraySize - 1).FindPropertyRelative("type").objectReferenceValue = resourceType;
            collectableResources.GetArrayElementAtIndex(collectableResources.arraySize - 1).FindPropertyRelative("amount").intValue = 1;
            resourceCollector_SO.ApplyModifiedProperties();

            // MISSING IS THE PART WHERE THE RESOURCE TYPE IS ADDED TO THE DropOffSource component

            return true;
        }

        private bool collectorsFoldout = false;
        private bool searchFoldout = false;
        private string newCollectorCode = "new_collector_code";
        protected void OnResourceInspectorGUITest()
        {
            base.OnResourceBuildingInspectorGUI();
            ResourceTypeInfo resourceType = SO.FindProperty("resourceType").objectReferenceValue as ResourceTypeInfo;

            return;

            if (!resourceType.IsValid())
                return;

            EditorGUILayout.Space();


            var collectors = RTSEditorHelper.GetEntities()
                .Values
                .Select(entity => entity as Unit)
                .Where(unit => CanCollect(unit, resourceType))
                .ToList();

            collectorsFoldout = EditorGUILayout.Foldout(collectorsFoldout, $"Collectors Count: {collectors.Count}");

            GUI.enabled = false;

            if (collectorsFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (var collector in collectors)
                {
                    EditorGUILayout.ObjectField(collector, typeof(Unit), allowSceneObjects: false);
                }
                EditorGUI.indentLevel--;
            }
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Add Collector By Code", EditorStyles.boldLabel);

            newCollectorCode = EditorGUILayout.TextField(newCollectorCode);

            string[] results = RTSEditorHelper.GetMatchingStrings(
                newCollectorCode,
                RTSEditorHelper.GetEntities().Keys.ToArray(),
                RTSEditorHelper.EntitySearchExceptions,
                code => !CanCollect(code, resourceType) && CanCollect(code));

            searchFoldout = EditorGUILayout.Foldout(searchFoldout, $"Suggestions: {results.Length}");

            if (results.Length > 0 && results.Length <= 5)
                searchFoldout = true;

            if (searchFoldout)
            {
                EditorGUI.indentLevel++;

                foreach (string result in results)
                {
                    if (GUILayout.Button(result))
                    {
                        AddCollector(result, resourceType);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }
        #endregion

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }
    }

    [CustomEditor(typeof(ResourceBuilding))]
    public class ResourceBuildingEditor : EntityEditor<ResourceBuilding>
    {
        private string[][] toolbars = new string[][] {
            new string[] { "Entity", "Faction Entity",  "Resource Building" }
        };

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }
    }

    public class EntityEditor<T> : TabsEditorBase<T> where T : Entity
    {
        protected override Int2D tabID {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        protected override void OnTabSwitch(string tabName)
        {
            switch (tabName)
            {
                case "Entity":
                    OnEntityInspectorGUI();
                    break;
                case "Faction Entity":
                    OnFactionEntityInspectorGUI();
                    break;
                case "Unit":
                    OnUnitInspectorGUI();
                    break;
                case "Building":
                    OnBuildingInspectorGUI();
                    break;
                case "Resource":
                    OnResourceInspectorGUI();
                    break;
                case "Resource Building":
                    OnResourceBuildingInspectorGUI();
                    break;
            }
        }

        private void OnDisable()
        {
            RTSEditorHelper.SetEntities();
        }

        protected virtual void OnEntityInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("_name"));
            EditorGUILayout.PropertyField(SO.FindProperty("code"));

            string category = SO.FindProperty("category").stringValue;
            EditorGUILayout.PropertyField(SO.FindProperty("category"));
            if(category != SO.FindProperty("category").stringValue)
                RTSEditorHelper.SetEntities();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("description"));
            EditorGUILayout.PropertyField(SO.FindProperty("icon"));
            if (comp is Unit)
                GUI.enabled = false;
            EditorGUILayout.PropertyField(SO.FindProperty("radius"));
            GUI.enabled = true;
            EditorGUILayout.PropertyField(SO.FindProperty("model"));
        }

        protected virtual void OnFactionEntityInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("isMainEntity"));
            EditorGUILayout.PropertyField(SO.FindProperty("isFactionLocked"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("initResources"));
            EditorGUILayout.PropertyField(SO.FindProperty("disableResources"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("coloredRenderers"));
        }

        protected virtual void OnUnitInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("spawnLookAt"));
        }

        protected virtual void OnBuildingInspectorGUI()
        {
            EditorGUILayout.LabelField("No fields.");
        }

        protected virtual void OnResourceInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("resourceType"));
            EditorGUILayout.PropertyField(SO.FindProperty("mainColor"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("canCollect"));
            EditorGUILayout.PropertyField(SO.FindProperty("canCollectOutsideBorder"));
            EditorGUILayout.PropertyField(SO.FindProperty("autoCollect"));
        }

        protected virtual void OnResourceBuildingInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("resourceType"));
            EditorGUILayout.PropertyField(SO.FindProperty("autoCollect"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("collectionAudio"));
        }
    }
}
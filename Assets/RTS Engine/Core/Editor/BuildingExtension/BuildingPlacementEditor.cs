using RTSEngine.BuildingExtension;
using RTSEngine.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RTSEngine.EditorOnly.BuildingExtension
{
    [CustomEditor(typeof(BuildingPlacement))]
    public class BuildingPlacementEditor : TabsEditorBase<BuildingPlacement>
    {
        protected override Int2D tabID {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        private string[][] toolbars = new string[][] {
            new string[] { "General", "Rotation", "Hold And Spawn" }
        };

        public override void OnInspectorGUI()
        {
            //EditorGUILayout.LabelField("Main", RTSEditorHelper.EditorTitleStyle);

            SO.Update();

            EditorGUILayout.PropertyField(SO.FindProperty("buildingPositionYOffset"));
            EditorGUILayout.PropertyField(SO.FindProperty("terrainMaxDistance"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("placableTerrainAreas"));
            EditorGUILayout.PropertyField(SO.FindProperty("ignoreTerrainAreas"));

            EditorGUILayout.Space();

            if(comp.gameObject.GetComponent<IBuildingPlacementHandler>() != null)
            {
                EditorGUILayout.HelpBox("A component that is responsible for local player building placement is attached to the same object!", MessageType.Warning);
                SO.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.LabelField("Local Player Faction Placement", RTSEditorHelper.EditorTitleStyle);

            EditorGUILayout.Space();

            SO.ApplyModifiedProperties();

            OnInspectorGUI(toolbars);
        }

        protected override void OnTabSwitch(string tabName)
        {
            switch(tabName)
            {
                case "General":
                    OnGeneralInspectorGUI();
                    break;
                case "Rotation":
                    OnRotationInspectorGUI();
                    break;
                case "Hold And Spawn":
                    OnHoldAndSpawnInspectorGUI();
                    break;
            }
        }

        protected virtual void OnGeneralInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("reservePlacementResources"));
            EditorGUILayout.PropertyField(SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("placeBuildingAudio"));
        }

        protected virtual void OnRotationInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("canRotate"));

            if (!SO.FindProperty("localFactionPlacementHandler").FindPropertyRelative("canRotate").boolValue)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("positiveRotationKey"));
            EditorGUILayout.PropertyField(SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("negativeRotationKey"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("rotationSpeed"));
        }

        private void OnHoldAndSpawnInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("holdAndSpawnEnabled"));
            
            if (!SO.FindProperty("localFactionPlacementHandler").FindPropertyRelative("holdAndSpawnEnabled").boolValue)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("holdAndSpawnKey"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("localFactionPlacementHandler")
                .FindPropertyRelative("preserveBuildingRotation"));
        }
    }
}
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using zacharysnewman.Inventory;

namespace zacharysnewman.Inventory.Editor
{
    [CustomEditor(typeof(ContainerDefinition))]
    public class ContainerDefinitionEditor : UnityEditor.Editor
    {
        private ReorderableList _acceptedTypesList;

        private void OnEnable()
        {
            var acceptedTypesProp = serializedObject.FindProperty("acceptedTypes");

            _acceptedTypesList = new ReorderableList(
                serializedObject, acceptedTypesProp,
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            _acceptedTypesList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Accepted Types");

            _acceptedTypesList.drawElementCallback = (rect, index, _, _) =>
            {
                rect.y += 1;
                rect.height = EditorGUIUtility.singleLineHeight;
                var element = acceptedTypesProp.GetArrayElementAtIndex(index);
                DrawTypePopup(rect, element, GUIContent.none);
            };

            _acceptedTypesList.onAddCallback = list =>
            {
                acceptedTypesProp.arraySize++;
                acceptedTypesProp.GetArrayElementAtIndex(acceptedTypesProp.arraySize - 1)
                    .stringValue = string.Empty;
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("acceptsAllTypes"));

            if (!serializedObject.FindProperty("acceptsAllTypes").boolValue)
                _acceptedTypesList.DoLayoutList();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("capacity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("capacityMode"));

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawTypePopup(Rect rect, SerializedProperty property, GUIContent label)
        {
            var config = ItemTypeSelectorDrawer.FindConfig();
            if (config == null || config.itemTypes.Count == 0)
            {
                EditorGUI.PropertyField(rect, property, label);
                return;
            }

            var options = new string[config.itemTypes.Count + 1];
            options[0] = "(None)";
            for (int i = 0; i < config.itemTypes.Count; i++)
                options[i + 1] = config.itemTypes[i];

            int current = Math.Max(0, Array.IndexOf(options, property.stringValue));
            int selected = EditorGUI.Popup(rect, label.text, current, options);
            property.stringValue = selected == 0 ? string.Empty : options[selected];
        }
    }
}

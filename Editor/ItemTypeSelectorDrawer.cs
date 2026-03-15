using System;
using UnityEditor;
using UnityEngine;
using zacharysnewman.Inventory;

namespace zacharysnewman.Inventory.Editor
{
    [CustomPropertyDrawer(typeof(ItemTypeSelectorAttribute))]
    public class ItemTypeSelectorDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var config = FindConfig();
            if (config == null || config.itemTypes.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var options = new string[config.itemTypes.Count + 1];
            options[0] = "(None)";
            for (int i = 0; i < config.itemTypes.Count; i++)
                options[i + 1] = config.itemTypes[i];

            int current = Math.Max(0, Array.IndexOf(options, property.stringValue));

            EditorGUI.BeginProperty(position, label, property);
            int selected = EditorGUI.Popup(position, label.text, current, options);
            property.stringValue = selected == 0 ? string.Empty : options[selected];
            EditorGUI.EndProperty();
        }

        internal static InventoryConfig FindConfig()
        {
            var guids = AssetDatabase.FindAssets("t:InventoryConfig");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<InventoryConfig>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}

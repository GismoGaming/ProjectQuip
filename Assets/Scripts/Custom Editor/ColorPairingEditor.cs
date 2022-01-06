#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gismo.PalletSwap.Editor
{
    [CustomPropertyDrawer(typeof(ColorPairing))]
    public class ColorPairingEditor : PropertyDrawer
    {
        private static readonly Dictionary<string, int> fieldCounts = new Dictionary<string, int>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int fieldCount = GetFieldCount(property);

            Rect contentPosition = EditorGUI.PrefixLabel(position, label);

            EditorGUIUtility.labelWidth = 50f;
            float fieldWidth = contentPosition.width / fieldCount;
            bool hideLabels = contentPosition.width < 185;
            contentPosition.width /= fieldCount;

            using var indent = new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel);
            for (int i = 0; i < fieldCount; i++)
            {
                if (!property.NextVisible(true))
                {
                    break;
                }

                label = EditorGUI.BeginProperty(contentPosition, new GUIContent(property.displayName), property);
                EditorGUI.PropertyField(contentPosition, property, hideLabels ? GUIContent.none : label);
                EditorGUI.EndProperty();

                contentPosition.x += fieldWidth;
            }
        }
        private static int GetFieldCount(SerializedProperty property)
        {
            if (!fieldCounts.TryGetValue(property.type, out int count))
            {
                var children = property.Copy().GetEnumerator();
                while (children.MoveNext())
                {
                    count++;
                }

                fieldCounts[property.type] = count;
            }

            return count;
        }
    }
}
#endif

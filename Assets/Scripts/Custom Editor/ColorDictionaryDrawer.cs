#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gismo.PalletSwap
{
    [CustomPropertyDrawer(typeof(ColorDictionary))]
    public class ColorDictionaryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ColorDictionary refrenceDictionary = (ColorDictionary)EditorExtensions.PropertyDrawerUtlities.GetValue(property);

            EditorGUI.BeginProperty(position, label, property);

            int indent = EditorGUI.indentLevel;

            //create object field for the sprite
            Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            SerializedProperty refrenceSprite = property.FindPropertyRelative("refrenceSprite");
            refrenceSprite.objectReferenceValue = EditorGUI.ObjectField(currentRect, refrenceSprite.displayName, refrenceSprite.objectReferenceValue, typeof(Sprite), false);


            currentRect.y += EditorGUIUtility.singleLineHeight;

            SerializedProperty compareAlpha = property.FindPropertyRelative("compareAlpha");
            EditorGUI.PropertyField(currentRect, compareAlpha, true);

            currentRect.y += EditorGUIUtility.singleLineHeight;

            if (GUI.Button(currentRect, "Generate Lookup Table"))
            {
                refrenceDictionary.GenerateColorLookup();
            }
            currentRect.y += EditorGUIUtility.singleLineHeight;

            SerializedProperty lookupTable = property.FindPropertyRelative("colorPairings");

            EditorGUI.PropertyField(currentRect, lookupTable, true);

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int totalLine = 4;

            if (property.FindPropertyRelative("colorPairings").isExpanded)
            {
                totalLine += property.FindPropertyRelative("colorPairings").arraySize + 1;
            }

            return EditorGUIUtility.singleLineHeight * totalLine + EditorGUIUtility.standardVerticalSpacing * (totalLine - 1);
        }
    }
}
#endif
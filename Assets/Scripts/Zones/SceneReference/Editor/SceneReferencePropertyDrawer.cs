using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Frankie.ZoneManagement.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferencePropertyDrawer : PropertyDrawer
    {
        // Tunables
        private float dotWidth = 18f;
        private float toggleWidth = 30f;

        // Constants
        public const string PROPERTY_SCENEASSET = "sceneAsset";
        public const string PROPERTY_SCENENAME = "sceneName";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect inputPosition = position;
            position = EditorGUI.PrefixLabel(position, label);
            Rect propertyRect = new Rect(position.xMin, position.yMin, Mathf.Max(position.width - toggleWidth, 0f), EditorGUIUtility.singleLineHeight);

            SerializedProperty sceneAssetProperty = property.FindPropertyRelative(PROPERTY_SCENEASSET);
            SerializedProperty sceneNameProperty = property.FindPropertyRelative(PROPERTY_SCENENAME);

            if (sceneAssetProperty.objectReferenceValue != null)
            {
                EditorGUI.BeginChangeCheck();
                sceneAssetProperty.objectReferenceValue = EditorGUI.ObjectField(propertyRect, GUIContent.none, sceneAssetProperty.objectReferenceValue, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    SceneAsset scene = sceneAssetProperty.objectReferenceValue as SceneAsset;
                    sceneNameProperty.stringValue = (scene != null) ? scene.name : string.Empty;
                }
            }
            else
            {
                Rect textRect = new Rect(propertyRect.xMin, propertyRect.yMin, Mathf.Max(propertyRect.width - dotWidth, 0f), propertyRect.height);
                Rect dotRect = new Rect(textRect.xMax, propertyRect.yMin, Mathf.Min(propertyRect.width - textRect.width, dotWidth), propertyRect.height);

                EditorGUI.BeginChangeCheck();
                sceneAssetProperty.objectReferenceValue = EditorGUI.ObjectField(dotRect, GUIContent.none, sceneAssetProperty.objectReferenceValue, typeof(SceneAsset), false);
                sceneNameProperty.stringValue = EditorGUI.TextField(textRect, sceneNameProperty.stringValue);

                Event interactionEvent = Event.current;
                if (interactionEvent.type == EventType.DragUpdated || interactionEvent.type == EventType.DragPerform)
                {
                    if (inputPosition.Contains(interactionEvent.mousePosition))
                    {
                        SceneAsset scene = DragAndDrop.objectReferences.FirstOrDefault((o) => o is SceneAsset) as SceneAsset;
                        DragAndDrop.visualMode = scene != null ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;

                        if (scene != null && interactionEvent.type == EventType.DragPerform)
                        {
                            sceneAssetProperty.objectReferenceValue = scene;
                            sceneNameProperty.stringValue = scene.name;
                        }
                    }
                }
            }

            Rect removeButtonRect = new Rect(propertyRect.xMax, position.yMin, Mathf.Min(toggleWidth, position.width - propertyRect.width), EditorGUIUtility.singleLineHeight);
            if (GUI.Button(removeButtonRect, "X"))
            {
                sceneAssetProperty.objectReferenceValue = null;
                sceneNameProperty.stringValue = string.Empty;
            }

            EditorGUI.EndProperty();
        }
    }
}

using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Frankie.ZoneManagement.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferencePropertyDrawer : PropertyDrawer
    {
        // Tunables
        private const float _dotWidth = 18f;
        private const float _toggleWidth = 30f;

        // Constants
        private const string _propertySceneAsset = "sceneAsset";
        private const string _propertySceneName = "sceneName";
        private const string _propertyScenePath = "scenePath";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect inputPosition = position;
            position = EditorGUI.PrefixLabel(position, label);
            Rect propertyRect = new Rect(position.xMin, position.yMin, Mathf.Max(position.width - _toggleWidth, 0f), EditorGUIUtility.singleLineHeight);

            SerializedProperty sceneAssetProperty = property.FindPropertyRelative(_propertySceneAsset);
            SerializedProperty sceneNameProperty = property.FindPropertyRelative(_propertySceneName);
            SerializedProperty scenePathProperty = property.FindPropertyRelative(_propertyScenePath);

            if (sceneAssetProperty.objectReferenceValue != null)
            {
                EditorGUI.BeginChangeCheck();
                sceneAssetProperty.objectReferenceValue = EditorGUI.ObjectField(propertyRect, GUIContent.none, sceneAssetProperty.objectReferenceValue, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    var scene = sceneAssetProperty.objectReferenceValue as SceneAsset;
                    sceneNameProperty.stringValue = (scene != null) ? scene.name : string.Empty;
                    scenePathProperty.stringValue = (scene != null) ? AssetDatabase.GetAssetPath(scene) : string.Empty;
                }
            }
            else
            {
                var textRect = new Rect(propertyRect.xMin, propertyRect.yMin, Mathf.Max(propertyRect.width - _dotWidth, 0f), propertyRect.height);
                var dotRect = new Rect(textRect.xMax, propertyRect.yMin, Mathf.Min(propertyRect.width - textRect.width, _dotWidth), propertyRect.height);

                EditorGUI.BeginChangeCheck();
                sceneAssetProperty.objectReferenceValue = EditorGUI.ObjectField(dotRect, GUIContent.none, sceneAssetProperty.objectReferenceValue, typeof(SceneAsset), false);
                sceneNameProperty.stringValue = EditorGUI.TextField(textRect, sceneNameProperty.stringValue);
                scenePathProperty.stringValue = EditorGUI.TextField(textRect, scenePathProperty.stringValue);

                Event interactionEvent = Event.current;
                if (interactionEvent.type is EventType.DragUpdated or EventType.DragPerform)
                {
                    if (inputPosition.Contains(interactionEvent.mousePosition))
                    {
                        var scene = DragAndDrop.objectReferences.FirstOrDefault((o) => o is SceneAsset) as SceneAsset;
                        DragAndDrop.visualMode = scene != null ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;

                        if (scene != null && interactionEvent.type == EventType.DragPerform)
                        {
                            sceneAssetProperty.objectReferenceValue = scene;
                            sceneNameProperty.stringValue = scene.name;
                            scenePathProperty.stringValue = AssetDatabase.GetAssetPath(scene);
                        }
                    }
                }
            }

            var removeButtonRect = new Rect(propertyRect.xMax, position.yMin, Mathf.Min(_toggleWidth, position.width - propertyRect.width), EditorGUIUtility.singleLineHeight);
            if (GUI.Button(removeButtonRect, "X"))
            {
                sceneAssetProperty.objectReferenceValue = null;
                sceneNameProperty.stringValue = string.Empty;
                scenePathProperty.stringValue = string.Empty;
            }

            EditorGUI.EndProperty();
        }
    }
}

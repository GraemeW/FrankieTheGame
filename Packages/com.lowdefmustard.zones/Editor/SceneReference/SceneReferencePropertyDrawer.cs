using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Zones.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferencePropertyDrawer : PropertyDrawer
    {
        // Tunables
        private const float _clearButtonWidth = 30f;

        // Constants
        private const string _propertySceneAsset = "sceneAsset";
        private const string _propertySceneName = "sceneName";
        private const string _propertyScenePath = "scenePath";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Pull out relevant properties
            SerializedProperty sceneAssetProperty = property.FindPropertyRelative(_propertySceneAsset);
            SerializedProperty sceneNameProperty = property.FindPropertyRelative(_propertySceneName);
            SerializedProperty scenePathProperty = property.FindPropertyRelative(_propertyScenePath);

            // Build UI
            var root = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            
            ObjectField assetField = MakeSceneAssetField(property.displayName);
            root.Add(assetField);

            Button clearButton = MakeClearButton();
            root.Add(clearButton);
            
            // Callbacks
            // Note:  field deliberately not bound due to quirk in Unity overwrites
            assetField.RegisterValueChangedCallback(evt => ApplyScene(sceneAssetProperty, sceneNameProperty, scenePathProperty, evt.newValue as SceneAsset));
            assetField.TrackPropertyValue(sceneAssetProperty, _ => Refresh());
            Refresh();
            
            clearButton.RegisterCallback<ClickEvent>(_ =>
            {
                ApplyScene(sceneAssetProperty, sceneNameProperty, scenePathProperty, null);
                Refresh();
            });
            
            return root;
            
            
            // Local Functions
            void Refresh()
            {
                assetField.showMixedValue = sceneAssetProperty.hasMultipleDifferentValues;
                assetField.SetValueWithoutNotify(sceneAssetProperty.objectReferenceValue);
            }
        }
        
        private static void ApplyScene(SerializedProperty assetProperty, SerializedProperty nameProperty, SerializedProperty pathProperty, SceneAsset scene)
        {
            string sceneName = scene != null ? scene.name : string.Empty;
            string scenePath = scene != null ? AssetDatabase.GetAssetPath(scene) : string.Empty;

            bool isUnchanged = !assetProperty.hasMultipleDifferentValues && assetProperty.objectReferenceValue == scene && nameProperty.stringValue == sceneName && pathProperty.stringValue == scenePath;
            if (isUnchanged) { return; }

            assetProperty.objectReferenceValue = scene;
            nameProperty.stringValue = sceneName;
            pathProperty.stringValue = scenePath;
            assetProperty.serializedObject.ApplyModifiedProperties();
        }

        private static ObjectField MakeSceneAssetField(string displayName)
        {
            var assetField = new ObjectField(displayName)
            {
                objectType = typeof(SceneAsset),
                allowSceneObjects = false
            };
            assetField.AddToClassList(BaseField<Object>.alignedFieldUssClassName);
            assetField.style.flexGrow = 1;
            return assetField;
        }

        private static Button MakeClearButton()
        {
            return new Button()
            {
                text = "X",
                tooltip = "Clear scene reference",
                style = { width = _clearButtonWidth }
            };
        }
    }
}

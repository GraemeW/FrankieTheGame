using System.IO;
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
        private const float _statusIndicatorDiameter = 10f;

        // Constants
        private const string _propertySceneAsset = "sceneAsset";
        private const string _propertySceneName = "sceneName";
        private const string _propertyScenePath = "scenePath";

        private static readonly Color _statusGreen = Color.lightGreen;
        private static readonly Color _statusOrange = Color.darkOrange;
        private static readonly Color _statusRed = Color.softRed;
        private static readonly Color _statusGray = Color.gray;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Pull out relevant properties
            SerializedProperty sceneAssetProperty = property.FindPropertyRelative(_propertySceneAsset);
            SerializedProperty sceneNameProperty = property.FindPropertyRelative(_propertySceneName);
            SerializedProperty scenePathProperty = property.FindPropertyRelative(_propertyScenePath);

            // Safety against loss of Scene reference - should then rebind via Path/Name (stable references)
            TryRelinkSceneAsset(sceneAssetProperty, sceneNameProperty, scenePathProperty);

            // Build UI
            var root = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            ObjectField assetField = MakeSceneAssetField(property.displayName);
            root.Add(assetField);

            VisualElement statusIndicator = MakeStatusIndicator();
            root.Add(statusIndicator);

            Button clearButton = MakeClearButton();
            root.Add(clearButton);

            // Callbacks
            // Note:  field deliberately not bound due to potential for quirky Unity overwrites
            assetField.RegisterValueChangedCallback(evt => ApplyScene(sceneAssetProperty, sceneNameProperty, scenePathProperty, evt.newValue as SceneAsset));
            root.TrackSerializedObjectValue(sceneAssetProperty.serializedObject, _ => Refresh());
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
                RefreshStatusIndicator(statusIndicator, sceneAssetProperty, sceneNameProperty, scenePathProperty);
            }
        }

        #region PrivateHelpers
        private static void ApplyScene(SerializedProperty assetProperty, SerializedProperty sceneNameProperty, SerializedProperty scenePathProperty, SceneAsset scene, bool recordUndo = true)
        {
            string sceneName = scene != null ? scene.name : string.Empty;
            string scenePath = scene != null ? AssetDatabase.GetAssetPath(scene) : string.Empty;

            bool isUnchanged = !assetProperty.hasMultipleDifferentValues && assetProperty.objectReferenceValue == scene && sceneNameProperty.stringValue == sceneName && scenePathProperty.stringValue == scenePath;
            if (isUnchanged) { return; }

            assetProperty.objectReferenceValue = scene;
            sceneNameProperty.stringValue = sceneName;
            scenePathProperty.stringValue = scenePath;

            if (recordUndo) { assetProperty.serializedObject.ApplyModifiedProperties(); }
            else { assetProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void TryRelinkSceneAsset(SerializedProperty assetProperty, SerializedProperty sceneNameProperty, SerializedProperty scenePathProperty)
        {
            SerializedObject serializedObject = assetProperty.serializedObject;
            if (assetProperty.objectReferenceValue != null) { return; } // Scene reference already exists
            if (serializedObject.isEditingMultipleObjects) { return; } // Multi-selection would copy one target's match onto all the others
            if (PrefabUtility.IsPartOfPrefabInstance(serializedObject.targetObject)) { return; } // Instances would gain an override instead of the prefab asset being healed

            string sceneName = sceneNameProperty.stringValue;
            string scenePath = scenePathProperty.stringValue;
            if (string.IsNullOrWhiteSpace(sceneName) && string.IsNullOrWhiteSpace(scenePath)) { return; } // Never set, nothing to repair

            SceneAsset scene = FindSceneAsset(scenePath, sceneName);
            if (scene == null)
            {
                Debug.LogWarning($"Could not repair scene reference for {assetProperty.displayName} (path: '{scenePath}', name: '{sceneName}')");
                return;
            }

            ApplyScene(assetProperty, sceneNameProperty, scenePathProperty, scene, recordUndo: false);
            Debug.Log($"Repaired scene reference for {assetProperty.displayName}: {AssetDatabase.GetAssetPath(scene)}");
        }

        private static SceneAsset FindSceneAsset(string scenePath, string sceneName)
        {
            // Try by Path
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                var sceneByPath = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneByPath != null) { return sceneByPath; }
            }

            // Try by Name
            if (string.IsNullOrWhiteSpace(sceneName)) { return null; }
            SceneAsset match = null;
            foreach (string guid in AssetDatabase.FindAssets($"t:SceneAsset {sceneName}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != sceneName) { continue; }

                if (match != null)
                {
                    Debug.LogWarning($"Attempting to repair scene reference for {sceneName}, but duplicate scene references found at 1. {AssetDatabase.GetAssetPath(match)} && 2. {path}.  Skipping repair process.");
                    return null;
                }
                match = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            }
            return match;
        }
        
        private static void RefreshStatusIndicator(VisualElement indicator, SerializedProperty assetProperty, SerializedProperty sceneNameProperty, SerializedProperty scenePathProperty)
        {
            if (assetProperty.hasMultipleDifferentValues || sceneNameProperty.hasMultipleDifferentValues || scenePathProperty.hasMultipleDifferentValues) { SetStatus(indicator, _statusGray, "Multiple values"); return; }

            var scene = assetProperty.objectReferenceValue as SceneAsset;
            string sceneName = sceneNameProperty.stringValue;
            string scenePath = scenePathProperty.stringValue;
            bool nameOrPathSet = !string.IsNullOrWhiteSpace(sceneName) || !string.IsNullOrWhiteSpace(scenePath);

            if (scene == null)
            {
                if (!nameOrPathSet) { SetStatus(indicator, _statusGray, "Scene not yet configured"); }
                else { SetStatus(indicator, _statusOrange, $"Scene reference missing for {sceneName} : {scenePath}"); }
                return;
            }

            bool matches = scene.name == sceneName && AssetDatabase.GetAssetPath(scene) == scenePath;
            if (matches) { SetStatus(indicator, _statusGreen, $"Scene configured with {sceneName} : {scenePath}"); }
            else { SetStatus(indicator, _statusRed, $"Inconsistent scene reference - actual {scene.name} vs. expected {sceneName} : {scenePath}"); }
        }

        private static void SetStatus(VisualElement indicator, Color color, string tooltip)
        {
            indicator.style.backgroundColor = color;
            indicator.tooltip = tooltip;
        }
        #endregion

        #region StaticUIBuilders
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

        private static VisualElement MakeStatusIndicator()
        {
            var indicator = new VisualElement();
            indicator.style.width = _statusIndicatorDiameter;
            indicator.style.height = _statusIndicatorDiameter;
            indicator.style.borderTopLeftRadius = _statusIndicatorDiameter / 2f;
            indicator.style.borderTopRightRadius = _statusIndicatorDiameter / 2f;
            indicator.style.borderBottomLeftRadius = _statusIndicatorDiameter / 2f;
            indicator.style.borderBottomRightRadius = _statusIndicatorDiameter / 2f;
            indicator.style.alignSelf = Align.Center;
            indicator.style.marginLeft = 4f;
            indicator.style.marginRight = 4f;
            return indicator;
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
        #endregion
    }
}

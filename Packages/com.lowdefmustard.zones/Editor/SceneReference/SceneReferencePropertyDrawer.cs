using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Core;

namespace LowDefMustard.Zones.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferencePropertyDrawer : PropertyDrawer
    {
        // Tunables
        private const float _clearButtonWidth = 30f;
        private const float _statusIndicatorDiameter = 10f;

        // Constants
        private static readonly Color _statusGreen = Color.lightGreen;
        private static readonly Color _statusOrange = Color.darkOrange;
        private static readonly Color _statusRed = Color.softRed;
        private static readonly Color _statusGray = Color.gray;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty sceneAssetProperty = property.FindPropertyRelative(SceneReference.sceneAssetField);
            SerializedProperty sceneNameProperty = property.FindPropertyRelative(SceneReference.sceneNameField);
            SerializedProperty scenePathProperty = property.FindPropertyRelative(SceneReference.scenePathField);
            SerializedProperty sceneGuidProperty = property.FindPropertyRelative(SceneReference.sceneGuidField);
            var properties = new SceneProperties(sceneAssetProperty, sceneNameProperty, scenePathProperty, sceneGuidProperty);

            // Safety against loss of Scene reference - should then rebind via Path/Name (stable references)
            TryRelinkSceneAsset(properties);
            TrySyncSceneGuid(properties);

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
            assetField.RegisterValueChangedCallback(evt => ApplyScene(properties, evt.newValue as SceneAsset));
            root.TrackSerializedObjectValue(sceneAssetProperty.serializedObject, _ => Refresh());
            Refresh();

            clearButton.RegisterCallback<ClickEvent>(_ =>
            {
                ApplyScene(properties, null);
                Refresh();
            });

            return root;


            // Local Functions
            void Refresh()
            {
                assetField.showMixedValue = sceneAssetProperty.hasMultipleDifferentValues;
                assetField.SetValueWithoutNotify(sceneAssetProperty.objectReferenceValue);
                RefreshStatusIndicator(statusIndicator, properties);
            }
        }

        #region PrivateHelpers
        private static void ApplyScene(SceneProperties properties, SceneAsset scene, bool recordUndo = true)
        {
            string sceneName = scene != null ? scene.name : string.Empty;
            string scenePath = scene != null ? AssetDatabase.GetAssetPath(scene) : string.Empty;
            string sceneGuid = scene != null ? AssetDatabase.AssetPathToGUID(scenePath) : string.Empty;

            bool isUnchanged = !properties.asset.hasMultipleDifferentValues && properties.asset.objectReferenceValue == scene
                && properties.name.stringValue == sceneName && properties.path.stringValue == scenePath && properties.guid.stringValue == sceneGuid;
            if (isUnchanged) { return; }

            properties.asset.objectReferenceValue = scene;
            properties.name.stringValue = sceneName;
            properties.path.stringValue = scenePath;
            properties.guid.stringValue = sceneGuid;

            if (recordUndo) { properties.asset.serializedObject.ApplyModifiedProperties(); }
            else { properties.asset.serializedObject.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void TryRelinkSceneAsset(SceneProperties properties)
        {
            SerializedProperty assetProperty = properties.asset;
            SerializedObject serializedObject = assetProperty.serializedObject;
            if (assetProperty.objectReferenceValue != null) { return; } // Scene reference already exists
            if (serializedObject.isEditingMultipleObjects) { return; } // Multi-selection would copy one target's match onto all the others
            if (PrefabUtility.IsPartOfPrefabInstance(serializedObject.targetObject)) { return; } // Instances would gain an override instead of the prefab asset being healed

            string sceneName = properties.name.stringValue;
            string scenePath = properties.path.stringValue;
            string sceneGuid = properties.guid.stringValue;
            if (string.IsNullOrWhiteSpace(sceneName) && string.IsNullOrWhiteSpace(scenePath) && string.IsNullOrWhiteSpace(sceneGuid)) { return; } // Never set, nothing to repair

            SceneAsset scene = FindSceneAsset(sceneGuid, scenePath, sceneName);
            if (scene == null)
            {
                LDMLog.Warning(LDMLogCategory.Zones, $"Could not repair scene reference for '{assetProperty.displayName}' (path: '{scenePath}', name: '{sceneName}'). Re-link it manually.");
                return;
            }

            ApplyScene(properties, scene, recordUndo: false);
            LDMLog.Info(LDMLogCategory.Zones, $"Repaired scene reference for '{assetProperty.displayName}' (path: '{AssetDatabase.GetAssetPath(scene)}').", assetProperty.serializedObject.targetObject);
        }
        
        private static void TrySyncSceneGuid(SceneProperties properties)
        {
            // Fills a missing / stale GUID
            if (properties.asset.hasMultipleDifferentValues || PrefabUtility.IsPartOfPrefabInstance(properties.asset.serializedObject.targetObject)) { return; }
            if (properties.asset.objectReferenceValue is not SceneAsset scene) { return; }

            string sceneGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(scene));
            if (properties.guid.stringValue == sceneGuid) { return; }
            properties.guid.stringValue = sceneGuid;
            properties.asset.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SceneAsset FindSceneAsset(string sceneGuid, string scenePath, string sceneName)
        {
            // Try by GUID
            if (!string.IsNullOrWhiteSpace(sceneGuid))
            {
                var sceneByGuid = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(sceneGuid));
                if (sceneByGuid != null) { return sceneByGuid; }
            }

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
                    LDMLog.Warning(LDMLogCategory.Zones, $"Found duplicate scenes named '{sceneName}' ('{AssetDatabase.GetAssetPath(match)}', '{path}'). Skipping scene reference repair.");
                    return null;
                }
                match = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            }
            return match;
        }
        
        private static void RefreshStatusIndicator(VisualElement indicator, SceneProperties properties)
        {
            if (properties.asset.hasMultipleDifferentValues || properties.name.hasMultipleDifferentValues || properties.path.hasMultipleDifferentValues || properties.guid.hasMultipleDifferentValues) { SetStatus(indicator, _statusGray, "Multiple values"); return; }

            var scene = properties.asset.objectReferenceValue as SceneAsset;
            string sceneName = properties.name.stringValue;
            string scenePath = properties.path.stringValue;
            string sceneGuid = properties.guid.stringValue;
            bool nameOrPathSet = !string.IsNullOrWhiteSpace(sceneName) || !string.IsNullOrWhiteSpace(scenePath);

            if (scene == null)
            {
                if (!nameOrPathSet) { SetStatus(indicator, _statusGray, "Scene not yet configured"); }
                else { SetStatus(indicator, _statusOrange, $"Scene reference missing for {sceneName} : {scenePath}"); }
                return;
            }

            bool matches = scene.name == sceneName && AssetDatabase.GetAssetPath(scene) == scenePath && AssetDatabase.AssetPathToGUID(scenePath) == sceneGuid;
            if (!matches) { SetStatus(indicator, _statusRed, $"Inconsistent scene reference - actual {scene.name} vs. expected {sceneName} : {scenePath}"); }
            else if (!AddressableSceneEntries.IsLoadableScene(sceneGuid)) { SetStatus(indicator, _statusOrange, $"Scene {sceneName} isn't loadable by GUID (an Addressables scene entry in a group with Include GUIDs in Catalog), so it can't load at runtime"); }
            else { SetStatus(indicator, _statusGreen, $"Scene configured with {sceneName} : {scenePath}"); }
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

#if UNITY_EDITOR
using System.Linq;
using Frankie.ZoneManagement;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Frankie.Core.GameStateModifiers
{
    [CustomEditor(typeof(GameStateModifier), true)]
    public class GameStateModifierEditor : Editor
    {
        // Functional State
        private GameStateModifier selectedGameStateModifier;
        private SerializedProperty gameStateModifierHandlerDataProperty;
        
        // Editor State
        private bool isListFoldedOut = true;
        private bool isEditingEnabled = false;

        // UI Styles
        private bool stylesInitialised;
        private readonly Color editingActiveColour = new Color(1f, 0.55f, 0.1f);
        private readonly Color editingInactiveColour = new Color(0.5f, 0.9f, 0.5f);
        protected GUIStyle headerStyle;
        private GUIStyle entryBoxStyle;
        private GUIStyle objectLabelStyle;
        private GUIStyle jumpButtonStyle;

        #region UnityMethods
        private void OnEnable()
        {
            selectedGameStateModifier = target as GameStateModifier;
            gameStateModifierHandlerDataProperty = serializedObject.FindProperty(GameStateModifier.GetGameStateModifierHandlerDataRef());
        }

        public override void OnInspectorGUI()
        {
            SetupStyles();
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, GameStateModifier.GetGameStateModifierHandlerDataRef(), "m_Script");
            
            MakeModifierHandlerHeader("Modifier Handler Data");
            MakeModifierHandlerList();
            serializedObject.ApplyModifiedProperties();
        }
        #endregion
        
        #region Draw Methods
        private void MakeModifierHandlerHeader(string headerTitle)
        {
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(headerTitle, headerStyle);
                GUILayout.FlexibleSpace();
                
                Color prevColor = GUI.contentColor;
                GUI.contentColor = isEditingEnabled ? editingActiveColour : editingInactiveColour;
                isEditingEnabled = GUILayout.Toggle(isEditingEnabled, isEditingEnabled ? "🔓 Editing" : "🔒 Locked", GUI.skin.button, GUILayout.Width(90));
                GUI.contentColor = prevColor;
            }
            EditorGUILayout.HelpBox( isEditingEnabled ? "Manual editing enabled.  Warning:  Does NOT update assets in scene, edit at your discretion." : "Manual editing locked.", isEditingEnabled ? MessageType.Warning : MessageType.Info);
            EditorGUILayout.Space(4);
        }

        private void MakeModifierHandlerList()
        {
            isListFoldedOut = EditorGUILayout.Foldout(isListFoldedOut, $"Linked Handlers ({gameStateModifierHandlerDataProperty.arraySize})", true);
            if (!isListFoldedOut) { return; }
            
            EditorGUI.indentLevel++;
            DrawList();
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space(4);
            DrawAddRemoveButtons();
            DrawCleanUpButton();
        }

        private void DrawList()
        {
            if (gameStateModifierHandlerDataProperty == null) { return; }
            
            if (gameStateModifierHandlerDataProperty.arraySize == 0)
            {
                EditorGUILayout.LabelField("No entries - add one below.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            for (int i = 0; i < gameStateModifierHandlerDataProperty.arraySize; i++)
            {
                DrawEntry(i);
                EditorGUILayout.Space(2);
            }
        }

        private void DrawEntry(int index)
        {
            SerializedProperty currentElement = gameStateModifierHandlerDataProperty?.GetArrayElementAtIndex(index);
            if (currentElement == null) { return;}
            
            SerializedProperty zoneNameProperty = currentElement.FindPropertyRelative(ZoneToGameObjectLinkData.GetZoneNameRef());
            SerializedProperty handlerParentObjectNameProperty = currentElement.FindPropertyRelative(ZoneToGameObjectLinkData.GetParentObjectNameRef());
            SerializedProperty handlerGameObjectNameProperty = currentElement.FindPropertyRelative(ZoneToGameObjectLinkData.GetGameObjectNameRef());
            SerializedProperty handlerGUIDProperty = currentElement.FindPropertyRelative(ZoneToGameObjectLinkData.GetGuidRef());

            using (new EditorGUILayout.VerticalScope(entryBoxStyle))
            {
                string zoneName = zoneNameProperty.stringValue;
                string handlerParentObjectName = handlerParentObjectNameProperty.stringValue;
                string handlerGameObjectName = handlerGameObjectNameProperty.stringValue;
                string handlerGUID = handlerGUIDProperty.stringValue;
                bool skipRenderingElement = AddEntryHeaderRow(index, zoneName, handlerGameObjectName, handlerParentObjectName, handlerGUID);
                if (skipRenderingElement) { return; }
                
                AddEntryFields(zoneNameProperty, handlerGameObjectNameProperty, handlerGUIDProperty);
            }
        }

        private bool AddEntryHeaderRow(int index, string zoneName, string handlerGameObjectName, string handlerParentObjectName, string handlerGUID)
        {
            bool skipRenderingElement = false;
            
            string parentStem = handlerParentObjectName != null ? $"{handlerParentObjectName}." : "";
            string displayName = string.IsNullOrWhiteSpace(handlerGameObjectName) || string.IsNullOrWhiteSpace(zoneName) ? $"Entry {index}" : $"{zoneName}/{parentStem}{handlerGameObjectName}";
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"[{index}]", GUILayout.Width(28));
                EditorGUILayout.LabelField(displayName, objectLabelStyle, GUILayout.ExpandWidth(true));
                bool viableSceneLoad = !string.IsNullOrWhiteSpace(zoneName) && !string.IsNullOrWhiteSpace(handlerGameObjectName);
                AddLoadZoneButton(viableSceneLoad, zoneName, handlerGameObjectName, handlerGUID);
                if (AddDeleteButton(index)) { skipRenderingElement = true; }
            }

            return skipRenderingElement;
        }

        private void AddLoadZoneButton(bool enabled, string zoneName, string handlerGameObjectName, string handlerGUID)
        {
            GUI.enabled = enabled;
            if (GUILayout.Button("Open & Select", jumpButtonStyle, GUILayout.Width(100)))
            {
                EditorApplication.delayCall += () => OpenSceneAndSelect(zoneName, handlerGameObjectName, handlerGUID);
            }
            GUI.enabled = true;
        }

        private bool AddDeleteButton(int index)
        {
            bool skipRenderingElement = false;
            if (gameStateModifierHandlerDataProperty == null) { return true; }
            
            using (new EditorGUI.DisabledScope(!isEditingEnabled))
            {
                GUIContent deleteIcon = EditorGUIUtility.IconContent("Toolbar Minus");
                if (GUILayout.Button(deleteIcon, GUILayout.Width(22), GUILayout.Height(18)))
                {
                    gameStateModifierHandlerDataProperty.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();

                    skipRenderingElement = true;
                }
            }
            return skipRenderingElement;
        }

        private void AddEntryFields(SerializedProperty zoneNameProperty, SerializedProperty gameObjectNameProperty, SerializedProperty guidProperty)
        {
            if (!isEditingEnabled) { return; }
            EditorGUILayout.Space(2);
            using (new EditorGUI.DisabledScope(!isEditingEnabled))
            {
                EditorGUILayout.PropertyField(zoneNameProperty, new GUIContent("Zone Name"));
                EditorGUILayout.PropertyField(gameObjectNameProperty, new GUIContent("Object Name"));
                EditorGUILayout.PropertyField(guidProperty, new GUIContent("GUID"));
            }
        }

        private void DrawAddRemoveButtons()
        {
            if (gameStateModifierHandlerDataProperty == null) { return; }
            
            using (new EditorGUI.DisabledScope(!isEditingEnabled))
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+ Add Entry", GUILayout.Width(100)))
                {
                    gameStateModifierHandlerDataProperty.arraySize++;
                    SerializedProperty newElement = gameStateModifierHandlerDataProperty.GetArrayElementAtIndex(gameStateModifierHandlerDataProperty.arraySize - 1);
                    newElement.FindPropertyRelative(ZoneToGameObjectLinkData.GetZoneNameRef()).stringValue = string.Empty;
                    newElement.FindPropertyRelative(ZoneToGameObjectLinkData.GetGameObjectNameRef()).stringValue = string.Empty;
                }

                if (gameStateModifierHandlerDataProperty.arraySize <= 0) { return; }
                if (GUILayout.Button("- Remove Last", GUILayout.Width(110))) { gameStateModifierHandlerDataProperty.arraySize--; }
            }
        }

        private void DrawCleanUpButton()
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove Invalid Entries", GUILayout.Width(180)))
                {
                    RemoveInvalidEntries();
                }
            }
        }
        #endregion

        #region ButtonFunctionality
        private static void OpenSceneAndSelect(string zoneName, string handlerGameObjectName, string handlerGUID)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }
            
            Zone zone = Zone.GetFromName(zoneName);
            bool didZoneOpen = OpenZone(zone);
            if (!didZoneOpen) { return; }
            
            SelectGameObject(handlerGUID, handlerGameObjectName);
        }

        private static bool OpenZone(Zone zone)
        {
            if (zone == null) { return false; }

            string scenePath = zone.GetSceneReference().GetScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("Scene Not Found", $"Could not locate {zone.name} in the project.","OK");
                return false;
            }
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"{zone.name} opened successfully.");
            return true;
        }

        private static void SelectGameObject(string handlerGUID, string handlerGameObjectName)
        {
            // Try first by GUID, back-up approach using game object name (though could be faulty, so send warning)
            GameObject foundGameObject = (from gameStateModifierHandler in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).OfType<IGameStateModifierHandler>() where gameStateModifierHandler.handlerGUID == handlerGUID select gameStateModifierHandler.gameObject).FirstOrDefault();
            if (foundGameObject == null)
            {
                Debug.LogWarning($"Warning:  GameStateModifierHandler GUID not found.  Attempting to find by name -- check GUID hook-up!");
                foundGameObject = FindObjectsByType<GameObject>(FindObjectsInactive.Include).FirstOrDefault(go => go.name == handlerGameObjectName);
            }
            
            if (foundGameObject == null) 
            { 
                Debug.LogWarning($"No GameObject {handlerGameObjectName} found.");
                return;
            }

            Selection.activeGameObject = foundGameObject;
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null) { sceneView.FrameSelected(); }
            Debug.Log($"{handlerGameObjectName} found and selected.");
        }
        
        private void RemoveInvalidEntries()
        {
            if (gameStateModifierHandlerDataProperty == null) { return; }
            if (selectedGameStateModifier == null) { return; }

            int removedCount = selectedGameStateModifier.CleanDanglingModifierHandlerData();
            serializedObject.ApplyModifiedProperties();

            string summary = removedCount == 0 ? "All entries are valid, nothing removed." : $"{removedCount} invalid entries removed.";
            EditorUtility.DisplayDialog("Remove Invalid Entries", summary, "OK");
        }
        #endregion

        #region UIStyles
        private void SetupStyles()
        {
            if (stylesInitialised) { return; }

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };

            entryBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(0, 0, 2, 2)
            };

            objectLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            jumpButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold
            };
            stylesInitialised = true;
        }
        #endregion
    }
}
#endif

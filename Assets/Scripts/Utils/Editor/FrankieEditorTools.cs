#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using LowDefMustard.Core;
using LowDefMustard.Control;

namespace Frankie.Utils.Editor
{
    public static class FrankieEditorTools
    {
        private const string _scenesFolderRef = "Assets/Scenes";
        private const string _zonesFolderRef = "Assets/Game/OnLoadAssets/Zones";
        private const string _characterPropertiesFolderRef = "Assets/Game/OnLoadAssets/CharacterProperties";
        private const string _characterObjectsFolderRef = "Assets/Game/CharacterObjects";
        private const string _skillsFolderRef = "Assets/Game/OnLoadAssets/Skills";
        private const string _skillTreesFolderRef = "Assets/Game/Combat/SkillTrees";
        private const string _battleActionsFolderRef = "Assets/Game/OnLoadAssets/BattleActions";
        private const string _battleActionPartsFolderRef = "Assets/Game/Combat/BattleActions";
        private const string _inventoryFolderRef = "Assets/Game/OnLoadAssets/Inventory";
        private const string _questsFolderRef = "Assets/Game/OnLoadAssets/Quests";
        private const string _startSceneRef = "Assets/Scenes/StartScreen.unity";
        private const string _debugSceneRef = "Assets/Scenes/_Debug/TEST_BattleRoyale.unity";
        private const string _debugPrefabRef = "Assets/Game/Core/CoreDep/Debugger.prefab";
        
        #region ToolBarEntries
        [MenuItem("Tools/Open/Debug Scene", false, 1)]
        private static void OpenDebugScene()
        {
            EditorSceneManager.OpenScene(_debugSceneRef);
        }
        
        [MenuItem("Tools/Open/Start Scene", false, 2)]
        private static void OpenStartScene()
        {
            EditorSceneManager.OpenScene(_startSceneRef);
        }
        
        [MenuItem("Tools/Select/Debugger", false, 1)]
        private static void SelectDebugger()
        {
            FrankieDebugger liveDebugger = Application.isPlaying ? Object.FindAnyObjectByType<FrankieDebugger>() : null;
            if (liveDebugger == null)
            {
                SelectAsset(_debugPrefabRef);
                return;
            }
            EditorGUIUtility.PingObject(liveDebugger);
            Selection.activeObject = liveDebugger.gameObject;
        }

        [MenuItem("Tools/Select/Scenes", false, 2)]
        private static void SelectScenesFolder() => OpenFolder(_scenesFolderRef);

        [MenuItem("Tools/Select/Zones", false, 3)]
        private static void SelectZonesFolder() => OpenFolder(_zonesFolderRef);

        [MenuItem("Tools/Select/Character/CharacterProperties", false, 4)]
        private static void SelectCharacterPropertiesFolder() => OpenFolder(_characterPropertiesFolderRef);

        [MenuItem("Tools/Select/Character/CharacterObjects", false, 5)]
        private static void SelectCharacterObjectsFolder() => OpenFolder(_characterObjectsFolderRef);

        [MenuItem("Tools/Select/Combat/Skills", false, 6)]
        private static void SelectSkillsFolder() => OpenFolder(_skillsFolderRef);

        [MenuItem("Tools/Select/Combat/SkillTrees", false, 7)]
        private static void SelectSkillTreesFolder() => OpenFolder(_skillTreesFolderRef);
        
        [MenuItem("Tools/Select/Combat/BattleActions", false, 8)]
        private static void SelectBattleActions() => OpenFolder(_battleActionsFolderRef);
        
        [MenuItem("Tools/Select/Combat/BattleActionParts", false, 9)]
        private static void SelectBattleActionParts() => OpenFolder(_battleActionPartsFolderRef);

        [MenuItem("Tools/Select/Inventory", false, 10)]
        private static void SelectInventoryFolder() => OpenFolder(_inventoryFolderRef);

        [MenuItem("Tools/Select/Quests", false, 11)]
        private static void SelectQuestsFolder() => OpenFolder(_questsFolderRef);

        [MenuItem("Tools/Make Selection Dirty", false, 100)]
        private static void MakeSelectionDirty()
        {
            foreach (Object selectedObject in Selection.objects)
            {
                if (selectedObject == null) { continue; }
                LDMLog.Info(LDMLogCategory.Utilities, $"Marking '{selectedObject.name}' dirty.", selectedObject);
                EditorUtility.SetDirty(selectedObject);
            }
        }

        [MenuItem("Tools/Force Reserialize Assets", false, 101)]
        private static void ForceReserializeAssets()
        {
            AssetDatabase.ForceReserializeAssets();
        }
        
        [MenuItem("Tools/Generate All Move Meshes", false, 501)]
        public static void GenerateAllMoveMeshes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }
            
            string originalScenePath = SceneManager.GetActiveScene().path;

            string[] scenePaths = FindAllScenes(_scenesFolderRef);
            if (scenePaths.Length == 0)
            {
                EditorUtility.DisplayDialog("Generate All Move Meshes", $"No scenes found under {_scenesFolderRef}.", "OK");
                return;
            }

            int totalScenes = scenePaths.Length;
            int scenesComplete = 0;
            try
            {
                foreach (string scenePath in scenePaths)
                {
                    string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    
                    bool cancelled = EditorUtility.DisplayCancelableProgressBar("Generate All Move Meshes", $"Opening scene: {sceneName}  ({scenesComplete + 1} / {totalScenes})", (float)scenesComplete / totalScenes);
                    if (cancelled) { break; }

                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    List<MoveMesh> moveMeshes = FindAllMoveMeshes();
                    if (moveMeshes.Count == 0)
                    {
                        scenesComplete++;
                        continue;
                    }

                    ProcessMoveMeshes(moveMeshes, sceneName, scenesComplete, totalScenes);
                    EditorSceneManager.SaveOpenScenes();
                    scenesComplete++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(originalScenePath)) { EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single); }
            }

            LDMLog.Info(LDMLogCategory.Control, $"Generated move meshes ({scenesComplete} of {totalScenes} scenes processed).");

            EditorUtility.DisplayDialog("Generate All Move Meshes", $"Processed {scenesComplete} / {totalScenes} scene(s).", "OK");
        }
        #endregion

        #region PrivateUtilityMethods
        private static void SelectAsset(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset == null) { return; }
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        private static void OpenFolder(string path)
        {
            var folder = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (folder == null) { return; }
            EditorUtility.FocusProjectWindow();

            EditorWindow projectBrowser = EditorWindow.focusedWindow;
            MethodInfo showFolderContents = projectBrowser != null ? projectBrowser.GetType().GetMethod("ShowFolderContents", BindingFlags.Instance | BindingFlags.NonPublic) : null;
            if (showFolderContents == null)
            {
                SelectAsset(path);
                return;
            }
            showFolderContents.Invoke(projectBrowser, new object[] { folder.GetEntityId(), true });
        }

        private static void ProcessMoveMeshes(List<MoveMesh> moveMeshes, string sceneName, int sceneIndex, int totalScenes)
        {
            int moveMeshCount = moveMeshes.Count;
            int moveMeshesComplete = 0;

            foreach (MoveMesh moveMesh in moveMeshes)
            {
                var localCompleted = moveMeshesComplete;
                string moveMeshName = moveMesh.gameObject.name;
                
                bool cancelled = EditorUtility.DisplayCancelableProgressBar($"Scene {sceneIndex + 1}/{totalScenes}: {sceneName}", $"Running detection on: {moveMeshName}  ({moveMeshesComplete + 1} / {moveMeshCount})", (float)moveMeshesComplete / moveMeshCount);
                if (cancelled) { return; }
                
                Undo.RecordObject(moveMesh, "Generate NavMesh");
                moveMesh.RunDetection((message, progress) =>
                {
                    float moveMeshBase  = (float)localCompleted / moveMeshCount;
                    float moveMeshShare = 1f / moveMeshCount;
                    EditorUtility.DisplayCancelableProgressBar($"Scene {sceneIndex + 1}/{totalScenes}: {sceneName}", $"{moveMeshName}: {message}", moveMeshBase + moveMeshShare * progress);
                });

                EditorUtility.SetDirty(moveMesh);
                moveMeshesComplete++;
            }
        }
        
        private static List<MoveMesh> FindAllMoveMeshes()
        {
            var results = new List<MoveMesh>();
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                results.AddRange(root.GetComponentsInChildren<MoveMesh>(true));
            }
            return results;
        }

        private static string[] FindAllScenes(string rootFolder)
        {
            if (!Directory.Exists(rootFolder)) { return System.Array.Empty<string>(); }
            
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { rootFolder });
            var paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }
            return paths;
        }
        #endregion
    }
}
#endif

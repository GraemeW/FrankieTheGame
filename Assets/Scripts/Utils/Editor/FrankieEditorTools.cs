#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Frankie.ZoneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Frankie.Utils.Editor
{
    public static class FrankieEditorTools
    {
        private const string _scenesFolderRef = "Assets/Scenes";
        private const string _startSceneRef = "Assets/Scenes/StartScreen.unity";
        private const string _debugSceneRef = "Assets/Scenes/_Debug/TEST_BattleRoyale.unity";
        private const string _debugPrefabRef = "Assets/Game/Core/CoreDep/Debugger.prefab";
        
        #region ToolBarEntries
        [MenuItem("Tools/Open Debug Scene", false, 1)]
        private static void OpenDebugScene()
        {
            EditorSceneManager.OpenScene(_debugSceneRef);
        }

        [MenuItem("Tools/Select Debugger Prefab", false, 2)]
        private static void SelectDebugger()
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(_debugPrefabRef);
            if (asset == null) { return; }
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }
        
        [MenuItem("Tools/Open Start Scene", false, 5)]
        private static void OpenStartScene()
        {
            EditorSceneManager.OpenScene(_startSceneRef);
        }
        
        [MenuItem("Tools/Make Selection Dirty", false, 100)]
        private static void MakeSelectionDirty()
        {
            foreach (Object selectedObject in Selection.objects)
            {
                if (selectedObject == null) { continue; }
                Debug.Log($"Dirtying {selectedObject.name}");
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

            Debug.Log($"Generate All Move Meshes - Done. Processed {scenesComplete} / {totalScenes} scene(s).");

            EditorUtility.DisplayDialog("Generate All Move Meshes", $"Processed {scenesComplete} / {totalScenes} scene(s).", "OK");
        }
        #endregion

        
        
        #region PrivateMethods
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

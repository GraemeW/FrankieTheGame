#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LowDefMustard.Zones.Tests
{
    // Test Notes:
    //  - Using a separate fixture w/ IPrebuildSetup/Cleanup instead of traditional [OneTimeSetup/Teardown]
    //      - this is required because EditorSceneManager methods can ONLY be called in Editor mode
    
    public class ScratchSceneFixtureSetup : IPrebuildSetup, IPostBuildCleanup
    {
        // Const Tunables
        public const string scratchScenePath = "Assets/ScratchTest_SafeToDelete.unity";
        public const string scratchSceneName = "ScratchTest_SafeToDelete";
        
        public void Setup()
        {
            Scene scratchScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SaveScene(scratchScene, scratchScenePath);
            EditorSceneManager.CloseScene(scratchScene, true);
            
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == scratchScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scratchScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        public void Cleanup()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int removedCount = scenes.RemoveAll(s => s.path == scratchScenePath);
            if (removedCount > 0) { EditorBuildSettings.scenes = scenes.ToArray(); } 
            
            AssetDatabase.DeleteAsset(scratchScenePath);
            AssetDatabase.Refresh();
        }
    }
}
#endif

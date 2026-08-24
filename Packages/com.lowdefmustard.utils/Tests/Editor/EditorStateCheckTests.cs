using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LowDefMustard.Utils.Tests.Editor
{
    // Coverage map for IsStandardEditorState's guard clauses
    //
    // Covered:
    //   - Application.isPlaying / EditorApplication.isPlaying (combined, via Play Mode)
    //   - gameObject == null (true null and Unity fake-null)
    //   - EditorUtility.IsPersistent (temp prefab asset)
    //   - PrefabStageUtility.GetCurrentPrefabStage() (temp opened prefab stage)
    //   - EditorSceneManager.IsPreviewScene (temp preview scene)
    //   - happy path (normal loaded scene object)
    //
    // NOT covered, and not practically testable:
    //   - !Application.isEditor - only false in a real player build, where UNITY_EDITOR isn't defined and this method is a no-op stub anyway
    //   - EditorApplication.isPlayingOrWillChangePlaymode in isolation - true only during the play/stop transition itself, too narrow window to test
    //   - EditorApplication.isCompiling / isUpdating - no seam to force these states (would require trigger recompile/asset refresh mid-test)
    //   - !gameObject.scene.isLoaded in isolation - unloading a scene generally destroys its GameObjects too, so "GameObject alive, scene unloaded" isn't a reliably constructible state
    
    public class EditorStateCheckTests
    {
        // Obviously disposable - safe to delete this folder if it's ever found lingering in the project (e.g. after a crashed test run)
        private const string _tempFolder = "Assets/_TEMP_EditorStateCheckTests_SafeToDelete";

        [TearDown]
        public void TearDown()
        {
            // Safety net against cross-test contamination - prefab stage is global editor state, not scoped to a single test
            // i.e. if an earlier test threw before its own cleanup ran, later tests would see a false failure here
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                StageUtility.GoToMainStage();
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (AssetDatabase.IsValidFolder(_tempFolder))
            {
                AssetDatabase.DeleteAsset(_tempFolder);
            }
        }

        [Test]
        public void IsStandardEditorState_NullGameObject_ReturnsFalse()
        {
            GameObject gameObject = null;

            Assert.IsFalse(EditorStateCheck.IsStandardEditorState(gameObject));
        }

        [Test]
        public void IsStandardEditorState_DestroyedGameObject_ReturnsFalse()
        {
            // Fake-null case
            var gameObject = new GameObject("Temp");
            Object.DestroyImmediate(gameObject);

            Assert.IsFalse(EditorStateCheck.IsStandardEditorState(gameObject));
        }

        [Test]
        public void IsStandardEditorState_NormalLoadedSceneObject_ReturnsTrue()
        {
            var gameObject = new GameObject("Temp");
            try
            {
                Assert.IsTrue(EditorStateCheck.IsStandardEditorState(gameObject));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void IsStandardEditorState_ObjectInPreviewScene_ReturnsFalse()
        {
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                var gameObject = new GameObject("Temp");
                SceneManager.MoveGameObjectToScene(gameObject, previewScene);

                Assert.IsFalse(EditorStateCheck.IsStandardEditorState(gameObject));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        [Test]
        public void IsStandardEditorState_PersistentPrefabAsset_ReturnsFalse()
        {
            EnsureTempFolderExists();
            var path = $"{_tempFolder}/EditorStateCheckTestPrefab.prefab";
            var sceneInstance = new GameObject("TempPrefabSource");

            try
            {
                GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(sceneInstance, path);

                Assert.IsFalse(EditorStateCheck.IsStandardEditorState(prefabAsset));
            }
            finally
            {
                Object.DestroyImmediate(sceneInstance);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void IsStandardEditorState_InsidePrefabStage_ReturnsFalse()
        {
            EnsureTempFolderExists();
            var path = $"{_tempFolder}/EditorStateCheckTestPrefabStage.prefab";
            var sceneInstance = new GameObject("TempPrefabStageSource");

            try
            {
                PrefabUtility.SaveAsPrefabAsset(sceneInstance, path);
                var prefabStage = PrefabStageUtility.OpenPrefab(path);

                Assert.IsFalse(EditorStateCheck.IsStandardEditorState(prefabStage.prefabContentsRoot));
            }
            finally
            {
                StageUtility.GoToMainStage();
                Object.DestroyImmediate(sceneInstance);
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void EnsureTempFolderExists()
        {
            if (!AssetDatabase.IsValidFolder(_tempFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_TEMP_EditorStateCheckTests_SafeToDelete");
            }
        }
    }
}

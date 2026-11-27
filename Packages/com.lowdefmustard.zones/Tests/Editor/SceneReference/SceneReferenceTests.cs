using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class SceneReferenceTests
    {
        // Const Tunables
        [NoAutoStaticsCleanup] private static readonly Regex _noPathWarning = new(@"Scene path for '.*' is not configured\. Re-link the scene reference in the editor\.$");
        private const string _sceneGuid = "0123456789abcdef0123456789abcdef";

        // State
        private TempAssetFolder tempFolder;

        #region Setup
        [TearDown]
        public void TearDown()
        {
            if (tempFolder == null) { return; }
            SceneTestUtility.ResetToEmptyScene();
            tempFolder.Dispose();
            tempFolder = null;
        }
        #endregion

        #region Tests
        [Test]
        public void Constructor_SetsSceneName_IsSetTrue()
        {
            var reference = new SceneReference("MyScene");

            Assert.AreEqual("MyScene", reference.SceneName);
            Assert.IsTrue(reference.IsSet());
        }

        [Test]
        public void DefaultInstance_IsSetFalse()
        {
            var reference = default(SceneReference);

            Assert.IsFalse(reference.IsSet());
        }

        [Test]
        public void SceneNameSetter_SameValue_DoesNotClearCachedPath()
        {
            var reference = new SceneReference("MyScene") { scenePath = "Assets/MyScene.unity" };

            reference.SceneName = "MyScene";

            Assert.AreEqual("Assets/MyScene.unity", reference.GetScenePath());
        }

        [Test]
        public void SceneNameSetter_DifferentValue_ClearsCachedPath()
        {
            var reference = new SceneReference("MyScene") { scenePath = "Assets/MyScene.unity" };

            reference.SceneName = "OtherScene";

            LogAssert.Expect(LogType.Warning, _noPathWarning);
            Assert.AreEqual(string.Empty, reference.GetScenePath());
        }

        [Test]
        public void GetScenePath_ReturnsCachedPath_WhenSet()
        {
            var reference = new SceneReference("MyScene") { scenePath = "Assets/MyScene.unity" };

            Assert.AreEqual("Assets/MyScene.unity", reference.GetScenePath());
        }

        [Test]
        public void GetScenePath_LogsWarningAndReturnsEmpty_WhenNothingConfigured()
        {
            var reference = new SceneReference("MyScene");

            LogAssert.Expect(LogType.Warning, _noPathWarning);
            Assert.AreEqual(string.Empty, reference.GetScenePath());
        }

        [Test]
        public void TryGetSceneGuid_StoredGuid_ReturnsIt()
        {
            var reference = new SceneReference("MyScene") { sceneGuid = _sceneGuid };

            Assert.IsTrue(reference.TryGetSceneGuid(out string guid));
            Assert.AreEqual(_sceneGuid, guid);
        }

        [Test]
        public void SceneNameSetter_DifferentValue_ClearsStoredGuid()
        {
            var reference = new SceneReference("MyScene") { sceneGuid = _sceneGuid };

            reference.SceneName = "OtherScene";

            Assert.IsFalse(reference.TryGetSceneGuid(out string guid));
            Assert.IsNull(guid);
        }

        [Test]
        public void TryGetSceneGuid_NotStored_ReturnsFalse_EvenWithSceneAsset()
        {
            // No editor fallback:  builds only have the stored GUID, so a missing one shows in the editor too
            tempFolder = new TempAssetFolder(nameof(SceneReferenceTests));
            Scene scratchScene = SceneTestUtility.CreateSavedScene(tempFolder, "ScratchTest_SceneReferenceGetSceneGuid");
            string scratchScenePath = scratchScene.path;
            EditorSceneManager.CloseScene(scratchScene, true);
            var reference = new SceneReference("MyScene") { sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scratchScenePath) };

            Assert.IsFalse(reference.TryGetSceneGuid(out string guid));
            Assert.IsNull(guid);
        }

        [Test]
        public void ImplicitOperator_FromString_SetsSceneName()
        {
            SceneReference reference = "SomeSceneName";

            Assert.AreEqual("SomeSceneName", reference.SceneName);
        }

        [Test]
        public void ImplicitOperator_ToString_ReturnsSceneName()
        {
            var reference = new SceneReference("SomeSceneName");

            string result = reference;

            Assert.AreEqual("SomeSceneName", result);
        }

        [Test]
        public void GetScenePath_WithRealSceneAssetAndNoCachedPath_ReturnsAssetPath()
        {
            tempFolder = new TempAssetFolder(nameof(SceneReferenceTests));
            Scene scratchScene = SceneTestUtility.CreateSavedScene(tempFolder, "ScratchTest_SceneReferenceGetScenePath");
            string scratchScenePath = scratchScene.path;
            EditorSceneManager.CloseScene(scratchScene, true);

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scratchScenePath);
            var reference = new SceneReference("MyScene") { sceneAsset = sceneAsset };

            Assert.AreEqual(scratchScenePath, reference.GetScenePath());
        }
        #endregion
    }
}

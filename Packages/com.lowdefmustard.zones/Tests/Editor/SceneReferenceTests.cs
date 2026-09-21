using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class SceneReferenceTests
    {
        // Const Tunables
        private const string _noPathWarning = "Scene path is not configured!  Please re-link the scene reference in editor";
        private const string _scratchScenePath = "Assets/ScratchTest_SceneReferenceGetScenePath_SafeToDelete.unity";

        // State
        private bool createdScratchSceneAsset;

        #region Setup
        [TearDown]
        public void TearDown()
        {
            if (!createdScratchSceneAsset) { return; }
            AssetDatabase.DeleteAsset(_scratchScenePath);
            AssetDatabase.Refresh();
            createdScratchSceneAsset = false;
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
            Scene scratchScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scratchScene, _scratchScenePath);
            EditorSceneManager.CloseScene(scratchScene, true);
            createdScratchSceneAsset = true;

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(_scratchScenePath);
            var reference = new SceneReference("MyScene") { sceneAsset = sceneAsset };

            Assert.AreEqual(_scratchScenePath, reference.GetScenePath());
        }
        #endregion
    }
}

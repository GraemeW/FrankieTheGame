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

        // State
        private TempAssetFolder tempFolder;

        #region Setup
        [TearDown]
        public void TearDown()
        {
            if (tempFolder == null) { return; }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // Nothing scratch left open when deleting
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
            string scratchScenePath = tempFolder.Path("ScratchTest_SceneReferenceGetScenePath.unity");
            Scene scratchScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scratchScene, scratchScenePath);
            EditorSceneManager.CloseScene(scratchScene, true);

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scratchScenePath);
            var reference = new SceneReference("MyScene") { sceneAsset = sceneAsset };

            Assert.AreEqual(scratchScenePath, reference.GetScenePath());
        }
        #endregion
    }
}

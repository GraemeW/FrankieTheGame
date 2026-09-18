using System.Collections.Generic;
using System.Linq;
using LowDefMustard.Zones.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneHandlerConduitOpenLinkedScenePathsTests
    {
        // Const Tunables
        private const string _scratchScenePath = "Assets/ScratchTest_OpenLinkedScenePaths_SafeToDelete.unity";
        private const string _scratchSceneName = "ScratchTest_OpenLinkedScenePaths_SafeToDelete";

        // State
        private Zone rootZone;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            Scene scratchScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scratchScene, _scratchScenePath);

            rootZone = ScriptableObject.CreateInstance<Zone>();
            rootZone.sceneReference = _scratchSceneName;
            rootZone.sceneReference.scenePath = _scratchScenePath;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(rootZone);
            AssetDatabase.DeleteAsset(_scratchScenePath);
            AssetDatabase.Refresh();
        }
        #endregion

        #region Tests
        [Test]
        public void OpenLinkedScenePaths_NoLinkedZones_YieldsOnlyRootScenePath()
        {
            List<string> result = ZoneHandlerConduit.OpenLinkedScenePaths(rootZone, maxZoneCount: 1, existingViewScenePaths: new HashSet<string>(), showProgressBar: false).ToList();

            CollectionAssert.AreEqual(new[] { _scratchScenePath }, result);
        }

        [Test]
        public void OpenLinkedScenePaths_RootAlreadyInExistingViews_YieldsNothing()
        {
            var existingViewScenePaths = new HashSet<string> { _scratchScenePath };

            List<string> result = ZoneHandlerConduit.OpenLinkedScenePaths(rootZone, maxZoneCount: 1, existingViewScenePaths, showProgressBar: false).ToList();

            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void OpenLinkedScenePaths_NullRootZone_YieldsNothing()
        {
            List<string> result = ZoneHandlerConduit.OpenLinkedScenePaths(null, maxZoneCount: 1, new HashSet<string>(), showProgressBar: false).ToList();

            CollectionAssert.IsEmpty(result);
        }
        #endregion
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using LowDefMustard.Utils.Tests;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneToolsTests
    {
        // Const Tunables
        private const string _scratchSceneName = "ScratchTest_ZoneToolsOpenSceneAndAct_SafeToDelete";

        // State
        private Zone zone;
        private TempAssetFolder tempFolder;
        private Dictionary<string, Zone> originalZoneLookupCache;
        private Dictionary<string, Zone> originalSceneReferenceCache;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            zone = ScriptableObject.CreateInstance<Zone>();
            zone.preventLocalizationForTests = true;
            zone.hideFlags = HideFlags.HideAndDontSave; // Loading a scene can unload loose unreferenced objects (so HideAndDontSave to keep it alive across)
            originalZoneLookupCache = Zone.zoneLookupCache;
            originalSceneReferenceCache = Zone.sceneReferenceCache;
        }

        [TearDown]
        public void TearDown()
        {
            Zone.zoneLookupCache = originalZoneLookupCache;
            Zone.sceneReferenceCache = originalSceneReferenceCache;
            Object.DestroyImmediate(zone);

            if (tempFolder == null) { return; }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // Nothing scratch left open when deleting
            tempFolder.Dispose();
            tempFolder = null;
        }
        #endregion

        #region PrivateMethods
        private SceneAsset CreateScratchSceneAsset()
        {
            tempFolder = new TempAssetFolder(nameof(ZoneToolsTests));
            string scratchScenePath = tempFolder.Path($"{_scratchSceneName}.unity");
            Scene scratchScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scratchScene, scratchScenePath);
            EditorSceneManager.CloseScene(scratchScene, true);
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(scratchScenePath);
        }
        #endregion

        #region Tests
        [Test]
        public void OpenSceneAndAct_NullZone_DoesNotInvokeCallback()
        {
            bool invoked = false;

            ZoneTools.OpenSceneAndAct((Zone)null, () => invoked = true, suppressDialogs: true);

            Assert.IsFalse(invoked);
        }

        [Test]
        public void OpenSceneAndAct_ScenePathEmpty_SuppressedDialogs_DoesNotInvokeCallback()
        {
            bool invoked = false;

            LogAssert.ignoreFailingMessages = true; // SceneReference.GetScenePath's own "not configured" warning
            ZoneTools.OpenSceneAndAct(zone, () => invoked = true, suppressDialogs: true);
            LogAssert.ignoreFailingMessages = false;

            Assert.IsFalse(invoked);
        }

        [Test]
        public void OpenSceneAndAct_ValidZone_SuppressedDialogs_OpensSceneAndInvokesCallback()
        {
            SceneAsset sceneAsset = CreateScratchSceneAsset();
            zone.sceneReference = new SceneReference(_scratchSceneName) { sceneAsset = sceneAsset };

            bool invoked = false;
            ZoneTools.OpenSceneAndAct(zone, () => invoked = true, suppressDialogs: true);

            Assert.IsTrue(invoked);
            Assert.AreEqual(_scratchSceneName, SceneManager.GetActiveScene().name);
        }

        [Test]
        public void OpenSceneAndAct_ByName_ResolvesZoneViaCache()
        {
            SceneAsset sceneAsset = CreateScratchSceneAsset();
            zone.name = "ZoneA";
            zone.sceneReference = new SceneReference(_scratchSceneName) { sceneAsset = sceneAsset };
            Zone.zoneLookupCache = new Dictionary<string, Zone> { { "ZoneA", zone } };
            Zone.sceneReferenceCache = new Dictionary<string, Zone>() { { _scratchSceneName, zone } };

            bool invoked = false;
            ZoneTools.OpenSceneAndAct("ZoneA", () => invoked = true, suppressDialogs: true);

            // Only reachable if Zone.GetFromName("ZoneA") actually resolved to our seeded zone
            //  - a null lookup result would have returned before ever opening a scene or invoking the callback
            Assert.IsTrue(invoked);
            Assert.AreEqual(_scratchSceneName, SceneManager.GetActiveScene().name);
        }
        #endregion
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneToolsTests
    {
        // Const Tunables
        private const string _scratchScenePath = "Assets/ScratchTest_ZoneToolsOpenSceneAndAct_SafeToDelete.unity";

        // State
        private Zone zone;
        private bool createdScratchSceneAsset;
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

            if (!createdScratchSceneAsset) { return; }
            AssetDatabase.DeleteAsset(_scratchScenePath);
            AssetDatabase.Refresh();
            createdScratchSceneAsset = false;
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
            Scene scratchScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scratchScene, _scratchScenePath);
            EditorSceneManager.CloseScene(scratchScene, true);
            createdScratchSceneAsset = true;

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(_scratchScenePath);
            zone.sceneReference = new SceneReference("ScratchTest_ZoneToolsOpenSceneAndAct_SafeToDelete") { sceneAsset = sceneAsset };

            bool invoked = false;
            ZoneTools.OpenSceneAndAct(zone, () => invoked = true, suppressDialogs: true);

            Assert.IsTrue(invoked);
            Assert.AreEqual("ScratchTest_ZoneToolsOpenSceneAndAct_SafeToDelete", SceneManager.GetActiveScene().name);
        }

        [Test]
        public void OpenSceneAndAct_ByName_ResolvesZoneViaCache()
        {
            Scene scratchScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scratchScene, _scratchScenePath);
            EditorSceneManager.CloseScene(scratchScene, true);
            createdScratchSceneAsset = true;

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(_scratchScenePath);
            zone.name = "ZoneA";
            zone.sceneReference = new SceneReference("ScratchTest_ZoneToolsOpenSceneAndAct_SafeToDelete") { sceneAsset = sceneAsset };
            Zone.zoneLookupCache = new Dictionary<string, Zone> { { "ZoneA", zone } };
            Zone.sceneReferenceCache = new Dictionary<string, Zone>() { { "ScratchTest_ZoneToolsOpenSceneAndAct_SafeToDelete", zone } };

            bool invoked = false;
            ZoneTools.OpenSceneAndAct("ZoneA", () => invoked = true, suppressDialogs: true);

            // Only reachable if Zone.GetFromName("ZoneA") actually resolved to our seeded zone
            //  - a null lookup result would have returned before ever opening a scene or invoking the callback
            Assert.IsTrue(invoked);
            Assert.AreEqual("ScratchTest_ZoneToolsOpenSceneAndAct_SafeToDelete", SceneManager.GetActiveScene().name);
        }
        #endregion
    }
}

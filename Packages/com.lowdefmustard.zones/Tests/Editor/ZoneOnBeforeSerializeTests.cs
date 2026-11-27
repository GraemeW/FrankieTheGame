using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneOnBeforeSerializeTests
    {
        // Tested:
        //  - Zone name vs scene name warning, loose ZoneNodes embedded as sub-assets
        //  - sceneGuid synced from the scene asset on save (missing + stale)

        // Const
        private const string _staleGuid = "00000000000000000000000000000001";

        // State
        private TempAssetFolder tempFolder;
        private string scratchAssetPath;
        private Zone zone;
        private SceneAsset sceneAsset;
        private string sceneGuid;

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            tempFolder = new TempAssetFolder(nameof(ZoneOnBeforeSerializeTests));
            scratchAssetPath = tempFolder.Path("ScratchTest_ZoneOnBeforeSerialize.asset");
            zone = ScriptableObject.CreateInstance<Zone>();
            zone.preventLocalizationForTests = true;
            AssetDatabase.CreateAsset(zone, scratchAssetPath);
            AssetDatabase.SaveAssets();

            Scene scratchScene = SceneTestUtility.CreateSavedScene(tempFolder, "ScratchTest_ZoneOnBeforeSerializeScene");
            string scenePath = scratchScene.path;
            EditorSceneManager.CloseScene(scratchScene, true);
            sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            SceneTestUtility.ResetToEmptyScene();
            tempFolder.Dispose();
        }
        #endregion

        #region Tests
        [Test]
        public void OnBeforeSerialize_SceneReferenceNameMismatch_LogsWarning()
        {
            zone.sceneReference = "SomeOtherSceneName";

            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape($"Zone name '{zone.name}' does not match its scene 'SomeOtherSceneName'. Use the Zone context menu > Match Zone Name to Scene.") + "$"));

            EditorUtility.SetDirty(zone);
            AssetDatabase.SaveAssets();
        }

        [Test]
        public void OnBeforeSerialize_SceneReferenceMatchesName_NoWarning()
        {
            zone.sceneReference = zone.name;

            EditorUtility.SetDirty(zone);
            AssetDatabase.SaveAssets();
        }

        [Test]
        public void OnBeforeSerialize_SceneAssetWithoutGuid_StoresItsGuid()
        {
            zone.sceneReference = new SceneReference(zone.name) { sceneAsset = sceneAsset };

            EditorUtility.SetDirty(zone);
            AssetDatabase.SaveAssets();

            Assert.AreEqual(sceneGuid, zone.sceneReference.sceneGuid);
        }

        [Test]
        public void OnBeforeSerialize_StaleGuid_ResyncsToSceneAsset()
        {
            zone.sceneReference = new SceneReference(zone.name) { sceneAsset = sceneAsset, sceneGuid = _staleGuid };

            EditorUtility.SetDirty(zone);
            AssetDatabase.SaveAssets();

            Assert.AreEqual(sceneGuid, zone.sceneReference.sceneGuid);
        }

        [Test]
        public void OnBeforeSerialize_EmbedsLooseZoneNodesAsSubAssets()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode rootNode = zone.GetRootNode();

            EditorUtility.SetDirty(zone);
            AssetDatabase.SaveAssets();

            Assert.AreEqual(scratchAssetPath, AssetDatabase.GetAssetPath(rootNode));
        }
        #endregion
    }
}

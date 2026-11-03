using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class ZoneOnBeforeSerializeTests
    {
        // Const Tunables
        private const string _scratchAssetPath = "Assets/ScratchTest_ZoneOnBeforeSerialize_SafeToDelete.asset";
        
        // State
        private Zone zone;

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            zone = ScriptableObject.CreateInstance<Zone>();
            zone.preventLocalizationForTests = true;
            AssetDatabase.CreateAsset(zone, _scratchAssetPath);
            AssetDatabase.SaveAssets();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AssetDatabase.DeleteAsset(_scratchAssetPath);
            AssetDatabase.Refresh();
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
        public void OnBeforeSerialize_EmbedsLooseZoneNodesAsSubAssets()
        {
            zone.CreateRootNodeIfMissing();
            ZoneNode rootNode = zone.GetRootNode();

            EditorUtility.SetDirty(zone);
            AssetDatabase.SaveAssets();

            Assert.AreEqual(_scratchAssetPath, AssetDatabase.GetAssetPath(rootNode));
        }
        #endregion
    }
}

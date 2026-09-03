using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.GameStateModifiers.Tests.Editor
{
    public class GameStateModifierTests
    {
        private class TestGameStateModifier : GameStateModifier { }

        private TestGameStateModifier modifier;
        private GameStateModifier.HasScenePathDelegate originalScenePathProvider;

        [SetUp]
        public void SetUp()
        {
            modifier = ScriptableObject.CreateInstance<TestGameStateModifier>();
            originalScenePathProvider = GameStateModifier.ScenePathProvider;
        }

        [TearDown]
        public void TearDown()
        {
            GameStateModifier.ScenePathProvider = originalScenePathProvider;
            Object.DestroyImmediate(modifier);
        }

        #region GuidGeneration
        [Test]
        public void OnBeforeSerialize_NoExistingGuid_GeneratesValidGuid()
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(modifier.GetGUID())); // sanity check on the fresh instance

            modifier.OnBeforeSerialize();

            Assert.IsFalse(string.IsNullOrWhiteSpace(modifier.GetGUID()));
            Assert.IsTrue(System.Guid.TryParse(modifier.GetGUID(), out _));
        }

        [Test]
        public void OnBeforeSerialize_ExistingGuid_IsNotRegenerated()
        {
            modifier.OnBeforeSerialize();
            string firstGuid = modifier.GetGUID();

            modifier.OnBeforeSerialize();

            Assert.AreEqual(firstGuid, modifier.GetGUID());
        }

        [Test]
        public void OnAfterDeserialize_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => modifier.OnAfterDeserialize()); // no-op, required by the interface only
        }
        #endregion

        #region AddOrUpdateGameStateModifierHandler
        [Test]
        public void AddOrUpdateGameStateModifierHandler_NewGuid_AppendsEntry()
        {
            var linkData = new ZoneToGameObjectLinkData("Zone", "Object", "Parent", "guid-1");

            modifier.AddOrUpdateGameStateModifierHandler(linkData);

            Assert.AreEqual(1, modifier.gameStateModifierHandlerData.Count);
            Assert.AreEqual("guid-1", modifier.gameStateModifierHandlerData[0].guid);
        }

        [Test]
        public void AddOrUpdateGameStateModifierHandler_ExistingGuid_UpdatesInPlace_NoDuplicate()
        {
            modifier.AddOrUpdateGameStateModifierHandler(new ZoneToGameObjectLinkData("OldZone", "OldObject", "OldParent", "guid-1"));
            
            modifier.AddOrUpdateGameStateModifierHandler(new ZoneToGameObjectLinkData("NewZone", "NewObject", "NewParent", "guid-1"));

            Assert.AreEqual(1, modifier.gameStateModifierHandlerData.Count); // update, not append
            ZoneToGameObjectLinkData entry = modifier.gameStateModifierHandlerData[0];
            Assert.AreEqual("NewZone", entry.zoneName);
            Assert.AreEqual("NewObject", entry.gameObjectName);
            Assert.AreEqual("NewParent", entry.parentObjectName);
        }
        #endregion

        #region RemoveGameStateModifierHandler
        [Test]
        public void RemoveGameStateModifierHandler_MatchingGuid_RemovesEntry()
        {
            modifier.AddOrUpdateGameStateModifierHandler(new ZoneToGameObjectLinkData("Zone", "Object", "Parent", "guid-1"));

            modifier.RemoveGameStateModifierHandler("guid-1");

            Assert.AreEqual(0, modifier.gameStateModifierHandlerData.Count);
        }

        [Test]
        public void RemoveGameStateModifierHandler_NoMatch_LeavesListUnchanged()
        {
            modifier.AddOrUpdateGameStateModifierHandler(new ZoneToGameObjectLinkData("Zone", "Object", "Parent", "guid-1"));

            modifier.RemoveGameStateModifierHandler("guid-does-not-exist");

            Assert.AreEqual(1, modifier.gameStateModifierHandlerData.Count);
        }
        #endregion

        #region CleanDanglingModifierHandlerData
        [Test]
        public void CleanDanglingModifierHandlerData_EmptyGuidEntry_IsRemoved()
        {
            modifier.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("Zone", "Object", "Parent", ""));

            int removedCount = modifier.CleanDanglingModifierHandlerData();

            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(0, modifier.gameStateModifierHandlerData.Count);
        }

        [Test]
        public void CleanDanglingModifierHandlerData_ScenePathProviderReturnsFalse_EntryRemoved()
        {
            GameStateModifier.ScenePathProvider = (string _, out string scenePath) => { scenePath = ""; return false; };
            modifier.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("MissingZone", "Object", "Parent", "guid-1"));

            LogAssert.Expect(LogType.Log, new Regex("Removing entry MissingZone.*not found")); // loose match, only pins the branch that fired
            int removedCount = modifier.CleanDanglingModifierHandlerData();

            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(0, modifier.gameStateModifierHandlerData.Count);
        }

        [Test]
        public void CleanDanglingModifierHandlerData_SceneFoundButHandlerNameBlank_EntryRemoved()
        {
            // sceneFound=true short-circuits straight to "object not found" without ever calling DoesGameStateModifierHandlerExist
            GameStateModifier.ScenePathProvider = (string _, out string scenePath) => { scenePath = "Assets/Fake.unity"; return true; };
            modifier.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("Zone", "", "Parent", "guid-1"));

            int removedCount = modifier.CleanDanglingModifierHandlerData();

            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(0, modifier.gameStateModifierHandlerData.Count);
        }

        [Test]
        public void CleanDanglingModifierHandlerData_ReturnsCombinedRemovedCount()
        {
            GameStateModifier.ScenePathProvider = (string _, out string scenePath) => { scenePath = ""; return false; };
            modifier.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("ZoneA", "Object", "Parent", "guid-1"));
            modifier.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("ZoneB", "Object", "Parent", "guid-2"));
            modifier.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("ZoneC", "Object", "Parent", "")); // empty-guid branch too

            int removedCount = modifier.CleanDanglingModifierHandlerData();

            Assert.AreEqual(3, removedCount);
            Assert.AreEqual(0, modifier.gameStateModifierHandlerData.Count);
        }
        #endregion

        #region StaticHelpers
        [Test]
        public void GetGameStateModifierHandlerDataRef_MatchesActualFieldName()
        {
            Assert.AreEqual("gameStateModifierHandlerData", GameStateModifier.GetGameStateModifierHandlerDataRef());
        }

        [Test]
        public void DefaultGetScenePath_NullZoneName_ReturnsFalse()
        {
            bool result = GameStateModifier.DefaultGetScenePath(null, out string scenePath);

            Assert.IsFalse(result);
            Assert.AreEqual("", scenePath);
        }

        [Test]
        public void DefaultGetScenePath_EmptyZoneName_ReturnsFalse()
        {
            bool result = GameStateModifier.DefaultGetScenePath("", out string scenePath);

            Assert.IsFalse(result);
        }
        #endregion
    }
}

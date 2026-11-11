using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.GameStateModifiers.Tests.Editor
{
    public class GameStateModifierTests
    {
        // State
        private TestGameStateModifier modifier;
        private GameStateModifier.HasScenePathDelegate originalScenePathProvider;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            modifier = ScriptableObject.CreateInstance<TestGameStateModifier>();
            originalScenePathProvider = GameStateModifier.scenePathProvider;
        }

        [TearDown]
        public void TearDown()
        {
            GameStateModifier.scenePathProvider = originalScenePathProvider;
            Object.DestroyImmediate(modifier);
        }
        #endregion

        #region Helpers
        private static GameStateModifierHandlerLookup MakeLookupLinking(string handlerGUID, params string[] modifierGUIDs)
        {
            var lookup = new GameStateModifierHandlerLookup();
            lookup.AddHandler("Zone", handlerGUID, modifierGUIDs);
            return lookup;
        }
        #endregion
        
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

        #region CleanDanglingWithIndex
        [Test]
        public void CleanDanglingModifierHandlerData_IndexedZone_LinkedHandler_KeptWithoutSceneLookup()
        {
            modifier.OnBeforeSerialize(); // force-generate modifier guid
            bool scenePathRequested = false;
            GameStateModifier.scenePathProvider = (string _, out string scenePath) => { scenePathRequested = true; scenePath = ""; return false; };
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("Zone", "Object", "", "handler-1"));

            int removedCount = modifier.CleanDanglingModifierHandlerData(true, MakeLookupLinking("handler-1", modifier.GetGUID()));

            Assert.AreEqual(0, removedCount);
            Assert.IsFalse(scenePathRequested);
        }

        [Test]
        public void CleanDanglingModifierHandlerData_IndexedZone_HandlerNoLongerLinked_Removed()
        {
            modifier.OnBeforeSerialize();
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("Zone", "Object", "", "handler-1"));

            LogAssert.Expect(LogType.Log, new Regex("Removing handler entry 'Zone/Object'.*not linked"));
            int removedCount = modifier.CleanDanglingModifierHandlerData(false, MakeLookupLinking("handler-1", "other-modifier"));

            Assert.AreEqual(1, removedCount);
        }

        [Test]
        public void CleanDanglingModifierHandlerData_IndexedZone_HandlerMissing_Removed()
        {
            modifier.OnBeforeSerialize();
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("Zone", "Object", "", "handler-gone"));

            LogAssert.Expect(LogType.Log, new Regex("Removing handler entry 'Zone/Object'.*object 'Object' not found"));
            int removedCount = modifier.CleanDanglingModifierHandlerData(false, MakeLookupLinking("handler-1", modifier.GetGUID()));

            Assert.AreEqual(1, removedCount);
        }

        [Test]
        public void CleanDanglingModifierHandlerData_UnindexedZone_FallsBackToSceneCheck()
        {
            modifier.OnBeforeSerialize();
            bool scenePathRequested = false;
            GameStateModifier.scenePathProvider = (string _, out string scenePath) => { scenePathRequested = true; scenePath = ""; return false; };
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("OtherZone", "Object", "", "handler-1"));

            int removedCount = modifier.CleanDanglingModifierHandlerData(false, MakeLookupLinking("handler-1", modifier.GetGUID()));

            Assert.IsTrue(scenePathRequested);
            Assert.AreEqual(0, removedCount); // scene not found + removeOnSceneCheck false -> kept, as without an index
        }
        #endregion

        #region AddOrUpdateGameStateModifierHandler
        [Test]
        public void AddOrUpdateGameStateModifierHandler_NewGuid_AppendsEntry()
        {
            var linkData = new LinkedHandlerRecord("Zone", "Object", "Parent", "guid-1");

            modifier.AddOrUpdateGameStateModifierHandler(linkData);

            Assert.AreEqual(1, modifier.linkedHandlers.Count);
            Assert.AreEqual("guid-1", modifier.linkedHandlers[0].handlerGUID);
        }

        [Test]
        public void AddOrUpdateGameStateModifierHandler_ExistingGuid_UpdatesInPlace_NoDuplicate()
        {
            modifier.AddOrUpdateGameStateModifierHandler(new LinkedHandlerRecord("OldZone", "OldObject", "OldParent", "guid-1"));
            
            modifier.AddOrUpdateGameStateModifierHandler(new LinkedHandlerRecord("NewZone", "NewObject", "NewParent", "guid-1", "NewType"));

            Assert.AreEqual(1, modifier.linkedHandlers.Count); // update, not append
            LinkedHandlerRecord entry = modifier.linkedHandlers[0];
            Assert.AreEqual("NewZone", entry.zoneName);
            Assert.AreEqual("NewObject", entry.gameObjectName);
            Assert.AreEqual("NewParent", entry.parentObjectName);
            Assert.AreEqual("NewType", entry.handlerTypeName);
        }
        #endregion

        #region RemoveGameStateModifierHandler
        [Test]
        public void RemoveGameStateModifierHandler_MatchingGuid_RemovesEntry()
        {
            modifier.AddOrUpdateGameStateModifierHandler(new LinkedHandlerRecord("Zone", "Object", "Parent", "guid-1"));

            modifier.RemoveGameStateModifierHandler("guid-1");

            Assert.AreEqual(0, modifier.linkedHandlers.Count);
        }

        [Test]
        public void RemoveGameStateModifierHandler_NoMatch_LeavesListUnchanged()
        {
            modifier.AddOrUpdateGameStateModifierHandler(new LinkedHandlerRecord("Zone", "Object", "Parent", "guid-1"));

            modifier.RemoveGameStateModifierHandler("guid-does-not-exist");

            Assert.AreEqual(1, modifier.linkedHandlers.Count);
        }
        #endregion

        #region CleanDanglingModifierHandlerData
        [Test]
        public void CleanDanglingModifierHandlerData_EmptyGuidEntry_IsRemoved()
        {
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("Zone", "Object", "Parent", ""));

            int removedCount = modifier.CleanDanglingModifierHandlerData();

            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(0, modifier.linkedHandlers.Count);
        }

        [Test]
        public void CleanDanglingModifierHandlerData_ScenePathProviderReturnsFalse_EntryRemoved()
        {
            GameStateModifier.scenePathProvider = (string _, out string scenePath) => { scenePath = ""; return false; };
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("MissingZone", "Object", "Parent", "guid-1"));

            LogAssert.Expect(LogType.Log, new Regex("Removing handler entry 'MissingZone/.*not found")); // loose match, only pins the branch that fired
            int removedCount = modifier.CleanDanglingModifierHandlerData();

            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(0, modifier.linkedHandlers.Count);
        }

        [Test]
        public void CleanDanglingModifierHandlerData_SceneFoundButHandlerNameBlank_EntryRemoved()
        {
            // sceneFound=true short-circuits straight to "object not found" without ever calling DoesGameStateModifierHandlerExist
            GameStateModifier.scenePathProvider = (string _, out string scenePath) => { scenePath = "Assets/Fake.unity"; return true; };
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("Zone", "", "Parent", "guid-1"));

            int removedCount = modifier.CleanDanglingModifierHandlerData();

            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(0, modifier.linkedHandlers.Count);
        }

        [Test]
        public void CleanDanglingModifierHandlerData_ReturnsCombinedRemovedCount()
        {
            GameStateModifier.scenePathProvider = (string _, out string scenePath) => { scenePath = ""; return false; };
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("ZoneA", "Object", "Parent", "guid-1"));
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("ZoneB", "Object", "Parent", "guid-2"));
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("ZoneC", "Object", "Parent", "")); // empty-guid branch too

            int removedCount = modifier.CleanDanglingModifierHandlerData();

            Assert.AreEqual(3, removedCount);
            Assert.AreEqual(0, modifier.linkedHandlers.Count);
        }
        #endregion

        #region RemoveDuplicateEntries
        [Test]
        public void RemoveDuplicateEntries_DuplicateGuids_KeepsFirstOccurrence_RemovesRest()
        {
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("ZoneA", "ObjectA", "Parent", "guid-1"));
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("ZoneB", "ObjectB", "Parent", "guid-1")); // same guid, later entry

            int removedCount = modifier.RemoveDuplicateEntries();

            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(1, modifier.linkedHandlers.Count);
            Assert.AreEqual("ZoneA", modifier.linkedHandlers[0].zoneName); // Distinct() keeps the first occurrence
        }

        [Test]
        public void RemoveDuplicateEntries_NoDuplicates_RemovesNothing()
        {
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("ZoneA", "Object", "Parent", "guid-1"));
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("ZoneB", "Object", "Parent", "guid-2"));

            int removedCount = modifier.RemoveDuplicateEntries();

            Assert.AreEqual(0, removedCount);
            Assert.AreEqual(2, modifier.linkedHandlers.Count);
        }
        #endregion
        
        #region StaticHelpers
        [Test]
        public void GetGameStateModifierHandlerDataRef_MatchesActualFieldName()
        {
            Assert.AreEqual("linkedHandlers", GameStateModifier.GetLinkedHandlersRef());
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

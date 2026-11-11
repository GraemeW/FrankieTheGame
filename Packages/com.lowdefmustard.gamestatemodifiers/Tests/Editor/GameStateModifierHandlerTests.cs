using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.GameStateModifiers.Tests.Editor
{
    // Covered:
    //  - MakeLinkedHandlerRecord 
    //  - GetModifierListHashCheck (deterministic via StableHash -> exact values asserted)
    //  - AddUpdateGameStateModifiers, RemoveStaleGameStateModifiers, RemoveSelfFromGameStateModifiers
    //      -  RemoveStale: only list-bookkeeping logic, missing GUID branch not viable w/ fake GUIDs
    //  - RecordHandlerModifications (non-prefab dirtying), OnBeforeSerialize (ISerializationCallbackReceiver, full reconciliation) + TriggerOnDestroy
    // Not Covered:
    //  - TriggerOnGizmos
    //  - RecordHandlerModifications on a prefab instance (test double can't live on a saved prefab) - verified via ForceSerializeAllHandlers + scene YAML

    public class GameStateModifierHandlerTests
    {
        // State
        private TestGameStateModifierHandler handler;
        private GameObject gameObject;
        private readonly TestObjectTracker tracker = new();
        private GameStateModifier.HasScenePathDelegate originalScenePathProvider;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("TestHandlerObject");
            handler = gameObject.AddComponent<TestGameStateModifierHandler>();
            originalScenePathProvider = GameStateModifier.scenePathProvider; // global static, save/restore
            GameStateModifier.scenePathProvider = (string _, out string scenePath) => { scenePath = ""; return false; }; // keep CleanDanglingModifierHandlerData light throughout this file
        }

        [TearDown]
        public void TearDown()
        {
            GameStateModifier.scenePathProvider = originalScenePathProvider;
            Object.DestroyImmediate(gameObject);
            tracker.DestroyAll();
        }

        private TestGameStateModifier CreateModifier() => tracker.Create<TestGameStateModifier>("TestModifier");
        #endregion

        #region MakeLinkedHandlerRecord
        [Test]
        public void MakeLinkedHandlerRecord_PopulatesZoneNameFromActiveScene_AndGameObjectName()
        {
            LinkedHandlerRecord linkData = ((IGameStateModifierHandler)handler).MakeLinkedHandlerRecord();

            Assert.AreEqual(SceneManager.GetActiveScene().name, linkData.zoneName);
            Assert.AreEqual("TestHandlerObject", linkData.gameObjectName);
        }

        [Test]
        public void MakeLinkedHandlerRecord_HandlerTypeName_IsConcreteClassName()
        {
            LinkedHandlerRecord linkData = ((IGameStateModifierHandler)handler).MakeLinkedHandlerRecord();

            Assert.AreEqual(nameof(TestGameStateModifierHandler), linkData.handlerTypeName);
        }

        [Test]
        public void MakeLinkedHandlerRecord_NoExistingHandlerGUID_GeneratesOne()
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(handler.handlerGUID)); // sanity check on the fresh double

            LinkedHandlerRecord linkData = ((IGameStateModifierHandler)handler).MakeLinkedHandlerRecord();

            Assert.IsFalse(string.IsNullOrWhiteSpace(linkData.handlerGUID));
            Assert.AreEqual(linkData.handlerGUID, handler.handlerGUID); // written back onto the handler
        }

        [Test]
        public void MakeLinkedHandlerRecord_ExistingHandlerGUID_IsNotRegenerated()
        {
            handler.handlerGUID = "existing-guid";

            LinkedHandlerRecord linkData = ((IGameStateModifierHandler)handler).MakeLinkedHandlerRecord();

            Assert.AreEqual("existing-guid", linkData.handlerGUID);
        }

        [Test]
        public void MakeLinkedHandlerRecord_NoParent_ParentObjectNameIsEmpty()
        {
            LinkedHandlerRecord linkData = ((IGameStateModifierHandler)handler).MakeLinkedHandlerRecord();

            Assert.AreEqual("", linkData.parentObjectName);
        }

        [Test]
        public void MakeLinkedHandlerRecord_WithParent_ParentObjectNameReflectsParentTransform()
        {
            var parentObject = tracker.Track(new GameObject("ParentObject"));
            gameObject.transform.SetParent(parentObject.transform);

            LinkedHandlerRecord linkData = ((IGameStateModifierHandler)handler).MakeLinkedHandlerRecord();

            Assert.AreEqual("ParentObject", linkData.parentObjectName);
        }
        #endregion

        #region GetModifierListHashCheck
        // Exact values computed independently ~ StableHash itself is covered in LowDefMustard.Utils
        [Test]
        public void GetModifierListHashCheck_KnownInputs_ProduceFixedValue_AcrossRuns()
        {
            handler.handlerGUID = "guid-1";

            Assert.AreEqual(958874102, ((IGameStateModifierHandler)handler).GetModifierListHashCheck("Zone", "Object", "", "Handler"));
        }

        [Test]
        public void GetModifierListHashCheck_NullModifierInList_HashedDistinctly()
        {
            handler.handlerGUID = "guid-1";
            handler.modifiersToReturn.Add(null);

            Assert.AreEqual(239672818, ((IGameStateModifierHandler)handler).GetModifierListHashCheck("Zone", "Object", "", "Handler"));
        }

        [Test]
        public void GetModifierListHashCheck_ShiftedFieldBoundary_ProducesDifferentHash()
        {
            handler.handlerGUID = "guid-1";

            int first = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("ab", "c", "", "Handler");
            int second = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("a", "bc", "", "Handler");

            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void GetModifierListHashCheck_DifferentZoneName_ProducesDifferentHash()
        {
            handler.handlerGUID = "guid-1";

            int first = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("ZoneA", "Object", "", "Handler");
            int second = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("ZoneB", "Object", "", "Handler");

            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void GetModifierListHashCheck_DifferentHandlerTypeName_ProducesDifferentHash()
        {
            handler.handlerGUID = "guid-1";

            int first = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("Zone", "Object", "", "HandlerA");
            int second = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("Zone", "Object", "", "HandlerB");

            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void GetModifierListHashCheck_DifferentParentName_ProducesDifferentHash()
        {
            handler.handlerGUID = "guid-1";

            int first = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("Zone", "Object", "ParentA", "Handler");
            int second = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("Zone", "Object", "ParentB", "Handler");

            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void GetModifierListHashCheck_DifferentModifierList_ProducesDifferentHash()
        {
            handler.handlerGUID = "guid-1";
            int emptyListHash = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("Zone", "Object", "", "Handler");

            TestGameStateModifier modifier = CreateModifier();
            modifier.OnBeforeSerialize(); // force-generate modifier guid
            handler.modifiersToReturn.Add(modifier);
            int withModifierHash = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("Zone", "Object", "", "Handler");

            Assert.AreNotEqual(emptyListHash, withModifierHash);
        }
        #endregion

        #region AddUpdateGameStateModifiers
        [Test]
        public void AddUpdateGameStateModifiers_EmptyModifierList_ReturnsEmptyList_HasGameStateModifiersFalse()
        {
            var linkData = new LinkedHandlerRecord("Zone", "Object", "", "guid-1");

            List<string> result = ((IGameStateModifierHandler)handler).AddUpdateGameStateModifiers(linkData);

            Assert.AreEqual(0, result.Count);
            Assert.IsFalse(handler.hasGameStateModifiers);
        }

        [Test]
        public void AddUpdateGameStateModifiers_OneModifier_ReturnsItsGuid_SetsHasGameStateModifiersTrue()
        {
            TestGameStateModifier modifier = CreateModifier();
            modifier.OnBeforeSerialize(); // force-generate modifier guid
            handler.modifiersToReturn.Add(modifier);
            var linkData = new LinkedHandlerRecord("Zone", "Object", "", "guid-1");

            List<string> result = ((IGameStateModifierHandler)handler).AddUpdateGameStateModifiers(linkData);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(modifier.GetGUID(), result[0]);
            Assert.IsTrue(handler.hasGameStateModifiers);
        }

        [Test]
        public void AddUpdateGameStateModifiers_NullModifierInList_SkippedSafely()
        {
            handler.modifiersToReturn.Add(null);
            var linkData = new LinkedHandlerRecord("Zone", "Object", "", "guid-1");

            List<string> result = null;
            Assert.DoesNotThrow(() => result = ((IGameStateModifierHandler)handler).AddUpdateGameStateModifiers(linkData));
            Assert.AreEqual(0, result.Count);
            Assert.IsFalse(handler.hasGameStateModifiers);
        }
        #endregion

        #region RemoveStaleGameStateModifiers
        [Test]
        public void RemoveStaleGameStateModifiers_FirstCall_SetsListDirectly()
        {
            Assert.IsNull(handler.gameStateModifierGUIDs); // sanity check on the fresh double

            ((IGameStateModifierHandler)handler).RemoveStaleGameStateModifiers(new List<string> { "guid-1", "guid-2" });

            CollectionAssert.AreEquivalent(new[] { "guid-1", "guid-2" }, handler.gameStateModifierGUIDs);
        }

        [Test]
        public void RemoveStaleGameStateModifiers_SecondCall_ReplacesContentsWithNewGuids()
        {
            ((IGameStateModifierHandler)handler).RemoveStaleGameStateModifiers(new List<string> { "guid-1", "guid-2" });

            // guid-2 is missing from this second call - falls into the "missing" branch, which safely no-ops since "guid-2" doesn't resolve to any real project asset
            Assert.DoesNotThrow(() => ((IGameStateModifierHandler)handler).RemoveStaleGameStateModifiers(new List<string> { "guid-1", "guid-3" }));
            CollectionAssert.AreEquivalent(new[] { "guid-1", "guid-3" }, handler.gameStateModifierGUIDs);
        }
        #endregion

        #region RemoveSelfFromGameStateModifiers
        [Test]
        public void RemoveSelfFromGameStateModifiers_RemovesHandlerFromEachAssignedModifier()
        {
            TestGameStateModifier modifierA = CreateModifier();
            TestGameStateModifier modifierB = CreateModifier();
            handler.handlerGUID = "guid-1";
            modifierA.linkedHandlers.Add(new LinkedHandlerRecord("Zone", "Object", "", "guid-1"));
            modifierB.linkedHandlers.Add(new LinkedHandlerRecord("Zone", "Object", "", "guid-1"));
            handler.modifiersToReturn.Add(modifierA);
            handler.modifiersToReturn.Add(modifierB);

            ((IGameStateModifierHandler)handler).RemoveSelfFromGameStateModifiers();

            Assert.AreEqual(0, modifierA.linkedHandlers.Count);
            Assert.AreEqual(0, modifierB.linkedHandlers.Count);
        }
        #endregion

        #region RecordHandlerModifications
        [Test]
        public void RecordHandlerModifications_NullHandler_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => IGameStateModifierHandler.RecordHandlerModifications(null));
        }

        [Test]
        public void RecordHandlerModifications_SceneHandler_MarksComponentDirty()
        {
            EditorUtility.ClearDirty(handler);

            IGameStateModifierHandler.RecordHandlerModifications(handler);

            Assert.IsTrue(EditorUtility.IsDirty(handler));
        }
        #endregion

        #region ReconcileGameStateModifiers
        [Test]
        public void ReconcileGameStateModifiers_NothingChanged_ReturnsFalse()
        {
            Assert.IsTrue(((IGameStateModifierHandler)handler).ReconcileGameStateModifiers());

            Assert.IsFalse(((IGameStateModifierHandler)handler).ReconcileGameStateModifiers());
        }

        [Test]
        public void ReconcileGameStateModifiers_CleanDisabled_LeavesDanglingEntries_UntilCleanEnabled()
        {
            TestGameStateModifier modifier = CreateModifier();
            modifier.OnBeforeSerialize(); // force-generate modifier guid
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("Zone", "Dangling", "", "")); // empty guid -> always removed by a clean
            handler.modifiersToReturn.Add(modifier);

            ((IGameStateModifierHandler)handler).ReconcileGameStateModifiers(false);
            Assert.AreEqual(2, modifier.linkedHandlers.Count); // own entry added, dangling one untouched

            handler.modifierListHashCheck = 0; // force a re-run
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("empty GUID"));
            ((IGameStateModifierHandler)handler).ReconcileGameStateModifiers();
            Assert.AreEqual(1, modifier.linkedHandlers.Count);
        }
        #endregion

        #region OnBeforeSerialize
        [Test]
        public void OnBeforeSerialize_FirstCall_ReconcilesAndUpdatesHash()
        {
            Assert.AreEqual(0, handler.modifierListHashCheck); // sanity check on the fresh double

            ((ISerializationCallbackReceiver)handler).OnBeforeSerialize();

            Assert.AreNotEqual(0, handler.modifierListHashCheck); // for-practical-purposes true - collision with the fresh-double default is vanishingly unlikely
            Assert.IsFalse(string.IsNullOrWhiteSpace(handler.handlerGUID)); // generated along the way, via MakeLinkedHandlerRecord
        }

        [Test]
        public void OnBeforeSerialize_SecondCallWithNothingChanged_SkipsReconciliation()
        {
            ((ISerializationCallbackReceiver)handler).OnBeforeSerialize();
            handler.getGameStateModifiersCallCount = 0; // reset after the first reconciliation

            ((ISerializationCallbackReceiver)handler).OnBeforeSerialize();

            // A full reconciliation calls GetGameStateModifiers() twice (once for the hash check, once inside AddUpdateGameStateModifiers) - a skip only calls it once
            Assert.AreEqual(1, handler.getGameStateModifiersCallCount);
        }

        [Test]
        public void OnBeforeSerialize_ModifierListChangedSinceLastCall_ReRunsReconciliation()
        {
            ((ISerializationCallbackReceiver)handler).OnBeforeSerialize();
            handler.getGameStateModifiersCallCount = 0;

            TestGameStateModifier modifier = CreateModifier();
            modifier.OnBeforeSerialize();  // force-generate modifier guid
            handler.modifiersToReturn.Add(modifier); // changes what GetModifierListHashCheck hashes over

            ((ISerializationCallbackReceiver)handler).OnBeforeSerialize();

            Assert.AreEqual(2, handler.getGameStateModifiersCallCount); // full reconciliation ran again
        }
        #endregion

        #region TriggerOnDestroy
        [Test]
        public void TriggerOnDestroy_RemovesHandlerFromEachAssignedModifier()
        {
            TestGameStateModifier modifier = CreateModifier();
            handler.handlerGUID = "guid-1";
            modifier.linkedHandlers.Add(new LinkedHandlerRecord("Zone", "Object", "", "guid-1"));
            handler.modifiersToReturn.Add(modifier);

            IGameStateModifierHandler.TriggerOnDestroy(handler);

            Assert.AreEqual(0, modifier.linkedHandlers.Count);
        }
        #endregion
    }
}

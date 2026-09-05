using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LowDefMustard.GameStateModifiers.Tests.Editor
{
    // Covered:
    //  - MakeZoneToGameObjectLinkData 
    //  - GetModifierListHashCheck (only relative comparisons asserted - System.HashCode output not stable across runs)
    //  - AddUpdateGameStateModifiers, RemoveStaleGameStateModifiers, RemoveSelfFromGameStateModifiers
    //      -  RemoveStale: only list-bookkeeping logic, missing GUID branch not viable w/ fake GUIDs
    //  - ForceSerializeGameObject, OnBeforeSerialize (ISerializationCallbackReceiver, full reconciliation) + TriggerOnDestroy
    // Not Covered:
    //  - TriggerOnGizmos

    public class GameStateModifierHandlerTests
    {
        // State
        private TestGameStateModifierHandler handler;
        private GameObject gameObject;
        private GameStateModifier.HasScenePathDelegate originalScenePathProvider;
        
        #region DataStructures
        private class TestGameStateModifierHandler : MonoBehaviour, IGameStateModifierHandler
        {
            // Explicit backing fields for each auto-property, since default interface property accessors can't have their own state
            [SerializeField] private string backingHandlerGUID;
            [SerializeField] private int backingModifierListHashCheck;
            [SerializeField] private bool backingHasGameStateModifiers;
            [SerializeField] private List<string> backingGameStateModifierGUIDs;

            public string handlerGUID { get => backingHandlerGUID; set => backingHandlerGUID = value; }
            public int modifierListHashCheck { get => backingModifierListHashCheck; set => backingModifierListHashCheck = value; }
            public bool hasGameStateModifiers { get => backingHasGameStateModifiers; set => backingHasGameStateModifiers = value; }
            public List<string> gameStateModifierGUIDs { get => backingGameStateModifierGUIDs; set => backingGameStateModifierGUIDs = value; }

            public List<GameStateModifier> modifiersToReturn = new();
            public int getGameStateModifiersCallCount;

            public IList<GameStateModifier> GetGameStateModifiers()
            {
                getGameStateModifiersCallCount++;
                return modifiersToReturn;
            }
        }

        private class TestGameStateModifier : GameStateModifier { }
        #endregion

        #region Setup
        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("TestHandlerObject");
            handler = gameObject.AddComponent<TestGameStateModifierHandler>();
            originalScenePathProvider = GameStateModifier.ScenePathProvider; // global static, save/restore
            GameStateModifier.ScenePathProvider = (string _, out string scenePath) => { scenePath = ""; return false; }; // keep CleanDanglingModifierHandlerData light throughout this file
        }

        [TearDown]
        public void TearDown()
        {
            GameStateModifier.ScenePathProvider = originalScenePathProvider;
            Object.DestroyImmediate(gameObject);
        }

        private static TestGameStateModifier CreateModifier() => ScriptableObject.CreateInstance<TestGameStateModifier>();
        #endregion

        #region MakeZoneToGameObjectLinkData
        [Test]
        public void MakeZoneToGameObjectLinkData_PopulatesZoneNameFromActiveScene_AndGameObjectName()
        {
            ZoneToGameObjectLinkData linkData = ((IGameStateModifierHandler)handler).MakeZoneToGameObjectLinkData();

            Assert.AreEqual(SceneManager.GetActiveScene().name, linkData.zoneName);
            Assert.AreEqual("TestHandlerObject", linkData.gameObjectName);
        }

        [Test]
        public void MakeZoneToGameObjectLinkData_NoExistingHandlerGUID_GeneratesOne()
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(handler.handlerGUID)); // sanity check on the fresh double

            ZoneToGameObjectLinkData linkData = ((IGameStateModifierHandler)handler).MakeZoneToGameObjectLinkData();

            Assert.IsFalse(string.IsNullOrWhiteSpace(linkData.guid));
            Assert.AreEqual(linkData.guid, handler.handlerGUID); // written back onto the handler
        }

        [Test]
        public void MakeZoneToGameObjectLinkData_ExistingHandlerGUID_IsNotRegenerated()
        {
            handler.handlerGUID = "existing-guid";

            ZoneToGameObjectLinkData linkData = ((IGameStateModifierHandler)handler).MakeZoneToGameObjectLinkData();

            Assert.AreEqual("existing-guid", linkData.guid);
        }

        [Test]
        public void MakeZoneToGameObjectLinkData_NoParent_ParentObjectNameIsEmpty()
        {
            ZoneToGameObjectLinkData linkData = ((IGameStateModifierHandler)handler).MakeZoneToGameObjectLinkData();

            Assert.AreEqual("", linkData.parentObjectName);
        }

        [Test]
        public void MakeZoneToGameObjectLinkData_WithParent_ParentObjectNameReflectsParentTransform()
        {
            var parentObject = new GameObject("ParentObject");
            gameObject.transform.SetParent(parentObject.transform);

            try
            {
                ZoneToGameObjectLinkData linkData = ((IGameStateModifierHandler)handler).MakeZoneToGameObjectLinkData();

                Assert.AreEqual("ParentObject", linkData.parentObjectName);
            }
            finally
            {
                Object.DestroyImmediate(parentObject);
            }
        }
        #endregion

        #region GetModifierListHashCheck
        [Test]
        public void GetModifierListHashCheck_SameInputs_ProduceSameHash_WithinSameRun()
        {
            handler.handlerGUID = "guid-1";

            int first = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("Zone", "Object");
            int second = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("Zone", "Object");

            Assert.AreEqual(first, second); // System.HashCode salts per process run, so only relative comparisons are valid
        }

        [Test]
        public void GetModifierListHashCheck_DifferentZoneName_ProducesDifferentHash_WithinSameRun()
        {
            handler.handlerGUID = "guid-1";

            int first = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("ZoneA", "Object");
            int second = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("ZoneB", "Object");

            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void GetModifierListHashCheck_DifferentModifierList_ProducesDifferentHash_WithinSameRun()
        {
            handler.handlerGUID = "guid-1";
            int emptyListHash = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("Zone", "Object");

            TestGameStateModifier modifier = CreateModifier();
            try
            {
                modifier.OnBeforeSerialize(); // force-generate modifier guid - otherwise guid stays null and the hash-comparer's GetHashCode NREs
                handler.modifiersToReturn.Add(modifier);
                int withModifierHash = ((IGameStateModifierHandler)handler).GetModifierListHashCheck("Zone", "Object");

                Assert.AreNotEqual(emptyListHash, withModifierHash);
            }
            finally
            {
                Object.DestroyImmediate(modifier);
            }
        }
        #endregion

        #region AddUpdateGameStateModifiers
        [Test]
        public void AddUpdateGameStateModifiers_EmptyModifierList_ReturnsEmptyList_HasGameStateModifiersFalse()
        {
            var linkData = new ZoneToGameObjectLinkData("Zone", "Object", "", "guid-1");

            List<string> result = ((IGameStateModifierHandler)handler).AddUpdateGameStateModifiers(linkData);

            Assert.AreEqual(0, result.Count);
            Assert.IsFalse(handler.hasGameStateModifiers);
        }

        [Test]
        public void AddUpdateGameStateModifiers_OneModifier_ReturnsItsGuid_SetsHasGameStateModifiersTrue()
        {
            TestGameStateModifier modifier = CreateModifier();
            try
            {
                modifier.OnBeforeSerialize(); // force-generate modifier guid
                handler.modifiersToReturn.Add(modifier);
                var linkData = new ZoneToGameObjectLinkData("Zone", "Object", "", "guid-1");

                List<string> result = ((IGameStateModifierHandler)handler).AddUpdateGameStateModifiers(linkData);

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual(modifier.GetGUID(), result[0]);
                Assert.IsTrue(handler.hasGameStateModifiers);
            }
            finally
            {
                Object.DestroyImmediate(modifier);
            }
        }

        [Test]
        public void AddUpdateGameStateModifiers_NullModifierInList_SkippedSafely()
        {
            handler.modifiersToReturn.Add(null);
            var linkData = new ZoneToGameObjectLinkData("Zone", "Object", "", "guid-1");

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
            try
            {
                handler.handlerGUID = "guid-1";
                modifierA.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("Zone", "Object", "", "guid-1"));
                modifierB.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("Zone", "Object", "", "guid-1"));
                handler.modifiersToReturn.Add(modifierA);
                handler.modifiersToReturn.Add(modifierB);

                ((IGameStateModifierHandler)handler).RemoveSelfFromGameStateModifiers();

                Assert.AreEqual(0, modifierA.gameStateModifierHandlerData.Count);
                Assert.AreEqual(0, modifierB.gameStateModifierHandlerData.Count);
            }
            finally
            {
                Object.DestroyImmediate(modifierA);
                Object.DestroyImmediate(modifierB);
            }
        }
        #endregion

        #region ForceSerializeGameObject
        [Test]
        public void ForceSerializeGameObject_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => ((IGameStateModifierHandler)handler).ForceSerializeGameObject());
        }
        #endregion

        #region OnBeforeSerialize
        [Test]
        public void OnBeforeSerialize_FirstCall_ReconcilesAndUpdatesHash()
        {
            Assert.AreEqual(0, handler.modifierListHashCheck); // sanity check on the fresh double

            ((ISerializationCallbackReceiver)handler).OnBeforeSerialize();

            Assert.AreNotEqual(0, handler.modifierListHashCheck); // for-practical-purposes true - collision with the fresh-double default is vanishingly unlikely
            Assert.IsFalse(string.IsNullOrWhiteSpace(handler.handlerGUID)); // generated along the way, via MakeZoneToGameObjectLinkData
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
            try
            {
                modifier.OnBeforeSerialize();  // force-generate modifier guid
                handler.modifiersToReturn.Add(modifier); // changes what GetModifierListHashCheck hashes over

                ((ISerializationCallbackReceiver)handler).OnBeforeSerialize();

                Assert.AreEqual(2, handler.getGameStateModifiersCallCount); // full reconciliation ran again
            }
            finally
            {
                Object.DestroyImmediate(modifier);
            }
        }
        #endregion

        #region TriggerOnDestroy
        [Test]
        public void TriggerOnDestroy_RemovesHandlerFromEachAssignedModifier()
        {
            TestGameStateModifier modifier = CreateModifier();
            try
            {
                handler.handlerGUID = "guid-1";
                modifier.gameStateModifierHandlerData.Add(new ZoneToGameObjectLinkData("Zone", "Object", "", "guid-1"));
                handler.modifiersToReturn.Add(modifier);

                IGameStateModifierHandler.TriggerOnDestroy(handler);

                Assert.AreEqual(0, modifier.gameStateModifierHandlerData.Count);
            }
            finally
            {
                Object.DestroyImmediate(modifier);
            }
        }
        #endregion
    }
}

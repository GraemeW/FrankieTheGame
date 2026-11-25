#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;
using Frankie.Inventory;

namespace Frankie.Tests
{
    public class InventoryBuildCopyTests
    {
        // Tested:
        //  - Knapsack - HasItem / FindSlotWithItem / RemoveItem match a build copy of a held item (by GUID)
        //  - WearablesLink - a build copy of a worn item isn't worn twice, and removing it takes off the original
        // Not Tested:
        //  - Equipment - looks items up through Knapsack.HasItem (covered above) and keys by EquipLocation
        // Note:  Uses project items (read-only);  play mode for Knapsack.Awake + real Destroy

        // Const (note that paths are necessarily string ref and brittle, must be updated w/ project layout/re-configuration)
        private const string _actionItemPath = "Assets/Game/OnLoadAssets/Inventory/ActionItems/ChocolateCheeseCurds.asset";
        private const string _wearableItemPath = "Assets/Game/OnLoadAssets/Inventory/WearableItems/GiantTurkeyLeg.asset";

        // State
        private readonly TestObjectTracker tracker = new();

        #region Setup
        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private static T LoadItem<T>(string path) where T : InventoryItem
        {
            var item = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(item, $"Missing item at '{path}'");
            return item;
        }

        private WearablesLink BuildWearablesLink(out Transform attachedObjectsRoot)
        {
            GameObject characterObject = tracker.CreateGameObject("InventoryBuildCopyTests_Wearer");
            characterObject.SetActive(false);
            attachedObjectsRoot = new GameObject("AttachedObjects").transform;
            attachedObjectsRoot.SetParent(characterObject.transform);
            var wearablesLink = characterObject.AddComponent<WearablesLink>();
            SerializedFieldInjector.Set(wearablesLink, WearablesLink.attachedObjectsRootField, attachedObjectsRoot);
            characterObject.SetActive(true);
            return wearablesLink;
        }
        #endregion

        #region Tests
        [Test]
        public void Knapsack_HeldItemBuildCopy_IsMatchedAndRemovable()
        {
            var item = LoadItem<InventoryItem>(_actionItemPath);
            InventoryItem itemBuildCopy = tracker.CreateBuildCopy(item);
            var knapsack = tracker.CreateComponent<Knapsack>("InventoryBuildCopyTests_Knapsack");
            Assert.IsTrue(knapsack.AddToFirstEmptySlot(item, false));

            Assert.IsTrue(knapsack.HasItem(itemBuildCopy));
            Assert.AreEqual(0, knapsack.FindSlotWithItem(itemBuildCopy));
            Assert.IsTrue(knapsack.RemoveItem(itemBuildCopy, false));
            Assert.IsTrue(knapsack.IsEmpty());
        }

        [UnityTest]
        public IEnumerator WearablesLink_WornItemBuildCopy_IsNotWornTwiceAndRemovesOriginal()
        {
            var wearableItem = LoadItem<WearableItem>(_wearableItemPath);
            WearableItem wearableBuildCopy = tracker.CreateBuildCopy(wearableItem);
            WearablesLink wearablesLink = BuildWearablesLink(out Transform attachedObjectsRoot);

            wearablesLink.SpawnWearable(wearableItem);
            wearablesLink.SpawnWearable(wearableBuildCopy);
            yield return null;
            Assert.AreEqual(1, attachedObjectsRoot.childCount, "Build copy was worn as a second wearable");

            wearablesLink.RemoveWearable(wearableBuildCopy);
            yield return null;
            Assert.AreEqual(0, attachedObjectsRoot.childCount, "Removing the build copy left the original on");
        }
        #endregion
    }
}
#endif

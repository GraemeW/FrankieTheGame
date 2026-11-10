#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;
using Frankie.Core;
using Frankie.Inventory;
using Frankie.Inventory.UI;
using Frankie.Speech.UI;

namespace Frankie.Tests
{
    public class ShopStateFlowTests
    {
        // Covers shop entry (Option 4 empty-shop check) and the transactionCompleted relay through the PlayerStateMachine
        // The dialogue-choice case calls Shop.InitiateBargain while in dialogue, as a choice action does

        // State
        private readonly TestObjectTracker objects = new();
        private PlayerStateRig rig;
        private Shop shop;
        private Shopper shopper;
        private int relayCount;

        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            rig = new PlayerStateRig();
            yield return rig.Build();

            shop = rig.Track(new GameObject("TestShop")).AddComponent<Shop>();
            shopper = rig.playerStateMachine.GetComponent<Shopper>();
            relayCount = 0;
            shop.GetTransactionCompleted().AddListener(() => relayCount++);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return rig.TearDown();
            objects.DestroyAll();
        }
        #endregion

        #region Helpers
        private InventoryItem Stock()
        {
            var item = objects.Create<KeyItem>("ShopStateFlowTests_Item");
            SerializedFieldInjector.SetArray(shop, Shop.stockField, item);
            return item;
        }

        private void CompleteBuy(InventoryItem item)
        {
            var knapsack = rig.playerStateMachine.GetComponentInChildren<Knapsack>();
            Assert.IsNotNull(knapsack, "No party knapsack on the Player prefab");
            shopper.CompleteTransaction(ShopType.Buy, item, knapsack);
        }
        #endregion
        
        #region Tests
        [UnityTest]
        public IEnumerator EmptyShop_StaysInWorldAndClearsShopper()
        {
            shop.InitiateBargain(rig.playerStateMachine);

            Assert.AreEqual(PlayerStateType.InWorld, rig.GetState());
            CollectionAssert.IsEmpty(rig.observedStates);
            Assert.IsTrue(shopper.GetCurrentShop() == null);
            yield return PlayerStateRig.Settle();
            Assert.IsNull(Object.FindAnyObjectByType<ShopSelect>());
        }

        [UnityTest]
        public IEnumerator StockedShop_EntersMenusAndRelaysTransactions()
        {
            InventoryItem item = Stock();

            shop.InitiateBargain(rig.playerStateMachine);
            Assert.AreEqual(PlayerStateType.InMenus, rig.GetState());
            yield return PlayerStateRig.Settle();

            CompleteBuy(item);
            Assert.AreEqual(1, relayCount);
        }

        [UnityTest]
        public IEnumerator ShopOpenedFromDialogueChoice_StillRelaysTransactions()
        {
            InventoryItem item = Stock();
            rig.playerStateMachine.EnterDialogue("Shop intro");
            shop.InitiateBargain(rig.playerStateMachine); // Queued behind the dialogue
            Assert.AreEqual(PlayerStateType.InDialogue, rig.GetState());

            yield return PlayerStateRig.Settle();
            Object.Destroy(Object.FindAnyObjectByType<DialogueBox>().gameObject);
            yield return TestWait.Until(() => rig.GetState() == PlayerStateType.InMenus, PlayerStateRig.waitTimeoutSeconds);
            Assert.AreEqual(PlayerStateType.InMenus, rig.GetState(), "Queued shop never opened");

            CompleteBuy(item);
            Assert.AreEqual(1, relayCount, "Shop lost its relay at the dialogue's World transition");
        }
        #endregion
    }
}
#endif

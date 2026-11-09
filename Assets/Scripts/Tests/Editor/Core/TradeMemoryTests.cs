using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LowDefMustard.Utils.Tests;
using Frankie.Core.PlayerStateMemory;
using Frankie.Inventory;

namespace Frankie.Tests.Editor
{
    public class TradeMemoryTests
    {
        // Covers TradeMemory.InitiateTrade's shop path:  null, empty and stocked shops
        // Note:  The Shop lives in a preview scene, so its editor-only OnDestroy hooks (localization, game state modifiers) skip it

        // State
        private readonly TestObjectTracker objects = new();
        private Scene previewScene;
        private GameObject shopObject;
        private Shop shop;
        private TradeMemory tradeMemory;
        private int shopInstantiations;
        private int bankInstantiations;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            previewScene = EditorSceneManager.NewPreviewScene();
            shopObject = new GameObject("TradeMemoryTests_Shop");
            SceneManager.MoveGameObjectToScene(shopObject, previewScene);
            shop = shopObject.AddComponent<Shop>();

            tradeMemory = new TradeMemory { tradeData = new TradeData(ShopType.Both) };
            shopInstantiations = 0;
            bankInstantiations = 0;
        }

        [TearDown]
        public void TearDown()
        {
            if (shopObject != null) { Object.DestroyImmediate(shopObject); }
            EditorSceneManager.ClosePreviewScene(previewScene);
            objects.DestroyAll();
        }
        #endregion
        
        #region Helpers
        private bool InitiateTrade(Shop tradeShop) => tradeMemory.InitiateTrade(tradeShop, () => shopInstantiations++, () => bankInstantiations++);

        private void SetStock(InventoryItem item)
        {
            var serializedShop = new SerializedObject(shop);
            SerializedProperty stock = serializedShop.FindProperty("stock");
            stock.arraySize = 1;
            stock.GetArrayElementAtIndex(0).objectReferenceValue = item;
            serializedShop.ApplyModifiedPropertiesWithoutUndo();
        }
        #endregion

        #region Tests
        [Test]
        public void InitiateTrade_NullShop_ReturnsFalseAndDoesNotInstantiate()
        {
            Assert.IsFalse(InitiateTrade(null));
            Assert.AreEqual(0, shopInstantiations);
        }

        [Test]
        public void InitiateTrade_EmptyShop_ReturnsFalseAndDoesNotInstantiate()
        {
            Assert.IsFalse(InitiateTrade(shop));
            Assert.AreEqual(0, shopInstantiations);
        }

        [Test]
        public void InitiateTrade_StockedShop_ReturnsTrueAndInstantiatesShop()
        {
            SetStock(objects.Create<KeyItem>("TradeMemoryTests_Item"));

            Assert.IsTrue(InitiateTrade(shop));
            Assert.AreEqual(1, shopInstantiations);
            Assert.AreEqual(0, bankInstantiations);
        }
        #endregion
    }
}

using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LowDefMustard.GameStateModifiers;
using LowDefMustard.Utils;
using LowDefMustard.Utils.Tests;
using static LowDefMustard.Utils.Tests.ConditionBuilder;
using Frankie.Core;
using Frankie.Inventory;
using Frankie.Quests;

namespace Frankie.Tests.Editor
{
    public class PredicateChildTogglerTests
    {
        // Covers PredicateChildToggler.GetGameStateModifiers
        // Note:  The toggler lives in a preview scene, so its editor-only serialization/OnDestroy hooks skip it

        // State
        private readonly TestObjectTracker tracker = new();
        private Scene previewScene;
        private GameObject togglerObject;
        private PredicateChildToggler toggler;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            previewScene = EditorSceneManager.NewPreviewScene();
            togglerObject = new GameObject("PredicateChildTogglerTests_Toggler");
            SceneManager.MoveGameObjectToScene(togglerObject, previewScene);
            toggler = togglerObject.AddComponent<PredicateChildToggler>();
        }

        [TearDown]
        public void TearDown()
        {
            if (togglerObject != null) { Object.DestroyImmediate(togglerObject); }
            EditorSceneManager.ClosePreviewScene(previewScene);
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private void SetCondition(Condition condition) => SerializedFieldInjector.SetValue(toggler, PredicateChildToggler.conditionField, condition);

        private HasQuestPredicate CreateHasQuestPredicate(Quest quest)
        {
            var predicate = tracker.Create<HasQuestPredicate>("HasQuest");
            SerializedFieldInjector.Set(predicate, HasQuestPredicate.questField, quest);
            return predicate;
        }
        #endregion

        #region GameStateModifiers
        [Test]
        public void GetGameStateModifiers_EmptyCondition_ReturnsEmpty()
        {
            Assert.IsEmpty(toggler.GetGameStateModifiers());
        }

        [Test]
        public void GetGameStateModifiers_NonModifierPredicates_ReturnsEmpty()
        {
            SetCondition(When(tracker.Create<TestPredicate>("Plain")));

            Assert.IsEmpty(toggler.GetGameStateModifiers());
        }

        [Test]
        public void GetGameStateModifiers_QuestPredicates_ReturnsEachQuestOnce()
        {
            var quest = tracker.Create<Quest>("Quest");
            var otherQuest = tracker.Create<Quest>("OtherQuest");
            var completedPredicate = tracker.Create<QuestCompletedPredicate>("QuestCompleted");
            SerializedFieldInjector.Set(completedPredicate, QuestCompletedPredicate.questField, quest);
            Condition condition = WhenAny(completedPredicate, CreateHasQuestPredicate(otherQuest));
            condition.AddDisjunction(When(CreateHasQuestPredicate(quest), negate: true).GetDisjunctions()[0]);
            SetCondition(condition);

            CollectionAssert.AreEquivalent(new GameStateModifier[] { quest, otherQuest }, toggler.GetGameStateModifiers().ToList());
        }

        [Test]
        public void GetGameStateModifiers_HasItemPredicate_ReturnsOnlyKeyItems()
        {
            var keyItem = tracker.Create<KeyItem>("KeyItem");
            var hasItemPredicate = tracker.Create<HasItemPredicate>("HasItem");
            SerializedFieldInjector.SetArray(hasItemPredicate, HasItemPredicate.inventoryItemsField, keyItem, tracker.Create<ActionItem>("ActionItem"), null);
            SetCondition(When(hasItemPredicate));

            CollectionAssert.AreEqual(new GameStateModifier[] { keyItem }, toggler.GetGameStateModifiers().ToList());
        }
        #endregion
    }
}

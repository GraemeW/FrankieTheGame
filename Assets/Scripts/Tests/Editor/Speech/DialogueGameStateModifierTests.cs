using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LowDefMustard.GameStateModifiers;
using LowDefMustard.Utils;
using LowDefMustard.Utils.Tests;
using static LowDefMustard.Utils.Tests.ConditionBuilder;
using Frankie.Inventory;
using Frankie.Quests;
using Frankie.Speech;

namespace Frankie.Tests.Editor
{
    public class DialogueGameStateModifierTests
    {
        // Covers Dialogue.GetAllPredicates + AIConversant.GetGameStateModifiers over in-memory dialogues/predicates
        // Note:  Test conversant lives in a preview scene ~ its editor-only serialization/OnDestroy hooks skip it
        // Not covered here:  QuestObjectiveCompletedPredicate (Quest.GetFromID resolves via the Addressables quest cache)

        // State
        private readonly TestObjectTracker tracker = new();
        private Scene previewScene;
        private GameObject conversantObject;
        private AIConversant conversant;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            previewScene = EditorSceneManager.NewPreviewScene();
            conversantObject = new GameObject("DialogueGameStateModifierTests_Conversant");
            SceneManager.MoveGameObjectToScene(conversantObject, previewScene);
            conversant = conversantObject.AddComponent<AIConversant>();
        }

        [TearDown]
        public void TearDown()
        {
            if (conversantObject != null) { Object.DestroyImmediate(conversantObject); }
            EditorSceneManager.ClosePreviewScene(previewScene);
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private HasQuestPredicate CreateHasQuestPredicate(Quest quest)
        {
            var predicate = tracker.Create<HasQuestPredicate>("HasQuest");
            SerializedFieldInjector.Set(predicate, HasQuestPredicate.questField, quest);
            return predicate;
        }
        #endregion

        #region DialoguePredicates
        [Test]
        public void GetAllPredicates_NoConditionNodes_ReturnsEmpty()
        {
            Assert.IsEmpty(tracker.CreateSingleLineDialogue().GetAllPredicates());
        }

        [Test]
        public void GetAllPredicates_AcrossConditionNodes_ReturnsEachPredicateOnce()
        {
            var first = tracker.Create<TestPredicate>("First");
            var second = tracker.Create<TestPredicate>("Second");
            var third = tracker.Create<TestPredicate>("Third");
            Dialogue dialogue = tracker.CreateGatedDialogue(WhenAny(first, second), When(second, negate: true), WhenAll(third), null);

            CollectionAssert.AreEquivalent(new Predicate[] { first, second, third }, dialogue.GetAllPredicates().ToList());
        }
        #endregion

        #region ConversantModifiers
        [Test]
        public void GetGameStateModifiers_NoDialogue_ReturnsEmpty()
        {
            Assert.IsEmpty(conversant.GetGameStateModifiers());
        }

        [Test]
        public void GetGameStateModifiers_NonModifierPredicates_ReturnsEmpty()
        {
            SerializedFieldInjector.Set(conversant, AIConversant.dialogueField, tracker.CreateGatedDialogue(When(tracker.Create<TestPredicate>("Plain"))));

            Assert.IsEmpty(conversant.GetGameStateModifiers());
        }

        [Test]
        public void GetGameStateModifiers_QuestPredicates_ReturnsEachQuestOnce()
        {
            var quest = tracker.Create<Quest>("Quest");
            var otherQuest = tracker.Create<Quest>("OtherQuest");
            var completedPredicate = tracker.Create<QuestCompletedPredicate>("QuestCompleted");
            SerializedFieldInjector.Set(completedPredicate, QuestCompletedPredicate.questField, quest);
            Dialogue dialogue = tracker.CreateGatedDialogue(When(CreateHasQuestPredicate(quest), negate: true), WhenAny(completedPredicate, CreateHasQuestPredicate(otherQuest)));
            SerializedFieldInjector.Set(conversant, AIConversant.dialogueField, dialogue);

            CollectionAssert.AreEquivalent(new GameStateModifier[] { quest, otherQuest }, conversant.GetGameStateModifiers().ToList());
        }

        [Test]
        public void GetGameStateModifiers_HasItemPredicate_ReturnsOnlyKeyItems()
        {
            var keyItem = tracker.Create<KeyItem>("KeyItem");
            var actionItem = tracker.Create<ActionItem>("ActionItem");
            var hasItemPredicate = tracker.Create<HasItemPredicate>("HasItem");
            SerializedFieldInjector.SetArray(hasItemPredicate, HasItemPredicate.inventoryItemsField, keyItem, actionItem, null);
            SerializedFieldInjector.Set(conversant, AIConversant.dialogueField, tracker.CreateGatedDialogue(When(hasItemPredicate)));

            CollectionAssert.AreEqual(new GameStateModifier[] { keyItem }, conversant.GetGameStateModifiers().ToList());
        }

        [Test]
        public void GetGameStateModifiers_UnsetPredicateReferences_ReturnsEmpty()
        {
            SerializedFieldInjector.Set(conversant, AIConversant.dialogueField, tracker.CreateGatedDialogue(When(CreateHasQuestPredicate(null)), When(tracker.Create<HasItemPredicate>("EmptyHasItem"))));

            Assert.IsEmpty(conversant.GetGameStateModifiers());
        }
        #endregion
    }
}

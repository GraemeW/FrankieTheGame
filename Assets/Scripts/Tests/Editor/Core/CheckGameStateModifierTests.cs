using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LowDefMustard.GameStateModifiers;
using LowDefMustard.Utils.Tests;
using static LowDefMustard.Utils.Tests.ConditionBuilder;
using Frankie.Control;
using Frankie.Quests;

namespace Frankie.Tests.Editor
{
    public class CheckGameStateModifierTests
    {
        // Covers CheckWithToggleChildren + CheckWithPredicateEvaluation as IGameStateModifierHandlers, and CheckWithToggleChildren's edit mode Start guard
        // Note:  Handler checks use a preview scene (editor-only serialization/OnDestroy hooks skip it)
        //  - the Start guard needs a loaded scene for edit mode Start to run ~ the test runner's own untitled scene

        // State
        private readonly TestObjectTracker tracker = new();
        private Scene previewScene;
        private GameObject loadedSceneObject;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            previewScene = EditorSceneManager.NewPreviewScene();
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
            if (loadedSceneObject != null) { Object.DestroyImmediate(loadedSceneObject); }
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private T AddToPreviewScene<T>() where T : Component
        {
            var gameObject = new GameObject($"CheckGameStateModifierTests_{typeof(T).Name}");
            SceneManager.MoveGameObjectToScene(gameObject, previewScene);
            return gameObject.AddComponent<T>();
        }

        private HasQuestPredicate CreateHasQuestPredicate(Quest quest)
        {
            var predicate = tracker.Create<HasQuestPredicate>("HasQuest");
            SerializedFieldInjector.Set(predicate, HasQuestPredicate.questField, quest);
            return predicate;
        }
        #endregion

        #region CheckWithToggleChildren
        [Test]
        public void ToggleChildren_EmptyCondition_ReturnsNoModifiers()
        {
            Assert.IsEmpty(AddToPreviewScene<CheckWithToggleChildren>().GetGameStateModifiers());
        }

        [Test]
        public void ToggleChildren_QuestCondition_ReturnsEachQuestOnce()
        {
            var quest = tracker.Create<Quest>("Quest");
            var otherQuest = tracker.Create<Quest>("OtherQuest");
            var check = AddToPreviewScene<CheckWithToggleChildren>();
            SerializedFieldInjector.SetValue(check, CheckWithToggleChildren.conditionField, WhenAny(CreateHasQuestPredicate(quest), CreateHasQuestPredicate(otherQuest), CreateHasQuestPredicate(quest)));

            CollectionAssert.AreEquivalent(new GameStateModifier[] { quest, otherQuest }, check.GetGameStateModifiers().ToList());
        }

        [UnityTest]
        public IEnumerator ToggleChildren_StartInEditMode_LeavesChildrenUntouched()
        {
            var checkObject = loadedSceneObject = new GameObject("CheckGameStateModifierTests_Toggle");
            var child = new GameObject("Child");
            child.transform.SetParent(checkObject.transform);
            checkObject.SetActive(false); // hook up before Start can run
            var check = checkObject.AddComponent<CheckWithToggleChildren>();
            SerializedFieldInjector.Set(check, CheckWithToggleChildren.parentTransformForTogglingField, checkObject.transform);
            checkObject.SetActive(true);

            for (int frame = 0; frame < 3; frame++)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }

            Assert.IsTrue(child.activeSelf); // unguarded Start would apply SetActive(!toggleToConditionMet) == false
        }
        #endregion

        #region CheckWithPredicateEvaluation
        [Test]
        public void PredicateEvaluation_EmptyCondition_ReturnsNoModifiers()
        {
            Assert.IsEmpty(AddToPreviewScene<CheckWithPredicateEvaluation>().GetGameStateModifiers());
        }

        [Test]
        public void PredicateEvaluation_QuestCondition_ReturnsQuest()
        {
            var quest = tracker.Create<Quest>("Quest");
            var check = AddToPreviewScene<CheckWithPredicateEvaluation>();
            SerializedFieldInjector.SetValue(check, CheckWithPredicateEvaluation.conditionField, When(CreateHasQuestPredicate(quest), negate: true));

            CollectionAssert.AreEqual(new GameStateModifier[] { quest }, check.GetGameStateModifiers().ToList());
        }
        #endregion
    }
}

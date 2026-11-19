using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Tests;
using Frankie.Combat;

namespace Frankie.Tests.Editor
{
    public class SkillPoolEntryDefaultsTests
    {
        // Tested:
        //  - New SkillPool entry from the inspector's add button - weight 1 (Unity zero-fills new entries, SkillPool.OnValidate fixes blanks)
        //  - Deliberate entries kept - a skill w/ weight 0, an empty skill w/ a set weight
        // Not Tested:
        //  - Adding to a non-empty list - Unity copies the last entry (standard list behaviour)

        // Const Tunables
        private const string _listAddButtonClass = "unity-list-view__add-button";

        // State
        private readonly TestObjectTracker tracker = new();
        private SkillPool skillPool;
        private SerializedObject serializedObject;
        private EditorWindow window;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            skillPool = tracker.CreateSkillPool("SkillPoolEntryDefaultsTests_Pool");
            serializedObject = new SerializedObject(skillPool);
        }

        [TearDown]
        public void TearDown()
        {
            HeadlessEditorWindow.Close(window);
            serializedObject.Dispose();
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private int GetWeight(int index)
        {
            serializedObject.Update();
            SerializedProperty pairsProperty = serializedObject.FindProperty(SkillPool.skillProbabilityPairsField);
            return pairsProperty.GetArrayElementAtIndex(index).FindPropertyRelative(SkillProbabilityPair.probabilityWeightField).intValue;
        }

        private int GetEntryCount()
        {
            serializedObject.Update();
            return serializedObject.FindProperty(SkillPool.skillProbabilityPairsField).arraySize;
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator InspectorAddButton_EmptyList_NewEntryWeightOne()
        {
            serializedObject.FindProperty(SkillPool.skillProbabilityPairsField).isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            window = HeadlessEditorWindow.CreateOffscreenWindow(500f, 600f);
            window.rootVisualElement.Add(new InspectorElement(serializedObject));
            yield return TestWait.Until(() => window.rootVisualElement.Q<Button>(className: _listAddButtonClass) != null);
            yield return HeadlessEditorWindow.Settle();
            var addButton = window.rootVisualElement.Q<Button>(className: _listAddButtonClass);
            Assert.IsNotNull(addButton, "List add button never built");

            HeadlessEditorWindow.SendSubmit(addButton);
            yield return TestWait.Until(() => GetEntryCount() == 1);

            Assert.AreEqual(1, GetEntryCount());
            Assert.AreEqual(1, GetWeight(0));
        }

        [Test]
        public void DeliberateEntries_KeptOnValidate()
        {
            Skill skill = CombatTestBuilders.LoadAddressableSkills(1)[0]; // Entries hold names ~ needs a resolvable skill
            skillPool.SetSkills((skill, 0), (null, 3)); // Applied through SerializedObject ~ OnValidate runs

            Assert.AreEqual(0, GetWeight(0), "a skill w/ weight 0 is deliberate (never picked)");
            Assert.AreEqual(3, GetWeight(1));
        }

        [Test]
        public void BlankEntry_DefaultsToWeightOneOnValidate()
        {
            skillPool.SetSkills((null, 0));

            Assert.AreEqual(1, GetWeight(0));
        }
        #endregion
    }
}

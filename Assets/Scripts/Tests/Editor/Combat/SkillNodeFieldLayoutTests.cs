using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Tests;
using Frankie.Combat;
using Frankie.Combat.Editor;

namespace Frankie.Tests.Editor
{
    public class SkillNodeFieldLayoutTests
    {
        // Tested:
        //  - Skill text fields (tree branch + pool card) keep their width for long text ~ clipped inside, row stays inside the card
        //  - Empty fields (placeholder showing) fit inside the card
        // Not Tested:
        //  - Theme stylesheet - not applied here, so widths are the default editor ones (same layout rules)

        // Const Tunables
        private const string _longName = "SkillNodeFieldLayoutTests_AVeryLongSkillNameThatIsFarWiderThanAnyField";
        private const float _tolerance = 0.5f;

        // State
        private readonly TestObjectTracker tracker = new();
        private EditorWindow window;
        private SkillTree skillTree;

        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            window = HeadlessEditorWindow.CreateOffscreenWindow(700f, 500f);
            yield return HeadlessEditorWindow.WaitUntilSettled(window);
        }

        [TearDown]
        public void TearDown()
        {
            HeadlessEditorWindow.Close(window);
            if (skillTree != null) { skillTree.ClearUndo(); }
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private IEnumerator Show(VisualElement card)
        {
            window.rootVisualElement.Add(card);
            yield return TestWait.Until(() => card.layout.width > 0f);
            yield return HeadlessEditorWindow.Settle();
        }

        private static void AssertRowsInsideCard(VisualElement card, IEnumerable<TextField> skillFields)
        {
            foreach (TextField skillField in skillFields)
            {
                VisualElement row = skillField.parent;
                VisualElement lastChild = row[row.childCount - 1];
                Assert.LessOrEqual(lastChild.worldBound.xMax, card.worldBound.xMax + _tolerance, $"row for '{skillField.value}' runs past the card");
            }
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator TreeBranch_LongNameAndEmpty_FieldsKeepWidthInsideCard()
        {
            skillTree = tracker.CreateSkillTree("SkillNodeFieldLayoutTests_Tree", out SkillBranch rootBranch);
            var branchView = new SkillBranchView(rootBranch, skillTree, true, null, null, null, null, () => 1f, null, null);
            yield return Show(branchView);
            List<TextField> skillFields = branchView.Query<TextField>().ToList();
            float emptyWidth = skillFields[0].layout.width;

            skillFields[0].SetValueWithoutNotify(_longName); // Display only ~ slots hold addressable names, none this long
            yield return HeadlessEditorWindow.Settle();

            Assert.AreEqual(emptyWidth, skillFields[0].layout.width, _tolerance, "long name widened the field");
            Assert.AreEqual(skillFields[1].layout.width, skillFields[0].layout.width, _tolerance);
            AssertRowsInsideCard(branchView, skillFields);
        }

        [UnityTest]
        public IEnumerator PoolCard_LongNameAndEmpty_FieldsKeepWidthInsideCard()
        {
            Skill[] projectSkills = CombatTestBuilders.LoadAddressableSkills(2);
            SkillPool skillPool = tracker.CreateSkillPool("SkillNodeFieldLayoutTests_Pool", (projectSkills[0], 1), (projectSkills[1], 1), (null, 1));
            var poolView = new SkillPoolView(skillPool, _ => null, null, null);
            yield return Show(poolView);
            List<TextField> skillFields = poolView.Query<TextField>().ToList();
            Assert.AreEqual(3, skillFields.Count);

            skillFields[0].SetValueWithoutNotify(_longName); // Display only ~ entries hold addressable names, none this long
            yield return HeadlessEditorWindow.Settle();

            Assert.AreEqual(skillFields[1].layout.width, skillFields[0].layout.width, _tolerance, "long name widened the field");
            Assert.AreEqual(skillFields[1].layout.width, skillFields[2].layout.width, _tolerance, "placeholder widened the field");
            Assert.AreEqual(SkillPoolView.cardWidth, poolView.layout.width, _tolerance, "card grew past its width");
            AssertRowsInsideCard(poolView, skillFields);
        }
        #endregion
    }
}

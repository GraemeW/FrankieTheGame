using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;
using LowDefMustard.Utils.Tests;
using Frankie.Combat;
using Frankie.Combat.Editor;

namespace Frankie.Tests.Editor
{
    public class SkillPoolViewTests
    {
        // Tested:
        //  - SkillPoolView card - one row per entry (skill name + share), skills-changed callback on edits
        //  - Skill field - known name sets the skill (any casing, shown in the asset's own), unknown name keeps the current one, empty clears it, a dropped skill is set (through the name field), an unresolvable one refused
        //  - Entry naming a missing skill (e.g. deleted) - shown blank (placeholder), not as a valid-looking name
        //  - Add / remove buttons - add appends a blank weight-1 entry, remove deletes that row's entry
        //  - External edits (e.g. the inspector) - rows rebuilt on a count change, names synced otherwise
        // Not Tested:
        //  - SkillSetEditor hosting the card (framing, selection, double-click) - since this opens the window, checked manually
        // Note:  Pool entries resolve skills by name through the Skill addressables cache, so this uses project skills

        // State
        private readonly TestObjectTracker tracker = new();
        private EditorWindow window;
        private SkillPool skillPool;
        private Skill skillA;
        private Skill skillB;
        private Skill skillC;
        private SkillPoolView poolView;
        private int skillsChangedCount;

        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Skill[] projectSkills = CombatTestBuilders.LoadAddressableSkills(3);
            (skillA, skillB, skillC) = (projectSkills[0], projectSkills[1], projectSkills[2]);
            skillPool = tracker.CreateSkillPool("SkillPoolViewTests_Pool", (skillA, 1), (skillB, 3));
            skillsChangedCount = 0;

            window = HeadlessEditorWindow.CreateOffscreenWindow(500f, 400f);
            yield return BuildView();
        }

        [TearDown]
        public void TearDown()
        {
            HeadlessEditorWindow.EndObjectDrag();
            HeadlessEditorWindow.Close(window);
            Undo.ClearUndo(skillPool);
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private IEnumerator BuildView()
        {
            window.rootVisualElement.Clear();
            poolView = new SkillPoolView(skillPool, ResolveSkill, () => skillsChangedCount++, null);
            window.rootVisualElement.Add(poolView);
            yield return HeadlessEditorWindow.WaitUntilSettled(window);
            yield return HeadlessEditorWindow.Settle();
            skillsChangedCount = 0;
        }

        private Skill ResolveSkill(string skillName) => SkillSetEditor.ResolveSkill(new[] { skillA, skillB, skillC }, skillName);

        private List<VisualElement> GetRows() => poolView.Query<VisualElement>(className: SkillPoolView.entryRowClass).ToList();
        private TextField GetSkillField(int index) => GetRows()[index].Q<TextField>();
        private Button GetAddButton() => poolView.Q<Button>(className: SkillPoolView.addButtonClass);
        private Button GetRemoveButton(int index) => GetRows()[index].Q<Button>(className: SkillPoolView.removeButtonClass);

        private List<(Skill skill, int weight)> GetEntries() => skillPool.GetWeightedSkills().Select(pair => (pair.value, pair.weight)).ToList();
        #endregion

        #region Tests
        [Test]
        public void Card_ShowsRowPerEntryWithSkillNameAndShare()
        {
            List<VisualElement> rows = GetRows();

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(skillA.name, GetSkillField(0).value);
            Assert.AreEqual(skillB.name, GetSkillField(1).value);
            Assert.AreEqual("25%", rows[0].Q<Label>(className: ProbabilityWeightField.shareLabelClass).text);
            Assert.AreEqual("75%", rows[1].Q<Label>(className: ProbabilityWeightField.shareLabelClass).text);
        }

        [Test]
        public void SkillField_KnownName_SetsSkill()
        {
            GetSkillField(0).value = skillC.name;

            Assert.AreEqual(skillC, GetEntries()[0].skill);
            Assert.AreEqual(skillC.name, GetSkillField(0).value);
            Assert.Greater(skillsChangedCount, 0);
        }

        [Test]
        public void SkillField_KnownNameOtherCase_SetsSkillWithAssetCasing()
        {
            GetSkillField(0).value = skillC.name.ToUpperInvariant();

            Assert.AreEqual(skillC, GetEntries()[0].skill);
            Assert.AreEqual(skillC.name, GetSkillField(0).value);
        }

        [Test]
        public void SkillField_UnknownName_KeepsCurrentSkill()
        {
            GetSkillField(0).value = "SkillPoolViewTests_Typo";

            Assert.AreEqual(skillA, GetEntries()[0].skill);
            Assert.AreEqual(skillA.name, GetSkillField(0).value);
        }

        [Test]
        public void SkillField_Emptied_ClearsSkill()
        {
            GetSkillField(1).value = string.Empty;

            Assert.IsNull(GetEntries()[1].skill);
        }

        [Test]
        public void SkillField_DroppedSkill_SetsSkill()
        {
            TextField skillField = GetSkillField(1);

            HeadlessEditorWindow.DropObjects(skillField, skillC);

            Assert.AreEqual(skillC, GetEntries()[1].skill);
            Assert.AreEqual(skillC.name, GetSkillField(1).value);
        }

        [Test]
        public void SkillField_DroppedUnresolvableSkill_RefusedAndEntryUnchanged()
        {
            Skill unresolvable = tracker.CreateSkill("SkillPoolViewTests_NotResolvable"); // Not addressable (and not in ResolveSkill's skill list)
            TextField skillField = GetSkillField(1);

            HeadlessEditorWindow.DropObjects(skillField, unresolvable);

            Assert.AreEqual(DragAndDropVisualMode.Rejected, DragAndDrop.visualMode);
            Assert.AreEqual(skillB, GetEntries()[1].skill);
            Assert.AreEqual(skillB.name, GetSkillField(1).value);
            Assert.AreEqual(0, skillsChangedCount);
        }

        [Test]
        public void AddButton_AppendsBlankWeightOneEntry()
        {
            HeadlessEditorWindow.SendSubmit(GetAddButton());

            List<(Skill skill, int weight)> entries = GetEntries();
            Assert.AreEqual(3, entries.Count);
            Assert.IsNull(entries[2].skill, "blank, not a copy of the last entry");
            Assert.AreEqual(1, entries[2].weight);
            Assert.AreEqual(3, GetRows().Count);
            Assert.AreEqual(string.Empty, GetSkillField(2).value);
        }

        [Test]
        public void RemoveButton_RemovesThatEntry()
        {
            HeadlessEditorWindow.SendSubmit(GetRemoveButton(0));

            List<(Skill skill, int weight)> entries = GetEntries();
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(skillB, entries[0].skill);
            Assert.AreEqual(1, GetRows().Count);
            Assert.AreEqual(skillB.name, GetSkillField(0).value);
        }

        [UnityTest]
        public IEnumerator ExternalEdit_CountChanged_RebuildsRows()
        {
            skillPool.SetSkills((skillC, 1), (skillA, 1), (skillB, 1));

            yield return TestWait.Until(() => GetRows().Count == 3);

            Assert.AreEqual(3, GetRows().Count);
            Assert.AreEqual(skillC.name, GetSkillField(0).value);
        }

        [UnityTest]
        public IEnumerator ExternalEdit_SkillChanged_SyncsName()
        {
            skillPool.SetSkills((skillC, 1), (skillB, 3));

            yield return TestWait.Until(() => GetSkillField(0).value == skillC.name);

            Assert.AreEqual(skillC.name, GetSkillField(0).value);
        }

        [UnityTest]
        public IEnumerator MissingSkillName_ShownBlank()
        {
            skillPool.AddSkillName("SkillPoolViewTests_DeletedSkill", 1);

            yield return TestWait.Until(() => GetRows().Count == 3);

            Assert.AreEqual(string.Empty, GetSkillField(2).value);
        }
        #endregion
    }
}

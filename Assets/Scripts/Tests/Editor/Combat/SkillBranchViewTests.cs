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
    public class SkillBranchViewTests
    {
        // Tested:
        //  - Skill field (typed) - known name sets the slot in the asset's own casing, unknown name keeps the current skill, empty clears it
        //  - Skills the runtime name lookup doesn't resolve back to (e.g. not addressable) - typed or dropped, refused and slot unchanged
        //  - Skill slots as drop targets - a dropped project skill fills the slot (field, branch, skills-changed callback, add-branch button)
        // Not Tested:
        //  - Dragging from SkillListRow - starting a real editor drag isn't safe headless, checked manually
        // Notes:
        //  - Drags are synthesized (see StandardObjectDropManipulatorTests)
        //  - Branch slots resolve skills by name through the Skill addressables cache (so this uses a project skill for test)

        // State
        private readonly TestObjectTracker tracker = new();
        private EditorWindow window;
        private SkillTree skillTree;
        private SkillBranch rootBranch;
        private SkillBranchView branchView;
        private Skill projectSkill;
        private Skill unaddressableSkill;
        private int skillsChangedCount;

        #region Setup
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            projectSkill = CombatTestBuilders.LoadAddressableSkills(1)[0];
            unaddressableSkill = tracker.CreateSkill("SkillBranchViewTests_NotAddressable");
            skillTree = tracker.CreateSkillTree("SkillBranchViewTests_Tree", out rootBranch);
            skillsChangedCount = 0;
            branchView = new SkillBranchView(rootBranch, skillTree, true, null, () => skillsChangedCount++, null, null, () => 1f, ResolveSkill, null);

            window = HeadlessEditorWindow.CreateOffscreenWindow(400f, 300f);
            window.rootVisualElement.Add(branchView);
            yield return HeadlessEditorWindow.WaitUntilSettled(window);
        }

        [TearDown]
        public void TearDown()
        {
            HeadlessEditorWindow.EndObjectDrag();
            HeadlessEditorWindow.Close(window);
            skillTree.ClearUndo();
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private Skill ResolveSkill(string skillName) => SkillSetEditor.ResolveSkill(new[] { projectSkill, unaddressableSkill }, skillName);

        // Up, left, right, down
        private List<TextField> GetSkillFields() => branchView.Query<TextField>().ToList();

        private static bool IsShown(VisualElement element) => element.resolvedStyle.display != DisplayStyle.None && element.style.display != DisplayStyle.None;
        #endregion

        #region Tests
        [Test]
        public void SkillField_KnownNameOtherCase_SetsSkillWithAssetCasing()
        {
            TextField rightField = GetSkillFields()[(int)SkillBranchMapping.Right];

            rightField.value = projectSkill.name.ToUpperInvariant();

            Assert.AreEqual(projectSkill, rootBranch.GetSkill(SkillBranchMapping.Right));
            Assert.AreEqual(projectSkill.name, rightField.value);
            Assert.AreEqual(1, skillsChangedCount);
        }

        [Test]
        public void SkillField_UnknownName_KeepsCurrentSkill()
        {
            TextField downField = GetSkillFields()[(int)SkillBranchMapping.Down];
            downField.value = projectSkill.name;

            downField.value = "SkillBranchViewTests_Typo";

            Assert.AreEqual(projectSkill, rootBranch.GetSkill(SkillBranchMapping.Down));
            Assert.AreEqual(projectSkill.name, downField.value);
            Assert.AreEqual(1, skillsChangedCount, "only the first edit counts");
        }

        [Test]
        public void SkillField_UnaddressableName_KeepsCurrentSkill()
        {
            TextField downField = GetSkillFields()[(int)SkillBranchMapping.Down];
            downField.value = projectSkill.name;

            downField.value = unaddressableSkill.name;

            Assert.AreEqual(projectSkill, rootBranch.GetSkill(SkillBranchMapping.Down));
            Assert.AreEqual(projectSkill.name, downField.value);
        }

        [Test]
        public void SkillField_Emptied_ClearsSkill()
        {
            TextField upField = GetSkillFields()[(int)SkillBranchMapping.Up];
            upField.value = projectSkill.name;

            upField.value = string.Empty;

            Assert.IsNull(rootBranch.GetSkill(SkillBranchMapping.Up));
            Assert.AreEqual(2, skillsChangedCount);
            Assert.IsFalse(IsShown(upField.parent.Q<Button>()), "add-branch button hidden for an empty slot");
        }

        [Test]
        public void Drop_ProjectSkill_FillsSlot()
        {
            TextField leftField = GetSkillFields()[(int)SkillBranchMapping.Left];

            HeadlessEditorWindow.DropObjects(leftField, projectSkill);

            Assert.AreEqual(projectSkill.name, leftField.value);
            Assert.AreEqual(projectSkill, rootBranch.GetSkill(SkillBranchMapping.Left));
            Assert.AreEqual(1, skillsChangedCount);
            Assert.IsTrue(IsShown(leftField.parent.Q<Button>()), "add-branch button shown for a filled slot");
        }

        [Test]
        public void Drop_UnaddressableSkill_RefusedAndSlotUnchanged()
        {
            TextField upField = GetSkillFields()[(int)SkillBranchMapping.Up];

            HeadlessEditorWindow.DropObjects(upField, unaddressableSkill);

            Assert.AreEqual(DragAndDropVisualMode.Rejected, DragAndDrop.visualMode);
            Assert.AreEqual(string.Empty, upField.value);
            Assert.IsNull(rootBranch.GetSkill(SkillBranchMapping.Up));
            Assert.AreEqual(0, skillsChangedCount);
        }
        #endregion
    }
}

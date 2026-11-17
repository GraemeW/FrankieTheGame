#if UNITY_EDITOR
using NUnit.Framework;
using LowDefMustard.Utils.Tests;
using Frankie.Combat;

namespace Frankie.Tests
{
    public class SkillHandlerTreeSelectionTests
    {
        // Tested:
        //  - SkillHandler tree selection - root slots on first read, traversal into a child branch (active skill = slot's skill), leaf slot selects w/o traversal
        //  - Traversal blocked (selects slot's skill instead) - child locked by trait level, child w/ only unaffordable skills, dangling child branch reference
        //  - Unaffordable slot - hidden, neither traverses nor changes the active skill
        //  - ResetSelection - back to root, active skill cleared;  first SetBranchOrSkill before any read only initializes
        //  - skillSelectionChanged - raised on each SetBranchOrSkill / ResetSelection
        // Not Tested:
        //  - Trait unlock against real stat values - BaseStats has no character properties here (0 in every stat), levels gate via the unlock multiplier
        //  - SkillFilterType other than All - only All is used by the UI
        // Notes:
        //  - Play Mode, since SkillHandler only caches its CombatParticipant in Awake
        //  - Branch slots resolve skills by name through the Skill addressables cache, so this uses project skills

        // Const Tunables
        private const float _noTraitGate = 0f;
        private const float _traitGate = 10f;

        // State
        private readonly TestObjectTracker tracker = new();
        private SkillTree skillTree;
        private SkillBranch rootBranch;
        private SkillHandler skillHandler;
        private Skill[] free;
        private Skill unaffordable;
        private int selectionChangedCount;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            free = CombatTestBuilders.LoadAddressableSkills(4, skill => skill.GetAPCost() <= 0f);
            unaffordable = CombatTestBuilders.LoadAddressableSkills(1, skill => skill.GetAPCost() > 0f)[0]; // Participant has 0 AP

            skillTree = tracker.CreateSkillTree("SkillHandlerTreeSelectionTests_Tree", out rootBranch);
            skillHandler = tracker.BuildSkillHandler("SkillHandlerTreeSelectionTests_Participant", skillTree);
            SetTraitGate(_noTraitGate);

            selectionChangedCount = 0;
            skillHandler.skillSelectionChanged += HandleSelectionChanged;
        }

        [TearDown]
        public void TearDown()
        {
            if (skillHandler != null) { skillHandler.skillSelectionChanged -= HandleSelectionChanged; }
            skillTree.ClearUndo();
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private void HandleSelectionChanged() => selectionChangedCount++;

        private void SetTraitGate(float levelMultiplier) => SerializedFieldInjector.SetValue(skillHandler, SkillHandler.skillTreeLevelMultiplierForStatUnlockField, levelMultiplier);

        private SkillBranch CreateChild(SkillBranch parentBranch, SkillBranchMapping skillBranchMapping) => tracker.CreateChildBranch(skillTree, parentBranch, skillBranchMapping);

        private void Select(SkillBranchMapping skillBranchMapping) => skillHandler.SetBranchOrSkill(skillBranchMapping, SkillFilterType.All);
        #endregion

        #region Tests
        [Test]
        public void TreeSelection_FirstRead_ShowsRootSlots()
        {
            rootBranch.SetSkills(up: free[0], right: free[1]);

            CollectionAssert.AreEqual(new[] { free[0], null, free[1], null }, skillHandler.GetSelectionSkills());
            Assert.IsNull(skillHandler.GetActiveSkill());
        }

        [Test]
        public void TreeSelection_SlotWithChildBranch_TraversesAndActivatesSlotSkill()
        {
            rootBranch.SetSkills(up: free[0]);
            CreateChild(rootBranch, SkillBranchMapping.Up).SetSkills(up: null, left: free[1], down: free[2]);
            skillHandler.GetSelectionSkills();

            Select(SkillBranchMapping.Up);

            Assert.AreEqual(free[0], skillHandler.GetActiveSkill());
            CollectionAssert.AreEqual(new[] { null, free[1], null, free[2] }, skillHandler.GetSelectionSkills());
            Assert.AreEqual(1, selectionChangedCount);
        }

        [Test]
        public void TreeSelection_SeveralLevels_TraversesEachInTurn()
        {
            rootBranch.SetSkills(up: free[0]);
            SkillBranch middleBranch = CreateChild(rootBranch, SkillBranchMapping.Up);
            middleBranch.SetSkills(up: null, right: free[1]);
            CreateChild(middleBranch, SkillBranchMapping.Right).SetSkills(up: null, down: free[2]);
            skillHandler.GetSelectionSkills();

            Select(SkillBranchMapping.Up);
            Select(SkillBranchMapping.Right);

            Assert.AreEqual(free[1], skillHandler.GetActiveSkill());
            CollectionAssert.AreEqual(new[] { null, null, null, free[2] }, skillHandler.GetSelectionSkills());
        }

        [Test]
        public void TreeSelection_LeafSlot_ActivatesSkillWithoutTraversing()
        {
            rootBranch.SetSkills(up: free[0], left: free[1]);
            Skill[] rootSelection = skillHandler.GetSelectionSkills();

            Select(SkillBranchMapping.Left);

            Assert.AreEqual(free[1], skillHandler.GetActiveSkill());
            CollectionAssert.AreEqual(rootSelection, skillHandler.GetSelectionSkills());
            Assert.AreEqual(1, selectionChangedCount);
        }

        [Test]
        public void TreeSelection_ChildLockedByTraitLevel_ActivatesSkillWithoutTraversing()
        {
            SetTraitGate(_traitGate); // 0 in every stat ~ root (level 0) passes, child (level 1) needs 10
            rootBranch.SetSkills(up: free[0]);
            CreateChild(rootBranch, SkillBranchMapping.Up).SetSkills(up: free[1]);
            Skill[] rootSelection = skillHandler.GetSelectionSkills();

            Select(SkillBranchMapping.Up);

            Assert.AreEqual(free[0], skillHandler.GetActiveSkill());
            CollectionAssert.AreEqual(rootSelection, skillHandler.GetSelectionSkills());
        }

        [Test]
        public void TreeSelection_ChildWithOnlyUnaffordableSkills_ActivatesSkillWithoutTraversing()
        {
            rootBranch.SetSkills(up: free[0]);
            CreateChild(rootBranch, SkillBranchMapping.Up).SetSkills(up: unaffordable);
            Skill[] rootSelection = skillHandler.GetSelectionSkills();

            Select(SkillBranchMapping.Up);

            Assert.AreEqual(free[0], skillHandler.GetActiveSkill());
            CollectionAssert.AreEqual(rootSelection, skillHandler.GetSelectionSkills());
        }

        [Test]
        public void TreeSelection_DanglingChildBranchReference_ActivatesSkillWithoutTraversing()
        {
            rootBranch.SetSkills(up: free[0]);
            rootBranch.SetBranch(SkillBranchMapping.Up, "SkillHandlerTreeSelectionTests_MissingBranch");
            Skill[] rootSelection = skillHandler.GetSelectionSkills();

            Select(SkillBranchMapping.Up);

            Assert.AreEqual(free[0], skillHandler.GetActiveSkill());
            CollectionAssert.AreEqual(rootSelection, skillHandler.GetSelectionSkills());
        }

        [Test]
        public void TreeSelection_UnaffordableSlot_HiddenAndKeepsActiveSkill()
        {
            rootBranch.SetSkills(up: unaffordable, left: free[0]);
            CreateChild(rootBranch, SkillBranchMapping.Up).SetSkills(up: free[1]);
            CollectionAssert.AreEqual(new[] { null, free[0], null, null }, skillHandler.GetSelectionSkills());

            Select(SkillBranchMapping.Left);
            Select(SkillBranchMapping.Up);

            Assert.AreEqual(free[0], skillHandler.GetActiveSkill());
            CollectionAssert.AreEqual(new[] { null, free[0], null, null }, skillHandler.GetSelectionSkills());
        }

        [Test]
        public void TreeSelection_ResetSelection_ReturnsToRootAndClearsActiveSkill()
        {
            rootBranch.SetSkills(up: free[0], left: free[1]);
            CreateChild(rootBranch, SkillBranchMapping.Up).SetSkills(up: free[2]);
            Skill[] rootSelection = skillHandler.GetSelectionSkills();
            Select(SkillBranchMapping.Up);

            skillHandler.ResetSelection();

            Assert.IsNull(skillHandler.GetActiveSkill());
            CollectionAssert.AreEqual(rootSelection, skillHandler.GetSelectionSkills());
            Assert.AreEqual(2, selectionChangedCount);
        }

        [Test]
        public void TreeSelection_SetBranchOrSkillBeforeAnyRead_OnlyInitializes()
        {
            rootBranch.SetSkills(up: free[0]);
            CreateChild(rootBranch, SkillBranchMapping.Up).SetSkills(up: free[1]);

            Select(SkillBranchMapping.Up);

            Assert.IsNull(skillHandler.GetActiveSkill());
            CollectionAssert.AreEqual(new[] { free[0], null, null, null }, skillHandler.GetSelectionSkills());
            Assert.AreEqual(1, selectionChangedCount);
        }
        #endregion
    }
}
#endif

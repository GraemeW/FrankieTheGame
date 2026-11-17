#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LowDefMustard.Utils.Tests;
using Frankie.Combat;

namespace Frankie.Tests
{
    public class SkillHandlerTests
    {
        // Tested:
        //  - SkillHandler.TryGetRandomSkill - exclusions, choose-from subset, AP filter, zero-weight / null entries skipped, no skill set
        //  - SkillHandler pool selection - slot fill order + count, no repeats, AP filter on roll, SetBranchOrSkill picks the slot without traversal
        //  - SkillHandler pool reroll - kept on reset (cancel), rerolled after own action or entering battle, kept after another's action
        // Not Tested:
        //  - Weight distribution - ProbabilityPairExtensions' own tests cover the roll
        //  - Unsubscribe on battle outro / abort - needs a staged battle state event
        //  - BattleAI / BattleAIPriority - need a live battle (targets, predicates)
        // Note:  Play Mode, since SkillHandler only caches its CombatParticipant in Awake

        // Const Tunables
        private const int _rollRepeats = 25;

        // State
        private readonly TestObjectTracker tracker = new();
        private CombatParticipant combatParticipant;
        private SkillHandler skillHandler;
        private SkillPool skillPool;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            skillPool = tracker.CreateSkillPool("SkillHandlerTests_Pool");
            skillHandler = tracker.BuildSkillHandler("SkillHandlerTests_Participant", skillPool);
            combatParticipant = skillHandler.GetComponent<CombatParticipant>();
        }

        [TearDown]
        public void TearDown()
        {
            if (combatParticipant != null) { combatParticipant.SubscribeToBattleStateChanges(false); }
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private static Skill[] Shown(Skill[] selectionSkills) => selectionSkills.Where(skill => skill != null).ToArray();

        private void AssertAlwaysPicks(SkillHandler handler, Skill expected, ICollection<Skill> skillsToExclude, bool filterByAP, ICollection<Skill> skillsToChooseFrom = null)
        {
            for (int i = 0; i < _rollRepeats; i++)
            {
                Assert.IsTrue(handler.TryGetRandomSkill(skillsToExclude, filterByAP, skillsToChooseFrom, out Skill skill));
                Assert.AreEqual(expected, skill);
            }
        }

        private static void AssertNoPick(SkillHandler handler, ICollection<Skill> skillsToExclude, bool filterByAP, ICollection<Skill> skillsToChooseFrom = null)
        {
            Assert.IsFalse(handler.TryGetRandomSkill(skillsToExclude, filterByAP, skillsToChooseFrom, out Skill skill));
            Assert.IsNull(skill);
        }
        #endregion

        #region Tests
        [Test]
        public void TryGetRandomSkill_ExcludedSkill_NeverPicked()
        {
            Skill skillA = tracker.CreateSkill("A");
            Skill skillB = tracker.CreateSkill("B");
            skillPool.SetSkills(skillA, skillB);

            AssertAlwaysPicks(skillHandler, skillB, new List<Skill> { skillA }, false);
        }

        [Test]
        public void TryGetRandomSkill_AllExcluded_ReturnsFalse()
        {
            Skill skillA = tracker.CreateSkill("A");
            skillPool.SetSkills(skillA);

            AssertNoPick(skillHandler, new List<Skill> { skillA }, false);
        }

        [Test]
        public void TryGetRandomSkill_ChooseFrom_LimitsToSubset()
        {
            Skill skillA = tracker.CreateSkill("A");
            Skill skillB = tracker.CreateSkill("B");
            Skill skillC = tracker.CreateSkill("C");
            skillPool.SetSkills(skillA, skillB, skillC);

            AssertAlwaysPicks(skillHandler, skillC, new List<Skill>(), false, new[] { skillC });
        }

        [Test]
        public void TryGetRandomSkill_ChooseFromOutsideSet_ReturnsFalse()
        {
            skillPool.SetSkills(tracker.CreateSkill("A"));

            AssertNoPick(skillHandler, new List<Skill>(), false, new[] { tracker.CreateSkill("Elsewhere") });
        }

        [Test]
        public void TryGetRandomSkill_FilterByAP_SkipsUnaffordable()
        {
            Skill expensive = tracker.CreateSkill("Expensive", 5f);
            Skill free = tracker.CreateSkill("Free");
            skillPool.SetSkills(expensive, free);

            AssertAlwaysPicks(skillHandler, free, new List<Skill>(), true);
            AssertNoPick(skillHandler, new List<Skill> { free }, true);
        }

        [Test]
        public void TryGetRandomSkill_NoAPFilter_KeepsUnaffordable()
        {
            Skill expensive = tracker.CreateSkill("Expensive", 5f);
            Skill free = tracker.CreateSkill("Free");
            skillPool.SetSkills(expensive, free);

            AssertAlwaysPicks(skillHandler, expensive, new List<Skill> { free }, false);
        }

        [Test]
        public void TryGetRandomSkill_ZeroWeightAndNullEntries_Skipped()
        {
            Skill zeroWeight = tracker.CreateSkill("ZeroWeight");
            Skill weighted = tracker.CreateSkill("Weighted");
            skillPool.SetSkills((zeroWeight, 0), (null, 3), (weighted, 1));

            AssertAlwaysPicks(skillHandler, weighted, new List<Skill>(), false);
        }

        [Test]
        public void TryGetRandomSkill_NoSkillSet_ReturnsFalse()
        {
            SkillHandler emptyHandler = tracker.BuildSkillHandler("SkillHandlerTests_Empty", null);

            Assert.IsFalse(emptyHandler.HasSkillSet());
            AssertNoPick(emptyHandler, new List<Skill>(), false);
        }

        [Test]
        public void PoolSelection_FewerThanFourSkills_FillsInOrderAndLeavesRestEmpty()
        {
            Skill skillA = tracker.CreateSkill("A");
            Skill skillB = tracker.CreateSkill("B");
            skillPool.SetSkills(skillA, skillB);

            Skill[] selectionSkills = skillHandler.GetSelectionSkills();

            CollectionAssert.AreEquivalent(new[] { skillA, skillB }, new[] { selectionSkills[0], selectionSkills[1] });
            Assert.IsNull(selectionSkills[2]);
            Assert.IsNull(selectionSkills[3]);
        }

        [Test]
        public void PoolSelection_MoreThanFourSkills_FillsFourDistinctFromPool()
        {
            Skill[] skills = Enumerable.Range(0, 6).Select(index => tracker.CreateSkill($"Skill{index}")).ToArray();
            skillPool.SetSkills(skills);

            Skill[] selectionSkills = skillHandler.GetSelectionSkills();

            Assert.AreEqual(4, Shown(selectionSkills).Length);
            CollectionAssert.AllItemsAreUnique(selectionSkills);
            CollectionAssert.IsSubsetOf(selectionSkills, skills);
        }

        [Test]
        public void PoolSelection_RepeatedEntries_ShownOnce()
        {
            Skill skillA = tracker.CreateSkill("A");
            Skill skillB = tracker.CreateSkill("B");
            skillPool.SetSkills(skillA, skillA, skillB);

            Skill[] shown = Shown(skillHandler.GetSelectionSkills());

            CollectionAssert.AreEquivalent(new[] { skillA, skillB }, shown);
        }

        [Test]
        public void PoolSelection_Roll_SkipsUnaffordable()
        {
            Skill expensive = tracker.CreateSkill("Expensive", 5f);
            Skill free = tracker.CreateSkill("Free");
            skillPool.SetSkills(expensive, free);

            CollectionAssert.AreEqual(new[] { free }, Shown(skillHandler.GetSelectionSkills()));
        }

        [Test]
        public void PoolSelection_SetBranchOrSkill_SelectsSlotSkillWithoutTraversal()
        {
            skillPool.SetSkills(tracker.CreateSkill("A"), tracker.CreateSkill("B"));
            Skill[] selectionSkills = skillHandler.GetSelectionSkills();

            skillHandler.SetBranchOrSkill(SkillBranchMapping.Left, SkillFilterType.All);
            Assert.AreEqual(selectionSkills[1], skillHandler.GetActiveSkill());

            skillHandler.SetBranchOrSkill(SkillBranchMapping.Up, SkillFilterType.All);
            Assert.AreEqual(selectionSkills[0], skillHandler.GetActiveSkill());
            CollectionAssert.AreEqual(selectionSkills, skillHandler.GetSelectionSkills());
        }

        [Test]
        public void PoolSelection_SetBranchOrSkill_EmptySlotKeepsActiveSkill()
        {
            skillPool.SetSkills(tracker.CreateSkill("A"));
            Skill[] selectionSkills = skillHandler.GetSelectionSkills();

            skillHandler.SetBranchOrSkill(SkillBranchMapping.Up, SkillFilterType.All);
            skillHandler.SetBranchOrSkill(SkillBranchMapping.Down, SkillFilterType.All);

            Assert.AreEqual(selectionSkills[0], skillHandler.GetActiveSkill());
        }

        [Test]
        public void PoolReroll_ResetWithoutAction_KeepsSlots()
        {
            Skill skillA = tracker.CreateSkill("A");
            Skill skillB = tracker.CreateSkill("B");
            skillPool.SetSkills(skillA, skillB);
            Skill[] firstRoll = skillHandler.GetSelectionSkills();

            skillPool.SetSkills(tracker.CreateSkill("C"), tracker.CreateSkill("D"));
            skillHandler.ResetSelection();

            CollectionAssert.AreEqual(firstRoll, skillHandler.GetSelectionSkills());
        }

        [Test]
        public void PoolReroll_AfterOwnAction_RerollsOnNextReset()
        {
            skillPool.SetSkills(tracker.CreateSkill("A"), tracker.CreateSkill("B"));
            combatParticipant.SubscribeToBattleStateChanges(true);
            Skill[] firstRoll = skillHandler.GetSelectionSkills();

            Skill skillC = tracker.CreateSkill("C");
            Skill skillD = tracker.CreateSkill("D");
            skillPool.SetSkills(skillC, skillD);
            CombatTestBuilders.RaiseQueuedAction(combatParticipant);
            CollectionAssert.AreEqual(firstRoll, skillHandler.GetSelectionSkills(), "Slots should hold until the next reset");

            skillHandler.ResetSelection();
            CollectionAssert.AreEquivalent(new[] { skillC, skillD }, Shown(skillHandler.GetSelectionSkills()));
        }

        [Test]
        public void PoolReroll_AfterOtherParticipantAction_KeepsSlots()
        {
            skillPool.SetSkills(tracker.CreateSkill("A"), tracker.CreateSkill("B"));
            combatParticipant.SubscribeToBattleStateChanges(true);
            Skill[] firstRoll = skillHandler.GetSelectionSkills();
            CombatParticipant otherParticipant = tracker.BuildSkillHandler("SkillHandlerTests_Other", null).GetComponent<CombatParticipant>();

            skillPool.SetSkills(tracker.CreateSkill("C"), tracker.CreateSkill("D"));
            CombatTestBuilders.RaiseQueuedAction(otherParticipant);
            skillHandler.ResetSelection();

            CollectionAssert.AreEqual(firstRoll, skillHandler.GetSelectionSkills());
        }

        [Test]
        public void PoolReroll_OnEnteringBattle_RerollsOnNextReset()
        {
            skillPool.SetSkills(tracker.CreateSkill("A"), tracker.CreateSkill("B"));
            skillHandler.GetSelectionSkills();

            Skill skillC = tracker.CreateSkill("C");
            Skill skillD = tracker.CreateSkill("D");
            skillPool.SetSkills(skillC, skillD);
            combatParticipant.SubscribeToBattleStateChanges(true);
            skillHandler.ResetSelection();

            CollectionAssert.AreEquivalent(new[] { skillC, skillD }, Shown(skillHandler.GetSelectionSkills()));
        }
        #endregion
    }
}
#endif

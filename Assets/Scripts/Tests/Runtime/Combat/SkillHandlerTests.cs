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
        //  - Entry naming a missing skill (e.g. deleted) - never rolled, never shown, no throw
        //  - SkillHandler pool selection - slot fill order + count, no repeats, AP filter on roll, SetBranchOrSkill picks the slot without traversal
        //  - SkillHandler pool reroll - kept on reset (cancel), rerolled after own action or entering battle, kept after another's action
        // Not Tested:
        //  - Weight distribution - ProbabilityPairExtensions' own tests cover the roll
        //  - Unsubscribe on battle outro / abort - needs a staged battle state event
        //  - BattleAI / BattleAIPriority - need a live battle (targets, predicates)
        // Notes:
        //  - Play Mode, since SkillHandler only caches its CombatParticipant in Awake
        //  - Pool entries resolve skills by name through the Skill addressables cache, so this uses project skills

        // Const Tunables
        private const int _rollRepeats = 25;
        private const string _missingSkillName = "SkillHandlerTests_DeletedSkill";

        // State
        private readonly TestObjectTracker tracker = new();
        private CombatParticipant combatParticipant;
        private SkillHandler skillHandler;
        private SkillPool skillPool;
        private Skill[] freeSkills;
        private Skill unaffordable;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            freeSkills = CombatTestBuilders.LoadAddressableSkills(6, skill => skill.GetAPCost() <= 0f);
            unaffordable = CombatTestBuilders.LoadAddressableSkills(1, skill => skill.GetAPCost() > 0f)[0]; // Participant has 0 AP
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
            Skill skillA = freeSkills[0];
            Skill skillB = freeSkills[1];
            skillPool.SetSkills(skillA, skillB);

            AssertAlwaysPicks(skillHandler, skillB, new List<Skill> { skillA }, false);
        }

        [Test]
        public void TryGetRandomSkill_AllExcluded_ReturnsFalse()
        {
            Skill skillA = freeSkills[0];
            skillPool.SetSkills(skillA);

            AssertNoPick(skillHandler, new List<Skill> { skillA }, false);
        }

        [Test]
        public void TryGetRandomSkill_ChooseFrom_LimitsToSubset()
        {
            Skill skillA = freeSkills[0];
            Skill skillB = freeSkills[1];
            Skill skillC = freeSkills[2];
            skillPool.SetSkills(skillA, skillB, skillC);

            AssertAlwaysPicks(skillHandler, skillC, new List<Skill>(), false, new[] { skillC });
        }

        [Test]
        public void TryGetRandomSkill_ChooseFromOutsideSet_ReturnsFalse()
        {
            skillPool.SetSkills(freeSkills[0]);

            AssertNoPick(skillHandler, new List<Skill>(), false, new[] { tracker.CreateSkill("Elsewhere") });
        }

        [Test]
        public void TryGetRandomSkill_FilterByAP_SkipsUnaffordable()
        {
            Skill expensive = unaffordable;
            Skill free = freeSkills[0];
            skillPool.SetSkills(expensive, free);

            AssertAlwaysPicks(skillHandler, free, new List<Skill>(), true);
            AssertNoPick(skillHandler, new List<Skill> { free }, true);
        }

        [Test]
        public void TryGetRandomSkill_NoAPFilter_KeepsUnaffordable()
        {
            Skill expensive = unaffordable;
            Skill free = freeSkills[0];
            skillPool.SetSkills(expensive, free);

            AssertAlwaysPicks(skillHandler, expensive, new List<Skill> { free }, false);
        }

        [Test]
        public void TryGetRandomSkill_ZeroWeightAndNullEntries_Skipped()
        {
            Skill zeroWeight = freeSkills[0];
            Skill weighted = freeSkills[1];
            skillPool.SetSkills((zeroWeight, 0), (null, 3), (weighted, 1));

            AssertAlwaysPicks(skillHandler, weighted, new List<Skill>(), false);
        }

        [Test]
        public void TryGetRandomSkill_MissingSkillName_NeverPicked()
        {
            skillPool.SetSkills(freeSkills[0]);
            skillPool.AddSkillName(_missingSkillName, 100);

            AssertAlwaysPicks(skillHandler, freeSkills[0], new List<Skill>(), false);
        }

        [Test]
        public void TryGetRandomSkill_OnlyMissingSkillNames_ReturnsFalse()
        {
            skillPool.AddSkillName(_missingSkillName, 1);

            AssertNoPick(skillHandler, new List<Skill>(), true);
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
            Skill skillA = freeSkills[0];
            Skill skillB = freeSkills[1];
            skillPool.SetSkills(skillA, skillB);

            Skill[] selectionSkills = skillHandler.GetSelectionSkills();

            CollectionAssert.AreEquivalent(new[] { skillA, skillB }, new[] { selectionSkills[0], selectionSkills[1] });
            Assert.IsNull(selectionSkills[2]);
            Assert.IsNull(selectionSkills[3]);
        }

        [Test]
        public void PoolSelection_MoreThanFourSkills_FillsFourDistinctFromPool()
        {
            Skill[] skills = freeSkills.ToArray();
            skillPool.SetSkills(skills);

            Skill[] selectionSkills = skillHandler.GetSelectionSkills();

            Assert.AreEqual(4, Shown(selectionSkills).Length);
            CollectionAssert.AllItemsAreUnique(selectionSkills);
            CollectionAssert.IsSubsetOf(selectionSkills, skills);
        }

        [Test]
        public void PoolSelection_RepeatedEntries_ShownOnce()
        {
            Skill skillA = freeSkills[0];
            Skill skillB = freeSkills[1];
            skillPool.SetSkills(skillA, skillA, skillB);

            Skill[] shown = Shown(skillHandler.GetSelectionSkills());

            CollectionAssert.AreEquivalent(new[] { skillA, skillB }, shown);
        }

        [Test]
        public void PoolSelection_MissingSkillName_NotShown()
        {
            skillPool.SetSkills(freeSkills[0]);
            skillPool.AddSkillName(_missingSkillName, 100);

            CollectionAssert.AreEqual(new[] { freeSkills[0] }, Shown(skillHandler.GetSelectionSkills()));
        }

        [Test]
        public void PoolSelection_Roll_SkipsUnaffordable()
        {
            Skill expensive = unaffordable;
            Skill free = freeSkills[0];
            skillPool.SetSkills(expensive, free);

            CollectionAssert.AreEqual(new[] { free }, Shown(skillHandler.GetSelectionSkills()));
        }

        [Test]
        public void PoolSelection_SetBranchOrSkill_SelectsSlotSkillWithoutTraversal()
        {
            skillPool.SetSkills(freeSkills[0], freeSkills[1]);
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
            skillPool.SetSkills(freeSkills[0]);
            Skill[] selectionSkills = skillHandler.GetSelectionSkills();

            skillHandler.SetBranchOrSkill(SkillBranchMapping.Up, SkillFilterType.All);
            skillHandler.SetBranchOrSkill(SkillBranchMapping.Down, SkillFilterType.All);

            Assert.AreEqual(selectionSkills[0], skillHandler.GetActiveSkill());
        }

        [Test]
        public void PoolReroll_ResetWithoutAction_KeepsSlots()
        {
            Skill skillA = freeSkills[0];
            Skill skillB = freeSkills[1];
            skillPool.SetSkills(skillA, skillB);
            Skill[] firstRoll = skillHandler.GetSelectionSkills();

            skillPool.SetSkills(freeSkills[2], freeSkills[3]);
            skillHandler.ResetSelection();

            CollectionAssert.AreEqual(firstRoll, skillHandler.GetSelectionSkills());
        }

        [Test]
        public void PoolReroll_AfterOwnAction_RerollsOnNextReset()
        {
            skillPool.SetSkills(freeSkills[0], freeSkills[1]);
            combatParticipant.SubscribeToBattleStateChanges(true);
            Skill[] firstRoll = skillHandler.GetSelectionSkills();

            Skill skillC = freeSkills[2];
            Skill skillD = freeSkills[3];
            skillPool.SetSkills(skillC, skillD);
            CombatTestBuilders.RaiseQueuedAction(combatParticipant);
            CollectionAssert.AreEqual(firstRoll, skillHandler.GetSelectionSkills(), "Slots should hold until the next reset");

            skillHandler.ResetSelection();
            CollectionAssert.AreEquivalent(new[] { skillC, skillD }, Shown(skillHandler.GetSelectionSkills()));
        }

        [Test]
        public void PoolReroll_AfterOtherParticipantAction_KeepsSlots()
        {
            skillPool.SetSkills(freeSkills[0], freeSkills[1]);
            combatParticipant.SubscribeToBattleStateChanges(true);
            Skill[] firstRoll = skillHandler.GetSelectionSkills();
            CombatParticipant otherParticipant = tracker.BuildSkillHandler("SkillHandlerTests_Other", null).GetComponent<CombatParticipant>();

            skillPool.SetSkills(freeSkills[2], freeSkills[3]);
            CombatTestBuilders.RaiseQueuedAction(otherParticipant);
            skillHandler.ResetSelection();

            CollectionAssert.AreEqual(firstRoll, skillHandler.GetSelectionSkills());
        }

        [Test]
        public void PoolReroll_OnEnteringBattle_RerollsOnNextReset()
        {
            skillPool.SetSkills(freeSkills[0], freeSkills[1]);
            skillHandler.GetSelectionSkills();

            Skill skillC = freeSkills[2];
            Skill skillD = freeSkills[3];
            skillPool.SetSkills(skillC, skillD);
            combatParticipant.SubscribeToBattleStateChanges(true);
            skillHandler.ResetSelection();

            CollectionAssert.AreEquivalent(new[] { skillC, skillD }, Shown(skillHandler.GetSelectionSkills()));
        }
        #endregion
    }
}
#endif

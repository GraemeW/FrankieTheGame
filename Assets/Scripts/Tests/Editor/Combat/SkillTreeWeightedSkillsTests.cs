using System.Linq;
using NUnit.Framework;
using LowDefMustard.Utils;
using LowDefMustard.Utils.Tests;
using Frankie.Combat;

namespace Frankie.Tests.Editor
{
    public class SkillTreeWeightedSkillsTests
    {
        // Tested:
        //  - SkillTree.GetWeightedSkills - one weight-1 entry per filled slot across all branches, repeats kept, empty tree
        // Note:  Branch slots resolve skills by name through the Skill addressables cache, so this uses two project skills

        // State
        private readonly TestObjectTracker tracker = new();
        private SkillTree skillTree;
        private SkillBranch rootBranch;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            skillTree = tracker.CreateSkillTree("SkillTreeWeightedSkillsTests_Tree", out rootBranch);
        }

        [TearDown]
        public void TearDown()
        {
            skillTree.ClearUndo();
            tracker.DestroyAll();
        }
        #endregion

        #region Tests
        [Test]
        public void GetWeightedSkills_EmptyTree_ReturnsNothing()
        {
            CollectionAssert.IsEmpty(skillTree.GetWeightedSkills());
        }

        [Test]
        public void GetWeightedSkills_SkillOnSeveralBranches_OneEntryPerSlot()
        {
            Skill[] skills = CombatTestBuilders.LoadAddressableSkills(2);
            Skill repeated = skills[0];
            Skill single = skills[1];
            SkillBranch childBranch = tracker.CreateChildBranch(skillTree, rootBranch, SkillBranchMapping.Down);
            rootBranch.SetSkills(up: repeated, left: single);
            childBranch.SetSkills(up: null, right: repeated);

            IObjectProbabilityPair<Skill>[] weightedSkills = skillTree.GetWeightedSkills().ToArray();

            CollectionAssert.AreEquivalent(new[] { repeated, repeated, single }, weightedSkills.Select(pair => pair.value));
            Assert.IsTrue(weightedSkills.All(pair => pair.weight == 1));
        }
        #endregion
    }
}

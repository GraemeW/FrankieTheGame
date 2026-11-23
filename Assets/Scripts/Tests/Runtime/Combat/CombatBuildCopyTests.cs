#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Utils.Tests;
using Frankie.Combat;
using Frankie.Stats;

namespace Frankie.Tests
{
    public class CombatBuildCopyTests
    {
        // Tested:
        //  - TargetSpecificCharacter.SetTarget - targets the ally / foe holding a build copy of its character
        //  - CharacterPresentBattleAIPredicate - finds a combatant holding a build copy
        //  - IsCharacterDeadPredicate - answers (rather than abstaining) for a combatant holding a build copy
        //  - CharacterFilter - keeps / negates a combatant holding a build copy
        // Not Tested:
        //  - BattleAI choosing the priority in a live battle - needs a running BattleController, the target pick is what changed
        // Test Details:
        //  - The skill has no battle action, so Skill.SetTargets is a no-op and only the chosen entity is checked
        //  - Play mode, since destroying an edit-mode CombatParticipant runs the localization deletion handler (ILocalizable.TriggerOnDestroy)
        //  - Synchronous tests ~ objects are torn down before Start;  BattleAI's local ally / foe lists are filled directly

        // Const (note that character names are necessarily string ref and brittle, must be updated w/ project layout/re-configuration)
        private const string _targetName = "Frankie";
        private const string _otherCharacterName = "Lucy";

        // State
        private readonly TestObjectTracker tracker = new();
        private CharacterProperties target;
        private CombatParticipant targetBuildCopyCombatant;
        private CombatParticipant otherCombatant;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            target = PartyTestBuilders.LoadCharacter(_targetName);
            CharacterProperties otherCharacter = PartyTestBuilders.LoadCharacter(_otherCharacterName);
            Assert.IsNotNull(target, $"Missing project character '{_targetName}'");
            Assert.IsNotNull(otherCharacter, $"Missing project character '{_otherCharacterName}'");

            targetBuildCopyCombatant = tracker.BuildCombatParticipant(tracker.CreateBuildCopy(target));
            otherCombatant = tracker.BuildCombatParticipant(otherCharacter);
        }

        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private BattleAI BuildBattleAI(bool targetIsAlly)
        {
            BattleAI battleAI = tracker.Track(new GameObject("CombatBuildCopyTests_BattleAI")).AddComponent<BattleAI>();
            var targetEntity = new BattleEntity(targetBuildCopyCombatant);
            var otherEntity = new BattleEntity(otherCombatant);
            battleAI.GetLocalAllies().Add(targetIsAlly ? targetEntity : otherEntity);
            battleAI.GetLocalFoes().Add(targetIsAlly ? otherEntity : targetEntity);
            return battleAI;
        }
        #endregion

        #region Tests
        [TestCase(true)]
        [TestCase(false)]
        public void TargetSpecificCharacter_CombatantHoldsBuildCopy_IsTargeted(bool targetIsAlly)
        {
            var targetPriority = tracker.Create<TargetSpecificCharacter>("CombatBuildCopyTests_TargetSpecificCharacter");
            SerializedFieldInjector.Set(targetPriority, TargetSpecificCharacter.characterPropertiesField, target);
            BattleAI battleAI = BuildBattleAI(targetIsAlly);
            var battleActionData = new BattleActionData(battleAI.GetComponent<CombatParticipant>());

            Assert.IsTrue(targetPriority.SetTarget(battleAI, battleActionData, tracker.Create<Skill>("CombatBuildCopyTests_Skill")));
            CollectionAssert.AreEqual(new[] { targetBuildCopyCombatant }, battleActionData.GetTargets().Select(battleEntity => battleEntity.combatParticipant));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CharacterPresentBattleAIPredicate_CombatantHoldsBuildCopy_EvaluatesTrue(bool targetIsAlly)
        {
            var predicate = tracker.Create<CharacterPresentBattleAIPredicate>("CombatBuildCopyTests_CharacterPresent");
            SerializedFieldInjector.Set(predicate, CharacterPresentBattleAIPredicate.characterPropertiesField, target);

            Assert.AreEqual(true, predicate.Evaluate(BuildBattleAI(targetIsAlly)));
        }

        [Test]
        public void IsCharacterDeadPredicate_CombatantHoldsBuildCopy_Answers()
        {
            var predicate = tracker.Create<IsCharacterDeadPredicate>("CombatBuildCopyTests_IsCharacterDead");
            SerializedFieldInjector.SetArray(predicate, IsCharacterDeadPredicate.charactersField, target);

            Assert.AreEqual(false, predicate.Evaluate(targetBuildCopyCombatant), "Expected alive (false), null means the character wasn't matched");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CharacterFilter_CombatantHoldsBuildCopy_IsFiltered(bool negate)
        {
            var filter = tracker.Create<CharacterFilter>("CombatBuildCopyTests_CharacterFilter");
            SerializedFieldInjector.SetArray(filter, CharacterFilter.characterPropertiesToFilterField, target);
            SerializedFieldInjector.SetValue(filter, CharacterFilter.negateField, negate);

            CombatParticipant[] kept = filter.Filter(new[] { new BattleEntity(targetBuildCopyCombatant), new BattleEntity(otherCombatant) })
                .Select(battleEntity => battleEntity.combatParticipant).ToArray();

            CollectionAssert.AreEqual(new[] { negate ? otherCombatant : targetBuildCopyCombatant }, kept);
        }
        #endregion
    }
}
#endif

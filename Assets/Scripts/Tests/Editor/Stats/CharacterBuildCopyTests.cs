using NUnit.Framework;
using UnityEditor;
using LowDefMustard.Utils.Tests;
using Frankie.Stats;

namespace Frankie.Tests.Editor
{
    public class CharacterBuildCopyTests
    {
        // Tested:
        //  - CharacterProperties.AreCharacterPropertiesMatched - build copy matches, different character / null don't
        //  - IsCharacterPredicate - matches a BaseStats holding the build copy
        //  - Progression - stat lookups (HasProgression, GetStat, GetStatSheet) + level-averaged sheets answer for the build copy
        // Not Tested:
        //  - Progression catch-up level-up sheets (GetLevelUpSheet) - random roll, same lookups as GetLevelAveragedStatSheet
        // Note:  A build copy is a second object with the same name + data (see BuildCopyTestBuilders), as player builds hold for Addressables assets
        // Note:  Level-averaged sheets read the project Progression asset (read-only, its serialized charts are already built)

        // Const (note that paths/names are necessarily string ref and brittle, must be updated w/ project layout/re-configuration)
        private const string _progressionPath = "Assets/Game/CharacterObjects/Progression.asset";
        private const string _characterName = "Frankie";
        private const string _otherCharacterName = "Lucy";
        private const int _chartedLevel = 5; // Past level 1, which is the base stat sheet copied as-is

        // State
        private readonly TestObjectTracker tracker = new();
        private CharacterProperties character;
        private CharacterProperties characterBuildCopy;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            character = PartyTestBuilders.LoadCharacter(_characterName);
            Assert.IsNotNull(character, $"Missing project character '{_characterName}'");
            characterBuildCopy = tracker.CreateBuildCopy(character);
        }

        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Tests
        [Test]
        public void AreCharacterPropertiesMatched_BuildCopy_Matches()
        {
            Assert.AreNotSame(character, characterBuildCopy);
            Assert.IsTrue(CharacterProperties.AreCharacterPropertiesMatched(character, characterBuildCopy));
        }

        [Test]
        public void AreCharacterPropertiesMatched_OtherCharacterOrNull_DoesNotMatch()
        {
            CharacterProperties otherCharacter = PartyTestBuilders.LoadCharacter(_otherCharacterName);
            Assert.IsNotNull(otherCharacter, $"Missing project character '{_otherCharacterName}'");

            Assert.IsFalse(CharacterProperties.AreCharacterPropertiesMatched(character, otherCharacter));
            Assert.IsFalse(CharacterProperties.AreCharacterPropertiesMatched(character, null));
        }

        [Test]
        public void IsCharacterPredicate_BaseStatsHoldsBuildCopy_EvaluatesTrue()
        {
            var predicate = tracker.Create<IsCharacterPredicate>("CharacterBuildCopyTests_IsCharacter");
            SerializedFieldInjector.Set(predicate, IsCharacterPredicate.characterField, character);
            BaseStats baseStats = tracker.BuildCharacter(characterBuildCopy);

            Assert.AreEqual(true, predicate.Evaluate(baseStats));
        }

        [Test]
        public void Progression_StatLookups_AnswerForBuildCopy()
        {
            var progression = tracker.Create<Progression>("CharacterBuildCopyTests_Progression");
            progression.AddToProgressionAsset(character);
            progression.UpdateProgressionAsset(character, Stat.Brawn, 42f);

            Assert.IsTrue(progression.HasProgression(characterBuildCopy));
            Assert.AreEqual(42f, progression.GetStat(Stat.Brawn, characterBuildCopy));
            CollectionAssert.AreEquivalent(progression.GetStatSheet(character), progression.GetStatSheet(characterBuildCopy));
            Undo.ClearUndo(progression);
        }

        [Test]
        public void Progression_LevelAveragedSheet_AnswersForBuildCopy()
        {
            var progression = AssetDatabase.LoadAssetAtPath<Progression>(_progressionPath);
            Assert.IsNotNull(progression, $"Missing progression at '{_progressionPath}'");

            var characterSheet = progression.GetLevelAveragedStatSheet(character, _chartedLevel);
            Assert.IsNotNull(characterSheet, $"No level-averaged chart for '{_characterName}' in the project progression");

            var buildCopySheet = progression.GetLevelAveragedStatSheet(characterBuildCopy, _chartedLevel);
            Assert.IsNotNull(buildCopySheet, "Build copy missed the level-averaged chart");
            CollectionAssert.AreEquivalent(characterSheet, buildCopySheet);
        }
        #endregion
    }
}

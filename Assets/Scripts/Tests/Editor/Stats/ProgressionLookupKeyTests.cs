using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;
using Frankie.Stats;

namespace Frankie.Tests.Editor
{
    public class ProgressionLookupKeyTests
    {
        // Tested:
        //  - Progression lookup build - a null character entry warns + is skipped, other characters still resolve
        //  - Progression public lookups - null character properties warn + fall back (false / default stat / empty sheet / null)
        //  - Progression.UpdateProgressionAsset - a null character entry warns instead of updating the lookup
        // Not Tested:
        //  - Level-averaged chart lookup with a null chart entry - charts are rebuilt from the CharacterProperties cache (never null)
        // Note:  The progression is in memory;  uses a project CharacterProperties as a read-only key

        // Const (note that character names are necessarily string ref and brittle, must be updated w/ project layout/re-configuration)
        private const string _progressionName = "ProgressionLookupKeyTests_Progression";
        private const string _characterName = "Frankie";
        private static readonly Regex _skippedLookupWarning = new($@"Skipping their progression lookup on '{_progressionName}'\.$");

        // State
        private readonly TestObjectTracker tracker = new();
        private Progression progression;
        private CharacterProperties character;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            character = PartyTestBuilders.LoadCharacter(_characterName);
            Assert.IsNotNull(character, $"Missing project character '{_characterName}'");

            progression = tracker.Create<Progression>(_progressionName);
            progression.AddToProgressionAsset(character);
        }

        [TearDown]
        public void TearDown()
        {
            if (progression != null) { Undo.ClearUndo(progression); }
            tracker.DestroyAll();
        }
        #endregion

        #region Tests
        [Test]
        public void BuildLookup_NullCharacterEntry_WarnsAndKeepsOtherCharacters()
        {
            LogAssert.Expect(LogType.Warning, _skippedLookupWarning);
            progression.AddToProgressionAsset(null);

            Assert.IsTrue(progression.HasProgression(character));
        }

        [Test]
        public void Lookups_NullCharacterProperties_WarnAndFallBack()
        {
            for (int i = 0; i < 5; i++) { LogAssert.Expect(LogType.Warning, _skippedLookupWarning); }

            Assert.IsFalse(progression.HasProgression(null));
            Assert.AreEqual(1f, progression.GetStat(Stat.Brawn, null), "Expected the default value for a missing stat");
            Assert.IsEmpty(progression.GetStatSheet(null));
            Assert.IsEmpty(progression.GetLevelUpSheet(null, 1, new()));
            Assert.IsNull(progression.GetLevelAveragedStatSheet(null, 1));
        }

        [Test]
        public void UpdateProgressionAsset_NullCharacterEntry_WarnsAndLeavesOtherCharacters()
        {
            LogAssert.Expect(LogType.Warning, _skippedLookupWarning);
            progression.AddToProgressionAsset(null);

            LogAssert.Expect(LogType.Warning, _skippedLookupWarning);
            progression.UpdateProgressionAsset(null, Stat.Brawn, 7f);

            Assert.AreNotEqual(7f, progression.GetStat(Stat.Brawn, character), "Update on the null entry leaked into another character");
        }
        #endregion
    }
}

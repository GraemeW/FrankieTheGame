using System.Linq;
using NUnit.Framework;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Utils.Tests;
using Frankie.Core;
using Frankie.Saving;
using Frankie.Speech;
using Frankie.Stats;

namespace Frankie.Tests.Editor
{
    public class DialogueVariableImplementerTests
    {
        // Tested:
        //  - PartyMemberVariable - leader / mid / last slot, clamping (negative -> leader, past end -> last member), empty party (leader error name), no party in context
        //  - PlayerQuestion.GetDialogueVariable - character name (w/ and w/o character), favourite food / thing (from PlayerPrefs), frame flavour (blank)
        // Not Tested:
        //  - Personalized names (from the name screen) - read from the current save's PlayerPrefs; compared against GetCharacterDisplayName

        // Const Tunables
        private const string _partyIndexField = "partyIndex";
        [NoAutoStaticsCleanup] private static readonly string[] _characterNames = { "Frankie", "Lucy", "Tilly" }; // Existing character assets ~ distinct display names

        // State
        private readonly TestObjectTracker tracker = new();
        private PartyBehaviour party;
        private CharacterProperties[] characters;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            characters = PartyTestBuilders.LoadCharacters(_characterNames);
            CollectionAssert.AllItemsAreNotNull(characters, "Character assets moved or renamed");
            party = tracker.BuildParty(characters);
        }

        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private PartyMemberVariable CreatePartyMemberVariable(int partyIndex)
        {
            var variable = tracker.Create<PartyMemberVariable>($"PartyMember{partyIndex}");
            SerializedFieldInjector.SetValue(variable, _partyIndexField, partyIndex);
            return variable;
        }

        private PlayerQuestion CreatePlayerQuestion(PlayerQuestionType questionType, CharacterProperties characterProperties = null)
        {
            var question = tracker.Create<PlayerQuestion>(questionType.ToString());
            question.questionType = questionType;
            question.optionalCharacterProperties = characterProperties;
            return question;
        }
        #endregion

        #region Tests
        [Test]
        public void Fixture_CharactersHaveDistinctDisplayNames()
        {
            Assert.AreEqual(characters.Length, characters.Select(CharacterProperties.GetCharacterDisplayName).Distinct().Count(), "Slot checks below need distinct names");
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(-1, 0)]
        [TestCase(5, 2)]
        public void PartyMember_ResolvesClampedSlot(int partyIndex, int expectedMember)
        {
            string resolved = CreatePartyMemberVariable(partyIndex).GetDialogueVariable(new DialogueVariableContext(party, null));
            Assert.AreEqual(CharacterProperties.GetCharacterDisplayName(characters[expectedMember]), resolved);
        }

        [Test]
        public void PartyMember_EmptyParty_UsesLeaderErrorName()
        {
            PartyBehaviour emptyParty = tracker.BuildParty();
            Assert.AreEqual(emptyParty.GetPartyLeaderName(), CreatePartyMemberVariable(1).GetDialogueVariable(new DialogueVariableContext(emptyParty, null)));
        }

        [Test]
        public void PartyMember_NoPartyInContext_ShowsBlank()
        {
            Assert.AreEqual("", CreatePartyMemberVariable(0).GetDialogueVariable(default));
        }

        [Test]
        public void PlayerQuestion_CharacterName_ResolvesDisplayName()
        {
            Assert.AreEqual(CharacterProperties.GetCharacterDisplayName(characters[1]), CreatePlayerQuestion(PlayerQuestionType.CharacterName, characters[1]).GetDialogueVariable(default));
            Assert.AreEqual("", CreatePlayerQuestion(PlayerQuestionType.CharacterName).GetDialogueVariable(default), "No character set ~ blank");
        }

        [Test]
        public void PlayerQuestion_Favourites_ResolveFromPlayerPrefs()
        {
            Assert.AreEqual(PlayerPrefsController.GetFavouriteFood(), CreatePlayerQuestion(PlayerQuestionType.FavouriteFood).GetDialogueVariable(default));
            Assert.AreEqual(PlayerPrefsController.GetFavouriteThing(), CreatePlayerQuestion(PlayerQuestionType.FavouriteThing).GetDialogueVariable(default));
        }

        [Test]
        public void PlayerQuestion_FrameFlavour_ShowsBlank()
        {
            Assert.AreEqual("", CreatePlayerQuestion(PlayerQuestionType.FrameFlavour).GetDialogueVariable(default));
        }
        #endregion
    }
}

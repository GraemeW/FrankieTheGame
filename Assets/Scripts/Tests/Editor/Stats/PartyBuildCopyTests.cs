using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Saving;
using LowDefMustard.Utils.Tests;
using Frankie.Stats;

namespace Frankie.Tests.Editor
{
    public class PartyBuildCopyTests
    {
        // Tested:
        //  - PartyBehaviour.GetMember - finds the member from a build copy of its character
        //  - AnyCharacterInPartyPredicate / CharacterIsLeaderPredicate - match members via build copies
        //  - Party unlocked characters - both copies count once, GetAvailableCharactersToAdd skips a member's build copy, removal by build copy
        //  - Party unlocked characters - stored by ID, so GetAvailableCharactersToAdd hands back the cached copy (never the build copy) + skips IDs missing from the cache
        //  - Party / PartyAssist.AddToParty(CharacterProperties) - build copy of a member is refused, nothing spawned
        //  - InactiveParty.RestoreCharacterState - a character benched as one copy is restored when it returns as the other
        // Not Tested:
        //  - Party world NPC lookup - needs CharacterNPCSwapper.Awake, so it's in PartyWorldNPCBuildCopyTests (play mode)
        //  - InactiveParty.CaptureCharacterState - appends to the session save file
        //  - The restored character's saved properties - this checks the bench entry is matched + consumed, the restore itself is SaveableEntity's
        // Note:  Parties are inactive (PartyTestBuilders), so these exercise the lookups without Awake / Start
        // Note:  The restore test reads (never writes) the real session file, and the fresh SaveableEntity has no entry in it

        // Const (note that names are necessarily string ref and brittle, must be updated w/ project layout/re-configuration)
        private const string _leaderName = "Frankie";
        private const string _otherCharacterName = "Lucy";
        private const string _assistName = "PhilRamen";
        private const string _uncachedCharacterName = "PartyBuildCopyTests_UncachedCharacter";

        // State
        private readonly TestObjectTracker tracker = new();
        private CharacterProperties leader;
        private CharacterProperties leaderBuildCopy;
        private CharacterProperties otherCharacter;
        private Party party;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            leader = PartyTestBuilders.LoadCharacter(_leaderName);
            otherCharacter = PartyTestBuilders.LoadCharacter(_otherCharacterName);
            Assert.IsNotNull(leader, $"Missing project character '{_leaderName}'");
            Assert.IsNotNull(otherCharacter, $"Missing project character '{_otherCharacterName}'");

            leaderBuildCopy = tracker.CreateBuildCopy(leader);
            party = (Party)tracker.BuildParty(leader);
            SerializedFieldInjector.Set(party, PartyBehaviour.containerField, party.transform);
        }

        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Tests
        [Test]
        public void GetMember_BuildCopy_FindsMember()
        {
            BaseStats member = party.GetMember(leaderBuildCopy);

            Assert.IsNotNull(member);
            Assert.AreSame(leader, member.GetCharacterProperties());
        }

        [Test]
        public void AnyCharacterInPartyPredicate_BuildCopy_EvaluatesTrue()
        {
            var predicate = tracker.Create<AnyCharacterInPartyPredicate>("PartyBuildCopyTests_AnyCharacterInParty");
            SerializedFieldInjector.SetArray(predicate, AnyCharacterInPartyPredicate.charactersToMatchField, leaderBuildCopy);

            Assert.AreEqual(true, predicate.Evaluate(party));
        }

        [Test]
        public void CharacterIsLeaderPredicate_BuildCopy_EvaluatesTrue()
        {
            var predicate = tracker.Create<CharacterIsLeaderPredicate>("PartyBuildCopyTests_CharacterIsLeader");
            SerializedFieldInjector.SetArray(predicate, CharacterIsLeaderPredicate.charactersToMatchField, leaderBuildCopy);

            Assert.AreEqual(true, predicate.Evaluate(party));
        }

        [Test]
        public void UnlockedCharacters_BothCopies_CountOnce()
        {
            CharacterProperties otherBuildCopy = tracker.CreateBuildCopy(otherCharacter);
            party.AddToUnlockedCharacters(otherCharacter);
            party.AddToUnlockedCharacters(otherBuildCopy);

            Assert.AreEqual(1, party.GetAvailableCharactersToAdd().Count);
        }

        [Test]
        public void GetAvailableCharactersToAdd_MemberUnlockedAsBuildCopy_IsSkipped()
        {
            party.AddToUnlockedCharacters(leaderBuildCopy);
            party.AddToUnlockedCharacters(otherCharacter);

            CollectionAssert.AreEqual(new[] { otherCharacter }, party.GetAvailableCharactersToAdd());
        }

        [Test]
        public void RemoveFromUnlockedCharacters_BuildCopy_RemovesOriginalEntry()
        {
            CharacterProperties otherBuildCopy = tracker.CreateBuildCopy(otherCharacter);
            party.AddToUnlockedCharacters(otherCharacter);

            party.RemoveFromUnlockedCharacters(otherBuildCopy);

            CollectionAssert.IsEmpty(party.GetAvailableCharactersToAdd());
        }

        [Test]
        public void GetAvailableCharactersToAdd_UnlockedAsBuildCopy_ReturnsCachedCopy()
        {
            CharacterProperties otherBuildCopy = tracker.CreateBuildCopy(otherCharacter);
            party.AddToUnlockedCharacters(otherBuildCopy);

            IList<CharacterProperties> availableCharacters = party.GetAvailableCharactersToAdd();

            Assert.AreEqual(1, availableCharacters.Count);
            Assert.AreSame(CharacterProperties.GetCharacterPropertiesFromName(_otherCharacterName), availableCharacters[0]);
            Assert.AreNotSame(otherBuildCopy, availableCharacters[0]);
        }

        [Test]
        public void GetAvailableCharactersToAdd_IDMissingFromCache_SkippedWithWarning()
        {
            party.AddToUnlockedCharacters(tracker.Create<CharacterProperties>(_uncachedCharacterName));

            LogAssert.Expect(LogType.Warning, new Regex($"Character ID '{_uncachedCharacterName}' isn't in the CharacterProperties cache"));
            CollectionAssert.IsEmpty(party.GetAvailableCharactersToAdd());
        }

        [Test]
        public void AddToParty_BuildCopyOfMember_RefusedWithoutSpawning()
        {
            int childCount = party.transform.childCount;

            Assert.IsFalse(party.AddToParty(leaderBuildCopy));
            Assert.AreEqual(1, party.GetPartySize());
            Assert.AreEqual(childCount, party.transform.childCount, "Refused add left a spawned character behind");
        }

        [Test]
        public void PartyAssistAddToParty_BuildCopyOfMember_RefusedWithoutSpawning()
        {
            CharacterProperties assist = PartyTestBuilders.LoadCharacter(_assistName);
            Assert.IsNotNull(assist, $"Missing project character '{_assistName}'");
            PartyAssist partyAssist = party.AddPartyAssist(assist);
            SerializedFieldInjector.Set(partyAssist, PartyBehaviour.containerField, party.transform);
            int childCount = party.transform.childCount;

            Assert.IsFalse(partyAssist.AddToParty(tracker.CreateBuildCopy(assist)));
            Assert.AreEqual(1, partyAssist.GetPartySize());
            Assert.AreEqual(childCount, party.transform.childCount, "Refused add left a spawned character behind");
        }

        [Test]
        public void InactivePartyRestoreCharacterState_BenchedAsOtherCopy_ConsumesBenchEntry()
        {
            var inactiveParty = party.GetComponent<InactiveParty>();
            inactiveParty.RestoreState(inactiveParty.ManualGetStateFromData(new HashSet<CharacterProperties> { otherCharacter }));
            BaseStats returningCharacter = tracker.BuildCharacter(tracker.CreateBuildCopy(otherCharacter));
            returningCharacter.gameObject.AddComponent<SaveableEntity>();

            inactiveParty.RestoreCharacterState(returningCharacter);

            Assert.IsTrue(inactiveParty.CaptureState().TryGetState(out List<string> benchedNames));
            CollectionAssert.IsEmpty(benchedNames, "Bench entry wasn't matched to the returning build copy");
        }
        #endregion
    }
}

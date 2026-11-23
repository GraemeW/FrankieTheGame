#if UNITY_EDITOR
using NUnit.Framework;
using LowDefMustard.Utils.Tests;
using Frankie.Stats;

namespace Frankie.Tests
{
    public class PartyWorldNPCBuildCopyTests
    {
        // Tested:
        //  - Party.UpdateWorldLookup - an NPC left in the world as one copy is cleared when it rejoins as the other
        //  - Party.UpdateWorldLookup - leaving as both copies keeps a single world entry
        // Not Tested:
        //  - RestoreWorldNPCs respawning NPCs - instantiates the project NPC prefabs into the active scene
        // Note:  Play mode for CharacterNPCSwapper.Awake (caches its BaseStats);  synchronous, so objects are torn down before Start
        // Note:  The lookup is read back through Party.CaptureState (world NPC names in the save data)

        // Const (note that names are necessarily string ref and brittle, must be updated w/ project layout/re-configuration)
        private const string _leaderName = "Frankie";
        private const string _worldNPCName = "Lucy";

        // State
        private readonly TestObjectTracker tracker = new();
        private Party party;
        private CharacterProperties worldNPCCharacter;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            CharacterProperties leader = PartyTestBuilders.LoadCharacter(_leaderName);
            worldNPCCharacter = PartyTestBuilders.LoadCharacter(_worldNPCName);
            Assert.IsNotNull(leader, $"Missing project character '{_leaderName}'");
            Assert.IsNotNull(worldNPCCharacter, $"Missing project character '{_worldNPCName}'");

            party = (Party)tracker.BuildParty(leader);
        }

        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private CharacterNPCSwapper BuildWorldNPC(CharacterProperties characterProperties)
        {
            BaseStats baseStats = tracker.BuildCharacter(characterProperties);
            var characterNPCSwapper = baseStats.gameObject.AddComponent<CharacterNPCSwapper>();
            baseStats.gameObject.SetActive(true);
            return characterNPCSwapper;
        }

        private int GetWorldNPCCount()
        {
            Assert.IsTrue(party.CaptureState().TryGetState(out PartySerializableSaveData partySaveData));
            return partySaveData.worldNPCNameLookup.Count;
        }
        #endregion

        #region Tests
        [Test]
        public void UpdateWorldLookup_RejoinsAsBuildCopy_ClearsWorldEntry()
        {
            party.UpdateWorldLookup(true, BuildWorldNPC(worldNPCCharacter));
            Assert.AreEqual(1, GetWorldNPCCount());

            party.UpdateWorldLookup(false, BuildWorldNPC(tracker.CreateBuildCopy(worldNPCCharacter)));

            Assert.AreEqual(0, GetWorldNPCCount(), "World entry kept ~ the NPC would respawn while in the party");
        }

        [Test]
        public void UpdateWorldLookup_LeavesAsBothCopies_SingleWorldEntry()
        {
            party.UpdateWorldLookup(true, BuildWorldNPC(worldNPCCharacter));
            party.UpdateWorldLookup(true, BuildWorldNPC(tracker.CreateBuildCopy(worldNPCCharacter)));

            Assert.AreEqual(1, GetWorldNPCCount());
        }
        #endregion
    }
}
#endif

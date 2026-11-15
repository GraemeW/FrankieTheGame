#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LowDefMustard.Utils.Tests;
using Frankie.Stats;
using Object = UnityEngine.Object;

namespace Frankie.Tests
{
    internal static class PartyTestBuilders
    {
        // Note:  Objects are owned by the tracker, so destroy via its DestroyAll in TearDown
        // Editor-only:  character assets load through AssetDatabase, serialized fields set through SerializedFieldInjector

        // Const Tunables
        private const string _partyMembersField = "members";
        private const string _characterPropertiesField = "characterProperties";

        #region PublicMethods
        public static CharacterProperties LoadCharacter(string characterName)
        {
            string path = AssetDatabase.FindAssets($"{characterName} t:{nameof(CharacterProperties)}") // N.B. FindAssets' name filter is a substring search 
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(candidate => Path.GetFileNameWithoutExtension(candidate) == characterName); // So still need to match on exact string
            return path != null ? AssetDatabase.LoadAssetAtPath<CharacterProperties>(path) : null;
        }

        public static CharacterProperties[] LoadCharacters(params string[] characterNames) => characterNames.Select(LoadCharacter).ToArray();

        public static PartyBehaviour BuildParty(this TestObjectTracker tracker, params CharacterProperties[] memberProperties)
        {
            // Inactive ~ party components stay inert (no Awake / Start), members are children so they go with the party
            GameObject partyObject = tracker.Track(new GameObject("Party"));
            partyObject.SetActive(false);
            var party = partyObject.AddComponent<Party>(); // Pulls in its required components (InactiveParty, PlayerMover, PlayerStateMachine)

            SerializedFieldInjector.SetArray(party, _partyMembersField, memberProperties.Select(member => (Object)BuildMember(partyObject.transform, member)).ToArray());
            return party;
        }
        #endregion

        #region PrivateMethods
        private static BaseStats BuildMember(Transform party, CharacterProperties characterProperties)
        {
            var memberObject = new GameObject(characterProperties.name);
            memberObject.transform.SetParent(party);
            var baseStats = memberObject.AddComponent<BaseStats>();
            SerializedFieldInjector.Set(baseStats, _characterPropertiesField, characterProperties);
            return baseStats;
        }
        #endregion
    }
}
#endif

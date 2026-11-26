using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using LowDefMustard.Saving;
using Frankie.Core;

namespace Frankie.Stats
{
    public static class CharacterPrefabScenePreload
    {
        // Notes:
        //  - Party / party assist / inactive party / this scene's world NPCs come from the save the restore is about to use (+ the live party, should they differ)
        //  - Scene users (collected via ICharacterPrefabUser) include inactive ones ~ e.g. spawners in rooms that are toggled on later

        #region PublicMethods
        public static IEnumerator PreloadForLoadedScene(string saveFile)
        {
            IReadOnlyCollection<CharacterProperties> characters = GatherForLoadedScene(saveFile);
            CharacterPrefabCache.ReleaseAllExcept(characters); // The previous scene's instances are gone by now
            yield return CharacterPrefabCache.Preload(characters);
        }
        #endregion

        #region PrivateInternalMethods
        internal static IReadOnlyCollection<CharacterProperties> GatherForLoadedScene(string saveFile) // Unique by character ID
        {
            var characters = new Dictionary<string, CharacterProperties>();
            GameObject playerObject = Player.FindPlayerObject();
            if (playerObject != null)
            {
                JObject savedState = string.IsNullOrWhiteSpace(saveFile) ? null : SavingSystem.ManualGetFullState(saveFile); // Load once for every party read
                AddPartyCharacters(playerObject, savedState, Add);
            }

            foreach (ICharacterPrefabUser characterPrefabUser in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).OfType<ICharacterPrefabUser>())
            {
                Add(characterPrefabUser.GetCharactersToPreload());
            }
            return characters.Values;

            
            // Local Functions
            void Add(IEnumerable<CharacterProperties> toAdd)
            {
                foreach (CharacterProperties characterProperties in toAdd ?? Enumerable.Empty<CharacterProperties>())
                {
                    if (characterProperties != null) { characters.TryAdd(characterProperties.GetCharacterID(), characterProperties); }
                }
            }
        }
        
        private static void AddPartyCharacters(GameObject playerObject, JObject savedState, System.Action<IEnumerable<CharacterProperties>> add)
        {
            if (playerObject.TryGetComponent(out Party party))
            {
                add(party.GetMembers().Where(member => member != null).Select(member => member.GetCharacterProperties()));
                add(party.GetAvailableCharactersToAdd());
                if (SavingSystem.TryManualGetData(savedState, party, out PartySaveData partySaveData))
                {
                    string activeSceneName = SceneManager.GetActiveScene().name;
                    add(partySaveData.partyCharacters);
                    add(partySaveData.unlockedCharacters);
                    add(partySaveData.worldNPCLookup.Where(entry => entry.Value.sceneName == activeSceneName).Select(entry => entry.Key));
                }
            }

            if (playerObject.TryGetComponent(out PartyAssist partyAssist))
            {
                add(partyAssist.GetMembers().Where(member => member != null).Select(member => member.GetCharacterProperties()));
                if (SavingSystem.TryManualGetData(savedState, partyAssist, out HashSet<CharacterProperties> partyAssistCharacters)) { add(partyAssistCharacters); }
            }

            if (playerObject.TryGetComponent(out InactiveParty inactiveParty) && SavingSystem.TryManualGetData(savedState, inactiveParty, out HashSet<CharacterProperties> inactiveCharacters))
            {
                add(inactiveCharacters);
            }
        }
        #endregion
    }
}

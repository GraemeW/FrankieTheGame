using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Utils.Tests;
using Frankie.Combat.Spawner;
using Frankie.Stats;

namespace Frankie.Tests.Editor
{
    public class CharacterPrefabScenePreloadTests
    {
        // Tested:
        //  - GatherForLoadedScene - includes ICharacterPrefabUsers on inactive objects (e.g. a spawner in a room toggled on later), once per character ID
        //  - SpawnConfiguration.GetPossibleEnemies - every configured enemy (what a spawner preloads), skipping empty / zero-maximum entries
        // Not Tested:
        //  - The party / save-file side - needs a Player with a SaveableEntity + a session save (play mode)
        //  - PreloadForLoadedScene's release - CharacterPrefabCacheTests covers ReleaseAllExcept

        // State
        private readonly TestObjectTracker tracker = new();

        #region DataStructures
        private sealed class TestCharacterPrefabUser : MonoBehaviour, ICharacterPrefabUser
        {
            public List<CharacterProperties> characters = new();
            public IEnumerable<CharacterProperties> GetCharactersToPreload() => characters;
        }
        #endregion

        #region Setup
        [TearDown]
        public void TearDown() => tracker.DestroyAll();
        #endregion

        #region Tests
        [Test]
        public void GatherForLoadedScene_InactiveUser_IsIncludedOncePerCharacter()
        {
            CharacterProperties characterA = tracker.Create<CharacterProperties>("PreloadTests_CharacterA");
            CharacterProperties characterACopy = tracker.Create<CharacterProperties>("PreloadTests_CharacterA"); // Same ID, e.g. a build copy
            CharacterProperties characterB = tracker.Create<CharacterProperties>("PreloadTests_CharacterB");
            GameObject inactiveRoom = tracker.CreateGameObject("PreloadTests_InactiveRoom");
            inactiveRoom.AddComponent<TestCharacterPrefabUser>().characters.AddRange(new[] { characterA, characterB });
            tracker.CreateComponent<TestCharacterPrefabUser>("PreloadTests_ActiveUser").characters.Add(characterACopy);
            inactiveRoom.SetActive(false);

            List<string> gatheredIDs = CharacterPrefabScenePreload.GatherForLoadedScene(null).Select(characterProperties => characterProperties.GetCharacterID()).ToList();

            CollectionAssert.Contains(gatheredIDs, characterB.GetCharacterID());
            Assert.AreEqual(1, gatheredIDs.Count(id => id == characterA.GetCharacterID()));
        }

        [Test]
        public void GetPossibleEnemies_SkipsEmptyAndZeroMaximumEntries()
        {
            CharacterProperties spawnable = tracker.Create<CharacterProperties>("PreloadTests_Spawnable");
            CharacterProperties neverSpawned = tracker.Create<CharacterProperties>("PreloadTests_NeverSpawned");
            var enemyConfigurations = new[]
            {
                new EnemyConfiguration { characterProperties = spawnable, minimum = 0, maximum = 2 },
                new EnemyConfiguration { characterProperties = neverSpawned, minimum = 0, maximum = 0 },
                new EnemyConfiguration { characterProperties = null, maximum = 3 }
            };

            CollectionAssert.AreEqual(new[] { spawnable }, SpawnConfiguration.GetPossibleEnemies(enemyConfigurations).ToList());
        }
        #endregion
    }
}

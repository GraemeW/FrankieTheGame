using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Utils.Tests;
using Frankie.Stats;

namespace Frankie.Tests.Editor
{
    public class CharacterPrefabCacheTests
    {
        // Tested:
        //  - The CharacterProperties mapping:  both CharacterPrefabType slots load the character's prefabs;  a slot the character lacks is empty (no warning)
        //  - Keyed by character ID ~ a build copy (same ID, different object) hits the original's entry
        // Not Tested:
        //  - The cache itself (miss, release, scene-load guard) - AddressablesKeyedAssetCacheTests (utils)
        // Note:  Loads project character prefabs (Addressables)

        // Const (note that names are necessarily string ref and brittle, must be updated w/ project layout/re-configuration)
        private const string _bothPrefabsCharacterName = "Lucy";
        private const string _characterPrefabOnlyName = "Frankie";

        // State
        private readonly TestObjectTracker tracker = new();

        #region Setup
        [TearDown]
        public void TearDown()
        {
            CharacterPrefabCache.ReleaseAll();
            tracker.DestroyAll();
        }
        #endregion

        #region Helpers
        private static CharacterProperties LoadCharacter(string characterName)
        {
            CharacterProperties characterProperties = AssetDatabase.FindAssets($"t:{nameof(CharacterProperties)} {characterName}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<CharacterProperties>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(candidate => candidate != null && candidate.GetCharacterID() == characterName);
            Assert.IsNotNull(characterProperties, $"No character '{characterName}' in the project");
            return characterProperties;
        }

        private static void PreloadAndWait(params CharacterProperties[] characters)
        {
            foreach (var handle in CharacterPrefabCache.StartPreload(characters)) { handle.WaitForCompletion(); }
        }
        #endregion

        #region Tests
        [Test]
        public void TryGetPrefab_AfterPreload_HitsBothTypesWithoutWarning()
        {
            CharacterProperties lucy = LoadCharacter(_bothPrefabsCharacterName);
            PreloadAndWait(lucy);

            Assert.IsTrue(CharacterPrefabCache.IsReady(new[] { lucy }));
            Assert.IsTrue(CharacterPrefabCache.TryGetPrefab(lucy, CharacterPrefabType.Character, out GameObject characterPrefab));
            Assert.IsTrue(CharacterPrefabCache.TryGetPrefab(lucy, CharacterPrefabType.NPC, out GameObject npcPrefab));
            Assert.IsNotNull(characterPrefab);
            Assert.IsNotNull(npcPrefab);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TryGetPrefab_TypeTheCharacterLacks_ReturnsFalseWithoutWarning()
        {
            CharacterProperties frankie = LoadCharacter(_characterPrefabOnlyName);
            Assert.IsFalse(frankie.GetCharacterPrefabReference(CharacterPrefabType.NPC).RuntimeKeyIsValid(), $"Expected '{_characterPrefabOnlyName}' to have no NPC prefab");
            PreloadAndWait(frankie);

            Assert.IsFalse(CharacterPrefabCache.TryGetPrefab(frankie, CharacterPrefabType.NPC, out GameObject npcPrefab));
            Assert.IsNull(npcPrefab);
            Assert.IsTrue(CharacterPrefabCache.IsReady(new[] { frankie }));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TryGetPrefab_BuildCopy_HitsTheOriginalsEntry()
        {
            CharacterProperties lucy = LoadCharacter(_bothPrefabsCharacterName);
            CharacterProperties buildCopy = tracker.Track(Object.Instantiate(lucy));
            buildCopy.name = lucy.name; // Same ID, different object (e.g. a bundle-loaded copy)
            PreloadAndWait(lucy);

            Assert.IsTrue(CharacterPrefabCache.IsReady(new[] { buildCopy }));
            Assert.IsTrue(CharacterPrefabCache.TryGetPrefab(buildCopy, CharacterPrefabType.NPC, out GameObject npcPrefab));
            Assert.IsNotNull(npcPrefab);
            LogAssert.NoUnexpectedReceived();
        }
        #endregion
    }
}

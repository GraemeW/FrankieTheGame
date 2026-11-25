#if UNITY_EDITOR
using UnityEngine;
using LowDefMustard.Utils.Tests;
using Frankie.Combat;
using Frankie.Stats;
using Object = UnityEngine.Object;

namespace Frankie.Tests
{
    internal static class BuildCopyTestBuilders
    {
        // Player builds can hold two copies of one asset:  the built-in copy (scenes / non-addressable refs) + the Addressables copy
        // The editor only ever has one, so these stand in for the second copy:  same serialized data + name, separate object

        #region PublicMethods
        public static T CreateBuildCopy<T>(this TestObjectTracker tracker, T asset) where T : Object
        {
            T buildCopy = tracker.Track(Object.Instantiate(asset));
            buildCopy.name = asset.name; // Instantiate appends "(Clone)"
            return buildCopy;
        }

        public static BaseStats BuildCharacter(this TestObjectTracker tracker, CharacterProperties characterProperties, Transform parent = null)
        {
            // Inactive until configured ~ BaseStats.Awake reads the injected character properties
            GameObject characterObject = tracker.CreateGameObject(characterProperties.name);
            characterObject.SetActive(false);
            characterObject.transform.SetParent(parent);
            var baseStats = characterObject.AddComponent<BaseStats>();
            SerializedFieldInjector.Set(baseStats, BaseStats.characterPropertiesField, characterProperties);
            return baseStats;
        }

        public static CombatParticipant BuildCombatParticipant(this TestObjectTracker tracker, CharacterProperties characterProperties)
        {
            // Play mode only:  destroying an edit-mode CombatParticipant runs the localization deletion handler
            BaseStats baseStats = tracker.BuildCharacter(characterProperties);
            var combatParticipant = baseStats.gameObject.AddComponent<CombatParticipant>();
            baseStats.gameObject.SetActive(true);
            return combatParticipant;
        }
        #endregion
    }
}
#endif

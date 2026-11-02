#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;

namespace LowDefMustard.Rendering.Editor
{
    internal static class BattleEffectRecipeSelection
    {
        // Notes:
        //  - Held statically so it survives the inspector being rebuilt && so the preview's timeline can drive it

        // State
        [NoAutoStaticsCleanup] private static readonly Dictionary<BattleEffectRecipe, List<BattleEffectEntryId>> _selectedEntries = new();

        // Events
        [NoAutoStaticsCleanup] public static event Action<BattleEffectRecipe> changed;

        #region PublicMethods
        public static IReadOnlyList<BattleEffectEntryId> Get(BattleEffectRecipe recipe)
        {
            return recipe != null && _selectedEntries.TryGetValue(recipe, out List<BattleEffectEntryId> entryIds) ? entryIds : Array.Empty<BattleEffectEntryId>();
        }

        public static void Set(BattleEffectRecipe recipe, IEnumerable<BattleEffectEntryId> entryIds)
        {
            if (recipe == null) { return; }
            List<BattleEffectEntryId> sortedIds = entryIds.Distinct().OrderBy(id => id.isInstance).ThenBy(id => id.index).ToList();
            if (Get(recipe).SequenceEqual(sortedIds)) { return; }
            _selectedEntries[recipe] = sortedIds;
            changed?.Invoke(recipe);
        }

        public static void Remove(BattleEffectRecipe recipe, BattleEffectEntryId removedId)
        {
            // Later entries of the same kind shift down a slot
            Set(recipe, Get(recipe).Where(id => !id.Equals(removedId))
                .Select(id => id.isInstance == removedId.isInstance && id.index > removedId.index ? new BattleEffectEntryId(id.index - 1, id.isInstance) : id));
        }
        #endregion
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace LowDefMustard.Rendering.Editor
{
    internal static class BattleEffectPreviewVisibility
    {
        // Notes:
        //  - Preview / export only:  hidden entries are ONLY skipped in the preview's own bake -- no impact to actual recipe asset or in-game behaviour
        //  - Held statically so it survives the inspector being rebuilt && for inter-op between recipe inspector <-> preview window (clears on domain reload)
        
        // State
        [NoAutoStaticsCleanup] private static readonly Dictionary<BattleEffectRecipe, HashSet<BattleEffectEntryId>> _hiddenEntries = new();

        // Events
        [NoAutoStaticsCleanup] public static event Action<BattleEffectRecipe> changed;

        #region Menus
        [MenuItem("Tools/Battle Effects/Clear Hidden Preview Entries", priority = 31)]
        internal static void ShowAllRecipes()
        {
            List<BattleEffectRecipe> recipes = _hiddenEntries.Keys.ToList();
            _hiddenEntries.Clear();
            foreach (BattleEffectRecipe recipe in recipes) { changed?.Invoke(recipe); }
        }
        #endregion

        #region PublicMethods
        public static bool HasAnyHidden() => _hiddenEntries.Values.Any(hiddenIds => hiddenIds.Count > 0);
        public static int GetHiddenCount(BattleEffectRecipe recipe) => recipe != null && _hiddenEntries.TryGetValue(recipe, out HashSet<BattleEffectEntryId> hiddenIds) ? hiddenIds.Count : 0;

        public static bool IsHidden(BattleEffectRecipe recipe, BattleEffectEntryId entryId)
        {
            return recipe != null && _hiddenEntries.TryGetValue(recipe, out HashSet<BattleEffectEntryId> hiddenIds) && hiddenIds.Contains(entryId);
        }

        public static bool IsHidden(BattleEffectRecipe recipe, int index, bool isInstance) => IsHidden(recipe, new BattleEffectEntryId(index, isInstance));

        public static void SetHidden(BattleEffectRecipe recipe, BattleEffectEntryId entryId, bool isHidden)
        {
            if (recipe == null || IsHidden(recipe, entryId) == isHidden) { return; }
            if (!_hiddenEntries.TryGetValue(recipe, out HashSet<BattleEffectEntryId> hiddenIds))
            {
                hiddenIds = new HashSet<BattleEffectEntryId>();
                _hiddenEntries[recipe] = hiddenIds;
            }
            if (isHidden) { hiddenIds.Add(entryId); }
            else { hiddenIds.Remove(entryId); }
            changed?.Invoke(recipe);
        }

        public static void Toggle(BattleEffectRecipe recipe, BattleEffectEntryId entryId) => SetHidden(recipe, entryId, !IsHidden(recipe, entryId));

        public static void ShowAll(BattleEffectRecipe recipe)
        {
            if (GetHiddenCount(recipe) == 0) { return; }
            _hiddenEntries.Remove(recipe);
            changed?.Invoke(recipe);
        }

        public static void Remove(BattleEffectRecipe recipe, BattleEffectEntryId removedId)
        {
            // Later entries of the same kind shift down a slot
            if (recipe == null || !_hiddenEntries.TryGetValue(recipe, out HashSet<BattleEffectEntryId> hiddenIds)) { return; }
            List<BattleEffectEntryId> shiftedIds = hiddenIds.Where(id => !id.Equals(removedId))
                .Select(id => id.isInstance == removedId.isInstance && id.index > removedId.index ? new BattleEffectEntryId(id.index - 1, id.isInstance) : id).ToList();
            if (shiftedIds.Count == hiddenIds.Count && shiftedIds.All(hiddenIds.Contains)) { return; }
            _hiddenEntries[recipe] = new HashSet<BattleEffectEntryId>(shiftedIds);
            changed?.Invoke(recipe);
        }
        #endregion
    }
}
#endif

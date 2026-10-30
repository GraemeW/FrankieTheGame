#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

namespace LowDefMustard.Rendering.Editor
{
    internal static class BattleEffectRecipeEntries
    {
        // Const Tunables
        private const string _noRecipeLabel = "(no recipe)";

        public static List<BattleEffectRecipeEntry> Collect(BattleEffectRecipe recipe, bool includeInstances = true)
        {
            var entries = new List<BattleEffectRecipeEntry>();
            if (recipe == null) { return entries; }

            IReadOnlyList<BattleEffectItem> items = recipe.GetItems();
            for (int i = 0; i < items.Count; i++)
            {
                BattleEffectItem item = items[i];
                if (item == null) { continue; }
                string label = string.IsNullOrEmpty(item.GetLabel()) ? $"Item {i}" : item.GetLabel();
                entries.Add(new BattleEffectRecipeEntry(i, false, label, BattleEffectShapeTypes.Format(item.GetShape()?.GetType()), null, item.GetStartTime(), item.GetEndTime()));
            }

            if (includeInstances)
            {
                IReadOnlyList<BattleEffectInstance> instances = recipe.GetInstances();
                for (int i = 0; i < instances.Count; i++)
                {
                    BattleEffectInstance instance = instances[i];
                    if (instance == null) { continue; }
                    BattleEffectRecipe instanced = instance.GetRecipe();
                    string label = !string.IsNullOrEmpty(instance.GetLabel()) ? instance.GetLabel() : instanced != null ? instanced.name : _noRecipeLabel;
                    float offset = instance.GetStartOffset();
                    float startTime = instanced != null ? offset + instanced.GetTotalStartTime() : offset;
                    float endTime = instanced != null ? offset + instanced.GetTotalEndTime() : offset;
                    entries.Add(new BattleEffectRecipeEntry(i, true, label, null, instanced != null ? instanced.name : null, startTime, endTime));
                }
            }
            return entries.OrderBy(entry => entry.startTime).ThenBy(entry => entry.endTime).ToList();
        }

        public static List<BattleEffectEntryId> GetActiveEntries(BattleEffectRecipe recipe, float time)
        {
            // Instances w/o a recipe draw nothing, so are never active
            return Collect(recipe).Where(entry => (!entry.isInstance || entry.recipeName != null) && entry.IsActiveAt(time)).Select(entry => entry.id).ToList();
        }
    }
}
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Frankie.Utils.Editor
{
    public class OverrideConfiguration
    {
        private readonly bool isOverrideAvailable;
        private readonly AnimatorOverrideController overrideController;
        private readonly OverrideDirectionLookup overrideDirectionLookup;
        private readonly string action;
        private readonly bool isIdle;
        private readonly bool isStandStill;
        
        public OverrideConfiguration(AnimatorOverrideController overrideController, OverrideDirectionLookup overrideDirectionLookup, string action, bool isIdle, bool isStandStill)
        {
            isOverrideAvailable = overrideController != null && overrideDirectionLookup != null;
            this.overrideController = overrideController;
            this.overrideDirectionLookup = overrideDirectionLookup;
            
            this.action = action;
            this.isIdle = isIdle;
            this.isStandStill = isStandStill;
        }
        
        public void ApplyOverride(AnimationClip newClip, AnimationBuildLog log)
        {
            if (!isOverrideAvailable) { return; }
            
            // Always fetch overrides fresh, since multiple OverrideConfigurations may exist
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new();
            overrideController.GetOverrides(overrides);

            if (overrideDirectionLookup.TryGetExactMatch(action, isIdle, out int slotIndex))
            {
                ExecuteApply(overrides, slotIndex, newClip);
                return;
            }
            
            var matchIndices = new List<int>();
            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip original = overrides[i].Key;
                if (original != null && IsOverrideMatch(original.name, action, isIdle, isStandStill)) { matchIndices.Add(i); }
            }

            switch (matchIndices.Count)
            {
                case 0:
                    return;
                case > 1:
                {
                    string conflicting = string.Join(", ", matchIndices.Select(i => overrides[i].Key.name));
                    log?.AppendLine($"Override ambiguous for {newClip.name}: multiple slots matched ({conflicting}) — skipped.");
                    return;
                }
            }
            
            int matchedIndex = matchIndices[0];
            ExecuteApply(overrides, matchedIndex, newClip);
        }
        
        private void ExecuteApply(List<KeyValuePair<AnimationClip, AnimationClip>> overrides, int slotIndex, AnimationClip newClip)
        {
            overrides[slotIndex] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[slotIndex].Key, newClip);
            overrideController.ApplyOverrides(overrides);
            EditorUtility.SetDirty(overrideController);
        }
        
        private static bool IsOverrideMatch(string overrideSlotName, string action, bool isIdle, bool isStandStill)
        {
            bool slotHasStandStill = overrideSlotName.IndexOf(SpriteAnimationGeneratorWindow.standStillOverrideToken, StringComparison.OrdinalIgnoreCase) >= 0;
            if (isStandStill) { return slotHasStandStill; }
            if (slotHasStandStill) { return false; } // StandStill slots never take movement/idle clips

            bool slotHasIdle = SpriteAnimationGeneratorWindow.idleTokens.Any(t => overrideSlotName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);
            if (isIdle != slotHasIdle) { return false; }

            return !string.IsNullOrEmpty(action) && overrideSlotName.EndsWith(action, StringComparison.OrdinalIgnoreCase);
        }
    }
}

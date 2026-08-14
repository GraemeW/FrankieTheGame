using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LowDefMustard.Utils.Editor
{
    public class OverrideDirectionLookup
    {
        private readonly Dictionary<(string action, bool isIdle), int> exactMatchSlotIndex = new();
        private readonly List<string> ambiguousKeys = new();

        public OverrideDirectionLookup(AnimatorOverrideController controller)
        {
            Build(controller);
        }
        
        public bool TryGetExactMatch(string action, bool isIdle, out int slotIndex) => exactMatchSlotIndex.TryGetValue((action, isIdle), out slotIndex);
        public List<string> GetAmbiguousKeys() => ambiguousKeys;

        private void Build(AnimatorOverrideController controller)
        {
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new();
            controller.GetOverrides(overrides);
            
            var candidates = new List<CandidateEntry>();
            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip original = overrides[i].Key;
                if (original == null) { continue; }

                string name = original.name;
                if (name.IndexOf(SpriteAnimationGenerator.standStillOverrideToken, StringComparison.OrdinalIgnoreCase) >= 0) { continue; }

                bool isIdle = SpriteAnimationGenerator.idleTokens.Any(t => name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);
                candidates.AddRange(from direction in SpriteAnimationGenerator.canonicalDirections where name.EndsWith(direction, StringComparison.OrdinalIgnoreCase) select new CandidateEntry(i, direction, isIdle));
            }

            var claimedSlots = new HashSet<int>();
            foreach (var group in candidates.GroupBy(c => (c.direction, c.isIdle)).OrderByDescending(g => g.Key.direction.Length))
            {
                var unclaimed = group.Select(c => c.slotIndex).Distinct().Where(i => !claimedSlots.Contains(i)).ToList();
                switch (unclaimed.Count)
                {
                    case 0:
                        continue;
                    case > 1:
                        ambiguousKeys.Add($"{(group.Key.isIdle ? SpriteAnimationGenerator.idleOverrideToken : "")}{group.Key.direction}");
                        continue;
                }
                
                int slotIndex = unclaimed[0];
                exactMatchSlotIndex[group.Key] = slotIndex;
                claimedSlots.Add(slotIndex);
            }
        }
        
        private struct CandidateEntry
        {
            public readonly int slotIndex;
            public readonly string direction;
            public readonly bool isIdle;

            public CandidateEntry(int slotIndex, string direction, bool isIdle)
            {
                this.slotIndex = slotIndex;
                this.direction = direction;
                this.isIdle = isIdle;
            }
        }
    }
}

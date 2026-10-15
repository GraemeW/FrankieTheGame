using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Rendering;

namespace Frankie.Combat
{
    [Serializable]
    public sealed class BattleEffectRecipe
    {
        // Note:  For materials on the shared BattleEffect shader graph;  leave empty for any other effect material (drawn as-is)

        [SerializeField][Tooltip("Anti-aliasing, in screen pixels (same crispness at any shape size)")][Min(0f)] private float edgeSoftness = 1f;
        [SerializeField][Tooltip("Extra soft edge, in UV")][Min(0f)] private float glowWidth = 0f;
        [SerializeField][Tooltip("Drawn in order (later shapes on top) - max 8")] private List<BattleEffectShape> shapes = new();

        public bool hasShapes => shapes is { Count: > 0 };

        public void ApplyTo(ref MaterialDefinition materialDefinition)
        {
            if (!hasShapes) { return; }

            int itemCount = Mathf.Min(shapes.Count, ShaderPropertyRefs.battleEffectItemCapacity);
            if (shapes.Count > itemCount) { Debug.LogWarning($"Battle effect recipe has {shapes.Count} shapes;  only the first {itemCount} are drawn"); }

            for (int i = 0; i < itemCount; i++) { shapes[i].ApplyTo(ref materialDefinition, i); }
            ShaderPropertyRefs.SetBattleEffectItemCount(ref materialDefinition, itemCount);
            ShaderPropertyRefs.SetBattleEffectEdge(ref materialDefinition, edgeSoftness, glowWidth);
        }
    }
}

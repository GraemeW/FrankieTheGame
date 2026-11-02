using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Rendering;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public sealed class BattleEffectHandle : EntryHandle
    {
        // Note:
        //  - Removes itself once its lifetime is up (non-positive lifetime = never) or its anchor is gone, via `expired`
        //  - BattleStage removes whatever is left when the battle is over

        // State
        private readonly BattleEffectPlacement placement;
        private readonly VisualElement targetAnchor; // enemy slide or row, unused for a full-screen effect
        private readonly Material material;
        private readonly BattleEffectRecipe recipe;
        private readonly BattleEffectTint tint;
        private readonly float spawnTime;
        private readonly float lifetime;

        // Constructor
        private BattleEffectHandle(UIToolkitBoxView view, BattleEffectPlacement placement, VisualElement targetAnchor, Material material, BattleEffectRecipe recipe, BattleEffectTint tint, float lifetime) : base(view, placement == BattleEffectPlacement.FullScreen ? typeof(BattleScreenEffectLayer) : typeof(BattleEffectLayer))
        {
            this.placement = placement;
            this.targetAnchor = targetAnchor;
            this.material = material;
            this.recipe = recipe;
            this.tint = tint;
            this.lifetime = lifetime;
            spawnTime = Time.time;
        }

        public static BattleEffectHandle OverSlide(UIToolkitBoxView view, VisualElement slide, Material material, BattleEffectRecipe recipe, BattleEffectTint tint, float lifetime) => new(view, BattleEffectPlacement.Slide, slide, material, recipe, tint, lifetime);
        public static BattleEffectHandle OverRow(UIToolkitBoxView view, VisualElement row, Material material, BattleEffectRecipe recipe, BattleEffectTint tint, float lifetime) => new(view, BattleEffectPlacement.Row, row, material, recipe, tint, lifetime);
        public static BattleEffectHandle FullScreen(UIToolkitBoxView view, Material material, BattleEffectRecipe recipe, BattleEffectTint tint, float lifetime) => new(view, BattleEffectPlacement.FullScreen, null, material, recipe, tint, lifetime);

        protected override VisualElement CreateElement()
        {
            // Note:  A rebuild (UI reload) only gets the time the effect has left
            float remainingLifetime = lifetime > 0f ? Mathf.Max(lifetime - (Time.time - spawnTime), 0.01f) : 0f;
            var battleEffectElement = new BattleEffectElement(placement, targetAnchor, material, recipe, tint, spawnTime, remainingLifetime);
            battleEffectElement.expired += Remove;
            return battleEffectElement;
        }

        protected override void UnhookElement(VisualElement detachingElement)
        {
            if (detachingElement is BattleEffectElement battleEffectElement) { battleEffectElement.expired -= Remove; }
        }
    }
}

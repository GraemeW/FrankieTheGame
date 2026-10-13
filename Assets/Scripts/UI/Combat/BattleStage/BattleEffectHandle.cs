using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public sealed class BattleEffectHandle : EntryHandle
    {
        // Note:
        //  - Removes itself once its lifetime is up (non-positive lifetime = never) or its anchor is gone, via `expired`
        //  - BattleStage removes whatever is left when the battle is over

        // State
        private readonly bool isFullScreen;
        private readonly VisualElement targetAnchor; // i.e. enemy slide;  unused for a full-screen effect
        private readonly Material material;
        private readonly Color tint;
        private readonly float spawnTime;
        private readonly float lifetime;

        // Constructor
        private BattleEffectHandle(UIToolkitBoxView view, bool isFullScreen, VisualElement targetAnchor, Material material, Color tint, float lifetime) : base(view, isFullScreen ? typeof(BattleScreenEffectLayer) : typeof(BattleEffectLayer))
        {
            this.isFullScreen = isFullScreen;
            this.targetAnchor = targetAnchor;
            this.material = material;
            this.tint = tint;
            this.lifetime = lifetime;
            spawnTime = Time.time;
        }

        public static BattleEffectHandle OverAnchor(UIToolkitBoxView view, VisualElement targetAnchor, Material material, Color tint, float lifetime) => new(view, false, targetAnchor, material, tint, lifetime);
        public static BattleEffectHandle FullScreen(UIToolkitBoxView view, Material material, Color tint, float lifetime) => new(view, true, null, material, tint, lifetime);

        protected override VisualElement CreateElement()
        {
            // Note:  A rebuild (UI reload) only gets the time the effect has left
            float remainingLifetime = lifetime > 0f ? Mathf.Max(lifetime - (Time.time - spawnTime), 0.01f) : 0f;
            var battleEffectElement = new BattleEffectElement(isFullScreen, targetAnchor, material, tint, spawnTime, remainingLifetime);
            battleEffectElement.expired += Remove;
            return battleEffectElement;
        }

        protected override void UnhookElement(VisualElement detachingElement)
        {
            if (detachingElement is BattleEffectElement battleEffectElement) { battleEffectElement.expired -= Remove; }
        }
    }
}

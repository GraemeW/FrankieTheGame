using System;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Rendering;

namespace Frankie.Combat.UI
{
    public sealed class BattleEffectElement : VisualElement
    {
        // Note:
        //  - Each effect gets its own _Phase (spawn time), tint + recipe, so effects sharing a material animate / colour / shape independently
        //  - An anchored effect expires with its anchor (i.e. when the anchor leaves the panel / if it was never there to begin with)

        // Const Tunables
        private const string _ussClassName = "battle-effect";
        private const string _fullScreenUssClassName = _ussClassName + "--full-screen";

        // State
        private readonly BattleEffectPlacement placement;
        private readonly VisualElement targetAnchor; // Unused for full-screen effects
        private readonly Color vertexTint;
        private bool isExpired = false;

        // Events
        public event Action expired;

        public BattleEffectElement(BattleEffectPlacement placement, VisualElement targetAnchor, Material material, BattleEffectRecipe recipe, BattleEffectTint tint, float phase, float lifetime)
        {
            this.placement = placement;
            this.targetAnchor = targetAnchor;
            AddToClassList(_ussClassName);
            EnableInClassList(_fullScreenUssClassName, placement == BattleEffectPlacement.FullScreen);
            pickingMode = PickingMode.Ignore;

            var materialDefinition = new MaterialDefinition(material);
            ShaderPropertyRefs.SetShaderPhase(ref materialDefinition, phase);
            if (recipe != null)
            {
                // Recipe effects are coloured in the shader (solid / gradient), while other materials keep the vertex tint
                recipe.ApplyTo(ref materialDefinition, placement == BattleEffectPlacement.Slide ? BattleEffectFit.AspectFit : BattleEffectFit.Stretch);
                tint?.ApplyTo(ref materialDefinition);
                vertexTint = Color.white;
            }
            else { vertexTint = tint?.GetColourA() ?? Color.white; }
            style.unityMaterial = new StyleMaterialDefinition(materialDefinition);
            generateVisualContent += DrawEffect;
            RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);

            if (lifetime > 0f) { schedule.Execute(Expire).StartingIn(Mathf.RoundToInt(lifetime * 1000f)); }
        }

        #region PrivateMethods
        private void HandleAttachToPanel(AttachToPanelEvent attachToPanelEvent)
        {
            if (placement == BattleEffectPlacement.FullScreen) { return; }
            if (targetAnchor?.panel == null || targetAnchor.panel != panel || parent == null) { schedule.Execute(Expire); return; } // Deferred:  avoid removing the effect while it is still being attached

            // Positioning the effect over the anchor in two steps:
            //  1. anchorRect:  the area to cover, in the anchor's own local space (0, 0 = the anchor's top-left)
            //      - slide:  the whole slide
            //      - row:  the row's content rect, i.e. excluding its padding (the offset rows' stagger)
            Rect anchorRect = placement == BattleEffectPlacement.Row ? targetAnchor.contentRect : new Rect(Vector2.zero, targetAnchor.layout.size);
            
            //  2. layerRect:  that same area converted into the effect layer's space (this element's parent), which is what style.left / top position against
            //      - the anchor + effect sit in different branches of the stage, so converted between the two elements directly
            Rect layerRect = targetAnchor.ChangeCoordinatesTo(parent, anchorRect);
            style.left = layerRect.x;
            style.top = layerRect.y;
            style.width = layerRect.width;
            style.height = layerRect.height;
            targetAnchor.RegisterCallback<DetachFromPanelEvent>(HandleAnchorDetached);
        }

        private void HandleDetachFromPanel(DetachFromPanelEvent detachFromPanelEvent)
        {
            if (placement != BattleEffectPlacement.FullScreen) { targetAnchor?.UnregisterCallback<DetachFromPanelEvent>(HandleAnchorDetached); }
        }

        private void HandleAnchorDetached(DetachFromPanelEvent detachFromPanelEvent) => Expire();

        private void Expire()
        {
            if (isExpired) { return; }
            isExpired = true;
            expired?.Invoke();
        }

        private void DrawEffect(MeshGenerationContext meshGenerationContext) => ShaderQuad.Draw(meshGenerationContext, new Rect(0f, 0f, 1f, 1f), vertexTint);
        #endregion
    }
}

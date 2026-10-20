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
        private readonly bool isFullScreen;
        private readonly VisualElement targetAnchor; // Unused for full-screen effects
        private readonly Color vertexTint;
        private bool isExpired = false;

        // Events
        public event Action expired;

        public BattleEffectElement(bool isFullScreen, VisualElement targetAnchor, Material material, BattleEffectRecipe recipe, BattleEffectTint tint, float phase, float lifetime)
        {
            this.isFullScreen = isFullScreen;
            this.targetAnchor = targetAnchor;
            AddToClassList(_ussClassName);
            EnableInClassList(_fullScreenUssClassName, isFullScreen);
            pickingMode = PickingMode.Ignore;

            var materialDefinition = new MaterialDefinition(material);
            ShaderPropertyRefs.SetShaderPhase(ref materialDefinition, phase);
            if (recipe != null)
            {
                // Recipe effects are coloured in the shader (solid / gradient), while other materials keep the vertex tint
                recipe.ApplyTo(ref materialDefinition, isFullScreen ? BattleEffectFit.Stretch : BattleEffectFit.AspectFit);
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
            if (isFullScreen) { return; }
            if (targetAnchor?.panel == null || targetAnchor.panel != panel || parent == null) { schedule.Execute(Expire); return; } // Deferred:  avoid removing the effect while it is still being attached

            // Converted between the two elements directly
            Rect layerRect = targetAnchor.ChangeCoordinatesTo(parent, new Rect(Vector2.zero, targetAnchor.layout.size));
            style.left = layerRect.x;
            style.top = layerRect.y;
            style.width = layerRect.width;
            style.height = layerRect.height;
            targetAnchor.RegisterCallback<DetachFromPanelEvent>(HandleAnchorDetached);
        }

        private void HandleDetachFromPanel(DetachFromPanelEvent detachFromPanelEvent)
        {
            if (!isFullScreen) { targetAnchor?.UnregisterCallback<DetachFromPanelEvent>(HandleAnchorDetached); }
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

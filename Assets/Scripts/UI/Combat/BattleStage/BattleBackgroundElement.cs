using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    [UxmlElement]
    public sealed partial class BattleBackgroundElement : VisualElement
    {
        // Note:  UVs are in tile units (i.e. beyond 0-1), so the tile texture must wrap / stay out of the dynamic atlas
        // The UV at the centre of the background is handed to the material as _UVCenter (e.g. the pivot for rotating / twirling shaders)

        // Const Tunables
        private const string _ussClassName = "battle-background";
        private const string _uvCenterReference = "_UVCenter";

        // State
        private Sprite internalTile;
        private Material internalMaterial;
        private float internalSpritePixelsPerUnit = 1f;

        [CreateProperty] public Sprite tile
        {
            get => internalTile;
            set
            {
                internalTile = value;
                Refresh();
            }
        }

        [CreateProperty] public Material material
        {
            get => internalMaterial;
            set
            {
                internalMaterial = value;
                Refresh();
            }
        }

        [CreateProperty] public float spritePixelsPerUnit
        {
            get => internalSpritePixelsPerUnit;
            set
            {
                internalSpritePixelsPerUnit = value;
                Refresh();
            }
        }

        public BattleBackgroundElement()
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += DrawBackground;
            RegisterCallback<GeometryChangedEvent>(_ => Refresh());
            SetBinding(nameof(tile), UIToolkitBindings.ToTarget(nameof(BattleStageModel.backgroundTile)));
            SetBinding(nameof(material), UIToolkitBindings.ToTarget(nameof(BattleStageModel.backgroundMaterial)));
            SetBinding(nameof(spritePixelsPerUnit), UIToolkitBindings.ToTarget(nameof(BattleStageModel.spritePixelsPerUnit)));
        }

        private bool isDrawable => internalTile != null && internalMaterial != null && internalSpritePixelsPerUnit > 0f;
        
        private Vector2 GetUVSize()
        {
            // UV extent of the background, in tiles
            Vector2 tileSize = internalTile.rect.size / internalTile.pixelsPerUnit * internalSpritePixelsPerUnit;
            return new Vector2(contentRect.width / tileSize.x, contentRect.height / tileSize.y);
        }

        private void Refresh()
        {
            if (isDrawable)
            {
                var materialDefinition = new MaterialDefinition(internalMaterial);
                materialDefinition.SetVector(_uvCenterReference, 0.5f * GetUVSize()); // Required for shaders to conduct relative warping/adjustment of the battle background UV
                style.unityMaterial = new StyleMaterialDefinition(materialDefinition);
            }
            else { style.unityMaterial = StyleKeyword.Null; }
            MarkDirtyRepaint();
        }

        private void DrawBackground(MeshGenerationContext meshGenerationContext)
        {
            if (!isDrawable) { return; }
            ShaderQuad.Draw(meshGenerationContext, new Rect(Vector2.zero, GetUVSize()), Color.white, internalTile.texture);
        }
    }
}

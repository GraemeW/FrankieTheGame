using Unity.Properties;
using UnityEngine;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public sealed class BattleStageModel : BindableModel
    {
        // State
        private Sprite internalBackgroundTile;
        private Material internalBackgroundMaterial;
        private float internalSpritePixelsPerUnit = 100f;

        [CreateProperty] public Sprite backgroundTile
        {
            get => internalBackgroundTile;
            set => SetProperty(ref internalBackgroundTile, value);
        }

        [CreateProperty] public Material backgroundMaterial
        {
            get => internalBackgroundMaterial;
            set => SetProperty(ref internalBackgroundMaterial, value);
        }
        
        [CreateProperty] public float spritePixelsPerUnit
        {
            get => internalSpritePixelsPerUnit;
            set => SetProperty(ref internalSpritePixelsPerUnit, value);
        }
    }
}

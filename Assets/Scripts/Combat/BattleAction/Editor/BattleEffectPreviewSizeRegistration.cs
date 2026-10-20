#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using LowDefMustard.Rendering.Editor;

namespace Frankie.Combat.Editor
{
    [InitializeOnLoad]
    public static class BattleEffectPreviewSizeRegistration
    {
        // Note:  Slide sizes from EnemySlide.uss (--enemy-slide-width / height) - keep in sync for accurate preview

        static BattleEffectPreviewSizeRegistration()
        {
            BattleEffectPreviewSizes.Register("Standard slide (280x320)", new Vector2(280f, 320f));
            BattleEffectPreviewSizes.Register("Mook slide (175x200)", new Vector2(175f, 200f));
            BattleEffectPreviewSizes.Register("Boss slide (420x480)", new Vector2(420f, 480f));
            BattleEffectPreviewSizes.Register("Full screen (640x360)", new Vector2(640f, 360f));
        }
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using LowDefMustard.Rendering.Editor;

namespace Frankie.Combat.Editor
{
    [InitializeOnLoad]
    public static class BattleEffectPreviewSizeRegistration
    {
        // Notes:
        //  - Slide sizes from EnemySlide.uss (--enemy-slide-width / height) - keep in sync for accurate preview
        //  - Full screen is the 2560x1440 panel at 1/4 scale
        //  - A row spans its slides + their margins (EnemySlide.uss), e.g. 3 standard slides = 3 x (280 + 2 x 12.5) wide

        static BattleEffectPreviewSizeRegistration()
        {
            BattleEffectPreviewSizes.Register("Standard slide (280x320)", new Vector2(280f, 320f));
            BattleEffectPreviewSizes.Register("Mook slide (175x200)", new Vector2(175f, 200f));
            BattleEffectPreviewSizes.Register("Boss slide (420x480)", new Vector2(420f, 480f));
            BattleEffectPreviewSizes.Register("Row of 3 standard (915x320)", new Vector2(915f, 320f));
            BattleEffectPreviewSizes.Register("Full screen (640x360)", new Vector2(640f, 360f));
        }
    }
}
#endif

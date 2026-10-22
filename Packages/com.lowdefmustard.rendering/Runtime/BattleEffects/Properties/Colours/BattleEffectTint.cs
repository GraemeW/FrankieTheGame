using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering
{
    [Serializable]
    public sealed class BattleEffectTint
    {
        // Notes:
        //  - Gradients are measured in effect units (0-1 = the effect's centred square)
        //  - Wave && radial w/ a frequency of 0 pulse the whole effect between A + B at the speed

        [SerializeField] private BattleEffectTintMode mode = BattleEffectTintMode.Solid;
        [SerializeField] private Color colourA = Color.white;
        [SerializeField][Tooltip("Gradient modes only")] private Color colourB = Color.white;
        [SerializeField][Tooltip("linear / wave only: counter-clockwise from +x")] private float angleDegrees = 0f;
        [SerializeField][Tooltip("Linear: gradients across one effect unit. Wave / radial: bands across one effect unit")][Min(0f)] private float bandsPerEU = 1f;
        [SerializeField][Tooltip("Wave / radial only: bands scrolled per second (0 = still)")] private float scrollRate = 0f;

        // Editor Field References
        public const string modeField = nameof(mode);
        public const string colourBField = nameof(colourB);
        public const string angleDegreesField = nameof(angleDegrees);
        public const string bandsPerEUField = nameof(bandsPerEU);
        public const string scrollRateField = nameof(scrollRate);

        // Constructors
        public BattleEffectTint() { }
        public BattleEffectTint(Color colour)
        {
            colourA = colour;
        }

        // Public Functions
        public Color GetColourA() => colourA;

        public void ApplyTo(ref MaterialDefinition materialDefinition)
        {
            BattleEffectPropertyRefs.SetTint(ref materialDefinition, colourA, colourB, new BattleEffectTintSettings(mode, angleDegrees * Mathf.Deg2Rad, bandsPerEU, scrollRate));
        }
    }
}

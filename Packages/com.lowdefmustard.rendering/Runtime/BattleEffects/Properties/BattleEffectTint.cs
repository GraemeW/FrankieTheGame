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
        [SerializeField][Tooltip("linear / wave only: degrees, counter-clockwise from +x")] private float angle = 0f;
        [SerializeField][Tooltip("Linear: gradients across the effect. Wave / radial: bands across the effect")][Min(0f)] private float frequency = 1f;
        [SerializeField][Tooltip("Wave / radial only: scroll in cycles per second (0 = still)")] private float speed = 0f;

        // Editor Field References
        public const string modeField = nameof(mode);
        public const string colourBField = nameof(colourB);
        public const string angleField = nameof(angle);
        public const string frequencyField = nameof(frequency);
        public const string speedField = nameof(speed);

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
            BattleEffectPropertyRefs.SetTint(ref materialDefinition, colourA, colourB, new BattleEffectTintSettings(mode, angle * Mathf.Deg2Rad, frequency, speed));
        }
    }
}

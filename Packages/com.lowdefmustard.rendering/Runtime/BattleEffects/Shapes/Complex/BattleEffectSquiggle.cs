using System;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    [Serializable]
    [BattleEffectShapeId(10)]
    public sealed class BattleEffectSquiggle : BattleEffectShape
    {
        // Note:
        //  - A wave (sine / triangle / square / steps / trapezoid) along the item's local x-axis, centred on it, w/ round ends (rotate the item 90 degrees for vertical)
        //      - Keep the item's scale uniform (e.g. 1, so shape units = effect units)
        //      - A scroll range slides the wave along the line (e.g. 0 -> 1 travels one shape unit over the item's duration)
        //  - Draw w/ the outline style for thickness

        [SerializeField][Tooltip("How far the wave extends either side of its centre")] private BattleEffectFloatRange halfLengthSU = new(0.9f);
        [SerializeField][Tooltip("Crest to crest")] private BattleEffectFloatRange wavelengthSU = new(0.2f);
        [SerializeField][Tooltip("Crest height either side of the line")] private BattleEffectFloatRange amplitudeSU = new(0.1f);
        [SerializeField][Tooltip("Slides the wave along the line")] private BattleEffectFloatRange scrollSU = new(0f);
        [SerializeField][Tooltip("Sine / triangle / trapezoid start on the line + rise;  square / steps start on the crest (negative amplitude flips them)")] private BattleEffectWaveform waveform = BattleEffectWaveform.Sine;
        [SerializeField][Tooltip("Trapezoid only:  each half wavelength's flat crest share (0 = triangle, 1 = square)")][BattleEffectShownWhen(nameof(waveform), (int)BattleEffectWaveform.Trapezoid)] private BattleEffectFloatRange plateauFraction = new(0.25f);

        public override void GetParameters(BattleEffectFloatRange[] parameters)
        {
            parameters[0] = halfLengthSU;
            parameters[1] = wavelengthSU;
            parameters[2] = amplitudeSU;
            parameters[3] = scrollSU;
            parameters[4] = new BattleEffectFloatRange((int)waveform);
            parameters[5] = plateauFraction;
        }
    }
}

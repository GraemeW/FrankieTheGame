using System;
using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Rendering;

namespace Frankie.Combat
{
    [Serializable]
    public sealed class BattleEffectShape
    {
        // Note:  Only the settings for the selected shape type are used (/ drawn in inspector ~ see BattleEffectShapeDrawer)

        [SerializeField] private BattleEffectShapeType shapeType = BattleEffectShapeType.Circle;
        [SerializeField][Tooltip("Seconds after the effect spawns")][Min(0f)] private float startTime = 0f;
        [SerializeField][Tooltip("Seconds - the shape is not drawn outside of its start + duration")][Min(0f)] private float duration = 1f;
        [SerializeField][Tooltip("Multiplied by the effect colour")] private Color colour = Color.white;
        [SerializeField] private BattleEffectCircle circle = new();
        [SerializeField] private BattleEffectSquiggle squiggle = new();

        // Editor Field References
        public const string shapeTypeField = nameof(shapeType);
        public const string circleField = nameof(circle);
        public const string squiggleField = nameof(squiggle);

        public void ApplyTo(ref MaterialDefinition materialDefinition, int index)
        {
            bool isVariant;
            Vector4 paramsA;
            Vector4 paramsB;
            switch (shapeType)
            {
                case BattleEffectShapeType.Squiggle:
                    squiggle.GetParameters(out isVariant, out paramsA, out paramsB);
                    break;
                case BattleEffectShapeType.Circle:
                default:
                    circle.GetParameters(out isVariant, out paramsA, out paramsB);
                    break;
            }

            var header = new Vector4((int)shapeType, startTime, duration, isVariant ? 1f : 0f);
            ShaderPropertyRefs.SetBattleEffectItem(ref materialDefinition, index, header, paramsA, paramsB, colour);
        }
    }
}

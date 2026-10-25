#if UNITY_EDITOR
using System;

namespace LowDefMustard.Rendering.Editor
{
    internal readonly struct BattleEffectRecipeEntry : IEquatable<BattleEffectRecipeEntry>
    {
        public int index { get; } // In the recipe's items or instances list
        public bool isInstance { get; }
        public string label { get; }
        public string shapeName { get; } // Items only
        public string recipeName { get; } // Instances only (null w/o a recipe)
        public float startTime { get; }
        public float endTime { get; }

        public BattleEffectRecipeEntry(int index, bool isInstance, string label, string shapeName, string recipeName, float startTime, float endTime)
        {
            this.recipeName = recipeName;
            this.index = index;
            this.isInstance = isInstance;
            this.label = label;
            this.shapeName = shapeName;
            this.startTime = startTime;
            this.endTime = endTime;
        }

        public BattleEffectEntryId id => new(index, isInstance);
        public bool IsActiveAt(float time) => IsActive(startTime, endTime, time);
        public static bool IsActive(float startTime, float endTime, float time) => time >= startTime && time <= endTime;

        public bool Equals(BattleEffectRecipeEntry other) => index == other.index && isInstance == other.isInstance && label == other.label && shapeName == other.shapeName && recipeName == other.recipeName
            && startTime.Equals(other.startTime) && endTime.Equals(other.endTime);
        public override bool Equals(object obj) => obj is BattleEffectRecipeEntry other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(index, isInstance, label, shapeName, recipeName, startTime, endTime);
    }
}
#endif

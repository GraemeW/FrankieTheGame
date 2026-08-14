using System;
using System.Collections.Generic;

namespace LowDefMustard.Utils
{
    public class ApproximateFloatComparer : IEqualityComparer<float>
    {
        private const float _defaultTolerance = 0.005f;
        private readonly float tolerance;

        public ApproximateFloatComparer(float tolerance = _defaultTolerance)
        {
            if (tolerance <= 0f) { tolerance = _defaultTolerance; }
            this.tolerance = tolerance;
        }

        public bool Equals(float a, float b) => Quantize(a) == Quantize(b);
        public int GetHashCode(float value) => Quantize(value).GetHashCode();
        private long Quantize(float value)
        {
            if (float.IsNaN(value)) { return 0; }
            if (float.IsPositiveInfinity(value)) { return long.MaxValue; }
            if (float.IsNegativeInfinity(value)) { return long.MinValue; }
            
            double scaled = Math.Round((double)value / tolerance, MidpointRounding.AwayFromZero);
            scaled = Math.Clamp(scaled, long.MinValue, long.MaxValue);

            return (long)scaled;
        }
    }
}

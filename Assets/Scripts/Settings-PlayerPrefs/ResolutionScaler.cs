using UnityEngine;

namespace Frankie.Settings
{
    [System.Serializable]
    public struct ResolutionScaler
    {
        public int numerator;
        public int denominator;
        public int cameraScaling;

        public ResolutionScaler(int numerator = 1, int denominator = 1, int cameraScaling = 1)
        {
            this.numerator = numerator;
            this.denominator = denominator;
            this.cameraScaling = cameraScaling;
        }

        public ResolutionScaler(ResolutionScaler other)
        {
            this.numerator = other.numerator;
            this.denominator = other.denominator;
            this.cameraScaling = other.cameraScaling;
        }
    }
}

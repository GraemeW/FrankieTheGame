namespace Frankie.Rendering
{
    [System.Serializable]
    public struct ResolutionScaler
    {
        public int numerator;
        public int denominator;

        public ResolutionScaler(int numerator = 1, int denominator = 1)
        {
            this.numerator = numerator;
            this.denominator = denominator;
        }

        public ResolutionScaler(ResolutionScaler other)
        {
            numerator = other.numerator;
            denominator = other.denominator;
        }
    }
}

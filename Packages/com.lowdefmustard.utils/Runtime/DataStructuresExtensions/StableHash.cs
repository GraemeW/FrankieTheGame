namespace LowDefMustard.Utils
{
    public static class StableHash
    {
        // Deterministic FNV-1a string hash -> identical across sessions, platforms + runtimes
        //  - Use for any hash that gets persisted (scenes, assets, saves)
        //  - Notably, do NOT use System.HashCode (seeded per domain reload) || string.GetHashCode (seeded per process in CoreCLR)
        
        // Const Tunables
        private const uint _fnvOffsetBasis = 2166136261;
        private const uint _fnvPrime = 16777619;

        // Public Methods
        public static uint GetOffsetBasis() => _fnvOffsetBasis; // Init

        public static uint Add(uint hash, string value)
        {
            // FNV-1a over the length prefix + each UTF-16 code unit (low byte, high byte)
            // Note:  length prefix keeps ("ab", "c") != ("a", "bc") and null (-1) != ""
            unchecked
            {
                int length = value?.Length ?? -1;
                for (int shift = 0; shift < 32; shift += 8) { hash = (hash ^ (byte)(length >> shift)) * _fnvPrime; }
                if (value == null) { return hash; }
                foreach (char character in value)
                {
                    hash = (hash ^ (byte)character) * _fnvPrime;
                    hash = (hash ^ (byte)(character >> 8)) * _fnvPrime;
                }
                return hash;
            }
        }
    }
}

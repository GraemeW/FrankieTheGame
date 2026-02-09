using System;

namespace Frankie.Core
{
    public interface IAddressablesCache
    {
        // Note:  Default Label Strategy:  Label == Class Name implementing this interface
        // Static abstracts not available in C# until ≥11.0, pending to add abstract keyword to methods below

        static void BuildCacheIfEmpty() => throw new NotImplementedException();
        static void ReleaseCache() => throw new NotImplementedException();
    }
}

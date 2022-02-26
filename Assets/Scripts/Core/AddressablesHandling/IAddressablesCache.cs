using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public interface IAddressablesCache
    {
        // Note:  Default Label Strategy:  Label == Class Name implementing this interface

        static void BuildCacheIfEmpty() => throw new NotImplementedException();
        static void ReleaseCache() => throw new NotImplementedException();
    }
}
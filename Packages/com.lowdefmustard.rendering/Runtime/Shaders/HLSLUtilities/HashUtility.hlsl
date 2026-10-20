#include "ShaderApiReflectionSupport.hlsl"

#ifndef HASHUTILITY_INCLUDED
#define HASHUTILITY_INCLUDED

// Note:  
//  - Cheap, stable pseudo-random hashes (no texture / sine based)
//  - Same input = same output on every platform

///<funchints>
/// <sg:ProviderKey>Hash22</sg:ProviderKey>
/// <sg:DisplayName>Hash 2-2</sg:DisplayName>
/// <sg:SearchCategory>LDM/Hash/Hash22</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void Hash22(float2 input,
    out float2 hashOutput)
{
    // 'Hash without Sine' (Dave Hoskins) - output in [0, 1)
    
    float3 hash = frac(input.xyx * float3(0.1031, 0.1030, 0.0973));
    hash += dot(hash, hash.yzx + 33.33);
    hashOutput = frac((hash.xx + hash.yz) * hash.zy);
}

#endif

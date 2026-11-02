#include "ShaderApiReflectionSupport.hlsl"

#ifndef SDFPARSER_INCLUDED
#define SDFPARSER_INCLUDED

///<funchints>
/// <sg:ProviderKey>SDFParser</sg:ProviderKey>
/// <sg:DisplayName>SDF Parser</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFParser</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFParser(float distance, float crispness, float4 drawColor, float4 backgroundColor,
    out float4 colorOutput, out float alphaOutput)
{
    alphaOutput = crispness == 0.0 ? (distance >= 0.0 ? 0.0 : 1.0) : 1.0 - smoothstep(0.0, crispness, distance);
    colorOutput = lerp(backgroundColor, drawColor, alphaOutput);
}

#endif

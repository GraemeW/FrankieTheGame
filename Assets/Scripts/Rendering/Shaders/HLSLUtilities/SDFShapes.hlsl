#include "ShaderApiReflectionSupport.hlsl"

#ifndef SDFSHAPES_INCLUDED
#define SDFSHAPES_INCLUDED

// Helper Functions
float SDFRemapUnclamped(float value, float inMin, float inMax, float outMin, float outMax)
{
    return outMin + (value - inMin) * (outMax - outMin) / (inMax - inMin);
}

// Unity ShaderGraph Functions

///<funchints>
/// <sg:ProviderKey>SDFCircle</sg:ProviderKey>
/// <sg:DisplayName>SDF Circle</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/Circle</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFCircle(float2 uv, float2 position, float radius, 
    out float distanceOutput)
{
    distanceOutput = length(uv - position) - radius;
}

///<funchints>
/// <sg:ProviderKey>SDFOnion</sg:ProviderKey>
/// <sg:DisplayName>SDF Onion</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/Onion</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFOnion(float distance, float thickness, 
    out float distanceOutput)
{
    distanceOutput = abs(distance) - thickness;
}

///<funchints>
/// <sg:ProviderKey>SDFSquiggle</sg:ProviderKey>
/// <sg:DisplayName>SDF Squiggle</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/Squiggle</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFSquiggle(float2 uv, bool isHorizontal, float position, float width, float frequency, float phaseOffset, float2 minMax, float intensityModifier,
    out float distanceOutput)
{
    float along = isHorizontal ? uv.x : uv.y;
    float perpendicular  = isHorizontal ? uv.y : uv.x;

    float sineValue = sin(along * frequency + phaseOffset);

    float amplitudeCeiling = SDFRemapUnclamped(intensityModifier, -1.0, 1.0, minMax.x, minMax.y);
    float waveValue = SDFRemapUnclamped(sineValue, -1.0, 1.0, minMax.x, amplitudeCeiling);

    float alongOffset = abs(along - position) / width;
    float perpOffset = abs(perpendicular - waveValue);

    distanceOutput = length(float2(alongOffset, perpOffset));
}

#endif

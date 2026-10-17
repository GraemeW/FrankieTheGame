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

    float alongOffset = abs(along - position) / max(abs(width), 1e-5);
    float perpOffset = abs(perpendicular - waveValue);

    distanceOutput = length(float2(alongOffset, perpOffset));
}

///<funchints>
/// <sg:ProviderKey>SDFRhombus</sg:ProviderKey>
/// <sg:DisplayName>SDF Rhombus</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/Rhombus</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFRhombus(float2 uv, float2 position, float2 halfSize,
    out float distanceOutput)
{
    // Diamond w/ its points at +/- half size along x / y (exact distance, Inigo Quilez)
    
    float2 safeHalfSize = max(abs(halfSize), 1e-5);
    float2 folded = abs(uv - position);
    float h = clamp(((safeHalfSize.x - 2.0 * folded.x) * safeHalfSize.x - (safeHalfSize.y - 2.0 * folded.y) * safeHalfSize.y) / dot(safeHalfSize, safeHalfSize), -1.0, 1.0);
    float distance = length(folded - 0.5 * safeHalfSize * float2(1.0 - h, 1.0 + h));
    distanceOutput = distance * sign(folded.x * safeHalfSize.y + folded.y * safeHalfSize.x - safeHalfSize.x * safeHalfSize.y);
}

///<funchints>
/// <sg:ProviderKey>SDFSegment</sg:ProviderKey>
/// <sg:DisplayName>SDF Segment</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/Segment</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFSegment(float2 uv, float2 start, float2 end,
    out float distanceOutput)
{
    // Distance to a line segment (never negative ~ give it thickness w/ an onion / outline)
    
    float2 toPoint = uv - start;
    float2 along = end - start;
    float h = saturate(dot(toPoint, along) / max(dot(along, along), 1e-8));
    distanceOutput = length(toPoint - along * h);
}

///<funchints>
/// <sg:ProviderKey>SDFChevron</sg:ProviderKey>
/// <sg:DisplayName>SDF Chevron</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/Chevron</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFChevron(float2 uv, float2 position, float halfWidth, float depth,
    out float distanceOutput)
{
    // A '^':  two mirrored segments from the apex down to (+/- half width, -depth), centred vertically on position
    // Note:  never negative, like a segment
    
    float2 local = uv - position;
    local.y -= depth * 0.5;
    local.x = abs(local.x);
    SDFSegment(local, float2(0.0, 0.0), float2(halfWidth, -depth), distanceOutput);
}

#endif

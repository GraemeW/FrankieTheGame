#include "ShaderApiReflectionSupport.hlsl"

#ifndef SDFSHAPES_INCLUDED
#define SDFSHAPES_INCLUDED

#include "Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/HashUtility.hlsl"

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
UNITY_EXPORT_REFLECTION void SDFSquiggle(float2 uv, float2 position, float halfLength, float wavelength, float amplitude, float scroll,
    out float distanceOutput)
{
    // A sine wave along x, centred on position (crests +/- amplitude, wavelength crest to crest)
    //  - scroll slides the wave along the line
    //  - round ends at +/- half length
    // Note:  never negative, like a segment
    // Distance is divided by the wave's local slope (first-order), so steep stretches keep their thickness
    
    float2 local = uv - position;
    float waveNumber = 6.28318531 / max(abs(wavelength), 1e-4);
    float phase = (local.x - scroll) * waveNumber;
    float slope = amplitude * waveNumber * cos(phase);
    float acrossDistance = abs(local.y - amplitude * sin(phase)) / sqrt(1.0 + slope * slope);
    distanceOutput = length(float2(max(abs(local.x) - halfLength, 0.0), acrossDistance));
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

///<funchints>
/// <sg:ProviderKey>SDFLens</sg:ProviderKey>
/// <sg:DisplayName>SDF Lens</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/Lens</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFLens(float2 uv, float2 position, float2 halfSize,
    out float distanceOutput)
{
    // Pointed ellipse (vesica / eye / leaf) w/ its tips at +/- half size x and its widest at +/- half size y (exact distance, Inigo Quilez)
    //  - two arcs, so half size y is capped at half size x (= a circle)

    float halfLength = max(abs(halfSize.x), 1e-5);
    float halfHeight = clamp(abs(halfSize.y) / halfLength, 1e-3, 1.0);
    float2 folded = abs(uv - position).yx / halfLength; // tips along the folded y-axis
    float arcOffset = (1.0 - halfHeight * halfHeight) / (2.0 * halfHeight);
    float arcRadius = halfHeight + arcOffset;
    float distance = (folded.y - 1.0) * arcOffset > folded.x ? length(folded - float2(0.0, 1.0)) : length(folded + float2(arcOffset, 0.0)) - arcRadius;
    distanceOutput = distance * halfLength;
}

///<funchints>
/// <sg:ProviderKey>SDFJaggedLine</sg:ProviderKey>
/// <sg:DisplayName>SDF Jagged Line</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/JaggedLine</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFJaggedLine(float2 uv, float2 position, float halfLength, float cellWidth, float amplitude, float calm, float seed, float bumpHalfWidth, float variation, float scroll,
    out float distanceOutput)
{
    // A flat line along x w/ a triangular bump in each cell (cells centred on position + scroll + x = cell width multiples)
    //  - each bump points up or down at random (seeded), or is flat for the calm fraction of cells
    //  - variation shrinks each bump's height by a random fraction, up to all of it (0 = every bump at full amplitude)
    //  - cells centred beyond +/- bump half width (of position, not scrolled) stay flat (negative = no bumps)
    //  - scroll slides the bumps along the line, square ends at +/- half-length
    // Note:  never negative, like a segment
    
    float2 local = uv - position;
    float width = max(abs(cellWidth), 1e-4);
    float nearestCell = round((local.x - scroll) / width);
    float wholeSeed = floor(seed);
    float distance = 1e5;
    [unroll]
    for (int offset = -1; offset <= 1; offset++)
    {
        // A bump can only reach into its neighbouring cells
        float cell = nearestCell + offset;
        float centre = cell * width + scroll;
        float2 shapeHash, heightHash;
        Hash22(float2(cell, wholeSeed), shapeHash);
        Hash22(float2(wholeSeed, cell), heightHash);
        bool isFlat = shapeHash.x < calm || abs(centre) > bumpHalfWidth;
        float height = isFlat ? 0.0 : (shapeHash.y < 0.5 ? -amplitude : amplitude) * (1.0 - saturate(variation) * heightHash.x);
        float2 peak = float2(centre, height);

        float riseDistance, fallDistance;
        SDFSegment(local, float2(centre - 0.5 * width, 0.0), peak, riseDistance);
        SDFSegment(local, peak, float2(centre + 0.5 * width, 0.0), fallDistance);
        distance = min(distance, min(riseDistance, fallDistance));
    }
    distanceOutput = length(float2(max(abs(local.x) - halfLength, 0.0), distance));
}

#endif

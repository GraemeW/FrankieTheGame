#include "ShaderApiReflectionSupport.hlsl"

#ifndef SDFDOMAIN_INCLUDED
#define SDFDOMAIN_INCLUDED

// Domain operations move the point an SDF is evaluated at - allows shapes to be written once centred on the origin
// Multiply a shape's local distance by the distance scale to get back to the original (e.g. UV) units

///<funchints>
/// <sg:ProviderKey>SDFAspectFit</sg:ProviderKey>
/// <sg:DisplayName>SDF Aspect Fit</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFDomain/AspectFit</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFAspectFit(float2 uv,
    out float2 fittedPoint)
{
    // Re-measures a 0-1 UV quad in units of its shorter side (such that shapes keep their proportions on any quad size)
    //  -> centred 0-1 on both axes is the largest centred square
    //  -> which means, room beyond 0-1 exists along the longer side
    // Quad's pixel size comes from screen-space derivatives (called outside per-pixel branching)
    
    float2 pixelsAcross = 1.0 / max(fwidth(uv), 1e-6);
    float shorterSide = min(pixelsAcross.x, pixelsAcross.y);
    fittedPoint = 0.5 + (uv - 0.5) * pixelsAcross / shorterSide;
}

///<funchints>
/// <sg:ProviderKey>SDFTransform</sg:ProviderKey>
/// <sg:DisplayName>SDF Transform</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFDomain/Transform</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFTransform(float2 uv, float2 position, float rotation, float2 scale,
    out float2 localPoint, out float distanceScale)
{
    // Rotation in radians (counter-clockwise)
    // Non-uniform scale gives an approximate (conservative) distance
    
    float2 safeScale = max(abs(scale), 1e-5);
    float2 offset = uv - position;
    float sine = sin(-rotation);
    float cosine = cos(-rotation);
    float2 rotated = float2(cosine * offset.x - sine * offset.y, sine * offset.x + cosine * offset.y);

    localPoint = rotated / safeScale;
    distanceScale = min(safeScale.x, safeScale.y);
}

///<funchints>
/// <sg:ProviderKey>SDFMirror</sg:ProviderKey>
/// <sg:DisplayName>SDF Mirror</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFDomain/Mirror</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFMirror(float2 localPoint, float mirrorX, float mirrorY,
    out float2 mirroredPoint)
{
    // Fold one half onto the other (across the y- / x-axis)
    
    mirroredPoint = float2(mirrorX > 0.5 ? abs(localPoint.x) : localPoint.x, mirrorY > 0.5 ? abs(localPoint.y) : localPoint.y);
}

///<funchints>
/// <sg:ProviderKey>SDFRepeatLine</sg:ProviderKey>
/// <sg:DisplayName>SDF Repeat Line</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFDomain/RepeatLine</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFRepeatLine(float2 localPoint, float2 spacing, float count,
    out float2 nearestPoint, out float2 neighbourPoint)
{
    // Count copies, centred on the origin, spacing apart
    //  - evaluate the shape at both points + take the min
    // Neighbour (next copy along) keeps shapes that overlap their neighbours' cells from being clipped
    
    float copies = max(round(count), 1.0);
    float centre = (copies - 1.0) * 0.5;
    float along = dot(localPoint, spacing) / max(dot(spacing, spacing), 1e-8) + centre;
    float nearest = clamp(round(along), 0.0, copies - 1.0);
    float neighbour = clamp(nearest + (along >= nearest ? 1.0 : -1.0), 0.0, copies - 1.0);
    nearestPoint = localPoint - (nearest - centre) * spacing;
    neighbourPoint = localPoint - (neighbour - centre) * spacing;
}

///<funchints>
/// <sg:ProviderKey>SDFRepeatGrid</sg:ProviderKey>
/// <sg:DisplayName>SDF Repeat Grid</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFDomain/RepeatGrid</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFRepeatGrid(float2 localPoint, float2 spacing, float2 count,
    out float2 nearestPoint, out float2 neighbourXPoint, out float2 neighbourYPoint, out float2 neighbourXYPoint)
{
    // Count.x by count.y copies, centred on the origin, spacing.x / spacing.y apart along x / y
    //  - evaluate the shape at all four points + take the min
    // Neighbours (the next copies along x, y + diagonally, on the point's side) keep shapes that overlap their neighbours' cells from being clipped

    float2 copies = max(round(count), 1.0);
    float2 centre = (copies - 1.0) * 0.5;
    float2 safeSpacing = float2(abs(spacing.x) < 1e-5 ? 1e-5 : spacing.x, abs(spacing.y) < 1e-5 ? 1e-5 : spacing.y);
    float2 along = localPoint / safeSpacing + centre;
    float2 nearest = clamp(round(along), 0.0, copies - 1.0);
    float2 neighbour = clamp(nearest + (along >= nearest ? 1.0 : -1.0), 0.0, copies - 1.0);
    nearestPoint = localPoint - (nearest - centre) * safeSpacing;
    neighbourXPoint = localPoint - (float2(neighbour.x, nearest.y) - centre) * safeSpacing;
    neighbourYPoint = localPoint - (float2(nearest.x, neighbour.y) - centre) * safeSpacing;
    neighbourXYPoint = localPoint - (neighbour - centre) * safeSpacing;
}

///<funchints>
/// <sg:ProviderKey>SDFElongate</sg:ProviderKey>
/// <sg:DisplayName>SDF Elongate</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFDomain/Elongate</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFElongate(float2 localPoint, float2 elongation,
    out float2 elongatedPoint)
{
    // Stretch shape's middle by +/- elongation along x / y (e.g. a diamond -> a long hexagon, a circle -> a capsule)
    //  - exact outside (approximate inside)
    //  - no folding (asymmetric shapes keep their form)
    //  - zero elongation = no change
    
    float2 halfStretch = max(elongation, 0.0);
    elongatedPoint = localPoint - clamp(localPoint, -halfStretch, halfStretch);
}

#endif

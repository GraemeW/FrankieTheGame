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
    // Negative scale flips that local axis (e.g. a mirrored copy)
    
    float2 safeScale = (scale < 0.0 ? -1.0 : 1.0) * max(abs(scale), 1e-5);
    float2 offset = uv - position;
    float sine = sin(-rotation);
    float cosine = cos(-rotation);
    float2 rotated = float2(cosine * offset.x - sine * offset.y, sine * offset.x + cosine * offset.y);

    localPoint = rotated / safeScale;
    distanceScale = min(abs(safeScale.x), abs(safeScale.y));
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
/// <sg:ProviderKey>SDFRepeatRadial</sg:ProviderKey>
/// <sg:DisplayName>SDF Repeat Radial</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFDomain/RepeatRadial</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFRepeatRadial(float2 localPoint, float count,
    out float2 nearestPoint, out float2 previousPoint, out float2 nextPoint)
{
    // Count copies rotated evenly about the origin (copy 0 unrotated, the rest counter-clockwise)
    //  - offset the shape from the origin (e.g. subtract its position from each point) + evaluate it at all three points + take the min
    // Previous / next (the neighbouring copies) keep shapes that reach into their neighbours' sectors from being clipped
    
    float copies = max(round(count), 1.0);
    float sector = 6.28318531 / copies;
    float nearest = round(atan2(localPoint.y, localPoint.x) / sector);
    float3 angles = float3(nearest, nearest - 1.0, nearest + 1.0) * -sector;
    float3 sines = sin(angles);
    float3 cosines = cos(angles);
    nearestPoint = float2(cosines.x * localPoint.x - sines.x * localPoint.y, sines.x * localPoint.x + cosines.x * localPoint.y);
    previousPoint = float2(cosines.y * localPoint.x - sines.y * localPoint.y, sines.y * localPoint.x + cosines.y * localPoint.y);
    nextPoint = float2(cosines.z * localPoint.x - sines.z * localPoint.y, sines.z * localPoint.x + cosines.z * localPoint.y);
}

///<funchints>
/// <sg:ProviderKey>SDFRepeatStaggeredGrid</sg:ProviderKey>
/// <sg:DisplayName>SDF Repeat Staggered Grid</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFDomain/RepeatStaggeredGrid</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFRepeatStaggeredGrid(float2 localPoint, float2 spacing, float2 count,
    out float2 nearestPoint, out float2 neighbourXPoint, out float2 neighbourYPoint, out float2 neighbourXYPoint)
{
    // Count.y rows, spacing.y apart + centred on the origin:  even rows (0, 2, ...) hold count.x copies, odd rows count.x - 1
    //  - each row's copies are centred, spacing.x apart ~ odd rows then sit half a spacing across to make honeycomb lattice
    //  - evaluate the shape at all four points (the nearest + next copies in the nearest + next rows) + take the min
    // A row w/o copies (count.x 1) returns points far away
    
    float2 copies = max(round(count), 1.0);
    float2 safeSpacing = float2(abs(spacing.x) < 1e-5 ? 1e-5 : spacing.x, abs(spacing.y) < 1e-5 ? 1e-5 : spacing.y);
    float rowCentre = (copies.y - 1.0) * 0.5;
    float alongY = localPoint.y / safeSpacing.y + rowCentre;
    float nearestRow = clamp(round(alongY), 0.0, copies.y - 1.0);
    float neighbourRow = clamp(nearestRow + (alongY >= nearestRow ? 1.0 : -1.0), 0.0, copies.y - 1.0);

    float2 rows = float2(nearestRow, neighbourRow);
    float2 rowCopies = copies.x - fmod(rows, 2.0); // odd rows:  one fewer
    float2 copyCentre = (rowCopies - 1.0) * 0.5;
    float2 alongX = localPoint.x / safeSpacing.x + copyCentre;
    float2 nearestCopy = clamp(round(alongX), 0.0, max(rowCopies - 1.0, 0.0));
    float2 neighbourCopy = clamp(nearestCopy + (alongX >= nearestCopy ? 1.0 : -1.0), 0.0, max(rowCopies - 1.0, 0.0));
    float2 rowY = (rows - rowCentre) * safeSpacing.y;
    float2 farAway = rowCopies < 0.5 ? 1e5 : 0.0;
    
    nearestPoint = localPoint - float2((nearestCopy.x - copyCentre.x) * safeSpacing.x, rowY.x) + farAway.x;
    neighbourXPoint = localPoint - float2((neighbourCopy.x - copyCentre.x) * safeSpacing.x, rowY.x) + farAway.x;
    neighbourYPoint = localPoint - float2((nearestCopy.y - copyCentre.y) * safeSpacing.x, rowY.y) + farAway.y;
    neighbourXYPoint = localPoint - float2((neighbourCopy.y - copyCentre.y) * safeSpacing.x, rowY.y) + farAway.y;
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

///<funchints>
/// <sg:ProviderKey>SDFStripeMask</sg:ProviderKey>
/// <sg:DisplayName>SDF Stripe Mask</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFDomain/StripeMask</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFStripeMask(float2 uv, float2 position, float stripesPerUnit, float angle, float duty, float phase,
    out float maskOutput)
{
    // Hard-edged parallel bands (1 on a band, 0 between), stripes per unit of them across uv (e.g. scanlines / interlacing)
    //  - angle 0 = horizontal bands, stacked up y (radians, counter-clockwise)
    //  - each band runs [phase, phase + duty) of its stripe, counted from position
    //  - half-open -> two masks w/ duty 0.5 + phases 0.5 apart interleave exactly
    
    float2 across = float2(-sin(angle), cos(angle));
    float stripe = frac(dot(uv - position, across) * stripesPerUnit - phase);
    maskOutput = stripe < duty ? 1.0 : 0.0;
}

#endif

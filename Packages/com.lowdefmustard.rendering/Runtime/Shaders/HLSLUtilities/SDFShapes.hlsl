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

///<funchints>
/// <sg:ProviderKey>SDFSquiggle</sg:ProviderKey>
/// <sg:DisplayName>SDF Squiggle</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/Squiggle</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFSquiggle(float2 uv, float2 position, float halfLength, float wavelength, float amplitude, float scroll, float waveform,
    out float distanceOutput)
{
    // A wave along x, centred on position (crests +/- amplitude, wavelength crest to crest), each starting at 0 and rising (negative amplitude flips it)
    //  - scroll slides the wave along the line
    //  - round ends at +/- half-length
    // Waveforms:  0 = sine, 1 = triangle, 2 = square, 3 = steps (a square wave's flats only)
    // Note:  never negative, like a segment
    
    float2 local = uv - position;
    float waveX = local.x - scroll;
    float safeWavelength = max(abs(wavelength), 1e-4);
    int form = (int)round(waveform);
    if (form == 3)  // Steps
    {
        float stepWidth = 0.5 * safeWavelength;
        float nearestStep = floor(waveX / stepWidth);
        distanceOutput = 1e5;
        [unroll]
        for (int offset = -1; offset <= 1; offset++)
        {
            float stepIndex = nearestStep + offset;
            float level = frac(stepIndex * 0.5) < 0.25 ? amplitude : -amplitude;
            float2 ends = clamp(float2(stepIndex, stepIndex + 1.0) * stepWidth + scroll, -halfLength, halfLength);
            float stepDistance;
            SDFSegment(local, float2(ends.x, level), float2(ends.y, level), stepDistance);
            distanceOutput = ends.y > ends.x ? min(distanceOutput, stepDistance) : distanceOutput;
        }
        return;
    }
    
    float acrossDistance;
    if (form == 1)  // Triangle
    {
        float triangleWave = 4.0 * abs(frac(waveX / safeWavelength - 0.25) - 0.5) - 1.0;
        float slope = 4.0 * amplitude / safeWavelength;
        acrossDistance = abs(local.y - amplitude * triangleWave) / sqrt(1.0 + slope * slope);
    }
    else if (form == 2)  // Square
    {
        // A flat per half wavelength (up, then down), joined by risers
        float halfPeriod = 0.5 * safeWavelength;
        float cell = floor(waveX / halfPeriod);
        float level = frac(cell * 0.5) < 0.25 ? amplitude : -amplitude;
        float riserGap = min(abs(waveX - cell * halfPeriod), abs(waveX - (cell + 1.0) * halfPeriod));
        acrossDistance = min(abs(local.y - level), length(float2(riserGap, max(abs(local.y) - abs(amplitude), 0.0))));
    }
    else  // Sine
    {
        float waveNumber = 6.28318531 / safeWavelength;
        float phase = waveX * waveNumber;
        float slope = amplitude * waveNumber * cos(phase);
        acrossDistance = abs(local.y - amplitude * sin(phase)) / sqrt(1.0 + slope * slope);
    }
    distanceOutput = length(float2(max(abs(local.x) - halfLength, 0.0), acrossDistance));
}

///<funchints>
/// <sg:ProviderKey>SDFWedge</sg:ProviderKey>
/// <sg:DisplayName>SDF Wedge</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/Wedge</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFWedge(float2 uv, float2 position, float halfAngle,
    out float distanceOutput)
{
    // An infinite wedge (a 'V' filled in) opening up (+y) from its apex at position, its sides half angle (radians) either side of +y
    //  - half angle > pi/2 = everything but a downward wedge (e.g. 3pi/4 = outside a square diamond's top corner)
    
    float2 local = uv - position;
    local.x = abs(local.x);
    float2 side = float2(sin(halfAngle), cos(halfAngle));
    float distance = dot(local, side) > 0.0 ? abs(local.x * side.y - local.y * side.x) : length(local);
    distanceOutput = atan2(local.x, local.y) < halfAngle ? -distance : distance;
}

///<funchints>
/// <sg:ProviderKey>SDFBox</sg:ProviderKey>
/// <sg:DisplayName>SDF Box</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/Box</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFBox(float2 uv, float2 position, float2 halfSize, float cornerRadius,
    out float distanceOutput)
{
    // Rectangle w/ optional rounded corners, its edges at +/- half size (exact distance, Inigo Quilez)
    // Corner radius defined inside the edges && capped at the shorter half size
    
    float2 safeHalfSize = max(abs(halfSize), 1e-5);
    float radius = clamp(cornerRadius, 0.0, min(safeHalfSize.x, safeHalfSize.y));
    float2 corner = abs(uv - position) - safeHalfSize + radius;
    distanceOutput = length(max(corner, 0.0)) + min(max(corner.x, corner.y), 0.0) - radius;
}

///<funchints>
/// <sg:ProviderKey>SDFTriangle</sg:ProviderKey>
/// <sg:DisplayName>SDF Triangle</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/Triangle</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFTriangle(float2 uv, float2 position, float2 halfSize,
    out float distanceOutput)
{
    // Isosceles triangle pointing up (+y), centred on its bounding box (exact distance, Inigo Quilez)
    // Apex at (0, +half size y), base corners at (+/- half size x, -half size y)
    
    float2 safeHalfSize = max(abs(halfSize), 1e-5);
    float2 side = float2(safeHalfSize.x, 2.0 * safeHalfSize.y); // apex -> base corner, w/ the apex at the origin + y flipped (pointing down)
    float2 local = uv - position;
    float2 folded = float2(abs(local.x), safeHalfSize.y - local.y);
    float2 toSide = folded - side * saturate(dot(folded, side) / dot(side, side));
    float2 toBase = folded - side * float2(saturate(folded.x / side.x), 1.0);
    float2 nearest = min(float2(dot(toSide, toSide), folded.y * side.x - folded.x * side.y), float2(dot(toBase, toBase), side.y - folded.y)); // (squared distance, inside test) ~ componentwise
    distanceOutput = nearest.y > 0.0 ? -sqrt(nearest.x) : sqrt(nearest.x);
}

///<funchints>
/// <sg:ProviderKey>SDFStar</sg:ProviderKey>
/// <sg:DisplayName>SDF Star</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/Star</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFStar(float2 uv, float2 position, float radius, float points, float innerRatio,
    out float distanceOutput)
{
    // A star w/ its tips at radius (one pointing up, +y) && its inner corners at radius x inner ratio
    //  - inner ratio cos(pi / points) = a regular polygon (straight sides);  above that, a 2 x points polygon
    //  - folded into half of one tip's sector, then the distance to the one edge (tip -> inner corner)
    
    float halfSector = 3.14159265 / max(round(points), 2.0);
    float safeRadius = max(abs(radius), 1e-5);
    float2 local = uv - position;
    float angle = atan2(local.x, local.y); // from +y
    angle = abs(angle - 2.0 * halfSector * round(angle / (2.0 * halfSector))); // 0 = a tip, half sector = an inner corner
    float2 folded = length(local) * float2(sin(angle), cos(angle));
    
    float2 tip = float2(0.0, safeRadius);
    float2 edge = safeRadius * max(innerRatio, 0.0) * float2(sin(halfSector), cos(halfSector)) - tip;
    float2 toPoint = folded - tip;
    float along = saturate(dot(toPoint, edge) / max(dot(edge, edge), 1e-8));
    float distance = length(toPoint - edge * along);
    distanceOutput = edge.x * toPoint.y - edge.y * toPoint.x < 0.0 ? -distance : distance;
}

///<funchints>
/// <sg:ProviderKey>SDFRegularPolygon</sg:ProviderKey>
/// <sg:DisplayName>SDF Regular Polygon</sg:DisplayName>
/// <sg:SearchCategory>LDM/SDFShapes/RegularPolygon</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SDFRegularPolygon(float2 uv, float2 position, float radius, float sides,
    out float distanceOutput)
{
    // Regular polygon w/ its corners at radius, one pointing up (+y) (exact distance)
    //  - AKA a star whose inner corners sit/lie in-line w/ its straight sides
    
    float safeSides = max(round(sides), 3.0);
    SDFStar(uv, position, radius, safeSides, cos(3.14159265 / safeSides), distanceOutput);
}

#endif

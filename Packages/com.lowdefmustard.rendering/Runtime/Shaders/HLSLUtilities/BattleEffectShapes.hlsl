#ifndef BATTLEEFFECTSHAPES_INCLUDED
#define BATTLEEFFECTSHAPES_INCLUDED

// Notes:
//  - Adapters from a battle effect item to the SDF library ~ one per BattleEffectShape subclass, named <ClassName>Distance
//  - The generated dispatch (Generated/BattleEffectDispatch.generated.hlsl) calls these by shape ID
//  - Parameters arrive already interpolated (start -> end at the item's eased progress), in the order the C# class writes them
//  - Returns a distance in the shape's local space (the composite scales it back to effect units)
//  - Parameter names follow the C# suffix convention (see BattleEffectShape)

#include "Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/SDFShapes.hlsl"

struct BattleEffectShapeInput
{
    float2 localPoint;
    float4 parametersA; // C# parameters 0-3
    float4 parametersB; // C# parameters 4-7
    float itemTime; // Seconds since the item started
    float progress; // Eased, 0-1 over the item's duration
};

float BattleEffectCircleDistance(BattleEffectShapeInput input)
{
    // Unit circle;  size comes from the item's scale
    
    float distance;
    SDFCircle(input.localPoint, float2(0.0, 0.0), 1.0, distance);
    return distance;
}

float BattleEffectDiamondDistance(BattleEffectShapeInput input)
{
    // Diamond w/ points at x = +/- 1;  A: (aspect ratio [height / width], -, -, -)
    
    float distance;
    SDFRhombus(input.localPoint, float2(0.0, 0.0), float2(1.0, input.parametersA.x), distance);
    return distance;
}

float BattleEffectSegmentDistance(BattleEffectShapeInput input)
{
    // Straight line from x = -1 to 1 (never negative ~ draw w/ the outline style)
    
    float distance;
    SDFSegment(input.localPoint, float2(-1.0, 0.0), float2(1.0, 0.0), distance);
    return distance;
}

float BattleEffectChevronDistance(BattleEffectShapeInput input)
{
    // '^' from x = -1 to 1;  A: (depth ratio [arm sweep / half width], -, -, -) (never negative ~ draw w/ the outline style)
    
    float distance;
    SDFChevron(input.localPoint, float2(0.0, 0.0), 1.0, input.parametersA.x, distance);
    return distance;
}

float BattleEffectSquiggleDistance(BattleEffectShapeInput input)
{
    // Along local x;  A: (half-length SU, wavelength SU, amplitude SU, scroll SU), B: (waveform, -, -, -) (never negative ~ draw w/ the outline style)
    
    float distance;
    SDFSquiggle(input.localPoint, float2(0.0, 0.0), input.parametersA.x, input.parametersA.y, input.parametersA.z, input.parametersA.w, input.parametersB.x, distance);
    return distance;
}

float BattleEffectLensDistance(BattleEffectShapeInput input)
{
    // Tips at x = +/- 1;  A: (aspect ratio [height / width], -, -, -)

    float distance;
    SDFLens(input.localPoint, float2(0.0, 0.0), float2(1.0, input.parametersA.x), distance);
    return distance;
}

float BattleEffectJaggedLineDistance(BattleEffectShapeInput input)
{
    // Along local x;  A: (half-length SU, cell width SU, amplitude SU, calm fraction), B: (seed, bump half width SU, variation fraction, scroll SU) (never negative ~ draw w/ the outline style)
    
    float distance;
    SDFJaggedLine(input.localPoint, float2(0.0, 0.0), input.parametersA.x, input.parametersA.y, input.parametersA.z, input.parametersA.w,input.parametersB.x, input.parametersB.y, input.parametersB.z, input.parametersB.w, distance);
    return distance;
}

float BattleEffectWedgeDistance(BattleEffectShapeInput input)
{
    // Opening up from its apex at (0, apex offset);  A: (half angle degrees, apex offset SU, -, -) (infinite ~ mirror y for a bow-tie;  > 90 degrees for the outside of a diamond)

    float distance;
    SDFWedge(input.localPoint, float2(0.0, input.parametersA.y), radians(input.parametersA.x), distance);
    return distance;
}

float BattleEffectBoxDistance(BattleEffectShapeInput input)
{
    // Edges at x = +/- 1;  A: (aspect ratio [height / width], corner radius SU, -, -)

    float distance;
    SDFBox(input.localPoint, float2(0.0, 0.0), float2(1.0, input.parametersA.x), input.parametersA.y, distance);
    return distance;
}

float BattleEffectTriangleDistance(BattleEffectShapeInput input)
{
    // Pointing up, base corners at x = +/- 1;  A: (aspect ratio [height / width], -, -, -)

    float distance;
    SDFTriangle(input.localPoint, float2(0.0, 0.0), float2(1.0, input.parametersA.x), distance);
    return distance;
}

float BattleEffectPolygonDistance(BattleEffectShapeInput input)
{
    // Corners at radius 1, one pointing up;  A: (sides, -, -, -)

    float distance;
    SDFRegularPolygon(input.localPoint, float2(0.0, 0.0), 1.0, input.parametersA.x, distance);
    return distance;
}

float BattleEffectStarDistance(BattleEffectShapeInput input)
{
    // Tips at radius 1, one pointing up;  A: (points, inner ratio [inner corner radius / tip radius], -, -)

    float distance;
    SDFStar(input.localPoint, float2(0.0, 0.0), 1.0, input.parametersA.x, input.parametersA.y, distance);
    return distance;
}

#endif

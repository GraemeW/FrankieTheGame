#include "ShaderApiReflectionSupport.hlsl"

#ifndef BATTLEEFFECTCOMPOSITE_INCLUDED
#define BATTLEEFFECTCOMPOSITE_INCLUDED

// Notes:
//  - This node is used in BattleEffect graph only (not general utility)
//  - BattleEffect ShaderGraph's blackboard properties are read directly so they are not wired into the node (&& thus:  property names / packing must match ShaderPropertyRefs + BattleEffectShape)
//  - Shape types defined below must match BattleEffectShapeType
//  - Item cap must match ShaderPropertyRefs.battleEffectItemCapacity
//  - Output is straight (non-premultiplied) colour + alpha, for the UI target's default alpha blend

#include "Assets/Scripts/Rendering/Shaders/HLSLUtilities/SDFShapes.hlsl"
#include "Assets/Scripts/Rendering/Shaders/HLSLUtilities/TimeUtility.hlsl"

// IDE-only declarations ~ in the shader, these come from the graph's blackboard
#ifdef __RESHARPER__
float _Phase, _ItemCount, _EdgeSoftness, _GlowWidth;
float4 _Item0Header, _Item0ParamsA, _Item0ParamsB, _Item0Colour;
float4 _Item1Header, _Item1ParamsA, _Item1ParamsB, _Item1Colour;
float4 _Item2Header, _Item2ParamsA, _Item2ParamsB, _Item2Colour;
float4 _Item3Header, _Item3ParamsA, _Item3ParamsB, _Item3Colour;
float4 _Item4Header, _Item4ParamsA, _Item4ParamsB, _Item4Colour;
float4 _Item5Header, _Item5ParamsA, _Item5ParamsB, _Item5Colour;
float4 _Item6Header, _Item6ParamsA, _Item6ParamsB, _Item6Colour;
float4 _Item7Header, _Item7ParamsA, _Item7ParamsB, _Item7Colour;
#endif

#define BATTLE_EFFECT_MAX_ITEMS 8
#define BATTLE_EFFECT_SHAPE_CIRCLE 0
#define BATTLE_EFFECT_SHAPE_SQUIGGLE 1

// Header: (shape type, start time, duration, variant) w/ variant = filled (circle) / horizontal (squiggle)
float BattleEffectItemDistance(float2 uv, float4 header, float4 paramsA, float4 paramsB, float itemTime, float progress)
{
    int shapeType = (int)round(header.x);
    bool isVariant = header.w > 0.5;
    float distance = 1e5;

    if (shapeType == BATTLE_EFFECT_SHAPE_CIRCLE)
    {
        // A: (position.xy, radius start, radius end)
        // B: (thickness start, thickness end, -, -)
        // Note:  thickness may go negative to fade a ring out through the glow
        float circleDistance;
        SDFCircle(uv, paramsA.xy, lerp(paramsA.z, paramsA.w, progress), circleDistance);
        if (isVariant) { distance = circleDistance; }
        else { SDFOnion(circleDistance, lerp(paramsB.x, paramsB.y, progress), distance); }
    }
    else if (shapeType == BATTLE_EFFECT_SHAPE_SQUIGGLE)
    {
        // A: (travel start, travel end, width, frequency)
        // B: (wave min, wave max, phase speed, thickness)
        float squiggleDistance;
        SDFSquiggle(uv, isVariant, lerp(paramsA.x, paramsA.y, progress), paramsA.z, paramsA.w, itemTime * paramsB.z, paramsB.xy, sin(itemTime), squiggleDistance);
        distance = squiggleDistance - paramsB.w;
    }
    return distance;
}

///<funchints>
/// <sg:ProviderKey>BattleEffectComposite</sg:ProviderKey>
/// <sg:DisplayName>Battle Effect Composite</sg:DisplayName>
/// <sg:SearchCategory>LDM/BattleEffects/Composite</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void BattleEffectComposite(float2 uv,
    out float3 colorOutput, out float alphaOutput)
{
    float4 headers[BATTLE_EFFECT_MAX_ITEMS] = { _Item0Header, _Item1Header, _Item2Header, _Item3Header, _Item4Header, _Item5Header, _Item6Header, _Item7Header };
    float4 paramsA[BATTLE_EFFECT_MAX_ITEMS] = { _Item0ParamsA, _Item1ParamsA, _Item2ParamsA, _Item3ParamsA, _Item4ParamsA, _Item5ParamsA, _Item6ParamsA, _Item7ParamsA };
    float4 paramsB[BATTLE_EFFECT_MAX_ITEMS] = { _Item0ParamsB, _Item1ParamsB, _Item2ParamsB, _Item3ParamsB, _Item4ParamsB, _Item5ParamsB, _Item6ParamsB, _Item7ParamsB };
    float4 colours[BATTLE_EFFECT_MAX_ITEMS] = { _Item0Colour, _Item1Colour, _Item2Colour, _Item3Colour, _Item4Colour, _Item5Colour, _Item6Colour, _Item7Colour };

    float elapsedTime, sineElapsedTime, cosineElapsedTime;
    TimeSinceInitialization(_Phase, elapsedTime, sineElapsedTime, cosineElapsedTime);

    // Edge = antialiasing (in screen pixels) + optional glow (in UV)
    float edgeHalfWidth = max(length(fwidth(uv)) * _EdgeSoftness, 1e-5); // Derivatives taken once, outside the loop (undefined under per-pixel branching)
    float edgeOuter = edgeHalfWidth + max(_GlowWidth, 0.0);

    int itemCount = (int)min(_ItemCount, BATTLE_EFFECT_MAX_ITEMS);
    float4 accumulated = 0.0; // Premultiplied;  later items paint over earlier ones

    [unroll]
    for (int i = 0; i < BATTLE_EFFECT_MAX_ITEMS; i++)
    {
        if (i >= itemCount) { break; }

        float4 header = headers[i];
        float itemTime = elapsedTime - header.y;
        if (itemTime < 0.0 || itemTime > header.z) { continue; }
        float progress = header.z > 0.0 ? itemTime / header.z : 1.0;

        float distance = BattleEffectItemDistance(uv, header, paramsA[i], paramsB[i], itemTime, progress);
        float coverage = 1.0 - smoothstep(-edgeHalfWidth, edgeOuter, distance);

        float itemAlpha = colours[i].a * coverage;
        accumulated = float4(colours[i].rgb * itemAlpha, itemAlpha) + accumulated * (1.0 - itemAlpha);
    }

    alphaOutput = accumulated.a;
    colorOutput = alphaOutput > 0.0 ? accumulated.rgb / alphaOutput : 0.0;
}

#endif

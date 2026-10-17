#include "ShaderApiReflectionSupport.hlsl"

#ifndef BATTLEEFFECTCOMPOSITE_INCLUDED
#define BATTLEEFFECTCOMPOSITE_INCLUDED

// Notes:
//  - This node is used in BattleEffect graph only (not general utility)
//  - BattleEffect ShaderGraph's blackboard properties are wired into the node's inputs (property names must match ShaderPropertyRefs)
//      - Items are read from the item data texture (one row per item, columns per BattleEffectDataLayout ~ mirrored by the generated dispatch)
//      - Per item:  time window -> ease -> interpolate (+ jitter) -> transform -> mirror -> repetition -> elongation -> shape (generated dispatch) -> style -> coverage (x flicker) -> composite
//  - Output is straight (non-premultiplied) colour + alpha
//  - Shapes are measured in units of the effect's shorter side (see SDFAspectFit) - they keep their proportions on any slide size

#include "Assets/Scripts/Rendering/Shaders/HLSLUtilities/SDFDomain.hlsl"
#include "Assets/Scripts/Rendering/Shaders/HLSLUtilities/TimeUtility.hlsl"
#include "Assets/Scripts/Rendering/Shaders/HLSLUtilities/ColourUtility.hlsl"
#include "Assets/Scripts/Rendering/Shaders/HLSLUtilities/Generated/BattleEffectDispatch.generated.hlsl"

#ifdef __RESHARPER__ // IDE-only declaration - in shader, this comes from render pipeline
struct UnityTexture2D
{
    Texture2D tex;
};
#endif

float4 LoadBattleEffectTexel(UnityTexture2D itemData, int item, int column)
{
    return LOAD_TEXTURE2D(itemData.tex, int2(column, item));
}

struct BattleEffectModifiers
{
    // Per-item stages (in order)
    float2 jitterOffset;
    float flickerAlpha;
    float pulseScale;
};

bool TryGetBattleEffectItemTime(UnityTexture2D itemData, int item, float elapsedTime,
    out int shapeId, out float itemTime, out float progress)
{
    // Half-open [start, end), w/ a little tolerance
    
    float4 header = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_HEADER); // (shape ID, start time, duration, easing)
    shapeId = (int)round(header.x);
    itemTime = elapsedTime - header.y;
    progress = 0.0;
    if (itemTime < -1e-4 || itemTime >= header.z - 1e-4) { return false; } // ensure stepped frames on an item's boundaries land on exactly one item

    EaseProgress(header.z > 0.0 ? itemTime / header.z : 1.0, header.w, progress);
    return true;
}

BattleEffectModifiers GetBattleEffectModifiers(UnityTexture2D itemData, int item, float elapsedTime, float disableFlicker, float disableJitter)
{
    // On the effect's clock (not the item's) - items w/ the same settings stay in sync
    
    float4 flicker = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_FLICKER); // (rate, depth, phase, -)
    float4 jitter = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_JITTER); // (rate, amount.x, amount.y, seed)
    float4 pulse = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_PULSE); // (rate, amount, phase, -)
    float4 style = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_STYLE); // (style, jitter mode, jitter phase, -)

    BattleEffectModifiers modifiers;
    Jitter(elapsedTime, disableJitter > 0.5 ? 0.0 : jitter.x, jitter.yz, jitter.w, style.y, style.z, modifiers.jitterOffset);
    Flicker(elapsedTime, disableFlicker > 0.5 ? 0.0 : flicker.x, flicker.y, flicker.z, modifiers.flickerAlpha);
    Pulse(elapsedTime, pulse.x, pulse.y, pulse.z, modifiers.pulseScale);
    return modifiers;
}

BattleEffectShapeInput GetBattleEffectShapeInput(UnityTexture2D itemData, int item, float2 fittedUV, float itemTime, float progress, BattleEffectModifiers modifiers,
    out float distanceScale)
{
    // Effect units -> the shape's local space (+ its interpolated parameters)
    
    float4 position = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_POSITION);
    float4 scale = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_SCALE);
    float4 rotationOutline = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_ROTATIONOUTLINE);

    BattleEffectShapeInput shapeInput;
    SDFTransform(fittedUV, lerp(position.xy, position.zw, progress) + modifiers.jitterOffset, 
        lerp(rotationOutline.x, rotationOutline.y, progress),
        lerp(scale.xy, scale.zw, progress) * modifiers.pulseScale, shapeInput.localPoint, distanceScale);
    shapeInput.parametersA = lerp(LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_PARAMETERSASTART), LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_PARAMETERSAEND), progress);
    shapeInput.parametersB = lerp(LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_PARAMETERSBSTART), LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_PARAMETERSBEND), progress);
    shapeInput.itemTime = itemTime;
    shapeInput.progress = progress;
    return shapeInput;
}

float GetBattleEffectCopyDistance(int shapeId, BattleEffectShapeInput shapeInput, float2 copyPoint, float2 elongation)
{
    SDFElongate(copyPoint, elongation, shapeInput.localPoint);
    return BattleEffectShapeDistance(shapeId, shapeInput);
}

float GetBattleEffectLocalDistance(UnityTexture2D itemData, int item, int shapeId, BattleEffectShapeInput shapeInput, float progress)
{
    // Mirror -> repetition -> elongation -> shape, at the nearest copies (in local space)
    
    float4 repetition = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_REPETITION); // (repetition, count x, count y, mirror [x = 1, y = 2])
    float4 spacingRange = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_SPACING);
    float4 elongationRange = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_ELONGATION);
    float2 spacing = lerp(spacingRange.xy, spacingRange.zw, progress);
    float2 elongation = lerp(elongationRange.xy, elongationRange.zw, progress);

    float2 mirroredPoint;
    SDFMirror(shapeInput.localPoint, fmod(repetition.w, 2.0), floor(repetition.w / 2.0), mirroredPoint);

    int repetitionMode = (int)round(repetition.x);
    if (repetitionMode == BATTLE_EFFECT_REPETITION_GRID)
    {
        float2 nearestPoint, neighbourXPoint, neighbourYPoint, neighbourXYPoint;
        SDFRepeatGrid(mirroredPoint, spacing, repetition.yz, nearestPoint, neighbourXPoint, neighbourYPoint, neighbourXYPoint);
        return min(min(GetBattleEffectCopyDistance(shapeId, shapeInput, nearestPoint, elongation), GetBattleEffectCopyDistance(shapeId, shapeInput, neighbourXPoint, elongation)),
            min(GetBattleEffectCopyDistance(shapeId, shapeInput, neighbourYPoint, elongation), GetBattleEffectCopyDistance(shapeId, shapeInput, neighbourXYPoint, elongation)));
    }
    if (repetitionMode != BATTLE_EFFECT_REPETITION_LINE && repetitionMode != BATTLE_EFFECT_REPETITION_CROSS)
    {
        return GetBattleEffectCopyDistance(shapeId, shapeInput, mirroredPoint, elongation);
    }

    // Line (+ cross:  a second line across it)
    float2 nearestPoint, neighbourPoint;
    SDFRepeatLine(mirroredPoint, spacing, repetition.y, nearestPoint, neighbourPoint);
    float distance = min(GetBattleEffectCopyDistance(shapeId, shapeInput, nearestPoint, elongation), GetBattleEffectCopyDistance(shapeId, shapeInput, neighbourPoint, elongation));
    if (repetitionMode == BATTLE_EFFECT_REPETITION_CROSS)
    {
        SDFRepeatLine(mirroredPoint, float2(-spacing.y, spacing.x), repetition.y, nearestPoint, neighbourPoint);
        distance = min(distance, min(GetBattleEffectCopyDistance(shapeId, shapeInput, nearestPoint, elongation), GetBattleEffectCopyDistance(shapeId, shapeInput, neighbourPoint, elongation)));
    }
    return distance;
}

float ApplyBattleEffectStyle(UnityTexture2D itemData, int item, float distance, float progress)
{
    // Fill = the distance as-is
    // Outline = an onion of the interpolated thickness
    
    float4 style = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_STYLE); // (style, jitter mode, jitter phase, -)
    if ((int)round(style.x) != BATTLE_EFFECT_STYLE_OUTLINE) { return distance; }

    float4 rotationOutline = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_ROTATIONOUTLINE);
    float outlineDistance;
    SDFOnion(distance, lerp(rotationOutline.z, rotationOutline.w, progress), outlineDistance);
    return outlineDistance;
}

float4 CompositeBattleEffectOver(float4 accumulated, float3 colour, float alpha)
{
    // Premultiplied 'over':  the new item paints over everything drawn so far
    
    return float4(colour * alpha, alpha) + accumulated * (1.0 - alpha);
}


///<funchints>
/// <sg:ProviderKey>BattleEffectComposite</sg:ProviderKey>
/// <sg:DisplayName>Battle Effect Composite</sg:DisplayName>
/// <sg:SearchCategory>LDM/BattleEffects/Composite</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void BattleEffectComposite(float2 uv, float isPreview, float previewTime, 
    float itemCount, UnityTexture2D itemData,
    float4 tintColourA, float4 tintColourB, float4 tintSettings,
    float phase, float frameRate,
    float edgeSoftness, float glowWidth,  
    float disableFlicker, float disableJitter,
    out float3 colorOutput, out float alphaOutput)
{
    // Effect clock (stepped)
    float elapsedTime, sineElapsedTime, cosineElapsedTime;
    TimeSinceInitialization(phase, elapsedTime, sineElapsedTime, cosineElapsedTime);
    if (isPreview > 0.5) { elapsedTime = previewTime; } // Editor preview drives time directly
    SteppedTime(elapsedTime, frameRate, elapsedTime);

    // Aspect-fitted space ::
    //  edge = antialiasing (in screen pixels) + optional glow (in fitted units)
    // Derivatives taken via SDFAspectFit/fwidth outside the loop (otherwise undefined under per-pixel branching)
    float2 fittedUV;
    SDFAspectFit(uv, fittedUV);
    float edgeHalfWidth = max(length(fwidth(fittedUV)) * edgeSoftness, 1e-5);
    float edgeOuter = edgeHalfWidth + max(glowWidth, 0.0);

    float4 accumulated = 0.0; // Premultiplied - later items paint over earlier ones
    int count = (int)itemCount;
    [loop]
    for (int i = 0; i < count; i++)
    {
        int shapeId;
        float itemTime, progress;
        if (!TryGetBattleEffectItemTime(itemData, i, elapsedTime, shapeId, itemTime, progress)) { continue; }

        BattleEffectModifiers modifiers = GetBattleEffectModifiers(itemData, i, elapsedTime, disableFlicker, disableJitter);
        float distanceScale;
        BattleEffectShapeInput shapeInput = GetBattleEffectShapeInput(itemData, i, fittedUV, itemTime, progress, modifiers, distanceScale);
        float distance = GetBattleEffectLocalDistance(itemData, i, shapeId, shapeInput, progress) * distanceScale;
        distance = ApplyBattleEffectStyle(itemData, i, distance, progress);

        float coverage = 1.0 - smoothstep(-edgeHalfWidth, edgeOuter, distance);
        float4 colour = LoadBattleEffectTexel(itemData, i, BATTLE_EFFECT_LAYOUT_COLOUR);
        accumulated = CompositeBattleEffectOver(accumulated, colour.rgb, colour.a * coverage * modifiers.flickerAlpha);
    }

    // Overall tint (solid / gradient, in effect units;  scrolls on the stepped clock)
    float4 tint;
    ColourGradient(fittedUV, tintSettings.x, tintColourA, tintColourB, tintSettings.y, tintSettings.z, elapsedTime * tintSettings.w, tint);
    alphaOutput = accumulated.a * tint.a;
    colorOutput = (accumulated.a > 0.0 ? accumulated.rgb / accumulated.a : 0.0) * tint.rgb;
}

#endif

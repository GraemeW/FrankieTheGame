#include "ShaderApiReflectionSupport.hlsl"

#ifndef BATTLEEFFECTCOMPOSITE_INCLUDED
#define BATTLEEFFECTCOMPOSITE_INCLUDED

// Notes:
//  - This node is used in BattleEffect graph only (not general utility)
//  - BattleEffect ShaderGraph's blackboard properties are wired into the node's inputs (property names must match ShaderPropertyRefs)
//      - Items are read from the item data texture (one row per item, columns per BattleEffectDataLayout ~ mirrored by the generated dispatch)
//      - Per item:
//          time window -> ease -> progress steps -> beat -> interpolate (+ beat offset, jitter)
//          -> transform -> mirror (+ spread beat offset) -> repetition -> shape rotation -> elongation
//          -> shape (generated dispatch) -> style (fill + outline:  the fill first) -> clip -> coverage (x flicker x stripes)
//          -> colour (palette [+ shade], outline colour + edge gradient) -> composite (over / erase)
//  - Logic Splits:
//      - BattleEffectItemData:  reading an item's settings (timing, beats, modifiers, colours, stripes)
//      - BattleEffectRepetition:  an item's shape at its nearest repetition copies (+ which copy)
//      - here:  the transform, style + clip, compositing + the node itself
//  - Fit mode (BattleEffectFit) sets the effect units
//      - aspect fit:  units of the effect's shorter side (see SDFAspectFit) - shapes keep their proportions on any slide size
//      - stretch:  the element's own 0-1 UV - fills it (e.g. full screen), shapes stretch w/ it
//  - Output is straight (non-premultiplied) colour + alpha

#include "Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/BattleEffectItemData.hlsl"
#include "Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/BattleEffectRepetition.hlsl"
#include "Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/ColourUtility.hlsl"

BattleEffectShapeInput GetBattleEffectShapeInput(UnityTexture2D itemData, int item, float2 fittedUV, float itemTime, float progress, int beat, BattleEffectModifiers modifiers,
    out float distanceScale, out float2 spreadOffset)
{
    // Effect units -> the shape's local space (+ its interpolated parameters && relevant offsetting)
    //  - position:  its range + the current beat's offset + jitter;  scale:  its range x pulse;  item rotation:  its range
    //  - spread beat offsets (mirrored items) skip the position:  returned in local space instead (for after the fold)

    float4 position = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_POSITION);
    float4 scale = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_SCALE);
    float4 rotation = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_ROTATION);
    bool isSpread = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_REPETITION).w >= 3.5; // mirror bit 4
    float2 beatOffset = GetBattleEffectBeatPair(itemData, item, beat, BATTLE_EFFECT_LAYOUT_BEATOFFSETS01);
    float2 itemPosition = lerp(position.xy, position.zw, progress) + (isSpread ? 0.0 : beatOffset) + modifiers.jitterOffset;
    float2 itemScale = lerp(scale.xy, scale.zw, progress) * modifiers.pulseScale;
    float itemAngle = lerp(rotation.x, rotation.y, progress);

    BattleEffectShapeInput shapeInput;
    SDFTransform(fittedUV, itemPosition, itemAngle, itemScale, shapeInput.localPoint, distanceScale);
    spreadOffset = 0.0;
    if (isSpread)
    {
        float spreadDistanceScale;
        SDFTransform(itemPosition + beatOffset, itemPosition, itemAngle, itemScale, spreadOffset, spreadDistanceScale);
    }
    shapeInput.parametersA = lerp(LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_PARAMETERSASTART), LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_PARAMETERSAEND), progress);
    shapeInput.parametersB = lerp(LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_PARAMETERSBSTART), LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_PARAMETERSBEND), progress);
    shapeInput.itemTime = itemTime;
    shapeInput.progress = progress;
    return shapeInput;
}

float ApplyBattleEffectOutline(UnityTexture2D itemData, int item, float distance, float progress)
{
    // An onion of the interpolated outline thickness (the outline + fill and outline styles' stroke)

    float4 style = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_STYLE);
    float outlineDistance;
    SDFOnion(distance, lerp(style.z, style.w, progress), outlineDistance);
    return outlineDistance;
}

float ApplyBattleEffectClip(UnityTexture2D itemData, int item, float distance, float2 localPoint, float distanceScale, float progress)
{
    // Intersects what the item draws (after its style) w/ a box / diamond in its local space, centred on its position + the clip offset, turned by the clip angle

    float4 clip = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_CLIP);
    int clipShape = (int)round(clip.x);
    if (clipShape == BATTLE_EFFECT_CLIPSHAPE_NONE) { return distance; }

    float4 clipSizeRange = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_CLIPSIZE);
    float4 clipOffsetRange = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_CLIPOFFSET);
    float2 halfSize = lerp(clipSizeRange.xy, clipSizeRange.zw, progress);
    float2 offsetPoint = localPoint - lerp(clipOffsetRange.xy, clipOffsetRange.zw, progress);
    float2 clipPoint = float2(cos(clip.y) * offsetPoint.x + sin(clip.y) * offsetPoint.y, -sin(clip.y) * offsetPoint.x + cos(clip.y) * offsetPoint.y);
    float clipDistance;
    if (clipShape == BATTLE_EFFECT_CLIPSHAPE_DIAMOND) { SDFRhombus(clipPoint, float2(0.0, 0.0), halfSize, clipDistance); }
    else { SDFBox(clipPoint, float2(0.0, 0.0), halfSize, 0.0, clipDistance); }
    return max(distance, clipDistance * distanceScale);
}

float GetBattleEffectCoverage(float distance, float edgeHalfWidth, float edgeOuter)
{
    // Antialiased (+ glow) coverage of a distance in effect units

    return 1.0 - smoothstep(-edgeHalfWidth, edgeOuter, distance);
}

float GetBattleEffectShadeWeight(UnityTexture2D itemData, int item, float2 copyPoint, float distanceScale, float progress, float edgeHalfWidth)
{
    // How much of the shaded side (0-1) at the copy's point:  past the line (the copy's centre + the offset, along the shaded direction, which turns w/ the shape rotation)
    //  - hard:  a step;  smooth:  antialiased across the line (matching the shapes' edges)

    float4 shade = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_SHADE); // (mode [0 = off, 1 = hard, 2 = smooth], fill step, outline step, -)
    if (shade.x < 0.5) { return 0.0; }
    
    float4 shadeLine = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_SHADELINE); // (angle start, end, offset start, end)
    float4 rotation = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_ROTATION);
    float angle = lerp(shadeLine.x, shadeLine.y, progress) + lerp(rotation.z, rotation.w, progress);
    float past = (dot(copyPoint, float2(cos(angle), sin(angle))) - lerp(shadeLine.z, shadeLine.w, progress)) * distanceScale; // effect units
    return shade.x > 1.5 ? 1.0 - GetBattleEffectCoverage(past, edgeHalfWidth, edgeHalfWidth) : (past > 0.0 ? 1.0 : 0.0);
}

float4 CompositeBattleEffectErase(float4 accumulated, float alpha)
{
    // Punches the item's coverage out of everything drawn so far

    return accumulated * (1.0 - alpha);
}

float4 CompositeBattleEffectOver(float4 accumulated, float3 colour, float alpha)
{
    // Premultiplied 'over':  the new item paints over everything drawn so far

    return float4(colour * alpha, alpha) + accumulated * (1.0 - alpha);
}

float4 CompositeBattleEffectItem(UnityTexture2D itemData, int item, float4 accumulated, float elapsedTime, float2 fittedUV, float edgeHalfWidth, float edgeOuter,
    float disableFlicker, float disableJitter)
{
    // One item (if it's in its time window), composited onto everything drawn before it

    int shapeId;
    float itemTime, progress;
    if (!TryGetBattleEffectItemTime(itemData, item, elapsedTime, shapeId, itemTime, progress)) { return accumulated; }

    // Geometry:  the shape's distance (effect units) at its nearest copy
    BattleEffectModifiers modifiers = GetBattleEffectModifiers(itemData, item, elapsedTime, itemTime, disableFlicker, disableJitter);
    int beat = GetBattleEffectBeat(itemData, item, itemTime);
    float distanceScale, copyIndex;
    float2 spreadOffset, copyPoint;
    BattleEffectShapeInput shapeInput = GetBattleEffectShapeInput(itemData, item, fittedUV, itemTime, progress, beat, modifiers, distanceScale, spreadOffset);
    float shapeDistance = GetBattleEffectLocalDistance(itemData, item, shapeId, shapeInput, spreadOffset, progress, beat, copyIndex, copyPoint) * distanceScale;

    // Style + colour
    int style = GetBattleEffectStyle(itemData, item);
    bool isErase = (int)round(LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_STYLE).y) == BATTLE_EFFECT_BLEND_ERASE;
    float stripeMask = GetBattleEffectStripeMask(itemData, item, fittedUV);
    float paletteIndex = GetBattleEffectPaletteIndex(itemData, item, elapsedTime, itemTime, copyIndex, beat);
    float shadeWeight = GetBattleEffectShadeWeight(itemData, item, copyPoint, distanceScale, progress, edgeHalfWidth);
    float2 shadeSteps = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_SHADE).yz; // (fill, outline) on the shaded side
    float4 colour = GetBattleEffectPaletteColour(itemData, item, paletteIndex);
    if (shadeWeight > 0.0) { colour = lerp(colour, GetBattleEffectPaletteColour(itemData, item, paletteIndex + shadeSteps.x), shadeWeight); }

    // Fill (fill + fill and outline styles):  stripes + the erase blend apply to it
    if (style != BATTLE_EFFECT_STYLEMODE_OUTLINE)
    {
        float fillDistance = ApplyBattleEffectClip(itemData, item, shapeDistance, shapeInput.localPoint, distanceScale, progress);
        float fillAlpha = colour.a * GetBattleEffectCoverage(fillDistance, edgeHalfWidth, edgeOuter) * stripeMask * modifiers.flickerAlpha;
        accumulated = isErase ? CompositeBattleEffectErase(accumulated, fillAlpha) : CompositeBattleEffectOver(accumulated, colour.rgb, fillAlpha);
    }
    if (style == BATTLE_EFFECT_STYLEMODE_FILL) { return accumulated; }

    // Outline (outline + fill and outline styles):  over the fill;  stripes + erase only when it's the item's whole style
    bool isOutlineOnly = style == BATTLE_EFFECT_STYLEMODE_OUTLINE;
    float outlineDistance = ApplyBattleEffectClip(itemData, item, ApplyBattleEffectOutline(itemData, item, shapeDistance, progress), shapeInput.localPoint, distanceScale, progress);
    float4 strokeColour = GetBattleEffectStrokeColour(itemData, item, GetBattleEffectPaletteColour(itemData, item, paletteIndex), paletteIndex, shapeDistance, progress);
    if (shadeWeight > 0.0)
    {
        float shadedIndex = paletteIndex + shadeSteps.y;
        float4 shadedStroke = GetBattleEffectStrokeColour(itemData, item, GetBattleEffectPaletteColour(itemData, item, shadedIndex), shadedIndex, shapeDistance, progress);
        strokeColour = lerp(strokeColour, shadedStroke, shadeWeight);
    }
    float strokeAlpha = strokeColour.a * GetBattleEffectCoverage(outlineDistance, edgeHalfWidth, edgeOuter) * (isOutlineOnly ? stripeMask : 1.0) * modifiers.flickerAlpha;
    return isOutlineOnly && isErase ? CompositeBattleEffectErase(accumulated, strokeAlpha) : CompositeBattleEffectOver(accumulated, strokeColour.rgb, strokeAlpha);
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
    float disableFlicker, float disableJitter, float fitMode,
    out float3 colorOutput, out float alphaOutput)
{
    // Effect clock (stepped)
    float elapsedTime, sineElapsedTime, cosineElapsedTime;
    TimeSinceInitialization(phase, elapsedTime, sineElapsedTime, cosineElapsedTime);
    if (isPreview > 0.5) { elapsedTime = previewTime; } // Editor preview drives time directly
    SteppedTime(elapsedTime, frameRate, elapsedTime);

    // Fitted space
    //  - Edge = antialiasing (in screen pixels) + optional glow (in fitted units)
    //  - Derivatives are taken via SDFAspectFit/fwidth outside the loop (otherwise undefined under per-pixel branching)
    //  - Fit mode is a select (not a branch), so derivatives stay defined
    float2 fittedUV;
    SDFAspectFit(uv, fittedUV);
    fittedUV = (int)round(fitMode) == BATTLE_EFFECT_FIT_STRETCH ? uv : fittedUV;
    float edgeHalfWidth = max(length(fwidth(fittedUV)) * edgeSoftness, 1e-5);
    float edgeOuter = edgeHalfWidth + max(glowWidth, 0.0);

    float4 accumulated = 0.0; // Premultiplied - later items paint over earlier ones
    int count = (int)itemCount;
    [loop]
    for (int i = 0; i < count; i++)
    {
        accumulated = CompositeBattleEffectItem(itemData, i, accumulated, elapsedTime, fittedUV, edgeHalfWidth, edgeOuter, disableFlicker, disableJitter);
    }

    // Overall tint (solid / gradient, in effect units;  scrolls on the stepped clock)
    float4 tint;
    ColourGradient(fittedUV, tintSettings.x, tintColourA, tintColourB, tintSettings.y, tintSettings.z, elapsedTime * tintSettings.w, tint);
    alphaOutput = accumulated.a * tint.a;
    colorOutput = (accumulated.a > 0.0 ? accumulated.rgb / accumulated.a : 0.0) * tint.rgb;
}

#endif

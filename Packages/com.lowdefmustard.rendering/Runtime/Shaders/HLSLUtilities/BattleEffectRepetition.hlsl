#ifndef BATTLEEFFECTREPETITION_INCLUDED
#define BATTLEEFFECTREPETITION_INCLUDED

// Notes:
//  - BattleEffect graph only (via BattleEffectComposite):  an item's shape, at its nearest repetition copies
//      - mirror -> repetition -> shape rotation -> elongation -> shape (generated dispatch)
//  - Each repetition family evaluates its nearest + neighbouring copies + keeps the closest - its copy index (the copy's number) feeds:
//      - per-copy colour:  line / radial = along;  grid = column + row
//      - per-copy size:  copy n = copy scale ratio ^ n, about its own centre (warning:  copies that outgrow ~1.5x spacings // past neighbours' cells can clip)

#include "Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/BattleEffectItemData.hlsl"

struct BattleEffectCopyShape
{
    // What every copy draws (only the point it's evaluated at differs)
    int shapeId;
    BattleEffectShapeInput shapeInput;
    float2 shapeRotation; // (sine, cosine)
    float2 elongation;
    float copyScaleRatio;
};

float GetBattleEffectCopyDistance(BattleEffectCopyShape copyShape, float2 copyPoint)
{
    // Shape rotation turns the shape within its copy (about the copy's centre), then elongation stretches it

    float2 rotation = copyShape.shapeRotation;
    float2 turnedPoint = float2(rotation.y * copyPoint.x + rotation.x * copyPoint.y, -rotation.x * copyPoint.x + rotation.y * copyPoint.y);
    BattleEffectShapeInput shapeInput = copyShape.shapeInput;
    SDFElongate(turnedPoint, copyShape.elongation, shapeInput.localPoint);
    return BattleEffectShapeDistance(copyShape.shapeId, shapeInput);
}

float GetBattleEffectScaledCopyDistance(BattleEffectCopyShape copyShape, float2 copyPoint, float copyNumber)
{
    // The copy at its own size (ratio ^ its number)

    float copyScale = copyShape.copyScaleRatio == 1.0 ? 1.0 : max(pow(max(copyShape.copyScaleRatio, 0.0), copyNumber), 1e-4);
    return GetBattleEffectCopyDistance(copyShape, copyPoint / copyScale) * copyScale;
}

void KeepNearestBattleEffectCopy(float candidateDistance, float candidateIndex, inout float distance, inout float copyIndex)
{
    if (candidateDistance < distance) { distance = candidateDistance; copyIndex = candidateIndex; }
}

float GetBattleEffectLineCopyIndex(float2 samplePoint, float2 copyPoint, float2 spacing, float count)
{
    // A copy's number along a line repetition, from where its point sits (sample point - copy point = the copy's offset)

    float2 copyOffset = samplePoint - copyPoint;
    float stepsAlong = dot(copyOffset, spacing) / max(dot(spacing, spacing), 1e-8);
    return round(stepsAlong + (max(round(count), 1.0) - 1.0) * 0.5);
}

float GetBattleEffectGridCopyIndex(float2 samplePoint, float2 copyPoint, float2 spacing, float2 count, bool isStaggered)
{
    // Column + row (so a two-colour palette alternates like a checkerboard);  staggered rows hold one fewer copy every other row

    float2 safeSpacing = float2(abs(spacing.x) < 1e-5 ? 1e-5 : spacing.x, abs(spacing.y) < 1e-5 ? 1e-5 : spacing.y);
    float2 copies = max(round(count), 1.0);
    float2 stepsAcross = (samplePoint - copyPoint) / safeSpacing;
    float row = round(stepsAcross.y + (copies.y - 1.0) * 0.5);
    float rowCopies = isStaggered ? copies.x - fmod(row, 2.0) : copies.x;
    float column = round(stepsAcross.x + (rowCopies - 1.0) * 0.5);
    return column + row;
}

float GetBattleEffectLineCopiesDistance(BattleEffectCopyShape copyShape, float2 mirroredPoint, float2 spacing, float count, bool isCross,
    out float copyIndex)
{
    // Line:  count copies along the spacing vector (the nearest + its neighbour)
    // Cross:  + a second line across it (the same count + spacing, turned a quarter)

    float distance = 1e5;
    copyIndex = 0.0;
    float2 lineSpacings[2] = { spacing, float2(-spacing.y, spacing.x) };
    [unroll]
    for (int lineIndex = 0; lineIndex < 2; lineIndex++)
    {
        if (lineIndex == 1 && !isCross) { break; }
        float2 lineSpacing = lineSpacings[lineIndex];
        float2 copyPoints[2];
        SDFRepeatLine(mirroredPoint, lineSpacing, count, copyPoints[0], copyPoints[1]);
        [unroll]
        for (int copy = 0; copy < 2; copy++)
        {
            float copyNumber = GetBattleEffectLineCopyIndex(mirroredPoint, copyPoints[copy], lineSpacing, count);
            float copyDistance = GetBattleEffectScaledCopyDistance(copyShape, copyPoints[copy], copyNumber);
            KeepNearestBattleEffectCopy(copyDistance, copyNumber, distance, copyIndex);
        }
    }
    return distance;
}

float GetBattleEffectGridCopiesDistance(BattleEffectCopyShape copyShape, float2 mirroredPoint, float2 spacing, float2 count, bool isStaggered,
    out float copyIndex)
{
    // Grid:  count.x by count.y copies;  staggered:  odd rows hold one fewer (half a spacing across) ~ the nearest copy + its x / y / xy neighbours

    float2 copyPoints[4];
    if (isStaggered) { SDFRepeatStaggeredGrid(mirroredPoint, spacing, count, copyPoints[0], copyPoints[1], copyPoints[2], copyPoints[3]); }
    else { SDFRepeatGrid(mirroredPoint, spacing, count, copyPoints[0], copyPoints[1], copyPoints[2], copyPoints[3]); }

    float distance = 1e5;
    copyIndex = 0.0;
    [unroll]
    for (int copy = 0; copy < 4; copy++)
    {
        float copyNumber = GetBattleEffectGridCopyIndex(mirroredPoint, copyPoints[copy], spacing, count, isStaggered);
        float copyDistance = GetBattleEffectScaledCopyDistance(copyShape, copyPoints[copy], copyNumber);
        KeepNearestBattleEffectCopy(copyDistance, copyNumber, distance, copyIndex);
    }
    return distance;
}

float GetBattleEffectRadialCopiesDistance(BattleEffectCopyShape copyShape, float2 mirroredPoint, float2 spacing, float count,
    out float copyIndex)
{
    // Radial:  count copies turned evenly about the origin, copy 0 offset by the spacing ~ the nearest copy + either side
    //  - a copy's number comes from how far it's turned

    float2 copyPoints[3];
    SDFRepeatRadial(mirroredPoint, count, copyPoints[0], copyPoints[1], copyPoints[2]);
    float copies = max(round(count), 1.0);
    float sector = 6.28318531 / copies;

    float distance = 1e5;
    copyIndex = 0.0;
    [unroll]
    for (int copy = 0; copy < 3; copy++)
    {
        float turns = round((atan2(mirroredPoint.y, mirroredPoint.x) - atan2(copyPoints[copy].y, copyPoints[copy].x)) / sector);
        float copyNumber = fmod(fmod(turns, copies) + copies, copies);
        float copyDistance = GetBattleEffectScaledCopyDistance(copyShape, copyPoints[copy] - spacing, copyNumber);
        KeepNearestBattleEffectCopy(copyDistance, copyNumber, distance, copyIndex);
    }
    return distance;
}

float GetBattleEffectLocalDistance(UnityTexture2D itemData, int item, int shapeId, BattleEffectShapeInput shapeInput, float progress, int beat,
    out float copyIndex)
{
    // The item's distance in its local space (mirror -> repetition -> shape rotation -> elongation -> shape), at its nearest copies
    //  - the current beat adds to the elongation

    // Settings at this point in the item's progress
    float4 repetition = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_REPETITION); // (repetition, count x, count y, mirror [x = 1, y = 2])
    float4 spacingRange = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_SPACING);
    float4 copyScaleRange = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_COPYSCALE); // (copy scale ratio start, end, -, -)
    float4 elongationRange = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_ELONGATION);
    float4 rotation = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_ROTATION); // (item rotation start, end, shape rotation start, end)
    int repetitionMode = (int)round(repetition.x);
    float2 count = repetition.yz;
    float2 spacing = lerp(spacingRange.xy, spacingRange.zw, progress);
    float shapeAngle = lerp(rotation.z, rotation.w, progress);

    BattleEffectCopyShape copyShape;
    copyShape.shapeId = shapeId;
    copyShape.shapeInput = shapeInput;
    copyShape.shapeRotation = float2(sin(shapeAngle), cos(shapeAngle));
    copyShape.copyScaleRatio = lerp(copyScaleRange.x, copyScaleRange.y, progress);
    copyShape.elongation = lerp(elongationRange.xy, elongationRange.zw, progress) + GetBattleEffectBeatPair(itemData, item, beat, BATTLE_EFFECT_LAYOUT_BEATELONGATIONS01);

    float2 mirroredPoint;
    SDFMirror(shapeInput.localPoint, fmod(repetition.w, 2.0), floor(repetition.w / 2.0), mirroredPoint);

    // Repetition families
    if (repetitionMode == BATTLE_EFFECT_REPETITIONMODE_LINE || repetitionMode == BATTLE_EFFECT_REPETITIONMODE_CROSS)
    {
        bool isCross = repetitionMode == BATTLE_EFFECT_REPETITIONMODE_CROSS;
        return GetBattleEffectLineCopiesDistance(copyShape, mirroredPoint, spacing, count.x, isCross, copyIndex);
    }
    if (repetitionMode == BATTLE_EFFECT_REPETITIONMODE_GRID || repetitionMode == BATTLE_EFFECT_REPETITIONMODE_STAGGEREDGRID)
    {
        bool isStaggered = repetitionMode == BATTLE_EFFECT_REPETITIONMODE_STAGGEREDGRID;
        return GetBattleEffectGridCopiesDistance(copyShape, mirroredPoint, spacing, count, isStaggered, copyIndex);
    }
    if (repetitionMode == BATTLE_EFFECT_REPETITIONMODE_RADIAL)
    {
        return GetBattleEffectRadialCopiesDistance(copyShape, mirroredPoint, spacing, count.x, copyIndex);
    }

    // No repetition:  the one shape
    copyIndex = 0.0;
    return GetBattleEffectCopyDistance(copyShape, mirroredPoint);
}

#endif

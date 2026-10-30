#ifndef BATTLEEFFECTREPETITION_INCLUDED
#define BATTLEEFFECTREPETITION_INCLUDED

// Notes:
//  - BattleEffect graph only (via BattleEffectComposite):  an item's shape, at its nearest repetition copies
//      - mirror (+ spread beat offset) -> repetition -> shape rotation -> elongation -> shape (generated dispatch)
//  - Each repetition family evaluates its nearest + neighbouring copies + keeps the closest - its copy index (the copy's number) feeds:
//      - per-copy colour:  line / radial = along;  grid = column + row
//      - per-copy size:  copy n = copy scale ratio ^ n, about its own centre (warning:  copies that outgrow ~1.5x spacings // past neighbours' cells can clip)
//      - line / cross copy wave:  copy n sits amplitude * wave(phase + n / copies per cycle) across its line
//  - Passes back the point in its copy's space (before shape rotation;  mirror folds undone) for the shade

#include "Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/BattleEffectItemData.hlsl"

#define BATTLE_EFFECT_MAX_COPY_CANDIDATES 4 // Line = 2, cross = 4, grid = 4, radial = 3, none = 1

struct BattleEffectCopyShape
{
    int shapeId;
    BattleEffectShapeInput shapeInput;
    float2 shapeRotation;
    float2 elongation;
    float copyScaleRatio;
    float4 copyWave;
    float wavePhase;
};

float GetBattleEffectCopyDistance(BattleEffectCopyShape copyShape, float2 copyPoint)
{
    // Shape rotation turns the shape within its copy (about the copy's centre), then elongation stretches it

    float2 rotation = copyShape.shapeRotation;
    float2 turnedPoint = float2(rotation.y * copyPoint.x + rotation.x * copyPoint.y, -rotation.x * copyPoint.x + rotation.y * copyPoint.y);
    BattleEffectShapeInput shapeInput = copyShape.shapeInput;
    float interiorDistance;
    SDFElongate(turnedPoint, copyShape.elongation, shapeInput.localPoint, interiorDistance);
    return BattleEffectShapeDistance(copyShape.shapeId, shapeInput) + interiorDistance;
}

float GetBattleEffectScaledCopyDistance(BattleEffectCopyShape copyShape, float2 copyPoint, float copyNumber)
{
    // The copy at its own size (ratio ^ copy number)

    float copyScale = copyShape.copyScaleRatio == 1.0 ? 1.0 : max(pow(max(copyShape.copyScaleRatio, 0.0), copyNumber), 1e-4);
    return GetBattleEffectCopyDistance(copyShape, copyPoint / copyScale) * copyScale;
}

void KeepNearestBattleEffectCopy(float candidateDistance, float candidateIndex, float2 candidatePoint, inout float distance, inout float copyIndex, inout float2 copyPoint)
{
    if (candidateDistance < distance) { distance = candidateDistance; copyIndex = candidateIndex; copyPoint = candidatePoint; }
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

float2 GetBattleEffectCopyWaveOffset(BattleEffectCopyShape copyShape, float2 lineSpacing, float copyNumber)
{
    // Line / cross:  copy n's offset across its line (to the left of the spacing vector)

    if (copyShape.copyWave.z == 0.0) { return 0.0; }
    float waveValue;
    WaveformValue(copyShape.copyWave.x, copyShape.wavePhase + copyNumber / max(copyShape.copyWave.y, 0.01), copyShape.copyWave.w, waveValue);
    float2 across = float2(-lineSpacing.y, lineSpacing.x) / max(length(lineSpacing), 1e-8);
    return across * copyShape.copyWave.z * waveValue;
}

void GetBattleEffectLineCandidates(BattleEffectCopyShape copyShape, float2 mirroredPoint, float2 spacing, float count, bool isCross,
    inout float2 candidatePoints[BATTLE_EFFECT_MAX_COPY_CANDIDATES], inout float candidateNumbers[BATTLE_EFFECT_MAX_COPY_CANDIDATES], out int candidateCount)
{
    // Line:  count copies along the spacing vector (the nearest + its neighbour)
    // Cross:  + a second line across it (the same count + spacing, turned a quarter)

    candidateCount = isCross ? 4 : 2;
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
            candidateNumbers[lineIndex * 2 + copy] = copyNumber;
            candidatePoints[lineIndex * 2 + copy] = copyPoints[copy] - GetBattleEffectCopyWaveOffset(copyShape, lineSpacing, copyNumber);
        }
    }
}

void GetBattleEffectGridCandidates(float2 mirroredPoint, float2 spacing, float2 count, bool isStaggered,
    inout float2 candidatePoints[BATTLE_EFFECT_MAX_COPY_CANDIDATES], inout float candidateNumbers[BATTLE_EFFECT_MAX_COPY_CANDIDATES], out int candidateCount)
{
    // Grid:  count.x by count.y copies;  staggered:  odd rows hold one fewer (half a spacing across) ~ the nearest copy + its x / y / xy neighbours

    candidateCount = 4;
    if (isStaggered) { SDFRepeatStaggeredGrid(mirroredPoint, spacing, count, candidatePoints[0], candidatePoints[1], candidatePoints[2], candidatePoints[3]); }
    else { SDFRepeatGrid(mirroredPoint, spacing, count, candidatePoints[0], candidatePoints[1], candidatePoints[2], candidatePoints[3]); }
    [unroll]
    for (int copy = 0; copy < 4; copy++) { candidateNumbers[copy] = GetBattleEffectGridCopyIndex(mirroredPoint, candidatePoints[copy], spacing, count, isStaggered); }
}

void GetBattleEffectRadialCandidates(float2 mirroredPoint, float2 spacing, float count,
    inout float2 candidatePoints[BATTLE_EFFECT_MAX_COPY_CANDIDATES], inout float candidateNumbers[BATTLE_EFFECT_MAX_COPY_CANDIDATES], out int candidateCount)
{
    // Radial:  count copies turned evenly about the origin, copy 0 offset by the spacing ~ the nearest copy + either side
    //  - a copy's number comes from how far it's turned

    candidateCount = 3;
    float2 copyPoints[3];
    SDFRepeatRadial(mirroredPoint, count, copyPoints[0], copyPoints[1], copyPoints[2]);
    float copies = max(round(count), 1.0);
    float sector = 6.28318531 / copies;
    [unroll]
    for (int copy = 0; copy < 3; copy++)
    {
        float turns = round((atan2(mirroredPoint.y, mirroredPoint.x) - atan2(copyPoints[copy].y, copyPoints[copy].x)) / sector);
        candidateNumbers[copy] = fmod(fmod(turns, copies) + copies, copies);
        candidatePoints[copy] = copyPoints[copy] - spacing;
    }
}

float GetBattleEffectLocalDistance(UnityTexture2D itemData, int item, int shapeId, BattleEffectShapeInput shapeInput, float2 spreadOffset, float progress, int beat,
    out float copyIndex, out float2 copyPoint)
{
    // The item's distance in its local space (mirror -> spread offset -> repetition -> shape rotation -> elongation -> shape), at its nearest copies
    //  - the current beat adds to the elongation
    //  - spread offset (local):  a spread beat's offset, after the fold - mirrored halves move apart (0 otherwise)

    // Settings at this point in the item's progress
    float4 repetition = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_REPETITION); // (repetition, count x, count y, mirror [x = 1, y = 2, half turn = 4, spread = 8])
    float4 spacingRange = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_SPACING);
    float4 copyScaleRange = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_COPYSCALE); // (copy scale ratio start, end, -, -)
    float4 copyWave = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_COPYWAVE); // (waveform, copies per cycle, amplitude start, end)
    float4 copyWavePhase = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_COPYWAVEPHASE); // (phase start, end, plateau start, end)
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
    copyShape.copyWave = float4(copyWave.x, copyWave.y, lerp(copyWave.z, copyWave.w, progress), lerp(copyWavePhase.z, copyWavePhase.w, progress));
    copyShape.wavePhase = lerp(copyWavePhase.x, copyWavePhase.y, progress);
    copyShape.elongation = lerp(elongationRange.xy, elongationRange.zw, progress) + GetBattleEffectBeatPair(itemData, item, beat, BATTLE_EFFECT_LAYOUT_BEATELONGATIONS01);

    // Folds: half turn, then x / y mirrors (unfold = their signs, to put the copy's point back on the item's side)
    uint mirrorFlags = (uint)round(repetition.w); // (x = 1, y = 2, half turn = 4, spread = 8)
    bool isMirrorX = (mirrorFlags & 1u) != 0u;
    bool isMirrorY = (mirrorFlags & 2u) != 0u;
    bool isTurned = (mirrorFlags & 4u) != 0u && shapeInput.localPoint.x < 0.0;
    float2 turnedPoint = shapeInput.localPoint;
    if (isTurned) { SDFHalfTurn(shapeInput.localPoint, turnedPoint); }
    float2 mirroredPoint;
    SDFMirror(turnedPoint, isMirrorX ? 1.0 : 0.0, isMirrorY ? 1.0 : 0.0, mirroredPoint);
    mirroredPoint -= spreadOffset;
    float2 unfold = float2(isMirrorX && turnedPoint.x < 0.0 ? -1.0 : 1.0, isMirrorY && turnedPoint.y < 0.0 ? -1.0 : 1.0) * (isTurned ? -1.0 : 1.0);

    // Repetition families:  each only works out its candidates (while no repetition = the one shape at its own size)
    float2 candidatePoints[BATTLE_EFFECT_MAX_COPY_CANDIDATES] = { mirroredPoint, float2(0.0, 0.0), float2(0.0, 0.0), float2(0.0, 0.0) };
    float candidateNumbers[BATTLE_EFFECT_MAX_COPY_CANDIDATES] = { 0.0, 0.0, 0.0, 0.0 };
    int candidateCount = 1;
    if (repetitionMode == BATTLE_EFFECT_REPETITIONMODE_LINE || repetitionMode == BATTLE_EFFECT_REPETITIONMODE_CROSS)
    {
        GetBattleEffectLineCandidates(copyShape, mirroredPoint, spacing, count.x, repetitionMode == BATTLE_EFFECT_REPETITIONMODE_CROSS, candidatePoints, candidateNumbers, candidateCount);
    }
    else if (repetitionMode == BATTLE_EFFECT_REPETITIONMODE_GRID || repetitionMode == BATTLE_EFFECT_REPETITIONMODE_STAGGEREDGRID)
    {
        GetBattleEffectGridCandidates(mirroredPoint, spacing, count, repetitionMode == BATTLE_EFFECT_REPETITIONMODE_STAGGEREDGRID, candidatePoints, candidateNumbers, candidateCount);
    }
    else if (repetitionMode == BATTLE_EFFECT_REPETITIONMODE_RADIAL)
    {
        GetBattleEffectRadialCandidates(mirroredPoint, spacing, count.x, candidatePoints, candidateNumbers, candidateCount);
    }
    else
    {
        copyShape.copyScaleRatio = 1.0;
    }

    // Fixed single shape evaluation site
    // N.B. Avoid splits, as each extra site inlines the whole dispatch (compile time multiplier)
    float distance = 1e5;
    copyIndex = candidateNumbers[0];
    copyPoint = candidatePoints[0];
    [unroll]
    for (int candidate = 0; candidate < BATTLE_EFFECT_MAX_COPY_CANDIDATES; candidate++)
    {
        if (candidate >= candidateCount) { break; }
        float candidateDistance = GetBattleEffectScaledCopyDistance(copyShape, candidatePoints[candidate], candidateNumbers[candidate]);
        KeepNearestBattleEffectCopy(candidateDistance, candidateNumbers[candidate], candidatePoints[candidate], distance, copyIndex, copyPoint);
    }
    copyPoint *= unfold;
    return distance;
}

#endif

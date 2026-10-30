#ifndef BATTLEEFFECTITEMDATA_INCLUDED
#define BATTLEEFFECTITEMDATA_INCLUDED

// Notes:
//  - BattleEffect graph only (via BattleEffectComposite):  reads one item's settings from the recipe's data texture
//      - one row per item, columns per BattleEffectDataLayout (mirrored into the generated dispatch as BATTLE_EFFECT_LAYOUT_*)
//  - Per-item values that aren't geometry:  timing, beats, modifiers (jitter / flicker / pulse), colours (+ patterns) + stripes

#include "Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/SDFDomain.hlsl"
#include "Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/TimeUtility.hlsl"
#include "Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/Generated/BattleEffectDispatch.generated.hlsl"

#ifdef __RESHARPER__ // IDE-only declaration - in shader, this comes from render pipeline
struct UnityTexture2D
{
    Texture2D tex;
};
#endif

struct BattleEffectModifiers
{
    // On the effect's clock (in order of use)
    float2 jitterOffset;
    float flickerAlpha;
    float pulseScale;
};

float4 LoadBattleEffectTexel(UnityTexture2D itemData, int item, int column)
{
    return LOAD_TEXTURE2D(itemData.tex, int2(column, item));
}

int GetBattleEffectStyle(UnityTexture2D itemData, int item)
{
    return (int)round(LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_STYLE).x);
}

bool TryGetBattleEffectItemTime(UnityTexture2D itemData, int item, float elapsedTime,
    out int shapeId, out float itemTime, out float progress)
{
    // Half-open [start, end), w/ a little tolerance
    // Eased, then held on the item's progress steps

    float4 timing = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_TIMING); // (start time, duration, easing, progress steps)
    shapeId = (int)round(LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_SHAPE).x);
    itemTime = elapsedTime - timing.x;
    progress = 0.0;
    if (itemTime < -1e-4 || itemTime >= timing.y - 1e-4) { return false; } // ensure stepped frames on an item's boundaries land on exactly one item

    float4 easingParameters = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_EASINGPARAMETERS); // (approach ratio, -, -, -)
    EaseProgress(timing.y > 0.0 ? itemTime / timing.y : 1.0, timing.z, easingParameters.x, progress);
    StepProgress(progress, timing.w, progress);
    return true;
}

int GetBattleEffectBeat(UnityTexture2D itemData, int item, float itemTime)
{
    // The item's current beat (the last one started), or -1 before / w/o beats (unused beats start never)
    //  - optional pattern can pick beat per step instead:  base-5 digits (0 = none, 1-4 = beats 0-3), from the item's start, repeating

    float4 beatPattern = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_BEATPATTERN); // (steps 0-9, 10-19, rate, step count)
    int beat = -1;
    if (beatPattern.w >= 1.0)
    {
        // Integer steps + digits:  float division can land just under a whole number on the GPU
        uint stepIndex = uint(max(floor(itemTime * beatPattern.z + 1e-3), 0.0)) % uint(round(beatPattern.w));
        uint digits = uint(round(stepIndex < 10u ? beatPattern.x : beatPattern.y));
        [unroll]
        for (uint shift = 0u; shift < 9u; shift++) { digits = shift < stepIndex % 10u ? digits / 5u : digits; } // extract 5-bit colour entry @ stepIndex from digits 
        beat = int(digits % 5u) - 1;
    }
    else
    {
        float4 beatStarts = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_BEATSTARTS);
        [unroll]
        for (int candidate = 0; candidate < BATTLE_EFFECT_LAYOUT_MAXBEATS; candidate++) { beat = itemTime >= beatStarts[candidate] - 1e-4 ? candidate : beat; }
    }
    return beat;
}

float2 GetBattleEffectBeatPair(UnityTexture2D itemData, int item, int beat, int firstColumn)
{
    // Beats' 2D values always pack two per texel (beats 0 + 1, then 2 + 3 immediately after)

    if (beat < 0) { return 0.0; }
    uint beatIndex = (uint)beat; // (unsigned:  the shifts below are cheaper than signed divides)
    float4 pair = LoadBattleEffectTexel(itemData, item, firstColumn + (int)(beatIndex >> 1u));
    return (beatIndex & 1u) == 0u ? pair.xy : pair.zw;
}

BattleEffectModifiers GetBattleEffectModifiers(UnityTexture2D itemData, int item, float elapsedTime, float itemTime, float disableFlicker, float disableJitter)
{
    // On the effect's clock (not the item's) - items w/ the same settings stay in sync
    //  - except a flicker pattern, which runs from the item's start

    float4 jitter = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_JITTER); // (rate, amount.x, amount.y, seed)
    float4 jitterMode = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_JITTERMODE); // (mode, phase, -, -)
    float4 flicker = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_FLICKER); // (rate, depth, phase, pattern)
    float4 pulse = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_PULSE); // (rate, amount, phase, -)

    BattleEffectModifiers modifiers;
    Jitter(elapsedTime, disableJitter > 0.5 ? 0.0 : jitter.x, jitter.yz, jitter.w, jitterMode.x, jitterMode.y, modifiers.jitterOffset);
    float flickerRate = disableFlicker > 0.5 ? 0.0 : flicker.x;
    if (flicker.w >= 2.0) { FlickerPattern(itemTime, flickerRate, flicker.y, flicker.z, flicker.w, modifiers.flickerAlpha); }
    else { Flicker(elapsedTime, flickerRate, flicker.y, flicker.z, modifiers.flickerAlpha); }
    Pulse(elapsedTime, pulse.x, pulse.y, pulse.z, modifiers.pulseScale);
    return modifiers;
}

float GetBattleEffectPaletteIndex(UnityTexture2D itemData, int item, float elapsedTime, float itemTime, float copyIndex, int beat)
{
    // The palette:  the item's colour (A) + its alt colours (B-D, up to the palette count) ~ returns index -> to get colour entry via GetBattleEffectPaletteColour
    // - Source Options:
    //      - time source:  steps through the palette on the effect's clock (rate 0 = stays on A)
    //      - copy source:  each repetition copy takes the next entry (+ the phase offset)
    //      - pattern source:  the pattern's palette index per step
    //  - Note that the current beat's colour index shifts any of the above (e.g. a beat that's B)

    float4 cycle = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_COLOURCYCLE); // (source, rate, phase offset [steps], palette count)
    float paletteCount = max(round(cycle.w), 1.0);
    int source = (int)round(cycle.x);
    float paletteIndex;
    if (source == BATTLE_EFFECT_COLOURSOURCE_COPY) { paletteIndex = floor(copyIndex + cycle.z); }
    else if (source == BATTLE_EFFECT_COLOURSOURCE_PATTERN) { PatternIndex(itemTime, cycle.y, cycle.z, LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_COLOURPATTERN), paletteIndex); }
    else { CycleIndex(elapsedTime, cycle.y, paletteCount, cycle.z, paletteIndex); }
    return paletteIndex + (beat >= 0 ? LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_BEATCOLOURS)[beat] : 0.0);
}

float4 GetBattleEffectPaletteColour(UnityTexture2D itemData, int item, float paletteIndex)
{
    // The palette entry at an index (wrapped to the palette count)

    float paletteCount = max(round(LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_COLOURCYCLE).w), 1.0);
    int entry = (int)round(fmod(fmod(paletteIndex, paletteCount) + paletteCount, paletteCount));
    return LoadBattleEffectTexel(itemData, item, entry == 0 ? BATTLE_EFFECT_LAYOUT_COLOUR : BATTLE_EFFECT_LAYOUT_ALTCOLOURB + entry - 1);
}

float4 GetBattleEffectStrokeColour(UnityTexture2D itemData, int item, float4 colour, float paletteIndex, float shapeDistance, float progress)
{
    // Returns the stroked outline colour:  
    //  - for outline: the item's colour (its palette entry)
    //  - for fill and outline: the outline colour (or the stepped/offset palette entry)
    // && edge gradient:  from outline colour on the stroke's inside edge -> the edge colour on its outside edge
    //  - for edge steps > 0, creates a banding effect (steps 3 -> inside / middle / outside colours)

    float4 style = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_STYLE); // (style, blend, outline thickness start, end)
    float4 strokeColour = colour;
    if ((int)round(style.x) == BATTLE_EFFECT_STYLEMODE_FILLANDOUTLINE)
    {
        float4 outlineSource = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_OUTLINESOURCE); // (from the palette, palette step, -, -)
        strokeColour = outlineSource.x > 0.5 ? GetBattleEffectPaletteColour(itemData, item, paletteIndex + outlineSource.y) : LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_OUTLINECOLOUR);
    }
    float4 edgeGradient = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_EDGEGRADIENT); // (on, steps, -, -)
    if (edgeGradient.x < 0.5) { return strokeColour; }

    float thickness = max(lerp(style.z, style.w, progress), 1e-5);
    float across = saturate((shapeDistance + thickness) / (2.0 * thickness)); // 0 = the stroke's inside edge, 1 = its outside edge
    float steps = round(edgeGradient.y);
    across = steps > 0.5 ? (steps > 1.5 ? min(floor(across * steps), steps - 1.0) / (steps - 1.0) : 0.0) : across;
    return lerp(strokeColour, LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_EDGECOLOUR), across);
}

float GetBattleEffectStripeMask(UnityTexture2D itemData, int item, float2 fittedUV)
{
    // Hard-edged to ensure complementary masks interleave w/o seams
    // In effect units, anchored on the centre

    float4 stripes = LoadBattleEffectTexel(itemData, item, BATTLE_EFFECT_LAYOUT_STRIPES); // (stripes per EU [0 = off], angle, duty, phase)
    if (stripes.x <= 0.0) { return 1.0; }

    float mask;
    SDFStripeMask(fittedUV, float2(0.5, 0.5), stripes.x, stripes.y, stripes.z, stripes.w, mask);
    return mask;
}

#endif

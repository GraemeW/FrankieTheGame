#include "ShaderApiReflectionSupport.hlsl"
#ifndef SHADERGRAPH_PREVIEW
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#endif

#ifndef TIMEUTILITY_INCLUDED
#define TIMEUTILITY_INCLUDED

#include "Packages/com.lowdefmustard.rendering/Runtime/Shaders/HLSLUtilities/HashUtility.hlsl"

///<funchints>
/// <sg:ProviderKey>TimeSinceInitialization</sg:ProviderKey>
/// <sg:DisplayName>Time Since Initialization</sg:DisplayName>
/// <sg:SearchCategory>LDM/TimeSinceInitialization</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void TimeSinceInitialization(float phase, 
    out float offsetTime, out float sineOffsetTime, out float cosineOffsetTime)
{
    offsetTime = _Time.y - phase;
    sineOffsetTime = sin(offsetTime);
    cosineOffsetTime = cos(offsetTime);
}

///<funchints>
/// <sg:ProviderKey>PeriodTime</sg:ProviderKey>
/// <sg:DisplayName>Period Time</sg:DisplayName>
/// <sg:SearchCategory>LDM/PeriodTime</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void PeriodTime(float period,
    out float periodTime)
{
    periodTime = _Time.y * TWO_PI / period;
}

///<funchints>
/// <sg:ProviderKey>SteppedTime</sg:ProviderKey>
/// <sg:DisplayName>Stepped Time</sg:DisplayName>
/// <sg:SearchCategory>LDM/SteppedTime</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void SteppedTime(float time, float frameRate,
    out float steppedTime)
{
    // Holds time on whole frames at the given rate (i.e. retro / held-frame animation)
    // A frame rate of 0 passes time through
    
    steppedTime = frameRate > 0.0 ? floor(time * frameRate) / frameRate : time;
}

///<funchints>
/// <sg:ProviderKey>Flicker</sg:ProviderKey>
/// <sg:DisplayName>Flicker</sg:DisplayName>
/// <sg:SearchCategory>LDM/Flicker</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void Flicker(float time, float rate, float depth, float phase,
    out float alphaMultiplier)
{
    // Square wave at rate (full cycles per second):  
    //  - on for the first half of each cycle, (1 - depth) for the second
    //  - rate 0 = off
    // Phase shifts the wave by that fraction of a cycle (0.5 = dim half first)
    
    bool isDim = rate > 0.0 && frac(time * rate + phase) >= 0.5;
    alphaMultiplier = isDim ? 1.0 - saturate(depth) : 1.0;
}

///<funchints>
/// <sg:ProviderKey>Pulse</sg:ProviderKey>
/// <sg:DisplayName>Pulse</sg:DisplayName>
/// <sg:SearchCategory>LDM/Pulse</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void Pulse(float time, float rate, float amount, float phase,
    out float scaleMultiplier)
{
    // Square wave at rate (full cycles per second):  
    //  - Full size for the first half of each cycle, (1 + amount) for the second
    //  - Rate 0 = off
    // A negative amount shrinks instead
    // Phase shifts the wave by that fraction of a cycle
    
    bool isPulsed = rate > 0.0 && frac(time * rate + phase) >= 0.5;
    scaleMultiplier = isPulsed ? max(1.0 + amount, 0.0) : 1.0;
}

// Jitter modes:  0 = random, 1 = orbit
// Note:  BattleEffectJitter's values follow these
///<funchints>
/// <sg:ProviderKey>Jitter</sg:ProviderKey>
/// <sg:DisplayName>Jitter</sg:DisplayName>
/// <sg:SearchCategory>LDM/Jitter</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void Jitter(float time, float rate, float2 amount, float seed, float mode, float phase,
    out float2 offset)
{
    // Offset (up to amount on each axis), held for 1 / rate seconds then moved on (rate 0 = off)
    //  - Random:  re-rolled each step
    //      - same seed + rate = same jumps
    //  - Orbit:  steps clockwise around the corners of the +/- amount box (top-left -> top-right -> bottom-right -> bottom-left)
    //      - seed picks the starting corner
    // Phase shifts the steps by that fraction of a step
    
    offset = float2(0.0, 0.0);
    if (rate <= 0.0) { return; }

    float step = floor(time * rate + phase);
    if ((int)round(mode) == 1)
    {
        int corner = (int)fmod(fmod(step + round(seed), 4.0) + 4.0, 4.0);
        float2 corners[4] = { float2(-1.0, 1.0), float2(1.0, 1.0), float2(1.0, -1.0), float2(-1.0, -1.0) };
        offset = corners[corner] * amount;
    }
    else
    {
        float2 hash;
        Hash22(float2(step, seed), hash);
        offset = (hash * 2.0 - 1.0) * amount;
    }
}

///<funchints>
/// <sg:ProviderKey>EaseProgress</sg:ProviderKey>
/// <sg:DisplayName>Ease Progress</sg:DisplayName>
/// <sg:SearchCategory>LDM/EaseProgress</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void EaseProgress(float progress, float easingType,
    out float easedProgress)
{
    // Easing types:  
    //  - 0 = linear
    //  - 1 = ease in (quadratic)
    //  - 2 = ease out (quadratic)
    //  - 3 = ease in-out (smoothstep)
    // BattleEffectEasing's values follow these same indices
    
    float clampedProgress = saturate(progress);
    int easing = (int)round(easingType);
    if (easing == 1) { easedProgress = clampedProgress * clampedProgress; }
    else if (easing == 2) { easedProgress = clampedProgress * (2.0 - clampedProgress); }
    else if (easing == 3) { easedProgress = smoothstep(0.0, 1.0, clampedProgress); }
    else { easedProgress = clampedProgress; }
}

#endif

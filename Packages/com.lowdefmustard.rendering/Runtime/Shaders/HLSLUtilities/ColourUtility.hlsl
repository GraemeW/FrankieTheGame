#include "ShaderApiReflectionSupport.hlsl"

#ifndef COLOURUTILITY_INCLUDED
#define COLOURUTILITY_INCLUDED

// Gradient modes:  0 = solid, 1 = linear, 2 = wave, 3 = radial
// Note:  BattleEffectTintMode's values follow these
///<funchints>
/// <sg:ProviderKey>ColourGradient</sg:ProviderKey>
/// <sg:DisplayName>Colour Gradient</sg:DisplayName>
/// <sg:SearchCategory>LDM/ColourGradient</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION void ColourGradient(float2 position, float mode, float4 colourA, float4 colourB, float angle, float frequency, float phase,
    out float4 colourOutput)
{
    // Position in 0-1 units (centre at 0.5) w/ angle in radians (counter-clockwise from +x) && phase in cycles (e.g. time * speed, to scroll)
    // Modes Summary:
    //  - Solid:  colour A
    //  - Linear:  A -> B along the angle, spanning 1 / frequency (1 = across 0-1)
    //  - Wave:  bands along the angle, A at the peaks + B in the troughs, frequency bands across 0-1 (0 = the whole area pulses A <-> B w/ phase)
    //  - Radial:  same as wave, but rings out from the centre
    
    int gradientMode = (int)round(mode);
    float2 offset = position - 0.5;
    float along = dot(offset, float2(cos(angle), sin(angle)));

    float blend = 0.0;
    if (gradientMode == 1) { blend = saturate(0.5 + along * frequency); }
    else if (gradientMode == 2) { blend = 0.5 - 0.5 * cos(TWO_PI * (along * frequency - phase)); }
    else if (gradientMode == 3) { blend = 0.5 - 0.5 * cos(TWO_PI * (length(offset) * frequency - phase)); }
    colourOutput = lerp(colourA, colourB, blend);
}

#endif

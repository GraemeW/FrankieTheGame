#include "ShaderApiReflectionSupport.hlsl"
#ifndef SHADERGRAPH_PREVIEW
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#endif

#ifndef TIMEUTILITY_INCLUDED
#define TIMEUTILITY_INCLUDED

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

#endif

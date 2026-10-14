// UITK SubShader:  applied as a backdrop-filter by the Fader's battle entry overlay
//  - twirls everything rendered behind the overlay, and blends a twirled + tinted graphic over it
//  - timing is driven by shader time && parameters are set once per fade
Shader "Frankie/UI Toolkit Filters/Battle Entry"
{
    Properties
    {
        [HideInInspector] _MainTex ("Backdrop", 2D) = "white" {}
        [NoScaleOffset] _GraphicTex ("Graphic", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Strength ("Twirl Strength", Float) = 5
        _Phase ("Phase (fade start time)", Float) = 0
        _FadeTime ("Fade In Time", Float) = 3.5
        _FadeOutTime ("Fade Out Time", Float) = 0.5
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "UnityUIEFilter.cginc"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_GraphicTex);
            SAMPLER(sampler_GraphicTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Strength;
                float _Phase;
                float _FadeTime;
                float _FadeOutTime;
            CBUFFER_END

            static const float2 _twirlCenter = float2(0.5, 0.5);
            static const float _minimumFadeTime = 0.0001;
            static const float _maximumBackdropUV = 0.999;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float rectIndex : TEXCOORD1;
            };

            Varyings vert(FilterVertexInput input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.vertex.xyz);
                output.uv = input.uv;
                output.rectIndex = GetFilterRectIndex(input);
                return output;
            }

            float GetNormalizedTime(float phase, float duration)
            {
                return (_Time.y - phase) / max(duration, _minimumFadeTime);
            }

            float2 Twirl(float2 uv, float strength)
            {
                float2 delta = uv - _twirlCenter;
                float angle = strength * length(delta);
                float sinAngle = sin(angle);
                float cosAngle = cos(angle);
                return float2(cosAngle * delta.x - sinAngle * delta.y, sinAngle * delta.x + cosAngle * delta.y) + _twirlCenter;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // The backdrop is a sub-rect of an atlas:  work in 0-1 space, map back when sampling
                float4 backdropRect = GetFilterUVRect((uint)(input.rectIndex + 0.5));
                float2 uv = (input.uv - backdropRect.xy) / backdropRect.zw;

                float fadeInTime = GetNormalizedTime(_Phase, _FadeTime);
                float fadeOutTime = GetNormalizedTime(_Phase + _FadeTime, _FadeOutTime);
                float fullFadeTime = GetNormalizedTime(_Phase, _FadeTime + _FadeOutTime);
                bool isFadingIn = fadeInTime <= 1.0;

                // Alpha:  graphic fades in over the backdrop, then back out
                float fadeProgress = smoothstep(0.0, 1.0, isFadingIn ? fadeInTime : fadeOutTime);
                float graphicOpacity = isFadingIn ? fadeProgress : 1.0 - fadeProgress;

                // Backdrop twirl:  winds up on the way in, unwinds from the opposite direction on the way out
                float backdropProgress = isFadingIn ? smoothstep(0.0, 1.0, fadeInTime) : smoothstep(0.0, 1.0, fadeOutTime) - 1.0;
                float backdropStrength = backdropProgress * (isFadingIn ? _Strength : _Strength * _FadeOutTime / max(_FadeTime, _minimumFadeTime));
                float2 backdropUV = clamp(Twirl(uv, backdropStrength), 0.0, _maximumBackdropUV);
                half3 backdrop = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, backdropUV * backdropRect.zw + backdropRect.xy).rgb;

                // Graphic twirl:  keeps winding in one direction across the whole fade
                float2 graphicUV = Twirl(uv, saturate(fullFadeTime) * _Strength);
                half3 graphic = SAMPLE_TEXTURE2D(_GraphicTex, sampler_GraphicTex, graphicUV).rgb * _Tint.rgb;

                // Opaque output:  the captured backdrop carries alpha, and transparent texels would let the un-twirled screen show through
                return half4(lerp(backdrop, graphic, graphicOpacity), 1.0);
            }
            ENDHLSL
        }
    }
}

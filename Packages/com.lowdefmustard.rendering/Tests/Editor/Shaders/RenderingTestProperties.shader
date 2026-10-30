Shader "Hidden/LowDefMustard/Rendering/Tests/RenderingTestProperties"
{
    // Test-only:  declares every property the package sets (shared + battle effect) so material round trips can be read back
    //  - Draws nothing (not for rendering, nor UI Toolkit compatible)
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Phase ("Phase", Float) = 0
        _FadeTime ("Fade Time", Float) = 0
        _FadeOutTime ("Fade Out Time", Float) = 0
        _IsPreview ("Is Preview", Float) = 0
        _PreviewTime ("Preview Time", Float) = 0
        _ItemCount ("Item Count", Float) = 0
        _ItemData ("Item Data", 2D) = "black" {}
        _TintColourA ("Tint Colour A", Color) = (1, 1, 1, 1)
        _TintColourB ("Tint Colour B", Color) = (1, 1, 1, 1)
        _TintSettings ("Tint Settings", Vector) = (0, 0, 0, 0)
        _FrameRate ("Frame Rate", Float) = 0
        _EdgeSoftness ("Edge Softness", Float) = 0
        _GlowWidth ("Glow Width", Float) = 0
        _DisableFlicker ("Disable Flicker", Float) = 0
        _DisableJitter ("Disable Jitter", Float) = 0
        _FitMode ("Fit Mode", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            float4 Vertex(float4 position : POSITION) : SV_POSITION
            {
                return position;
            }

            half4 Fragment() : SV_Target
            {
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}

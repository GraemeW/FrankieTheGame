Shader "CustomRenderTexture/PixelArtShader"
{
    // Following the approach detailed here:
    // https://www.youtube.com/watch?v=d6tp43wZqps
    // Note:  Backside culling disabled to allow for sprite flipping

    Properties
    {
        _MainTex("Texture", 2D) = "" {}
    }

    SubShader
    {
        Tags
        {
        "Queue" = "Transparent"
        "IgnoreProjector" = "True"
        "RenderType" = "Transparent"
        "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "PixelArtShader"

        HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vertexShader
            #pragma fragment fragmentShader

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct vertexInput
            {
                float4 vertex : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct vertexOutput
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            vertexOutput vertexShader(vertexInput input)
            {
                vertexOutput output;
                output.vertex = TransformObjectToHClip(input.vertex.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 fragmentShader(vertexOutput input) : SV_Target
            {
                // Query the live texture resource (cannot access _TexelSize directly, otherwise no SRP batching)
                uint texWidth, texHeight;
                _MainTex.GetDimensions(texWidth, texHeight);
                float4 texelSize = float4(1.0 / texWidth, 1.0 / texHeight, texWidth, texHeight);

                // Box filter in texel units
                float2 boxSize = clamp(fwidth(input.uv) * texelSize.zw, 1e-5, 1);
                // Scale uv by texture size to get texel coordinates
                float2 tx = input.uv * texelSize.zw - 0.5 * boxSize;
                // compute offset for pixel-sized box filter
                float2 txOffset = smoothstep(1 - boxSize, 1, frac(tx));
                // compute bilinear sample uv coordinates
                float2 uv = (floor(tx) + 0.5 + txOffset) * texelSize.xy;
                // return
                return SAMPLE_TEXTURE2D_GRAD(_MainTex, sampler_MainTex, uv, ddx(input.uv), ddy(input.uv)) * input.color;
            }

            ENDHLSL
        }
    }
}

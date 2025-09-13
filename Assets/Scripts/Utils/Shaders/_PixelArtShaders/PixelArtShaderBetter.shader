Shader "CustomRenderTexture/PixelArtShaderBetter"
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
        }

        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "PixelArtShaderBetter"

        CGPROGRAM
            #pragma vertex vertexShader
            #pragma fragment fragmentShader

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            struct vertexInput
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct vertexOutput
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            vertexOutput vertexShader(vertexInput input)
            {
                vertexOutput output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            fixed4 fragmentShader(vertexOutput input) : SV_Target
            {
                // Box filter in texel units
                float2 boxSize = clamp(fwidth(input.uv) * _MainTex_TexelSize.zw, 1e-5, 1);
                // Scale uv by texture size to get texel coordinates
                float2 tx = input.uv * _MainTex_TexelSize.zw - 0.5 * boxSize;
                // compute offset for pixel-sized box filter
                float2 txOffset = smoothstep(1 - boxSize, 1, frac(tx));
                // compute bilinear sample uv coordinates
                float2 uv = (floor(tx) + 0.5 + txOffset) * _MainTex_TexelSize.xy;
                // return
                return tex2Dgrad(_MainTex, uv, ddx(input.uv), ddy(input.uv)) * input.color;
            }

            ENDCG
        }
    }
}

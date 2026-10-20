Shader "CustomRenderTexture/PixelArtShader"
{
    // Following the approach detailed here:
    // https://colececil.dev/blog/2017/scaling-pixel-art-without-destroying-it/
    // , but setting the texelsPerPixel parameter based on input rather than manually.

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

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "PixelArtShader"

        CGPROGRAM
            #pragma vertex vertexShader
            #pragma fragment fragmentShader

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float2 _PixelsPerTexel; // Set externally from script

            struct vertexInput
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 textureCoords : TEXCOORD0;
            };

            struct vertexOutput
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 textureCoords : TEXCOORD0;
            };

            float2 GetTexelsPerPixel(in float2 texture_coordinate)
            {
                // Deprecated -- use global
                float2 dx_vtc = ddx(texture_coordinate);
                float2 dy_vtc = ddy(texture_coordinate);
                float2 tpp = float2(dot(dx_vtc, dx_vtc), dot(dy_vtc, dy_vtc));
                return tpp;
            }

            vertexOutput vertexShader(vertexInput input)
            {
                vertexOutput output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.textureCoords = input.textureCoords;
                output.color = input.color;
                return output;
            }

            fixed4 fragmentShader(vertexOutput input) : SV_Target
            {
                float2 tx = input.textureCoords * _MainTex_TexelSize.zw; // Convert to texel space
                float2 locationWithinTexel = frac(tx);
                float2 interpolationAmount = clamp(locationWithinTexel * _PixelsPerTexel, 0, .5) + clamp((1 - locationWithinTexel) * _PixelsPerTexel, 0, .5);
                float2 finalTextureCoords = (floor(tx) + 0.5 + interpolationAmount) * _MainTex_TexelSize.xy;
                return tex2D(_MainTex, finalTextureCoords);
            }

            ENDCG
        }
    }
}

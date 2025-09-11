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

            float mip_map_level(in float2 texture_coordinate)
            {
                float2 dx_vtc = ddx(texture_coordinate);
                float2 dy_vtc = ddy(texture_coordinate);
                float md = max(dot(dx_vtc, dx_vtc), dot(dy_vtc, dy_vtc));
                return 0.5 * log2(md);
            }

            vertexOutput vertexShader(vertexInput input)
            {
                vertexOutput output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.textureCoords = input.textureCoords * _MainTex_TexelSize.zw;
                output.color = input.color;
                return output;
            }

            fixed4 fragmentShader(vertexOutput input) : SV_Target
            {
                float texelsPerPixel = mip_map_level(input.textureCoords * _MainTex_TexelSize.zw);
                float2 locationWithinTexel = frac(input.textureCoords);
                float2 interpolationAmount = clamp(locationWithinTexel / texelsPerPixel,
                0, .5) + clamp((locationWithinTexel - 1) / texelsPerPixel + .5, 0,
                .5);
                float2 finalTextureCoords = (floor(input.textureCoords) +
                interpolationAmount) / _MainTex_TexelSize.zw;
                return tex2D(_MainTex, finalTextureCoords) * input.color;
            }

            ENDCG
        }
    }
}

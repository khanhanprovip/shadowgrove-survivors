Shader "RoguelikeKit/BossFrameSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Material Tint", Color) = (1,1,1,1)
        _BlendColor ("Hit Blend Color", Color) = (0,0,0,0)
        _FlameRedBlend ("Burn Tint", Color) = (1,1,1,1)
        _NoiseTex ("Dissolve Noise", 2D) = "white" {}
        _Cutoff ("Dissolve Cutoff", Range(0,1)) = 0
        _EdgeWidth ("Dissolve Edge Width", Range(0,1)) = 0
        [HDR] _EdgeColor ("Dissolve Edge Color", Color) = (0.45,0.95,1,1)
        _ShadowColor ("Sprite Shadow Color", Color) = (0,0,0,0.32)
        _ShadowOffset ("Sprite Shadow Offset", Vector) = (0,-1.06,0,0)
        _ShadowScale ("Sprite Shadow Scale", Vector) = (1,0.12,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "BossShadow"
            Tags { "LightMode"="Always" }

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            sampler2D _NoiseTex;
            fixed4 _ShadowColor;
            float4 _ShadowOffset;
            float4 _ShadowScale;
            float _Cutoff;

            v2f ShadowVert(appdata_t input)
            {
                input.vertex.xy *= _ShadowScale.xy;
                input.vertex.xy += _ShadowOffset.xy;
                return SpriteVert(input);
            }

            fixed4 ShadowFrag(v2f input) : SV_Target
            {
                fixed4 sprite = SampleSpriteTexture(input.texcoord) * input.color;
                if (_Cutoff > 0.001)
                {
                    float noise = tex2D(_NoiseTex, input.texcoord).r;
                    clip(noise - _Cutoff - 0.001);
                }

                fixed4 shadow = _ShadowColor;
                shadow.a = min(saturate(_ShadowColor.a), 0.22) * sprite.a * (1.0 - saturate(_Cutoff));
                return shadow;
            }
            ENDCG
        }

        Pass
        {
            Name "BossSprite"
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex SpriteVert
            #pragma fragment BossFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            sampler2D _NoiseTex;
            fixed4 _BlendColor;
            fixed4 _FlameRedBlend;
            fixed4 _EdgeColor;
            float _Cutoff;
            float _EdgeWidth;

            fixed4 BossFrag(v2f input) : SV_Target
            {
                float noise = tex2D(_NoiseTex, input.texcoord).r;
                if (_Cutoff > 0.001)
                    clip(noise - _Cutoff - 0.001);

                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                if (_EdgeWidth > 0.001 && noise < _Cutoff + _EdgeWidth)
                    color.rgb = _EdgeColor.rgb * input.color.rgb;

                color.rgb *= _FlameRedBlend.rgb;
                color.rgb += _BlendColor.rgb;
                return color;
            }
            ENDCG
        }
    }
}

Shader "RoguelikeKit/BossAnimatedSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Material Tint", Color) = (1,1,1,1)
        _BlendColor ("Hit Blend Color", Color) = (0,0,0,0)
        _NoiseTex ("Dissolve Noise", 2D) = "white" {}
        _Cutoff ("Dissolve Cutoff", Range(0,1)) = 0
        _EdgeWidth ("Dissolve Edge Width", Range(0,1)) = 0
        [HDR] _EdgeColor ("Dissolve Edge Color", Color) = (0.45,0.95,1,1)
        _ShadowColor ("Dissolve Shadow Color", Color) = (0,0,0,0.7)
        _ShadowOffset ("Shadow Offset", Vector) = (0.1,-0.1,0,0)
        _ShadowScale ("Shadow Scale", Vector) = (1,0.2,1,1)

        [HideInInspector] _AnimationTime ("Animation Time", Float) = 0
        [HideInInspector] _BossKind ("Boss Kind", Float) = 0
        [HideInInspector] _Motion ("Motion", Float) = 0
        [HideInInspector] _CastAge ("Cast Age", Float) = -1
        [HideInInspector] _SkillIndex ("Skill Index", Float) = -1
        [HideInInspector] _HitPulse ("Hit Pulse", Float) = 0
        [HideInInspector] _DeathAmount ("Death Amount", Float) = 0
        [HideInInspector] _MoveStrength ("Move Strength", Float) = 0
        [HideInInspector] _MoveVector ("Move Vector", Vector) = (0,0,0,0)
        [HideInInspector] _SpriteBounds ("Sprite Bounds", Vector) = (-0.5,0,1,1)
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
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            fixed4 _ShadowColor;
            float4 _ShadowOffset;
            float4 _ShadowScale;
            float _Cutoff;
            float _DeathAmount;

            v2f ShadowVert(appdata_t input)
            {
                input.vertex.xy *= _ShadowScale.xy;
                input.vertex.xy += _ShadowOffset.xy;
                return SpriteVert(input);
            }

            fixed4 ShadowFrag(v2f input) : SV_Target
            {
                fixed4 sprite = SampleSpriteTexture(input.texcoord) * input.color;
                fixed4 shadow = _ShadowColor;
                shadow.a *= sprite.a * (1.0 - saturate(_Cutoff)) * (1.0 - saturate(_DeathAmount));
                return shadow;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex BossVert
            #pragma fragment BossFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            sampler2D _NoiseTex;
            fixed4 _BlendColor;
            fixed4 _EdgeColor;
            fixed4 _ShadowColor;
            float _Cutoff;
            float _EdgeWidth;
            float _AnimationTime;
            float _BossKind;
            float _Motion;
            float _CastAge;
            float _SkillIndex;
            float _HitPulse;
            float _DeathAmount;
            float _MoveStrength;
            float4 _MoveVector;
            float4 _SpriteBounds;

            float EllipseMask(float2 uv, float2 center, float2 radius)
            {
                float2 normalized = (uv - center) / max(radius, float2(0.001, 0.001));
                return 1.0 - smoothstep(0.78, 1.08, length(normalized));
            }

            float SegmentMask(float2 uv, float2 start, float2 end, float radius)
            {
                float2 segment = end - start;
                float t = saturate(dot(uv - start, segment) / max(dot(segment, segment), 0.0001));
                float distanceToSegment = length(uv - (start + segment * t));
                return 1.0 - smoothstep(radius * 0.72, radius * 1.45, distanceToSegment);
            }

            float2 RotateAround(float2 vertexPosition, float2 pivot, float angle)
            {
                float sine = sin(angle);
                float cosine = cos(angle);
                float2 offset = vertexPosition - pivot;
                return pivot + float2(cosine * offset.x - sine * offset.y,
                                      sine * offset.x + cosine * offset.y);
            }

            float2 LocalPivot(float2 uv)
            {
                return _SpriteBounds.xy + uv * _SpriteBounds.zw;
            }

            float2 DeformVertex(float2 uv, float2 originalPosition)
            {
                float kind = floor(_BossKind + 0.5);
                float time = _AnimationTime;
                float idle = 1.0 - step(0.5, abs(_Motion - 0.0));
                float moving = 1.0 - step(0.5, abs(_Motion - 1.0));
                float windup = 1.0 - step(0.5, abs(_Motion - 2.0));
                float attack = 1.0 - step(0.5, abs(_Motion - 3.0));
                float hitMotion = 1.0 - step(0.5, abs(_Motion - 4.0));
                float hit = max(hitMotion, saturate(_HitPulse));
                float death = 1.0 - step(0.5, abs(_Motion - 5.0));
                float castProgress = saturate(_CastAge / 1.1) * 0.45;
                float attackPulse = attack * exp2(-max(0.0, _CastAge - 1.1) * 5.0) * 0.22;
                float skillPhase = max(0.0, _SkillIndex) * 1.17;
                float move = max(moving, _MoveStrength * 0.65);

                float2 position = originalPosition;
                float2 displacement = 0.0;
                float torso = EllipseMask(uv, float2(0.5, 0.55), float2(0.2, 0.32));
                float head = EllipseMask(uv, float2(0.5, 0.79), float2(0.17, 0.14));
                float leftArm = max(EllipseMask(uv, float2(0.31, 0.57), float2(0.22, 0.17)),
                                    SegmentMask(uv, float2(0.43, 0.61), float2(0.27, 0.52), 0.07));
                float rightArm = max(EllipseMask(uv, float2(0.69, 0.57), float2(0.22, 0.17)),
                                     SegmentMask(uv, float2(0.57, 0.61), float2(0.73, 0.52), 0.07));
                float hem = 1.0 - smoothstep(0.12, 0.55, uv.y);
                float outer = saturate(abs(uv.x - 0.5) * 2.3);
                float strideWave = sin(time * 8.2 + uv.y * 9.0);

                // Torso, hood, sleeves, and trailing cloth each have separate weights.
                displacement.y += torso * (sin(time * 1.45) * 0.045 + strideWave * move * 0.085);
                displacement.x += torso * (_MoveVector.x * move * 0.035);
                displacement += (RotateAround(position, LocalPivot(float2(0.5, 0.77)),
                    sin(time * 1.15) * 0.025 + castProgress * windup * 0.08 - attackPulse * 0.11) - position) * head;

                float leftArmAngle = sin(time * 1.9 + 0.4) * 0.055 - castProgress * windup * 0.19 - attackPulse * 0.42;
                float rightArmAngle = -sin(time * 1.8 + 0.8) * 0.055 + castProgress * windup * 0.19 + attackPulse * 0.42;
                leftArmAngle += hit * sin(time * 62.0) * 0.075;
                rightArmAngle -= hit * sin(time * 59.0) * 0.075;
                displacement += (RotateAround(position, LocalPivot(float2(0.41, 0.61)), leftArmAngle) - position) * leftArm;
                displacement += (RotateAround(position, LocalPivot(float2(0.59, 0.61)), rightArmAngle) - position) * rightArm;

                float clothWave = sin(time * 2.05 + uv.y * 14.0 + uv.x * 5.0);
                displacement.x += hem * outer * clothWave * (0.055 + moving * 0.055 + windup * castProgress * 0.025);
                displacement.y += hem * outer * sin(time * 1.7 + uv.x * 11.0) * 0.028;

                // Each boss gets a different moving silhouette tied to its actual art.
                if (kind < 0.5)
                {
                    // Eclipse Wraith: lift and sweep the long crescent staff; float the torn shroud.
                    float staff = max(SegmentMask(uv, float2(0.36, 0.60), float2(0.34, 0.06), 0.045),
                                      EllipseMask(uv, float2(0.25, 0.88), float2(0.2, 0.12)));
                    float staffAngle = sin(time * 1.7) * 0.025 + windup * castProgress * 0.18 - attackPulse * 0.62;
                    displacement += (RotateAround(position, LocalPivot(float2(0.37, 0.59)), staffAngle) - position) * staff;
                    float shroud = max(EllipseMask(uv, float2(0.13, 0.41), float2(0.22, 0.28)),
                                       EllipseMask(uv, float2(0.86, 0.42), float2(0.2, 0.25)));
                    displacement.x += shroud * sin(time * 2.1 + uv.y * 12.0) * 0.1;
                }
                else if (kind < 1.5)
                {
                    // Thorn Regent: antler branches and the grounded root skirt flex independently.
                    float branches = max(SegmentMask(uv, float2(0.49, 0.8), float2(0.1, 0.96), 0.065),
                                         SegmentMask(uv, float2(0.51, 0.8), float2(0.9, 0.96), 0.065));
                    float roots = max(SegmentMask(uv, float2(0.49, 0.34), float2(0.13, 0.04), 0.09),
                                      SegmentMask(uv, float2(0.51, 0.34), float2(0.87, 0.04), 0.09));
                    displacement.x += branches * sin(time * 1.2 + uv.y * 5.0) * 0.11;
                    displacement.x += roots * sin(time * 1.7 + uv.x * 8.0) * 0.12;
                    displacement.y += roots * (sin(time * 1.4 + uv.x * 6.0) * 0.035 - attackPulse * 0.04);
                }
                else if (kind < 2.5)
                {
                    // Frost Mourner: crystal wings fan from the shoulders while ice ribbons trail.
                    float wings = max(SegmentMask(uv, float2(0.46, 0.67), float2(0.16, 0.87), 0.075),
                                      SegmentMask(uv, float2(0.54, 0.67), float2(0.84, 0.87), 0.075));
                    float ribbons = max(EllipseMask(uv, float2(0.16, 0.49), float2(0.17, 0.23)),
                                        EllipseMask(uv, float2(0.84, 0.49), float2(0.17, 0.23)));
                    float wingFan = sin(time * 1.55 + 0.3) * 0.035 + windup * castProgress * 0.16 + attackPulse * 0.28;
                    displacement += (RotateAround(position, LocalPivot(float2(0.5, 0.68)), wingFan) - position) * wings;
                    displacement.x += ribbons * sin(time * 2.3 + uv.y * 15.0) * 0.095;
                    displacement.y += ribbons * cos(time * 1.8 + uv.x * 9.0) * 0.035;
                }
                else if (kind < 3.5)
                {
                    // Crimson Harvester: the long execution scythe arcs across the body on release.
                    float shaft = max(SegmentMask(uv, float2(0.38, 0.64), float2(0.34, 0.04), 0.05),
                                      EllipseMask(uv, float2(0.2, 0.84), float2(0.22, 0.15)));
                    float bladeAngle = sin(time * 1.55) * 0.02 + windup * castProgress * 0.2 - attackPulse * 0.78;
                    displacement += (RotateAround(position, LocalPivot(float2(0.38, 0.62)), bladeAngle) - position) * shaft;
                    float cape = max(EllipseMask(uv, float2(0.17, 0.37), float2(0.2, 0.26)),
                                     EllipseMask(uv, float2(0.83, 0.39), float2(0.19, 0.25)));
                    displacement.x += cape * sin(time * 1.8 + uv.y * 13.0) * 0.09;
                }
                else
                {
                    // Abyss Oracle: multiple curled tentacles ripple with staggered phase offsets.
                    float leftTentacle = max(SegmentMask(uv, float2(0.42, 0.58), float2(0.12, 0.48), 0.075),
                                             SegmentMask(uv, float2(0.4, 0.43), float2(0.18, 0.24), 0.075));
                    float rightTentacle = max(SegmentMask(uv, float2(0.58, 0.58), float2(0.88, 0.48), 0.075),
                                              SegmentMask(uv, float2(0.6, 0.43), float2(0.82, 0.24), 0.075));
                    float tentaclePhase = time * 2.7 + uv.y * 17.0 + uv.x * 4.0;
                    float tentacleStrength = 0.11 + attackPulse * 0.12 + windup * castProgress * 0.045;
                    displacement.x += leftTentacle * sin(tentaclePhase) * tentacleStrength;
                    displacement.x -= rightTentacle * sin(tentaclePhase + 1.2) * tentacleStrength;
                    displacement.y += max(leftTentacle, rightTentacle) * sin(tentaclePhase * 0.72) * 0.035;
                }

                float charge = windup * castProgress;
                displacement.x += (uv.x - 0.5) * _SpriteBounds.z * charge * 0.035;
                displacement.y -= torso * charge * (0.045 + 0.018 * sin(time * 5.0 + skillPhase));
                displacement.x += (uv.x - 0.5) * _SpriteBounds.z * hit * sin(time * 44.0) * 0.025;
                displacement.y += torso * hit * sin(time * 55.0) * 0.035;

                // Death folds the upper silhouette downward as the dissolve clips it away.
                float deathEase = death * saturate(_DeathAmount);
                displacement.y -= uv.y * _SpriteBounds.w * deathEase * 0.31;
                displacement.x += (uv.x - 0.5) * _SpriteBounds.z * deathEase * 0.065;
                displacement.x += _MoveVector.x * moving * (uv.y - 0.5) * _SpriteBounds.z * 0.018;
                displacement.y += torso * idle * sin(time * 1.25) * 0.012;

                // Keep adjacent grid cells from folding or stretching the painted anatomy.
                float maxOffset = death > 0.5 ? _SpriteBounds.w * 0.4 : _SpriteBounds.w * 0.016;
                displacement *= min(1.0, maxOffset / max(length(displacement), 0.0001));
                return position + displacement;
            }

            v2f BossVert(appdata_t input)
            {
                input.vertex.xy = DeformVertex(input.texcoord, input.vertex.xy);
                return SpriteVert(input);
            }

            fixed4 BossFrag(v2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                float noise = tex2D(_NoiseTex, input.texcoord).r;
                if (_Cutoff > 0.001)
                    clip(noise - _Cutoff - 0.001);

                if (_EdgeWidth > 0.001 && noise < _Cutoff + _EdgeWidth)
                    color.rgb = _EdgeColor.rgb * input.color.rgb;

                // EnemyHit drives this additive tint; keep the source alpha untouched.
                color.rgb = saturate(color.rgb + _BlendColor.rgb);
                return color;
            }
            ENDCG
        }
    }
}

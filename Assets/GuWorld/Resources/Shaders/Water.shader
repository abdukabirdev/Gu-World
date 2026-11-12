// GU World's water: rivers, pools and lakes. Its surface is moved by waves that run with the
// current (a two-phase flow map, so the waves never stretch), fine ripples over them, the sky
// reflected by Fresnel (much at a low angle, little looking straight down), the sun's glitter, the
// colour of depth (clear and green at the shallows, dark teal in the deep, seen through by the
// scene's depth), foam where the water meets the bank and where it runs fast, and the fog.
// The mesh's vertex colour carries the flow: rg = the way it runs (x, z) * 0.5 + 0.5, b = how far
// from the bank (0 at the edge, 1 in the middle), a = how fast. Found by name (GuWorld/Water).
Shader "GuWorld/Water"
{
    Properties
    {
        _Deep("Deep colour", Color) = (0.02, 0.09, 0.11, 1)
        _Shallow("Shallow colour", Color) = (0.10, 0.27, 0.25, 1)
        _Foam("Foam colour", Color) = (0.88, 0.93, 0.95, 1)
        _Clarity("Clarity (metres seen into)", Range(0.2, 8)) = 2.2
        _WaveScale("Wave size (1 / metres)", Range(0.05, 1)) = 0.32
        _WaveStrength("Wave strength", Range(0, 2)) = 0.55
        _Ripples("Fine ripples", Range(0, 1)) = 0.35
        _FlowSpeed("Flow speed", Range(0, 4)) = 1.4
        _Sun("Sun glitter", Range(0, 8)) = 3.5
        _FoamShore("Foam at the bank (metres)", Range(0, 2)) = 0.6
        _FoamRapids("Foam in rapids", Range(0, 2)) = 1
        _Opacity("Least opacity", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-20" }

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Deep;
                half4 _Shallow;
                half4 _Foam;
                half _Clarity;
                half _WaveScale;
                half _WaveStrength;
                half _Ripples;
                half _FlowSpeed;
                half _Sun;
                half _FoamShore;
                half _FoamRapids;
                half _Opacity;
            CBUFFER_END

            // The sky's colours, set by the game's atmosphere every frame (a fallback grey-blue if not).
            half4 _GW_SkyTop;
            half4 _GW_SkyHorizon;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 colour : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screen : TEXCOORD1;
                half4 colour : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half fog : TEXCOORD4;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.screen = ComputeScreenPos(position.positionCS);
                output.colour = input.colour;
                output.uv = input.uv;
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            // ---------------------------------------------------------------- Noise

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Value noise with its gradient (for the surface's slope).
            float3 NoiseGrad(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float2 du = 6.0 * f * (1.0 - f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                float value = a + (b - a) * u.x + (c - a) * u.y + (a - b - c + d) * u.x * u.y;
                float2 grad = du * (float2(b - a, c - a) + (a - b - c + d) * u.yx);
                return float3(value, grad);
            }

            // Waves: three octaves turned against each other; returns the slope (dh/dx, dh/dz).
            float2 Waves(float2 p)
            {
                float2 slope = 0;
                float amplitude = 1;
                float2x2 turn = float2x2(0.8, -0.6, 0.6, 0.8);

                for (int octave = 0; octave < 3; octave++)
                {
                    float3 n = NoiseGrad(p);
                    slope += n.yz * amplitude;
                    p = mul(turn, p) * 2.03 + 17.1;
                    amplitude *= 0.5;
                }

                return slope;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 world = input.positionWS;
                float2 flow = input.colour.rg * 2 - 1;
                float flowLength = length(flow);
                flow = flowLength > 0.01 ? flow / flowLength : float2(0, 1);
                half speed = input.colour.a;
                half middle = input.colour.b;

                // Two phases of the flow map, half a cycle apart, faded so the reset is never seen.
                float time = _Time.y * 0.35;
                float phaseA = frac(time);
                float phaseB = frac(time + 0.5);
                float weightA = 1 - abs(1 - 2 * phaseA);
                float run = _FlowSpeed * (0.3 + speed * 2.2);
                float2 p = world.xz * _WaveScale;
                float2 slopeA = Waves(p - flow * phaseA * run + float2(0.0, 0.0));
                float2 slopeB = Waves(p - flow * phaseB * run + float2(5.2, 1.3));
                float2 slope = (slopeA * weightA + slopeB * (1 - weightA)) * _WaveStrength;

                // Fine ripples that do not follow the current (the wind's).
                slope += Waves(world.xz * 1.9 + _Time.y * float2(0.31, -0.23)) * _Ripples * 0.25;

                float3 normal = normalize(float3(-slope.x, 1, -slope.y));
                float3 view = normalize(GetWorldSpaceViewDir(world));

                // The depth of water under this point, from the scene's depth (if the camera keeps it).
                float2 screenUV = input.screen.xy / input.screen.w;
                float sceneRaw = SampleSceneDepth(screenUV + slope * 0.01);
                float sceneEye = LinearEyeDepth(sceneRaw, _ZBufferParams);
                float surfaceEye = input.screen.w;
                float depth = sceneEye - surfaceEye;
                // Without a depth texture (it reads as the far plane), fall back on the distance from the bank.
                depth = sceneRaw <= 0.000001 || sceneRaw >= 0.999999 ? middle * 3 : max(0, depth);

                half shallowShare = saturate(exp(-depth / _Clarity));
                half3 water = lerp(_Deep.rgb, _Shallow.rgb, shallowShare);

                // Light: the sun on the water, the shadows over it, the sky's light into it.
                Light sun = GetMainLight(TransformWorldToShadowCoord(world));
                half shadow = lerp(0.55, 1, sun.shadowAttenuation);
                water *= (0.35 + 0.65 * saturate(dot(normal, sun.direction) * 0.5 + 0.5)) * shadow;
                water += SampleSH(normal) * 0.12;

                // The sky reflected (Fresnel, Schlick's with the water's 0.02).
                half fresnel = 0.02 + 0.98 * pow(1 - saturate(dot(normal, view)), 5);
                float3 reflected = reflect(-view, normal);
                half3 top = _GW_SkyTop.a > 0 ? _GW_SkyTop.rgb : half3(0.25, 0.35, 0.5);
                half3 horizon = _GW_SkyHorizon.a > 0 ? _GW_SkyHorizon.rgb : half3(0.55, 0.62, 0.7);
                half3 sky = lerp(horizon, top, saturate(reflected.y * 1.6));
                half3 colour = lerp(water, sky, saturate(fresnel * 0.85));

                // The sun's glitter.
                float3 half_ = normalize(sun.direction + view);
                half glitter = pow(saturate(dot(normal, half_)), 180) * _Sun * shadow;
                colour += sun.color * glitter;

                // Foam: a band at the bank where the water is shallow, and the white of rapids.
                float foamNoise = NoiseGrad(world.xz * 0.9 - flow * _Time.y * run * 0.5).x;
                half shore = saturate(1 - depth / max(0.01, _FoamShore)) * step(0.35, foamNoise + 0.25);
                half rapids = saturate((speed - 0.45) * 2.5) * _FoamRapids * step(0.55, foamNoise + speed * 0.3);
                half foam = saturate(shore * 0.8 + rapids);
                colour = lerp(colour, _Foam.rgb * (0.6 + 0.4 * shadow), foam);

                // Seen through where it is shallow, and at the bank it thins out to nothing.
                half alpha = saturate(lerp(_Opacity, 1, 1 - shallowShare) + fresnel * 0.4 + foam);
                alpha *= saturate(depth * 4);
                colour = MixFog(colour, input.fog);

                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}

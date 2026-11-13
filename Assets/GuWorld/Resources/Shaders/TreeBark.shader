// GU World's trees: the bark. A tiling bark texture with its height map, moss laid over it by a
// weight in the vertices' red (and the moss's own height, so it creeps in from the hollows), shaded
// by the vertices' green (the damp dark between buttresses, the dim inside of the crown). The
// relief is lit from the height maps themselves — bumped by their screen-space slopes, so the
// mesh needs no tangents. Half-Lambert light with the main light's shadows, ambient from the sky,
// additional lights, fog. Instancing on: the woods are drawn in batches.
Shader "GuWorld/TreeBark"
{
    Properties
    {
        [MainTexture] _BaseMap("Bark", 2D) = "gray" {}
        [MainColor] _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _HeightMap("Bark height", 2D) = "gray" {}
        _MossMap("Moss", 2D) = "gray" {}
        _MossHeight("Moss height", 2D) = "gray" {}
        _MossTile("Moss tiles per bark tile", Range(0.5, 4)) = 1.7
        _Bump("Relief depth (m)", Range(0, 0.2)) = 0.06
        _Cutoff("Alpha cut-off", Range(0, 1)) = 0.5
        _Wrap("Light wrap (half-Lambert share)", Range(0, 1)) = 0.3
        _Ambient("Ambient share", Range(0, 2)) = 0.9
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half _Wrap;
            half _Ambient;
            half _MossTile;
            half _Bump;
        CBUFFER_END

        TEXTURE2D(_HeightMap);
        TEXTURE2D(_MossMap);
        TEXTURE2D(_MossHeight);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 colour : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fog : TEXCOORD3;
                half4 colour : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fog = ComputeFogFactor(output.positionCS.z);
                output.colour = input.colour;

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
                float2 mossUV = uv * _MossTile;
                half3 bark = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb;
                half barkHeight = SAMPLE_TEXTURE2D(_HeightMap, sampler_BaseMap, uv).r;
                half3 moss = SAMPLE_TEXTURE2D(_MossMap, sampler_BaseMap, mossUV).rgb;
                half mossHeight = SAMPLE_TEXTURE2D(_MossHeight, sampler_BaseMap, mossUV).r;

                // Moss where the vertices call for it, creeping up out of the bark's hollows first.
                half cover = saturate((input.colour.r * 2 - 1 + (mossHeight - 0.5) * 0.9 + (0.5 - barkHeight) * 0.5) * 3 + 0.5);
                half3 albedo = lerp(bark, moss, cover) * input.colour.g;
                float height = lerp(barkHeight, 0.55 + mossHeight * 0.45, cover) * _Bump;

                // The relief: the normal turned by the height's slopes across the screen (no tangents needed).
                float3 normal = normalize(input.normalWS);
                float3 dpx = ddx(input.positionWS);
                float3 dpy = ddy(input.positionWS);
                float3 r1 = cross(dpy, normal);
                float3 r2 = cross(normal, dpx);
                float det = dot(dpx, r1);
                float3 slope = sign(det) * (ddx(height) * r1 + ddy(height) * r2);
                normal = normalize(abs(det) * normal - slope);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light main = GetMainLight(shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half lambert = dot(normal, main.direction);
                half wrapped = saturate((lambert + _Wrap) / (1 + _Wrap));
                half3 light = main.color * main.distanceAttenuation * wrapped * lerp(0.3, 1, main.shadowAttenuation);
                half3 ambient = max(half3(0, 0, 0), SampleSH(normal)) * _Ambient;

                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light extra = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                    light += extra.color * extra.distanceAttenuation * saturate(dot(normal, extra.direction) * 0.5 + 0.5);
                LIGHT_LOOP_END
                #endif

                half3 colour = albedo * (light + ambient);
                colour = MixFog(colour, input.fog);

                return half4(colour, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

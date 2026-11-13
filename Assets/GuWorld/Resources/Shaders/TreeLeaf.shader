// GU World's trees: the foliage. Sprays of leaves, needles or blossom painted with alpha, on
// cards: cut out by the alpha, drawn from both sides, tinted and shaded by the vertices' colour,
// swaying in the wind by the vertices' alpha (0 at the twig, 1 at the tips). The normals are the
// generator's soft ones (out from the crown), so a whole tree lights as one mass; a little of the
// light comes through from behind. Instancing on.
Shader "GuWorld/TreeLeaf"
{
    Properties
    {
        [MainTexture] _BaseMap("Foliage", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha cut-off", Range(0, 1)) = 0.4
        _Wrap("Light wrap (half-Lambert share)", Range(0, 1)) = 0.5
        _Ambient("Ambient share", Range(0, 2)) = 1
        _Through("Light through the leaves", Range(0, 1)) = 0.35
        _Sway("Sway in the wind (m)", Range(0, 1)) = 0.18
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half _Wrap;
            half _Ambient;
            half _Through;
            half _Sway;
        CBUFFER_END

        // The wind: a slow wave across the world and a quick flutter, by how free the vertex is.
        float3 Swayed(float3 positionWS, half free)
        {
            float wave = sin(_Time.y * 1.3 + positionWS.x * 0.11 + positionWS.z * 0.07) + 0.4 * sin(_Time.y * 2.9 + positionWS.z * 0.23);
            float flutter = sin(_Time.y * 6.1 + positionWS.x * 2.7 + positionWS.y * 1.9 + positionWS.z * 2.3);
            positionWS.x += (wave + 0.25 * flutter) * _Sway * free;
            positionWS.z += (wave * 0.6 - 0.25 * flutter) * _Sway * free;
            positionWS.y += 0.35 * flutter * _Sway * free;
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
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
                positionWS = Swayed(positionWS, input.colour.a);
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

                half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(texel.a - _Cutoff);
                half3 albedo = texel.rgb * _BaseColor.rgb * input.colour.rgb;
                half3 normal = normalize(input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light main = GetMainLight(shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half lambert = dot(normal, main.direction);
                half wrapped = saturate((lambert + _Wrap) / (1 + _Wrap));
                // Light from behind glows through the leaves.
                half through = saturate(-lambert) * _Through;
                half3 light = main.color * main.distanceAttenuation * (wrapped + through) * lerp(0.35, 1, main.shadowAttenuation);
                half3 ambient = max(half3(0, 0, 0), SampleSH(normal)) * _Ambient;
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
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #define _ALPHATEST_ON 1
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
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #define _ALPHATEST_ON 1
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

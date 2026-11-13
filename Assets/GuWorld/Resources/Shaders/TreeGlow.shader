// GU World's trees: the glowing moss of the ancient ones. Unlit: the vertices' colour times the
// tint, breathing slowly, the same by day and by night (bloom does the rest on high quality).
Shader "GuWorld/TreeGlow"
{
    Properties
    {
        [MainColor] _BaseColor("Glow", Color) = (0.6, 1.6, 1.2, 1)
        _Breath("Breathing share", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Breath;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 colour : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 colour : COLOR;
                half fog : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.colour = input.colour;
                output.fog = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half breath = 1 - _Breath * (0.5 + 0.5 * sin(_Time.y * 0.9 + input.positionWS.x * 1.3 + input.positionWS.y * 0.7));
                half3 colour = input.colour.rgb * _BaseColor.rgb * breath;
                colour = MixFog(colour, input.fog);

                return half4(colour, 1);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

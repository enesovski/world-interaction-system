
Shader "Archura/WIS/Water Ripple Debug"
{
    Properties
    {
        _RippleTex("Ripple Texture", 2D) = "gray" {}
        _Contrast("Contrast", Range(0.1, 10)) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_RippleTex);
            SAMPLER(sampler_RippleTex);

            float _Contrast;

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float height = SAMPLE_TEXTURE2D(
                    _RippleTex,
                    sampler_RippleTex,
                    input.uv
                ).r;

                float value = saturate(0.5 + height * _Contrast);

                return half4(value, value, value, 1);
            }
            ENDHLSL
        }
    }
}

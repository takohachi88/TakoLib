Shader "Hiddden/TakoLib/PostProcess/Painting"
{
    SubShader
    {
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

        half _Intensity;
        float _AspectRatio;
        int _SampleCount;

        half4 SnnFilter(float2 uv)
        {
            half4 output = 0;

            float2 cellSize = _Intensity;
            half3 color0 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb;

            int count = 0;
            float2 offset = -_SampleCount * cellSize;
            for (int x = 0; x <= _SampleCount; ++x)
            {
                offset.y = -_SampleCount * cellSize.y;

                for (int y = -_SampleCount; y <= _SampleCount; ++y)
                {
                    if (x == 0 && y <= 0)
                    {
                        continue;
                    }
                    half3 color1 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset).rgb;
                    half3 color2 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - offset).rgb;
                    float3 diff1 = color1 - color0.rgb;
                    float3 diff2 = color2 - color0.rgb;
                    output.rgb += dot(diff1, diff1) < dot(diff2, diff2) ? color1 : color2;
                    count++;
                    offset.y += cellSize.y;
                }
                offset.x += cellSize.x;
            }

            output.rgb *= rcp(count);
            return output;

        }

        half4 Fragment (Varyings input) : SV_Target
        {
            half4 output = SnnFilter(input.texcoord);

            return output;
        }

        ENDHLSL

        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }
        ZWrite Off
        Cull Off
        ZTest Always


        Pass
        {            
            Name "SNN Filter"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Fragment

            ENDHLSL
        }
    }
}
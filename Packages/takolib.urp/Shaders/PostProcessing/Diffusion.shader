Shader "Hiddden/TakoLib/PostProcess/Diffusion"
{
    SubShader
    {
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

        half _Intensity;
        half _Threshold;
        float2 _BlurDirection;
        int _BlendMode;
        float _AspectRatio;

        TEXTURE2D_X(_DiffusionTexture);
        TEXTURE2D_X(_DiffusionMipTexture);


        half4 FragmentPrefilter (Varyings input) : SV_Target
        {
            half4 output = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
            output.rgb = max(_Threshold, output.rgb) - _Threshold;
            return output;
        }



        half4 FragmentBlur (Varyings input) : SV_Target
        {
            half4 output = 0;

            half rcpSampleCount = rcp(5);

            for(int i = -2; i <= 2; i++)
            {
                float2 direction = i * rcpSampleCount * _BlurDirection;
                direction.y *= _AspectRatio;
                float2 uv = input.texcoord + direction;
                output += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }

            output.rgb *= rcpSampleCount;

            return output;
        }

        half4 FragmentUpSampling (Varyings input) : SV_Target
        {
            half4 output = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
            output.rgb += SAMPLE_TEXTURE2D_X(_DiffusionMipTexture, sampler_LinearClamp, input.texcoord).rgb;
            output.rgb *= 0.5;
            return output;
        }

        half4 FragmentComposite (Varyings input) : SV_Target
        {
            half4 output = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
            half3 diffusion = SAMPLE_TEXTURE2D_X(_DiffusionTexture, sampler_LinearClamp, input.texcoord).rgb;

            half3 color = 0;
            color += (_BlendMode == 0) * lerp(output.rgb, diffusion, saturate(_Intensity));
            color += (_BlendMode == 1) * (output.rgb + _Intensity * diffusion);
            color += (_BlendMode == 2) * lerp(output.rgb, output.rgb * diffusion, saturate(_Intensity));
            color += (_BlendMode == 3) * lerp(output.rgb, (1 - output.rgb) * diffusion, _Intensity);
            
            output.rgb = color;

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
            Name "Prefilter"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment FragmentPrefilter

            ENDHLSL
        }

        Pass
        {            
            Name "Blur"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment FragmentBlur
            #pragma multi_compile_local _ _COMPOSITE
            
            ENDHLSL
        }

        Pass
        {            
            Name "UpSampling"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment FragmentUpSampling
            
            ENDHLSL
        }

        Pass
        {            
            Name "Composite"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment FragmentComposite
            
            ENDHLSL
        }
    }
}
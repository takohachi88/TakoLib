Shader "Hiddden/TakoLib/PostProcess/TemporalBlur"
{
    SubShader
    {
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

        half _Intensity;

        TEXTURE2D(_PreviousBlitTexture);

        half4 Fragment (Varyings input) : SV_Target
        {
            half4 output = 0;

            half4 current = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
            half4 previous = SAMPLE_TEXTURE2D(_PreviousBlitTexture, sampler_LinearClamp, input.texcoord);

            output = lerp(current, previous, _Intensity);

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
            Name "Temporal Blur"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Fragment

            ENDHLSL
        }
    }
}
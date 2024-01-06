Shader "Hiddden/TakoLib/PostProcess/Uber"
{
    SubShader
    {
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

        half _VignetteIntensity;
        half _Smoothness;
        float2 _VignetteCenter;
        half3 _VignetteColor;
        int _Rounded;
        float _AspectRatio;
        int _BlendMode;
        int _SampleCount;

        half4 Fragment (Varyings input) : SV_Target
        {
            half4 output = 0;

            output += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);

            #if defined(_VIGNETTE)
            float2 dist = abs(input.texcoord - _VignetteCenter) * _VignetteIntensity;
            dist.x *= _Rounded ? _AspectRatio : 1;
            half vignette = saturate(1 - pow(saturate(1.0 - dot(dist, dist)), _Smoothness + HALF_EPS));
            
            half3 color = 0;
            color += (_BlendMode == 0) * lerp(output.rgb, _VignetteColor, vignette);
            color += (_BlendMode == 1) * (output.rgb + vignette * _VignetteColor);
            color += (_BlendMode == 2) * lerp(output.rgb, output.rgb * _VignetteColor, vignette);
            color += (_BlendMode == 3) * lerp(output.rgb, (1 - output.rgb) * _VignetteColor, vignette);
            
            output.rgb = color;

            #endif

            return output;
        }

        ENDHLSL

        Tags
        {
            "RenderPipeline" = "UniversalRenderPipeline"
        }
        ZWrite Off
        Cull Off
        ZTest Always


        Pass
        {            
            Name "Composite"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Fragment

            #pragma multi_compile_local _ _VIGNETTE
            
            ENDHLSL
        }
    }
}
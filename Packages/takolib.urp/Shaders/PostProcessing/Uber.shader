Shader "Hiddden/TakoLib/PostProcess/Uber"
{
    SubShader
    {
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

        half _VignetteIntensity;
        half _VignetteSmoothness;
        float2 _VignetteCenter;
        half3 _VignetteColor;
        int _Rounded;
        float _AspectRatio;
        int _BlendMode;

        half _MosaicIntensity;
        half _MosaicCellDensity;

        half _PosterizationIntensity;
        int _ToneCount;

        bool _Nega;
        half _NegaIntensity;

        half4 Vignette (Varyings input, half4 destination)
        {
            half4 output = destination;

            float2 dist = abs(input.texcoord - _VignetteCenter) * _VignetteIntensity;
            dist.x *= _Rounded ? _AspectRatio : 1;
            half vignette = saturate(1 - pow(saturate(1.0 - dot(dist, dist)), _VignetteSmoothness + HALF_EPS));
            
            half3 color = 0;
            color += (_BlendMode == 0) * lerp(output.rgb, _VignetteColor, vignette);
            color += (_BlendMode == 1) * (output.rgb + vignette * _VignetteColor);
            color += (_BlendMode == 2) * lerp(output.rgb, output.rgb * _VignetteColor, vignette);
            color += (_BlendMode == 3) * lerp(output.rgb, (1 - output.rgb) * _VignetteColor, vignette);
            
            output.rgb = color;

            return output;
        }

        half4 Fragment (Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;

            if(0 < _MosaicIntensity)
            {
                float cellDensity = lerp(_ScreenParams.x, _MosaicCellDensity, _MosaicIntensity);
                uv -= 0.5;
                uv.x *= _AspectRatio;
                uv = round(uv * cellDensity) * rcp(cellDensity);
                uv.x *= rcp(_AspectRatio);
                uv += 0.5;
            }

            half4 output = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

            if(0 < _PosterizationIntensity)
            {
                output.rgb = lerp(output.rgb, round(output.rgb * _ToneCount) * rcp(_ToneCount), _PosterizationIntensity);
            }

            if(_Nega) output.rgb = lerp(output.rgb, (1 - output.rgb), _NegaIntensity);

            #if defined(_VIGNETTE)

            output = Vignette(input, output);

            #endif

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
            Name "Composite"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Fragment

            #pragma multi_compile_local _ _VIGNETTE
            
            ENDHLSL
        }
    }
}
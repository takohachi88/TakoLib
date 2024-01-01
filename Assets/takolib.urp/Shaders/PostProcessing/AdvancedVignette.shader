Shader "Hiddden/TakoLib/PostProcess/AdvancedVignette"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalRenderPipeline"
        }
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half _Intensity;
            half _Smoothness;
            float2 _Center;
            half3 _Color;
            float _Roundness;
            uint _BlendMode;

half3 ApplyVignette(half3 input, float2 uv, float2 center, float intensity, float roundness, float smoothness, half3 color)
{
    center = UnityStereoTransformScreenSpaceTex(center);
    float2 dist = abs(uv - center) * intensity;

#if defined(UNITY_SINGLE_PASS_STEREO)
    dist.x /= unity_StereoScaleOffset[unity_StereoEyeIndex].x;
#endif

    dist.x *= roundness;
    float vfactor = pow(saturate(1.0 - dot(dist, dist)), smoothness);
    return input * lerp(color, (1.0).xxx, vfactor);
}

            half4 Fragment (Varyings input) : SV_Target
            {
                half4 output = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);

                float2 dist = abs(input.texcoord - _Center) * _Intensity;
                dist.x *= _Roundness;
                half vignette = saturate(1 - pow(saturate(1.0 - dot(dist, dist)), _Smoothness + HALF_EPS));
                
                half3 color = 0;
                color += (_BlendMode == 0) * lerp(output.rgb, _Color, vignette);
                color += (_BlendMode == 1) * (output.rgb + vignette * _Color);
                color += (_BlendMode == 2) * lerp(output.rgb, output.rgb * _Color, vignette);
                color += (_BlendMode == 3) * lerp(output.rgb, (1 - output.rgb) * _Color, vignette);
                
                output.rgb = color;

                return output;
            }
            
            ENDHLSL
        }
    }
}

Shader "Hidden/TakoLib/PostProcess/SmartDof"
{
    HLSLINCLUDE

        #pragma exclude_renderers gles
        #pragma multi_compile_local_fragment _ _USE_FAST_SRGB_LINEAR_CONVERSION

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"



        half _FocusDistance;
        half _FocusWidth;
        half _FocusSmooth;

        half _Intensity;

        float _AspectRatio;

        int _SampleCount;

        TEXTURE2D(_TempTarget1);
        TEXTURE2D(_TempTarget2);


        half4 FragFar(Varyings input) : SV_Target
        {
            half depth = SampleSceneDepth(input.texcoord);
            half4 blitTexture = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
            return lerp(0, blitTexture, step(depth, _FocusDistance - _FocusWidth * 0.5));
        }

        half4 FragNear(Varyings input) : SV_Target
        {
            half depth = SampleSceneDepth(input.texcoord);
            half4 blitTexture = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
            return lerp(0, blitTexture, step(_FocusDistance + _FocusWidth * 0.5, depth));
        }


        half4 Blur(texture2D tex, float2 uv, float2 positionCS, half2 direction, int sampleCount)
        {
            half4 color = 0;

            UNITY_LOOP
            for(int i = 0; i < sampleCount; i++)
            {
                half random = InterleavedGradientNoise(positionCS, 0);
                float2 offset = direction * ((i + random - 0.5 - (sampleCount - 1) * 0.5) / (float)(sampleCount - 1));
                color += SAMPLE_TEXTURE2D_X(tex, sampler_LinearClamp, uv + offset);
            }
            color *= rcp(sampleCount);
            return color;
        }

        half GetIntensity(float2 uv)
        {
            half depth = SampleSceneDepth(uv);
            half far = _FocusDistance + _FocusWidth * 0.5;
            half near = _FocusDistance - _FocusWidth * 0.5;
            return (smoothstep(far, far + _FocusSmooth, depth) + 1 - smoothstep(near - _FocusSmooth, near, depth)) * _Intensity;
        }


        half4 FragBlurHorizontal(Varyings input) : SV_Target
        {
            half intensity = GetIntensity(input.texcoord);
            return Blur(_BlitTexture, input.texcoord, input.positionCS.xy, float2(intensity, 0), _SampleCount);
        }


        half4 FragBlurVertical(Varyings input) : SV_Target
        {
            half intensity = GetIntensity(input.texcoord);
            return Blur(_BlitTexture, input.texcoord, input.positionCS.xy, float2(0, intensity * _AspectRatio), _SampleCount);
        }

        
        half4 FragComposite(Varyings input) : SV_Target
        {
            half4 output = 0;
            half4 blitTexture = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
            half4 far = SAMPLE_TEXTURE2D_X(_TempTarget1, sampler_LinearClamp, input.texcoord);
            half4 near = SAMPLE_TEXTURE2D_X(_TempTarget2, sampler_LinearClamp, input.texcoord);
            output.rgb = lerp(blitTexture.rgb, far.rgb, far.a);
            output.rgb = lerp(output.rgb, near.rgb, near.a);
            return output;
        }
        


    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "Bokeh Split Far"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragFar
            ENDHLSL
        }

        Pass
        {
            Name "Bokeh Split Near"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragNear
            ENDHLSL
        }

        Pass
        {
            Name "Bokeh Blur Horizontal"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBlurHorizontal
            ENDHLSL
        }

        Pass
        {
            Name "Bokeh Blur Vertical"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBlurVertical
            ENDHLSL
        }

        Pass
        {
            Name "Bokeh Composite"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragComposite
            ENDHLSL
        }
    }
}

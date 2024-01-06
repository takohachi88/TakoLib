Shader "Hiddden/TakoLib/PostProcess/MovieBasic"
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

        HLSLINCLUDE

        #pragma multi_compile_local _CONTROL_MODE_NONE _CONTROL_MODE_FRINGE _CONTROL_MODE_TEXTURE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

        half _Intensity;
        half _BlurIntensity;
        half _ChromaticAberrationIntensity;
        half _VignetteIntensity;
        half _VignetteSmoothness;
        float2 _Center;
        float2 _Direction;
        int _Rounded;
        float _AspectRatio;
        int _SampleCount;
        int _BlendMode;
        half _Smoothness;

        half3 _ControlIntensity; //R:intensity, GB: direction

        half4 _VignetteColor;
        float2 _BlurDirection;

        TEXTURE2D_X(_ControlTexture);
        SAMPLER(sampler_ControlTexture);

        half3 Control(float2 uv, half intensity)
        {
            half3 output = _ControlIntensity;

            #if defined(_CONTROL_MODE_NONE)
            
            output.r *= intensity;
            output.gb *= _Direction * intensity;
            
            #elif defined(_CONTROL_MODE_FRINGE)

            uv.x = _Rounded ? (uv.x - _Center.x) * _AspectRatio + _Center.x : uv.x;
            output.r = distance(_Center, uv);
            output.r = smoothstep(1 - _Smoothness, 1, output.r);
            output.r *= intensity;
            output.gb = (_Center - uv) * output.r;

            #elif defined(_CONTROL_MODE_TEXTURE)

            half4 controlTexture = SAMPLE_TEXTURE2D_X(_ControlTexture, sampler_ControlTexture, uv);
            output.r *= controlTexture.r * intensity;
            output.gb += (controlTexture.gb - 0.5) * intensity * output.r;

            //TODO: ŽÀ‘•
            
            #endif

            return output;
        }

        half4 FragmentChromaticAberration (Varyings input) : SV_Target
        {
            half4 output = 0;
            float2 uv = input.texcoord;
            float intensity = _Intensity * _ChromaticAberrationIntensity;
            half3 control = Control(uv, intensity);

            #if defined(_CONTROL_MODE_NONE)

            output.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + control.gb).r;
            output.g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - control.gb).g;
            output.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).b;

            #elif defined(_CONTROL_MODE_FRINGE)

            output.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).r;
            output.g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + control.gb * 0.5).g;
            output.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + control.gb).b;


            #elif defined(_CONTROL_MODE_TEXTURE)

            //TODO: ŽÀ‘•

            #endif

            return output;
        }

        half4 Blur(float2 uv, float2 positionCS, float2 direction)
        {
            half4 output = 0;
            half rcpSampleCount = rcp(_SampleCount);
            half random = InterleavedGradientNoise(positionCS.xy, 0);
            direction.y *= _AspectRatio;
            half control = Control(uv, _Intensity * _BlurIntensity).x;
            half2 range = control.r * direction;

            for(int i = 0; i < _SampleCount; i++)
            {
                float t = (i + random) * rcpSampleCount;
                float2 offset = lerp(-range, range, t);
                output += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offset);
            }

            output *= rcpSampleCount;

            return output;
        }

        half4 FragmentBlur (Varyings input) : SV_Target
        {
            return Blur(input.texcoord, input.positionCS.xy, _BlurDirection);
        }


        half4 FragmentVignette (Varyings input) : SV_Target
        {
            half4 output = 0;

            output += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);

            float2 dist = abs(input.texcoord - _Center) * _VignetteIntensity * _Intensity;
            dist.x *= _Rounded ? _AspectRatio : 1;
            half vignette = saturate(1 - pow(saturate(1.0 - dot(dist, dist)), _VignetteSmoothness * _Smoothness + HALF_EPS));
            
            half3 color = 0;
            color += (_BlendMode == 0) * lerp(output.rgb, _VignetteColor, vignette);
            color += (_BlendMode == 1) * (output.rgb + vignette * _VignetteColor);
            color += (_BlendMode == 2) * lerp(output.rgb, output.rgb * _VignetteColor, vignette);
            color += (_BlendMode == 3) * lerp(output.rgb, (1 - output.rgb) * _VignetteColor, vignette);
            
            output.rgb = color;

            return output;
        }

        ENDHLSL

        Pass
        {
            Name "ChromaticAberration"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragmentChromaticAberration

            ENDHLSL
        }

        Pass
        {
            Name "DirectionalBlur"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragmentBlur

            ENDHLSL
        }

        Pass
        {            
            Name "Vignette"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragmentVignette

            ENDHLSL
        }

    }
}

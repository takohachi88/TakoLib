using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace TakoLib.Urp
{
    public static class TakoLibUrpCommon
    {
        public static class ShaderId
        {
            public static readonly int Intensity = Shader.PropertyToID("_Intensity");
            public static readonly int Center = Shader.PropertyToID("_Center");
            public static readonly int SampleCount = Shader.PropertyToID("_SampleCount");
            public static readonly int Texture = Shader.PropertyToID("_Texture");
            public static readonly int Tiling = Shader.PropertyToID("_Tiling");
            public static readonly int Direction = Shader.PropertyToID("_Direction");

            public static readonly int FullCoCTexture = Shader.PropertyToID("_FullCoCTexture");
            public static readonly int HalfCoCTexture = Shader.PropertyToID("_HalfCoCTexture");
            public static readonly int DofTexture = Shader.PropertyToID("_DofTexture");
            public static readonly int CoCParams = Shader.PropertyToID("_CoCParams");
            public static readonly int BokehKernel = Shader.PropertyToID("_BokehKernel");
            public static readonly int BokehConstants = Shader.PropertyToID("_BokehConstants");
            public static readonly int SourceSize = Shader.PropertyToID("_SourceSize");
            public static readonly int DownSampleScaleFactor = Shader.PropertyToID("_DownSampleScaleFactor");

            public static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
            public static readonly int Color = Shader.PropertyToID("_Color");
            public static readonly int VignetteColor = Shader.PropertyToID("_VignetteColor");
            public static readonly int Rounded = Shader.PropertyToID("_Rounded");
            public static readonly int BlendMode = Shader.PropertyToID("_BlendMode");
            
            public static readonly int FocusDistance = Shader.PropertyToID("_FocusDistance");
            public static readonly int FocusWidth = Shader.PropertyToID("_FocusWidth");
            public static readonly int FocusSmooth = Shader.PropertyToID("_FocusSmooth");
            
            public static readonly int Temptarget1 = Shader.PropertyToID("_TempTarget1");
            public static readonly int TempTarget2 = Shader.PropertyToID("_TempTarget2");
            public static readonly int AspectRatio = Shader.PropertyToID("_AspectRatio");
            public static readonly int VignetteIntensity = Shader.PropertyToID("_VignetteIntensity");
            public static readonly int VignetteSmoothness = Shader.PropertyToID("_VignetteSmoothness");
            public static readonly int VignetteCenter = Shader.PropertyToID("_VignetteCenter");
            public static readonly int BlurCenter = Shader.PropertyToID("_BlurCenter");
            public static readonly int BlurDirection = Shader.PropertyToID("_BlurDirection");
            public static readonly int ControlIntensity = Shader.PropertyToID("_ControlIntensity");
            public static readonly int ControlTextureRgb = Shader.PropertyToID("_ControlTextureRgb");
            public static readonly int ControlTiling = Shader.PropertyToID("_ControlTiling");

            public static readonly int BlurIntensity = Shader.PropertyToID("_BlurIntensity");
            public static readonly int BlurScale = Shader.PropertyToID("_BlurScale");
            public static readonly int NoiseGradientTexture = Shader.PropertyToID("_NoiseGradientTexture");
            public static readonly int NoiseTiling = Shader.PropertyToID("_NoiseTiling");
            public static readonly int NoiseIntensity = Shader.PropertyToID("_NoiseIntensity");
            public static readonly int ChromaticAberrationIntensity = Shader.PropertyToID("_ChromaticAberrationIntensity");
            public static readonly int ChromaticAberrationLimit = Shader.PropertyToID("_ChromaticAberrationLimit");


        }

        public static class Keyword
        {
            public static readonly string UseFastSrgbLinearConversion = "_USE_FAST_SRGB_LINEAR_CONVERSION";
            public static readonly string Dither = "_DITHER";
            public static readonly string NoiseGradientTexture = "_NOISE_GRADIENT_TEXTURE";
            public static readonly string ControlModeNone = "_CONTROL_MODE_NONE";
            public static readonly string ControlModeFringe = "_CONTROL_MODE_FRINGE";
            public static readonly string ControlModeTexture = "_CONTROL_MODE_TEXTURE";
            public static readonly string Vignette = "_VIGNETTE";
        }

        public static void SetSourceSize(CommandBuffer cmd, RenderTextureDescriptor desc)
        {
            float width = desc.width;
            float height = desc.height;
            if (desc.useDynamicScale)
            {
                width *= ScalableBufferManager.widthScaleFactor;
                height *= ScalableBufferManager.heightScaleFactor;
            }
            cmd.SetGlobalVector(ShaderId.SourceSize, new Vector4(width, height, 1.0f / width, 1.0f / height));
        }

        public static RenderTextureDescriptor PostProcessDescriptor(int width, int height, GraphicsFormat format)
        {
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height);
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = (int)DepthBits.None;
            descriptor.graphicsFormat = format;
            return descriptor;
        }
    }
}
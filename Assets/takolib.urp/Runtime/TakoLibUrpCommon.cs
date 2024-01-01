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
            public static readonly int Roundness = Shader.PropertyToID("_Roundness");
            public static readonly int BlendMode = Shader.PropertyToID("_BlendMode");


        }

        public static class Keyword
        {
            public static readonly string UseFastSrgbLinearConversion = "_USE_FAST_SRGB_LINEAR_CONVERSION";
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
using TakoLib.Common.Extensions;
using UnityEngine;
using UnityEngine.Rendering;

namespace TakoLib.Common
{
    public enum AlphaBlendMode
    {
        Custom,
        Opqaue,
        AlphaTest,
        Transparent,
        Additive,
        Mutiply,
        Screen,
        Nega,
        Subtractive,
    }

    public enum VertexColorBlendMode
    {
        Mutiply,
        Additive,
    }

    public static class ShaderUtility
    {
        public static readonly int IdDefault = Shader.PropertyToID("_IdDefault");
        public static readonly int IdIn = Shader.PropertyToID("_IdIn");
        public static readonly int IdIdle = Shader.PropertyToID("_IdIdle");
        public static readonly int IdOut = Shader.PropertyToID("_IdOut");

        public static readonly int IdBlendSrc = Shader.PropertyToID("_BlendSrc");
        public static readonly int IdBlendDst = Shader.PropertyToID("_BlendDst");
        public static readonly int IdMultiplyRgbA = Shader.PropertyToID("_MultiplyRgbA");
        public static readonly int IdBlendOp = Shader.PropertyToID("_BlendOp");
        public static readonly int IdAlphaBlend = Shader.PropertyToID("_AlphaBlend");
        public static readonly int IdVertexColorBlend = Shader.PropertyToID("_VertexColorBlend");
        public static readonly int IdZWrite = Shader.PropertyToID("_ZWrite");

        public static bool HasVertexColorBlendRequiredProperties(Material material)
         => material.HasFloat(IdBlendSrc) &&
            material.HasFloat(IdBlendDst) &&
            material.HasFloat(IdMultiplyRgbA) &&
            material.HasFloat(IdBlendOp) &&
            material.HasFloat(IdAlphaBlend);

        public static bool HasAlphaBlendRequiredProperties(Material material)
         => material.HasFloat(IdVertexColorBlend);

        public static void SetAlphaBlendMode(Material material, AlphaBlendMode mode, bool validate)
        {
            material.SetFloat(IdAlphaBlend, (float)mode);

            switch (mode)
            {
                case AlphaBlendMode.Custom: return;
                case AlphaBlendMode.Opqaue: SetAlphaBlendMode(material, BlendMode.One, BlendMode.Zero, false, BlendOp.Add); break;
                case AlphaBlendMode.AlphaTest: SetAlphaBlendMode(material, BlendMode.One, BlendMode.Zero, false, BlendOp.Add); break;
                case AlphaBlendMode.Transparent: SetAlphaBlendMode(material, BlendMode.One, BlendMode.OneMinusSrcAlpha, true, BlendOp.Add); break;
                case AlphaBlendMode.Additive: SetAlphaBlendMode(material, BlendMode.SrcAlpha, BlendMode.One, false, BlendOp.Add); break;
                case AlphaBlendMode.Mutiply: SetAlphaBlendMode(material, BlendMode.DstColor, BlendMode.OneMinusSrcAlpha, true, BlendOp.Add); break;
                case AlphaBlendMode.Screen: SetAlphaBlendMode(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcColor, true, BlendOp.Add); break;
                case AlphaBlendMode.Nega: SetAlphaBlendMode(material, BlendMode.OneMinusDstColor, BlendMode.OneMinusSrcAlpha, true, BlendOp.Add); break;
                case AlphaBlendMode.Subtractive: SetAlphaBlendMode(material, BlendMode.SrcAlpha, BlendMode.One, true, BlendOp.ReverseSubtract); break;
            }

            if (validate) ValidateByAlphaBlendMode(material, mode);
        }

        private static void SetAlphaBlendMode(Material material, BlendMode blendSrc, BlendMode blendDst, bool multiplyRgbA, BlendOp blendOp)
        {
            material.SetFloat(IdBlendSrc, (float)blendSrc);
            material.SetFloat(IdBlendDst, (float)blendDst);
            material.SetFloat(IdMultiplyRgbA, multiplyRgbA.ToInt());
            material.SetFloat(IdBlendOp, (float)blendOp);
        }

        public static void SetVertexColorBlendMode(Material material, VertexColorBlendMode mode)
        {
            material.SetFloat(IdVertexColorBlend, (float)mode);
        }

        private static void ValidateByAlphaBlendMode(Material material, AlphaBlendMode mode)
        {
            if (mode == AlphaBlendMode.Custom) return;
            else if (mode == AlphaBlendMode.Opqaue)
            {
                material.renderQueue = (int)RenderQueue.Geometry;
                material.SetFloat(IdZWrite, 1);
            }
            else if (mode == AlphaBlendMode.AlphaTest)
            {
                material.renderQueue = (int)RenderQueue.AlphaTest;
                material.SetFloat(IdZWrite, 1);
            }
            else
            {
                material.renderQueue = (int)RenderQueue.Transparent;
                material.SetFloat(IdZWrite, 0);
            }
        }
    }
}

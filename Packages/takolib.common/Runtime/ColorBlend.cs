using TakoLib.Common.Extensions;
using UnityEngine;
using UnityEngine.Rendering;

namespace TakoLib.Common
{
    public enum AlphaBlendMode
    {
        Custom,
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

    /// <summary>
    /// 色のブレンドに関する処理。
    /// </summary>
    public static class ColorBlend
    {
        public static readonly int IdBlendSrc = Shader.PropertyToID("_BlendSrc");
        public static readonly int IdBlendDst = Shader.PropertyToID("_BlendDst");
        public static readonly int IdMultiplyRgbA = Shader.PropertyToID("_MultiplyRgbA");
        public static readonly int IdBlendOp = Shader.PropertyToID("_BlendOp");
        public static readonly int IdAlphaBlend = Shader.PropertyToID("_AlphaBlend");

        public static readonly int IdVertexColorBlend = Shader.PropertyToID("_VertexColorBlend");

        public static bool HasVertexColorBlendRequiredProperties(Material material)
         => material.HasFloat(IdBlendSrc) &&
            material.HasFloat(IdBlendDst) &&
            material.HasFloat(IdMultiplyRgbA) &&
            material.HasFloat(IdBlendOp) &&
            material.HasFloat(IdAlphaBlend);

        public static bool HasAlphaBlendRequiredProperties(Material material)
         => material.HasFloat(IdVertexColorBlend);

        public static void SetAlphaBlendMode(Material material, AlphaBlendMode mode)
        {
            switch (mode)
            {
                case AlphaBlendMode.Custom: break;
                case AlphaBlendMode.Transparent: SetAlphaBlendMode(material, BlendMode.One, BlendMode.OneMinusSrcAlpha, true, BlendOp.Add); break;
                case AlphaBlendMode.Additive: SetAlphaBlendMode(material, BlendMode.SrcAlpha, BlendMode.One, false, BlendOp.Add); break;
                case AlphaBlendMode.Mutiply: SetAlphaBlendMode(material, BlendMode.DstColor, BlendMode.OneMinusSrcAlpha, true, BlendOp.Add); break;
                case AlphaBlendMode.Screen: SetAlphaBlendMode(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcColor, true, BlendOp.Add); break;
                case AlphaBlendMode.Nega: SetAlphaBlendMode(material, BlendMode.OneMinusDstColor, BlendMode.OneMinusSrcAlpha, true, BlendOp.Add); break;
                case AlphaBlendMode.Subtractive: SetAlphaBlendMode(material, BlendMode.SrcAlpha, BlendMode.One, true, BlendOp.ReverseSubtract); break;
            }
            material.SetFloat(IdAlphaBlend, (float)mode);
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
    }
}
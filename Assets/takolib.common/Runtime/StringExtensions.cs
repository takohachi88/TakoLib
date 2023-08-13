using UnityEngine;

namespace TakoLib.Common.Extensions
{
    public static class StringExtensions
    {
        public static string TagBold(this string self) => $"<b>{self}</b>";

        public static string TagItalic(this string self) => $"<i>{self}</i>";

        public static string TagColor(this string self, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{self}</color>";
        
        public static string TagSize(this string self, float size) => $"<size={size}>{self}</size>";
    }
}

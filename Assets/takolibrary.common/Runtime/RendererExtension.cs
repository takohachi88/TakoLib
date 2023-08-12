using UnityEngine;
using UnityEngine.UI;

namespace TakoLibrary.Common.Extensions
{
    public static class RendererExtensions
    {
        public static Material SetAlpha(this Material self, float alpha)
        {
            var color = self.color;
            color.a = alpha;
            self.color = color;
            return self;
        }

        #region Renderer

        public static SpriteRenderer SetAlpha(this SpriteRenderer self, float alpha)
        {
            var color = self.color;
            color.a = alpha;
            self.color = color;
            return self;
        }

        #endregion

        #region Graphic

        public static Graphic SetAlpha(this Graphic self, float alpha)
        {
            var color = self.color;
            color.a = alpha;
            self.color = color;
            return self;
        }

        #endregion
    }
}

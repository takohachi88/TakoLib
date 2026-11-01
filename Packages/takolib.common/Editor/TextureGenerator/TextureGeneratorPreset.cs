#if UNITY_EDITOR
using System;
using UnityEngine;

namespace TakoLibEditor.Common
{
    [Serializable]
    public sealed class TextureGeneratorTone
    {
        public bool enabled;
        public float contrastR = 1, contrastG = 1, contrastB = 1, contrastA = 1;
        public AnimationCurve rgb = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve red = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve green = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve blue = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve alpha = AnimationCurve.Linear(0, 0, 1, 1);
        private static float Curve(AnimationCurve curve, float value)
            => curve == null || curve.length == 0 ? value : Mathf.Clamp01(curve.Evaluate(value));
        public Color32[] Apply(Color32[] pixels)
        {
            if (!enabled && contrastR == 1 && contrastG == 1 && contrastB == 1 && contrastA == 1) return pixels;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color p = pixels[i];
                p.r = Mathf.Clamp01((p.r - 0.5f) * contrastR + 0.5f);
                p.g = Mathf.Clamp01((p.g - 0.5f) * contrastG + 0.5f);
                p.b = Mathf.Clamp01((p.b - 0.5f) * contrastB + 0.5f);
                p.a = Mathf.Clamp01((p.a - 0.5f) * contrastA + 0.5f);
                if (enabled) p = new Color(Curve(red, Curve(rgb, p.r)), Curve(green, Curve(rgb, p.g)),
                    Curve(blue, Curve(rgb, p.b)), Curve(alpha, p.a));
                pixels[i] = p;
            }
            return pixels;
        }
    }

    [Serializable]
    public sealed class TextureGeneratorRecipe
    {

        public bool generate3D;
        public Vector2Int size = new Vector2Int(512, 512);
        public Vector3Int volumeSize = new Vector3Int(64, 64, 64);
        public TextureGenerationSettings noise = new TextureGenerationSettings();
        public TextureGeneratorTone tone = new TextureGeneratorTone();
        public TextureGeneratorRecipe Clone() => JsonUtility.FromJson<TextureGeneratorRecipe>(JsonUtility.ToJson(this));
        public void Normalize()
        {
            size.x = Mathf.Clamp(size.x, 16, 4096); size.y = Mathf.Clamp(size.y, 16, 4096);
            volumeSize.x = Mathf.Clamp(volumeSize.x, 4, 256); volumeSize.y = Mathf.Clamp(volumeSize.y, 4, 256);
            volumeSize.z = Mathf.Clamp(volumeSize.z, 4, 256);
            if (noise == null) noise = new TextureGenerationSettings();
            if (tone == null) tone = new TextureGeneratorTone();
            tone.contrastR = Mathf.Clamp(tone.contrastR, 0, 4); tone.contrastG = Mathf.Clamp(tone.contrastG, 0, 4);
            tone.contrastB = Mathf.Clamp(tone.contrastB, 0, 4); tone.contrastA = Mathf.Clamp(tone.contrastA, 0, 4);
        }
        public FilterMode Filter
        {
            get
            {
                if (noise.pattern == GeneratedPattern.Block || noise.pattern == GeneratedPattern.RectangleLayers) return FilterMode.Point;
                GeneratedChannel[] channels = { noise.red, noise.green, noise.blue, noise.alpha };
                foreach (GeneratedChannel c in channels)
                    if (c == GeneratedChannel.CenterU || c == GeneratedChannel.CenterV || c == GeneratedChannel.CenterW)
                        return FilterMode.Point;
                return FilterMode.Bilinear;
            }
        }
    }

    // Authoring data only. Exported PNG textures do not reference this preset.
    public sealed class TextureGeneratorPreset : ScriptableObject
    {
        public TextureGeneratorRecipe recipe = new TextureGeneratorRecipe();
    }
}
#endif

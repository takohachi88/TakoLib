using UnityEngine;

namespace TakoLib.Common
{
    public enum EasingType
    {
        Linear,
        EaseInSine,
        EaseOutSine,
        EaseInOutSine,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        EaseInQuart,
        EaseOutQuart,
        EaseInOutQuart,
        EaseInQuint,
        EaseOutQuint,
        EaseInOutQuint,
        EaseInExpo,
        EaseOutExpo,
        EaseInOutExpo,
        EaseInCirc,
        EaseOutCirc,
        EaseInOutCirc,
        EaseInBack,
        EaseOutBack,
        EaseInOutBack,
        EaseInElastic,
        EaseOutElastic,
        EaseInOutElastic,
        EaseInBounce,
        EaseOutBounce,
        EaseInOutBounce,
        Hold,
        Custom,
    }

    public static class Easing
    {
        public static float Evaluate(EasingType ease, float value) => Evaluate(ease, null, value);

        public static float Evaluate(EasingType ease, AnimationCurve curve, float value)
        {
            float t = Mathf.Clamp01(value);
            if (ease == EasingType.Custom) return curve == null ? t : curve.Evaluate(t);
            if (t == 0 || t == 1) return t;
            const float BACK = 1.70158f;
            switch (ease)
            {
                case EasingType.EaseInQuad: return t * t;
                case EasingType.EaseOutQuad: return 1 - (1 - t) * (1 - t);
                case EasingType.Hold: return 0;
                case EasingType.EaseInSine: return 1 - Mathf.Cos(t * Mathf.PI / 2);
                case EasingType.EaseOutSine: return Mathf.Sin(t * Mathf.PI / 2);
                case EasingType.EaseInOutSine: return (1 - Mathf.Cos(t * Mathf.PI)) / 2;
                case EasingType.EaseInOutQuad: return PowerInOut(t, 2);
                case EasingType.EaseInCubic: return Power(t, 3);
                case EasingType.EaseOutCubic: return 1 - Power(1 - t, 3);
                case EasingType.EaseInOutCubic: return PowerInOut(t, 3);
                case EasingType.EaseInQuart: return Power(t, 4);
                case EasingType.EaseOutQuart: return 1 - Power(1 - t, 4);
                case EasingType.EaseInOutQuart: return PowerInOut(t, 4);
                case EasingType.EaseInQuint: return Power(t, 5);
                case EasingType.EaseOutQuint: return 1 - Power(1 - t, 5);
                case EasingType.EaseInOutQuint: return PowerInOut(t, 5);
                case EasingType.EaseInExpo: return Mathf.Pow(2, 10 * t - 10);
                case EasingType.EaseOutExpo: return 1 - Mathf.Pow(2, -10 * t);
                case EasingType.EaseInOutExpo: return t < 0.5f ? Mathf.Pow(2, 20 * t - 10) / 2 : 1 - Mathf.Pow(2, 10 - 20 * t) / 2;
                case EasingType.EaseInCirc: return 1 - Mathf.Sqrt(1 - t * t);
                case EasingType.EaseOutCirc: return Mathf.Sqrt(1 - (t - 1) * (t - 1));
                case EasingType.EaseInOutCirc: return t < 0.5f ? (1 - Mathf.Sqrt(1 - 4 * t * t)) / 2 : (Mathf.Sqrt(1 - 4 * (1 - t) * (1 - t)) + 1) / 2;
                case EasingType.EaseInBack: return (BACK + 1) * t * t * t - BACK * t * t;
                case EasingType.EaseOutBack: return 1 + (BACK + 1) * Power(t - 1, 3) + BACK * Power(t - 1, 2);
                case EasingType.EaseInOutBack:
                    float b = BACK * 1.525f;
                    float x = t < 0.5f ? t * 2 : t * 2 - 2;
                    return t < 0.5f ? x * x * ((b + 1) * x - b) / 2 : (x * x * ((b + 1) * x + b) + 2) / 2;
                case EasingType.EaseInElastic: return -Mathf.Pow(2, 10 * t - 10) * Mathf.Sin((10 * t - 10.75f) * 2 * Mathf.PI / 3);
                case EasingType.EaseOutElastic: return 1 + Mathf.Pow(2, -10 * t) * Mathf.Sin((10 * t - 0.75f) * 2 * Mathf.PI / 3);
                case EasingType.EaseInOutElastic:
                    float wave = Mathf.Sin((20 * t - 11.125f) * 2 * Mathf.PI / 4.5f);
                    return t < 0.5f ? -Mathf.Pow(2, 20 * t - 10) * wave / 2 : Mathf.Pow(2, 10 - 20 * t) * wave / 2 + 1;
                case EasingType.EaseInBounce: return 1 - BounceOut(1 - t);
                case EasingType.EaseOutBounce: return BounceOut(t);
                case EasingType.EaseInOutBounce: return t < 0.5f ? (1 - BounceOut(1 - 2 * t)) / 2 : (1 + BounceOut(2 * t - 1)) / 2;
                default: return t;
            }
        }

        private static float PowerInOut(float t, int power) =>
            t < 0.5f ? Power(2 * t, power) / 2 : 1 - Power(2 * (1 - t), power) / 2;

        private static float Power(float value, int power)
        {
            // 2～5乗は乗算で求め、汎用の累乗計算を省く。
            float squared = value * value;
            switch (power)
            {
                case 2: return squared;
                case 3: return squared * value;
                case 4: return squared * squared;
                default: return squared * squared * value;
            }
        }

        private static float BounceOut(float t)
        {
            const float DIVISOR = 2.75f;
            if (t < 1 / DIVISOR) return 7.5625f * t * t;
            if (t < 2 / DIVISOR) { t -= 1.5f / DIVISOR; return 7.5625f * t * t + 0.75f; }
            if (t < 2.5f / DIVISOR) { t -= 2.25f / DIVISOR; return 7.5625f * t * t + 0.9375f; }
            t -= 2.625f / DIVISOR;
            return 7.5625f * t * t + 0.984375f;
        }
    }
}

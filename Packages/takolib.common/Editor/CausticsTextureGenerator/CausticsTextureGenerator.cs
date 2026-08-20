#if UNITY_EDITOR

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace TakoLibEditor.Common
{
    /// <summary>
    /// <see cref="CausticsTextureGenerator"/>で使用する設定。
    /// 空間周波数を整数に限定し、時間変化を円運動にすることで、
    /// テクスチャ空間と時間方向の両方を周期化する。
    /// </summary>
    [Serializable]
    public sealed class CausticsTextureSettings
    {
        public int Width = 256;
        public int Height = 256;
        public int FrameCount = 16;
        public int Supersampling = 4;
        public int WaveCount = 32;
        public int PatternScale = 4;
        public int Seed = 12345;
        [Tooltip("ループ中に時間方向へ移動する距離。0で静止し、小さいほどフレーム間の変化が滑らかになります。")]
        public float AnimationSpeed = 0.25f;
        public float RefractionStrength = 0.015f;
        public float ChromaticAberration = 0.219f;
        public int BlurRadius;
        public float BlackPoint;
        public float Exposure = 0.1f;
        public float Contrast = 0.73f;
        public Color Tint = Color.white;
        public bool AlphaFromIntensity;
        public bool Linear = true;
        public bool GenerateMipmaps = true;

        public CausticsTextureSettings Copy()
        {
            return (CausticsTextureSettings)MemberwiseClone();
        }

        public string Validate()
        {
            if (Width < 32 || Width > 2048 || Height < 32 || Height > 2048)
                return "Resolution must be between 32 and 2048 pixels.";
            if (FrameCount < 1 || FrameCount > 256)
                return "Frame count must be between 1 and 256.";
            if (Supersampling < 1 || Supersampling > 4)
                return "Supersampling must be between 1 and 4.";
            if (WaveCount < 4 || WaveCount > 32)
                return "Wave count must be between 4 and 32.";
            if (PatternScale < 1 || PatternScale > 12)
                return "Pattern scale must be between 1 and 12.";
            if (AnimationSpeed < 0f || AnimationSpeed > 2f)
                return "Animation speed must be between 0 and 2.";
            if (RefractionStrength < 0f || RefractionStrength > 0.25f)
                return "Refraction strength must be between 0 and 0.25.";
            if (ChromaticAberration < 0f || ChromaticAberration > 0.5f)
                return "Chromatic aberration must be between 0 and 0.5.";
            if (BlurRadius < 0 || BlurRadius > 16)
                return "Blur radius must be between 0 and 16.";
            if (BlackPoint < 0f || BlackPoint > 2f)
                return "Black point must be between 0 and 2.";
            if (Exposure <= 0f || Exposure > 20f)
                return "Exposure must be greater than 0 and no more than 20.";
            if (Contrast <= 0f || Contrast > 4f)
                return "Contrast must be greater than 0 and no more than 4.";
            return null;
        }
    }

    /// <summary>
    /// 周期的にアニメーションする波面で屈折したサンプルを集積し、
    /// ループおよびタイリング可能なコースティクスを生成する。
    /// </summary>
    public static class CausticsTextureGenerator
    {
        private const double TwoPi = Math.PI * 2.0;

        private readonly struct Wave
        {
            public readonly int FrequencyX;
            public readonly int FrequencyY;
            public readonly double TemporalX;
            public readonly double TemporalY;
            public readonly double Phase;
            public readonly double GradientX;
            public readonly double GradientY;

            public Wave(
                int frequencyX,
                int frequencyY,
                double temporalX,
                double temporalY,
                double phase,
                double gradientX,
                double gradientY)
            {
                FrequencyX = frequencyX;
                FrequencyY = frequencyY;
                TemporalX = temporalX;
                TemporalY = temporalY;
                Phase = phase;
                GradientX = gradientX;
                GradientY = gradientY;
            }
        }

        private readonly struct WaveSource
        {
            public readonly int FrequencyX;
            public readonly int FrequencyY;
            public readonly double TemporalX;
            public readonly double TemporalY;
            public readonly double Phase;
            public readonly double Amplitude;

            public WaveSource(
                int frequencyX,
                int frequencyY,
                double temporalX,
                double temporalY,
                double phase,
                double amplitude)
            {
                FrequencyX = frequencyX;
                FrequencyY = frequencyY;
                TemporalX = temporalX;
                TemporalY = temporalY;
                Phase = phase;
                Amplitude = amplitude;
            }
        }

        /// <summary>
        /// 1フレーム分のピクセルを生成する。
        /// 時刻をframeIndex / FrameCountとして扱うため、最終フレームから
        /// 先頭フレームまでの時間間隔は、ほかのフレーム間隔と等しくなる。
        /// </summary>
        public static Color32[] GenerateFrame(CausticsTextureSettings settings, int frameIndex)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            string validationError = settings.Validate();
            if (validationError != null)
                throw new ArgumentException(validationError, nameof(settings));
            if (frameIndex < 0 || frameIndex >= settings.FrameCount)
                throw new ArgumentOutOfRangeException(nameof(frameIndex));

            int width = settings.Width;
            int height = settings.Height;
            int sampleWidth = width * settings.Supersampling;
            int sampleHeight = height * settings.Supersampling;
            int sampleCount = sampleWidth * sampleHeight;
            float sampleEnergy = 1f / (settings.Supersampling * settings.Supersampling);
            double time = (double)frameIndex / settings.FrameCount;
            double loopAngle = TwoPi * time;
            double loopX = Math.Cos(loopAngle);
            double loopY = Math.Sin(loopAngle);
            double animationPhaseScale = TwoPi * settings.AnimationSpeed;

            Wave[] waves = CreateWaves(settings);
            float[] gradientX = new float[sampleCount];
            float[] gradientY = new float[sampleCount];

            // 三角関数の計算が生成時間の大半を占め、各サンプルは独立しているため並列化する。
            Parallel.For(0, sampleHeight, y =>
            {
                double v = (y + 0.5) / sampleHeight;
                int row = y * sampleWidth;
                for (int x = 0; x < sampleWidth; x++)
                {
                    double u = (x + 0.5) / sampleWidth;
                    double dx = 0.0;
                    double dy = 0.0;

                    for (int waveIndex = 0; waveIndex < waves.Length; waveIndex++)
                    {
                        Wave wave = waves[waveIndex];
                        double temporalOffset = animationPhaseScale * (wave.TemporalX * loopX + wave.TemporalY * loopY);
                        double phase = TwoPi * (wave.FrequencyX * u + wave.FrequencyY * v) + wave.Phase + temporalOffset;
                        double cosine = Math.Cos(phase);
                        dx += wave.GradientX * cosine;
                        dy += wave.GradientY * cosine;
                    }

                    int index = row + x;
                    gradientX[index] = (float)dx;
                    gradientY[index] = (float)dy;
                }
            });

            int pixelCount = width * height;
            float[] red = new float[pixelCount];
            float[] green = new float[pixelCount];
            float[] blue = new float[pixelCount];
            float redStrength = settings.RefractionStrength * (1f + settings.ChromaticAberration);
            float greenStrength = settings.RefractionStrength;
            float blueStrength = settings.RefractionStrength * (1f - settings.ChromaticAberration);

            for (int y = 0; y < sampleHeight; y++)
            {
                float v = (y + 0.5f) / sampleHeight;
                int row = y * sampleWidth;
                for (int x = 0; x < sampleWidth; x++)
                {
                    int index = row + x;
                    float u = (x + 0.5f) / sampleWidth;
                    float dx = gradientX[index];
                    float dy = gradientY[index];

                    SplatPeriodic(
                        red,
                        width,
                        height,
                        u + dx * redStrength,
                        v + dy * redStrength,
                        sampleEnergy);
                    SplatPeriodic(
                        green,
                        width,
                        height,
                        u + dx * greenStrength,
                        v + dy * greenStrength,
                        sampleEnergy);
                    SplatPeriodic(
                        blue,
                        width,
                        height,
                        u + dx * blueStrength,
                        v + dy * blueStrength,
                        sampleEnergy);
                }
            }

            if (settings.BlurRadius > 0)
            {
                BlurPeriodic(red, width, height, settings.BlurRadius);
                BlurPeriodic(green, width, height, settings.BlurRadius);
                BlurPeriodic(blue, width, height, settings.BlurRadius);
            }

            Color32[] pixels = new Color32[pixelCount];
            float tintR = Mathf.Max(0f, settings.Tint.r);
            float tintG = Mathf.Max(0f, settings.Tint.g);
            float tintB = Mathf.Max(0f, settings.Tint.b);
            for (int i = 0; i < pixels.Length; i++)
            {
                byte r = ToneMap(red[i], settings, tintR);
                byte g = ToneMap(green[i], settings, tintG);
                byte b = ToneMap(blue[i], settings, tintB);
                byte a = settings.AlphaFromIntensity
                    ? (byte)Mathf.Max(r, Mathf.Max(g, b))
                    : byte.MaxValue;
                pixels[i] = new Color32(r, g, b, a);
            }

            return pixels;
        }

        private static Wave[] CreateWaves(CausticsTextureSettings settings)
        {
            System.Random random = new(settings.Seed);
            WaveSource[] sources = new WaveSource[settings.WaveCount];
            double gradientPower = 0.0;

            for (int i = 0; i < sources.Length; i++)
            {
                int frequencyX;
                int frequencyY;
                int minimumFrequency = settings.PatternScale;
                int maximumFrequency = settings.PatternScale * 3;
                int attempts = 0;

                do
                {
                    frequencyX = random.Next(-maximumFrequency, maximumFrequency + 1);
                    frequencyY = random.Next(-maximumFrequency, maximumFrequency + 1);
                    attempts++;
                }
                while ((frequencyX == 0 && frequencyY == 0 ||
                        Math.Sqrt(frequencyX * frequencyX + frequencyY * frequencyY) < minimumFrequency) &&
                       attempts < 100);

                double lengthSquared = frequencyX * frequencyX + frequencyY * frequencyY;
                double amplitudeVariation = 0.7 + random.NextDouble() * 0.6;
                double amplitude = amplitudeVariation / Math.Max(1.0, lengthSquared);
                double phase = random.NextDouble() * TwoPi;
                double temporalAngle = random.NextDouble() * TwoPi;
                double gradientX = TwoPi * frequencyX * amplitude;
                double gradientY = TwoPi * frequencyY * amplitude;
                gradientPower += (gradientX * gradientX + gradientY * gradientY) * 0.5;

                sources[i] = new WaveSource(
                    frequencyX,
                    frequencyY,
                    Math.Cos(temporalAngle),
                    Math.Sin(temporalAngle),
                    phase,
                    amplitude);
            }

            double normalization = gradientPower > double.Epsilon
                ? 1.0 / Math.Sqrt(gradientPower)
                : 1.0;
            Wave[] waves = new Wave[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                WaveSource source = sources[i];
                waves[i] = new Wave(
                    source.FrequencyX,
                    source.FrequencyY,
                    source.TemporalX,
                    source.TemporalY,
                    source.Phase,
                    TwoPi * source.FrequencyX * source.Amplitude * normalization,
                    TwoPi * source.FrequencyY * source.Amplitude * normalization);
            }

            return waves;
        }

        private static void SplatPeriodic(
            float[] destination,
            int width,
            int height,
            float u,
            float v,
            float energy)
        {
            float pixelX = Repeat01(u) * width - 0.5f;
            float pixelY = Repeat01(v) * height - 0.5f;
            int x0 = Mathf.FloorToInt(pixelX);
            int y0 = Mathf.FloorToInt(pixelY);
            float fractionX = pixelX - x0;
            float fractionY = pixelY - y0;
            int x1 = WrapIndex(x0 + 1, width);
            int y1 = WrapIndex(y0 + 1, height);
            x0 = WrapIndex(x0, width);
            y0 = WrapIndex(y0, height);

            destination[y0 * width + x0] += energy * (1f - fractionX) * (1f - fractionY);
            destination[y0 * width + x1] += energy * fractionX * (1f - fractionY);
            destination[y1 * width + x0] += energy * (1f - fractionX) * fractionY;
            destination[y1 * width + x1] += energy * fractionX * fractionY;
        }

        private static void BlurPeriodic(float[] values, int width, int height, int radius)
        {
            float sigma = Mathf.Max(0.5f, radius * 0.5f);
            float[] weights = new float[radius * 2 + 1];
            float weightSum = 0f;
            for (int offset = -radius; offset <= radius; offset++)
            {
                float weight = Mathf.Exp(-(offset * offset) / (2f * sigma * sigma));
                weights[offset + radius] = weight;
                weightSum += weight;
            }

            for (int i = 0; i < weights.Length; i++)
                weights[i] /= weightSum;

            float[] temporary = new float[values.Length];
            Parallel.For(0, height, y =>
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    for (int offset = -radius; offset <= radius; offset++)
                    {
                        int sourceX = WrapIndex(x + offset, width);
                        sum += values[row + sourceX] * weights[offset + radius];
                    }
                    temporary[row + x] = sum;
                }
            });

            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    float sum = 0f;
                    for (int offset = -radius; offset <= radius; offset++)
                    {
                        int sourceY = WrapIndex(y + offset, height);
                        sum += temporary[sourceY * width + x] * weights[offset + radius];
                    }
                    values[y * width + x] = sum;
                }
            });
        }

        private static byte ToneMap(
            float density,
            CausticsTextureSettings settings,
            float tint)
        {
            float signal = Mathf.Max(0f, density - settings.BlackPoint);
            float value = 1f - Mathf.Exp(-signal * settings.Exposure);
            value = Mathf.Pow(Mathf.Clamp01(value), 1f / settings.Contrast);
            return (byte)Mathf.RoundToInt(Mathf.Clamp01(value * tint) * byte.MaxValue);
        }

        private static float Repeat01(float value)
        {
            return value - Mathf.Floor(value);
        }

        private static int WrapIndex(int value, int length)
        {
            int result = value % length;
            return result < 0 ? result + length : result;
        }
    }
}

#endif

using System;
using UnityEngine;

namespace TakoLib.Common.Timeline
{
    public static class CameraShake
    {
        public static double Phase(CameraShotClip clip, double time)
        {
            time = Math.Max(0, time);
            CameraPassageKey previous = null;
            double phase = clip.shakePhaseOffset;
            foreach (var key in clip.keys)
            {
                if ((key.channels & CameraKeyChannels.Shake) == 0) continue;
                if (previous == null)
                {
                    phase += Math.Min(time, key.time) * Math.Max(0, key.shakeFrequency);
                    if (time <= key.time) return phase;
                }
                else
                {
                    double duration = key.time - previous.time;
                    double u = Math.Min(1, (time - previous.time) / duration);
                    double from = Math.Max(0, previous.shakeFrequency);
                    double to = Math.Max(0, key.shakeFrequency);
                    phase += duration * (IntegrateFrequency(previous, u, from, to));
                    if (time <= key.time) return phase;
                }
                previous = key;
            }
            if (previous != null) phase += (time - previous.time) * Math.Max(0, previous.shakeFrequency);
            return phase;
        }

        internal static double IntegrateFrequency(CameraPassageKey key, double u, double from, double to)
        {
            switch (key.ease)
            {
                case EasingType.Linear: return from * u + (to - from) * (u * u / 2);
                case EasingType.EaseInQuad: return from * u + (to - from) * (u * u * u / 3);
                case EasingType.EaseOutQuad: return from * u + (to - from) * (u * u - u * u * u / 3);
                case EasingType.Hold: return from * u;
                default:
                    // 端数区間も固定格子から積分し、評価順序による差を防ぐ。
                    const int RESOLUTION = 128;
                    double sum = 0;
                    for (int i = 0; i < RESOLUTION && (double)i / RESOLUTION < u; ++i)
                    {
                        double start = (double)i / RESOLUTION;
                        double end = Math.Min(u, (double)(i + 1) / RESOLUTION);
                        double a = Math.Max(0, from + (to - from) * key.EvaluateEase((float)start));
                        double b = Math.Max(0, from + (to - from) * key.EvaluateEase((float)((double)(i + 1) / RESOLUTION)));
                        double partial = (end - start) * RESOLUTION;
                        sum += (end - start) * (a + (b - a) * partial / 2);
                    }
                    return sum;
            }
        }

        public static Vector3 Noise(int seed, double phase, int firstChannel)
        {
            return new Vector3(Sample(seed, phase, firstChannel), Sample(seed, phase, firstChannel + 1), Sample(seed, phase, firstChannel + 2));
        }

        private static float Sample(int seed, double phase, int channel)
        {
            double floor = Math.Floor(phase);
            double fraction = phase - floor;
            float blend = (float)(fraction * fraction * fraction * (fraction * (fraction * 6 - 15) + 10));
            long cell = (long)floor;
            return Mathf.Lerp(Hash(seed, cell, channel), Hash(seed, unchecked(cell + 1), channel), blend);
        }

        private static float Hash(int seed, long cell, int channel)
        {
            unchecked
            {
                uint value = (uint)seed ^ (uint)cell * 0x9e3779b9u ^ (uint)(cell >> 32) ^ (uint)(channel + 1) * 0x85ebca6bu;
                value ^= value >> 16; value *= 0x7feb352du;
                value ^= value >> 15; value *= 0x846ca68bu;
                value ^= value >> 16;
                return (value & 0xffffffu) / 8388607.5f - 1;
            }
        }
    }
}

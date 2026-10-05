using System;
using UnityEngine;

namespace TakoLib.Common.Timeline
{
    // 周波数の積分結果を区間ごとに保持し、スクラブの順序に依存せず位相を求める。
    internal sealed class CameraShakeCache
    {
        private const int RESOLUTION = 128;
        private Segment[] _segments = Array.Empty<Segment>();
        private int _count;

        public double Evaluate(CameraShotClip clip, double time)
        {
            int count = 0;
            bool changed = false;
            for (int i = 0; i < clip.keys.Count; i++)
            {
                var key = clip.keys[i];
                if ((key.channels & CameraKeyChannels.Shake) == 0) continue;
                if (count >= _segments.Length)
                {
                    int previousLength = _segments.Length;
                    Array.Resize(ref _segments, Math.Max(4, previousLength * 2));
                    for (int j = previousLength; j < _segments.Length; j++) _segments[j] = new Segment();
                }
                if (!_segments[count].Matches(key))
                {
                    _segments[count].Capture(key);
                    changed = true;
                }
                count++;
            }
            changed |= count != _count;
            _count = count;
            if (_count == 0) return clip.shakePhaseOffset;

            if (changed)
            {
                _segments[0].phase = _segments[0].time * _segments[0].frequency;
                for (int i = 0; i < _count - 1; i++)
                {
                    var segment = _segments[i];
                    var next = _segments[i + 1];
                    segment.Build(next);
                    next.phase = segment.phase + (next.time - segment.time) * segment.Integrate(1);
                }
            }

            time = Math.Max(0, time);
            if (time <= _segments[0].time) return clip.shakePhaseOffset + time * _segments[0].frequency;
            int low = 0;
            int high = _count - 1;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (_segments[middle].time <= time) low = middle;
                else high = middle - 1;
            }
            var current = _segments[low];
            if (low == _count - 1)
                return clip.shakePhaseOffset + current.phase + (time - current.time) * current.frequency;
            double duration = _segments[low + 1].time - current.time;
            return clip.shakePhaseOffset + current.phase + duration * current.Integrate((time - current.time) / duration);
        }

        private sealed class Segment
        {
            public double time;
            public double frequency;
            public double phase;
            private CameraPassageKey _key;
            private EasingType _ease;
            private AnimationCurve _curve;
            private WrapMode _preWrapMode;
            private WrapMode _postWrapMode;
            private Keyframe[] _curveKeys = Array.Empty<Keyframe>();
            private int _curveKeyCount;
            private double _nextFrequency;
            private readonly double[] _areas = new double[RESOLUTION + 1];
            private readonly double[] _frequencies = new double[RESOLUTION + 1];

            public bool Matches(CameraPassageKey key)
            {
                if (_key != key || time != key.time || frequency != Math.Max(0, key.shakeFrequency) || _ease != key.ease)
                    return false;
                if (_ease != EasingType.Custom) return true;
                var curve = key.curve;
                if (_curve != curve) return false;
                if (curve == null) return true;
                if (_curveKeyCount != curve.length || _preWrapMode != curve.preWrapMode || _postWrapMode != curve.postWrapMode)
                    return false;
                for (int i = 0; i < _curveKeyCount; i++)
                {
                    // curve.keysの配列生成と、ValueType.Equalsによるボックス化を避ける。
                    var a = curve[i];
                    var b = _curveKeys[i];
                    if (a.time != b.time || a.value != b.value || a.inTangent != b.inTangent || a.outTangent != b.outTangent ||
                        a.inWeight != b.inWeight || a.outWeight != b.outWeight || a.weightedMode != b.weightedMode)
                        return false;
                }
                return true;
            }

            public void Capture(CameraPassageKey key)
            {
                _key = key;
                time = key.time;
                frequency = Math.Max(0, key.shakeFrequency);
                _ease = key.ease;
                _curve = key.curve;
                _curveKeyCount = _ease == EasingType.Custom && _curve != null ? _curve.length : 0;
                if (_curveKeys.Length < _curveKeyCount) Array.Resize(ref _curveKeys, _curveKeyCount);
                if (_curve == null) return;
                _preWrapMode = _curve.preWrapMode;
                _postWrapMode = _curve.postWrapMode;
                for (int i = 0; i < _curveKeyCount; i++) _curveKeys[i] = _curve[i];
            }

            public void Build(Segment next)
            {
                _nextFrequency = next.frequency;
                if (IsAnalytic()) return;
                _areas[0] = 0;
                _frequencies[0] = Math.Max(0, frequency + (_nextFrequency - frequency) * _key.EvaluateEase(0));
                for (int i = 1; i <= RESOLUTION; i++)
                {
                    _frequencies[i] = Math.Max(0, frequency + (_nextFrequency - frequency) * _key.EvaluateEase((float)i / RESOLUTION));
                    _areas[i] = _areas[i - 1] + (_frequencies[i - 1] + _frequencies[i]) / (2 * RESOLUTION);
                }
            }

            public double Integrate(double progress)
            {
                if (IsAnalytic()) return CameraShake.IntegrateFrequency(_key, progress, frequency, _nextFrequency);
                if (progress >= 1) return _areas[RESOLUTION];
                double grid = Math.Max(0, progress) * RESOLUTION;
                int index = (int)grid;
                double fraction = grid - index;
                return _areas[index] + fraction / RESOLUTION *
                    (_frequencies[index] + (_frequencies[index + 1] - _frequencies[index]) * fraction / 2);
            }

            private bool IsAnalytic() => _ease == EasingType.Linear || _ease == EasingType.EaseInQuad ||
                _ease == EasingType.EaseOutQuad || _ease == EasingType.Hold;
        }
    }
}

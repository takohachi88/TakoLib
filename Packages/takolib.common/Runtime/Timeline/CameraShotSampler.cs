using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace TakoLib.Common.Timeline
{
    public sealed class CameraShotSampler : IDisposable
    {
        private Spline _source;
        private Matrix4x4 _matrix;
        private NativeSpline _worldSpline;
        private bool _created;
        private bool _dirty = true;
        private static int _cacheVersion;
        private int _version;
        private float[] _normalizedKnots = Array.Empty<float>();
        private readonly CameraShakeCache _shakeCache = new CameraShakeCache();

        public static void InvalidateCaches()
        {
            unchecked { ++_cacheVersion; }
        }

        public CameraShotSampler()
        {
            Spline.Changed += OnSplineChanged;
        }

        private void OnSplineChanged(Spline spline, int index, SplineModification change)
        {
            if (spline == _source) _dirty = true;
        }

        public bool TryEvaluate(CameraShotClip clip, SplineContainer path, double time,
            out Vector3 position, out Quaternion rotation)
        {
            bool success = TryEvaluateState(clip, path, time, out var state);
            position = state.position;
            rotation = state.rotation;
            return success;
        }

        public bool TryEvaluateState(CameraShotClip clip, SplineContainer path, double time, out CameraShotState state)
        {
            state = new CameraShotState { rotation = Quaternion.identity };
            if (clip == null || clip.keys.Count == 0 ||
                double.IsNaN(time) || double.IsInfinity(time)) return false;
            double previous = -1;
            foreach (var key in clip.keys)
            {
                if (key == null || double.IsNaN(key.time) || double.IsInfinity(key.time) ||
                    key.time < 0 || key.time <= previous)
                    return false;
                previous = key.time;
                state.channels |= key.channels;
            }

            if (Bracket(clip, CameraKeyChannels.Position, time, out var before, out var after, out var fraction))
            {
                if (clip.positionMode == CameraPositionMode.Direct)
                    state.position = Vector3.LerpUnclamped(before.position, after.position, fraction);
                else
                {
                    if (path == null || path.Splines.Count != 1 || path.Spline.Closed || path.Spline.Count < 2) return false;
                    foreach (var key in clip.keys)
                        if ((key.channels & CameraKeyChannels.Position) != 0 && CameraKnotIdentity.Find(path.Spline, key.knotId) < 0) return false;
                    PrepareSpline(path);
                    float from = _normalizedKnots[CameraKnotIdentity.Find(_source, before.knotId)];
                    float to = _normalizedKnots[CameraKnotIdentity.Find(_source, after.knotId)];
                    state.position = _worldSpline.EvaluatePosition(Mathf.Clamp01(Mathf.LerpUnclamped(from, to, fraction)));
                }
            }
            if (Bracket(clip, CameraKeyChannels.Rotation, time, out before, out after, out fraction))
                state.rotation = Quaternion.SlerpUnclamped(Quaternion.Euler(before.eulerAngles), Quaternion.Euler(after.eulerAngles), fraction);
            if (Bracket(clip, CameraKeyChannels.FieldOfView, time, out before, out after, out fraction))
                state.fieldOfView = Mathf.Clamp(Mathf.LerpUnclamped(before.fieldOfView, after.fieldOfView, fraction), 1, 179);
            if (Bracket(clip, CameraKeyChannels.LensShift, time, out before, out after, out fraction))
                state.lensShift = Vector2.LerpUnclamped(before.lensShift, after.lensShift, fraction);
            if (!clip.muteShake && Bracket(clip, CameraKeyChannels.Shake, time, out before, out after, out fraction))
            {
                double phase = _shakeCache.Evaluate(clip, time);
                float scale = Mathf.Max(0, Mathf.LerpUnclamped(before.shakeScale, after.shakeScale, fraction));
                state.shakePosition = Vector3.Scale(CameraShake.Noise(clip.shakeSeed, phase, 0), Vector3.LerpUnclamped(before.shakePositionScale, after.shakePositionScale, fraction)) * scale;
                state.shakeRotation = Vector3.Scale(CameraShake.Noise(clip.shakeSeed, phase, 3), Vector3.LerpUnclamped(before.shakeRotationScale, after.shakeRotationScale, fraction)) * scale;
            }
            return state.channels != CameraKeyChannels.None;
        }

        private static bool Bracket(CameraShotClip clip, CameraKeyChannels channel, double time,
            out CameraPassageKey before, out CameraPassageKey after, out float fraction)
        {
            before = after = null;
            CameraPassageKey first = null, last = null;
            foreach (var key in clip.keys)
            {
                if ((key.channels & channel) == 0) continue;
                if (first == null) first = key;
                last = key;
                if (key.time <= time) before = key;
                if (key.time >= time)
                {
                    after = key;
                    break;
                }
            }
            before = before ?? first;
            after = after ?? last;
            fraction = before == null || before == after ? 0 : before.EvaluateEase((float)((time - before.time) / (after.time - before.time)));
            return before != null;
        }

        private void PrepareSpline(SplineContainer path)
        {
            var matrix = path.transform.localToWorldMatrix;
            if (!_created || _dirty || _source != path.Spline || _matrix != matrix || _version != _cacheVersion)
            {
                if (_created) _worldSpline.Dispose();
                _source = path.Spline;
                _matrix = matrix;
                _worldSpline = new NativeSpline(_source, (float4x4)matrix, Allocator.Persistent);
                if (_normalizedKnots.Length < _source.Count) Array.Resize(ref _normalizedKnots, _source.Count);
                // 距離の累積を一度だけ求め、毎フレームのKnot位置変換を省く。
                float length = _worldSpline.GetLength();
                float distance = 0;
                for (int i = 0; i < _source.Count; i++)
                {
                    _normalizedKnots[i] = length > 0 ? distance / length : 0;
                    if (i < _source.Count - 1) distance += _worldSpline.GetCurveLength(i);
                }
                _created = true;
                _dirty = false;
                _version = _cacheVersion;
            }
        }

        public void Dispose()
        {
            Spline.Changed -= OnSplineChanged;
            if (_created) _worldSpline.Dispose();
            _created = false;
        }
    }
}

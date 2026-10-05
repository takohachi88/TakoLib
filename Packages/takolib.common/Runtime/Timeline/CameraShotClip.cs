using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Splines;
using UnityEngine.Timeline;

namespace TakoLib.Common.Timeline
{
    public enum CameraPositionMode
    {
        Spline,
        Direct,
    }

    [Flags]
    public enum CameraKeyChannels
    {
        None = 0,
        Position = 1,
        Rotation = 2,
        FieldOfView = 4,
        LensShift = 8,
        Shake = 16,
        All = Position | Rotation | FieldOfView | LensShift | Shake,
    }

    [Serializable]
    public sealed class CameraPassageKey
    {
        public double time;
        public int knotId;
        public CameraKeyChannels channels = CameraKeyChannels.Position | CameraKeyChannels.Rotation;
        public Vector3 position;
        public Vector3 eulerAngles;
        [Range(1, 179)] public float fieldOfView = 60;
        public Vector2 lensShift;
        public Vector3 shakePositionScale;
        public Vector3 shakeRotationScale;
        [Min(0)] public float shakeFrequency = 8;
        [Min(0)] public float shakeScale = 1;
        public EasingType ease;
        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

        public float EvaluateEase(float value)
            => Easing.Evaluate(ease, curve, value);
    }

    [Serializable]
    public sealed class CameraShotClip : PlayableAsset, ITimelineClipAsset
    {
        public CameraPositionMode positionMode;
        public ExposedReference<SplineContainer> path;
        public int shakeSeed = 1;
        public double shakePhaseOffset;
        public bool muteShake;
        public List<CameraPassageKey> keys = new List<CameraPassageKey>();
        public ClipCaps clipCaps => ClipCaps.ClipIn | ClipCaps.SpeedMultiplier | ClipCaps.Blending;
        public override double duration
        {
            get
            {
                double end = 0;
                foreach (var key in keys) if (key != null) end = Math.Max(end, key.time);
                return end > 0 ? end : 5;
            }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CameraShotBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.Initialize(this, path.Resolve(graph.GetResolver()));
            return playable;
        }
    }
}

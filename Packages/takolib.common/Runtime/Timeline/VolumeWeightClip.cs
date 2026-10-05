using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace TakoLib.Common.Timeline
{
    [Serializable]
    public sealed class VolumeWeightKey
    {
        [Min(0)] public double time;
        [Range(0, 1)] public float weight = 1;
        public EasingType ease;
        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
    }

    [Serializable]
    public sealed class VolumeWeightClip : PlayableAsset, ITimelineClipAsset
    {
        public List<VolumeWeightKey> keys = new List<VolumeWeightKey> { new VolumeWeightKey() };
        public ClipCaps clipCaps => ClipCaps.ClipIn | ClipCaps.SpeedMultiplier | ClipCaps.Blending;
        public override double duration => keys.Count > 1 ? Math.Max(1, keys[keys.Count - 1].time) : 5;

        public bool TryEvaluate(double time, out float weight)
        {
            weight = 0;
            if (keys.Count == 0 || double.IsNaN(time) || double.IsInfinity(time)) return false;
            VolumeWeightKey before = null, after = null;
            double previous = -1;
            foreach (var key in keys)
            {
                if (key == null || double.IsNaN(key.time) || double.IsInfinity(key.time) || key.time < 0 ||
                    key.time <= previous || float.IsNaN(key.weight) || float.IsInfinity(key.weight)) return false;
                previous = key.time;
                if (key.time <= time) before = key;
                if (key.time >= time && after == null) after = key;
            }
            before = before ?? keys[0];
            after = after ?? keys[keys.Count - 1];
            float fraction = before == after ? 0 : Easing.Evaluate(before.ease, before.curve, (float)((time - before.time) / (after.time - before.time)));
            weight = Mathf.Clamp01(Mathf.LerpUnclamped(before.weight, after.weight, fraction));
            return true;
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<VolumeWeightBehaviour>.Create(graph);
            playable.GetBehaviour().clip = this;
            return playable;
        }
    }

    public sealed class VolumeWeightBehaviour : PlayableBehaviour
    {
        public VolumeWeightClip clip;
    }
}

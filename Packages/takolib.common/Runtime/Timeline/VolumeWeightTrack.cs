using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Timeline;

namespace TakoLib.Common.Timeline
{
    [TrackColor(0.65f, 0.4f, 0.85f)]
    [TrackClipType(typeof(VolumeWeightClip))]
    [TrackBindingType(typeof(Volume))]
    public sealed class VolumeWeightTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<VolumeWeightMixer>.Create(graph, inputCount);
            mixer.GetBehaviour().Initialize(graph.GetResolver() as PlayableDirector);
            return mixer;
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            var volume = director.GetGenericBinding(this) as Volume;
            if (volume != null) driver.AddFromName(volume, "weight");
        }
    }

    public sealed class VolumeWeightMixer : PlayableBehaviour
    {
        private Volume _volume;
        private float _originalWeight;
        private PlayableDirector _director;

        public void Initialize(PlayableDirector director)
        {
            _director = director;
            if (_director != null) _director.stopped += OnStopped;
        }

        private void Restore()
        {
            if (_volume != null) _volume.weight = _originalWeight;
        }

        private void OnStopped(PlayableDirector director) => Restore();

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var volume = playerData as Volume;
            if (volume != _volume)
            {
                Restore();
                _volume = volume;
                if (_volume != null) _originalWeight = _volume.weight;
            }
            if (_volume == null) return;
            float total = 0, result = 0;
            for (int i = 0; i < playable.GetInputCount(); ++i)
            {
                float influence = playable.GetInputWeight(i);
                if (influence <= 0) continue;
                var input = (ScriptPlayable<VolumeWeightBehaviour>)playable.GetInput(i);
                var clip = input.GetBehaviour().clip;
                if (clip == null || !clip.TryEvaluate(input.GetTime(), out float value)) continue;
                total += influence;
                result += value * influence;
            }
            _volume.weight = Mathf.Clamp01(result / Mathf.Max(1, total) + _originalWeight * Mathf.Max(0, 1 - total));
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_director != null) _director.stopped -= OnStopped;
            Restore();
            _volume = null;
        }
    }
}

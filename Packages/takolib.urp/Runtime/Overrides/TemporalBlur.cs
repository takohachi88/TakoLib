using System;
using UnityEditor.Rendering;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

namespace TakoLib.Urp.PostProcess
{
    [Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/TemporalBlur", typeof(UniversalRenderPipeline))]
    public class TemporalBlur : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new(0, 0, 1);
        public TemporalBlurIntervalModeParameter intervalMode = new(TemporalBlurIntervalMode.Time);
        public NoInterpMinFloatParameter intervalTime = new(0.3f, 0);
        public NoInterpMinIntParameter intervalFrameCount = new(1, 1);
        public ClampedFloatParameter fadeOut = new(0.1f, 0.001f, 1f);

        public bool IsActive() => 0 < intensity.value;
        public bool IsTileCompatible() => true;
    }

    public enum TemporalBlurIntervalMode
    {
        FrameCount,
        Time,
    }

    [Serializable]
    public sealed class TemporalBlurIntervalModeParameter : VolumeParameter<TemporalBlurIntervalMode>
    {
        public TemporalBlurIntervalModeParameter(TemporalBlurIntervalMode value, bool overrideState = false) : base(value, overrideState) { }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(TemporalBlur))]
    public sealed class TemporalBlurEditor : VolumeComponentEditor
    {
        private SerializedDataParameter _intensity;
        private SerializedDataParameter _intervalMode;
        private SerializedDataParameter _intervalTime;
        private SerializedDataParameter _intervalFrameCount;
        private SerializedDataParameter _fadeOut;


        public override void OnEnable()
        {
            PropertyFetcher<TemporalBlur> properties = new PropertyFetcher<TemporalBlur>(serializedObject);

            _intensity = Unpack(properties.Find(x => x.intensity));
            _intervalMode = Unpack(properties.Find(x => x.intervalMode));
            _intervalTime = Unpack(properties.Find(x => x.intervalTime));
            _intervalFrameCount = Unpack(properties.Find(x => x.intervalFrameCount));
            _fadeOut = Unpack(properties.Find(x => x.fadeOut));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(_intensity);
            PropertyField(_intervalMode);
            PropertyField(_fadeOut);
            switch ((TemporalBlurIntervalMode)_intervalMode.value.enumValueIndex)
            {
                case TemporalBlurIntervalMode.Time:
                    PropertyField(_intervalTime);
                    break;
                case TemporalBlurIntervalMode.FrameCount:
                    PropertyField(_intervalFrameCount);
                    break;
            }
        }
    }

#endif
}
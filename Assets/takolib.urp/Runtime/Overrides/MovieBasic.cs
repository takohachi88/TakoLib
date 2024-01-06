using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace TakoLib.Urp.PostProcess
{
    [Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/MovieBasic", typeof(UniversalRenderPipeline))]
    public class MovieBasic : VolumeComponent, IPostProcessComponent
    {
        [Header("Common")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);

        [Header("Blur")]
        public ClampedFloatParameter blurIntensity = new ClampedFloatParameter(1, 0, 1);
        public NoInterpVector2Parameter blurScale = new NoInterpVector2Parameter(Vector2.one);
        public ClampedIntParameter sampleCount = new ClampedIntParameter(4, 2, 15);
        public MinIntParameter downSamplingFactor = new MinIntParameter(1, 1);

        [Header("Control")]
        public MovieControlModeParameter controlMode = new MovieControlModeParameter(MovieControlMode.None);
        public NoInterpVector2Parameter direction = new NoInterpVector2Parameter(new Vector2(1, 0));

        public NoInterpVector2Parameter center = new NoInterpVector2Parameter(new Vector2(0.5f, 0.5f));
        public ClampedFloatParameter smoothness = new ClampedFloatParameter(0, 0, 1);
        public BoolParameter rounded = new BoolParameter(false);

        [Tooltip("R:intensity, GB: direction")]
        public TextureParameter controlTextureRgb = new TextureParameter(null);
        public ClampedFloatParameter controlIntensityR = new ClampedFloatParameter(0, 0, 1);
        public NoInterpVector2Parameter controlIntensityGb = new NoInterpVector2Parameter(new Vector2(0.5f, 0.5f));
        public NoInterpVector2Parameter tiling = new NoInterpVector2Parameter(Vector2.one);

        [Header("Chromatic Aberration")]
        public ClampedFloatParameter chromaticAberrationIntensity = new ClampedFloatParameter(0, 0, 1);

        [Header("Vignette")]
        public ClampedFloatParameter vignetteIntensity = new ClampedFloatParameter(0, 0, 1);
        public ClampedFloatParameter vignetteSmoothness = new ClampedFloatParameter(0, 0, 1);
        public ColorParameter vignetteColor = new ColorParameter(new Color(0, 0, 0));
        public BlendModeParameter vignetteBlendMode = new BlendModeParameter(BlendMode.Multiply);

        public bool IsActive() => 0 < intensity.value;
        public bool IsTileCompatible() => true;
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(MovieBasic))]
    sealed class MovieBasicEditor : VolumeComponentEditor
    {
        private SerializedDataParameter _intensity;

        private SerializedDataParameter _blurIntensity;
        private SerializedDataParameter _blurScale;
        private SerializedDataParameter _sampleCount;
        private SerializedDataParameter _downSampleFactor;

        private SerializedDataParameter _controlMode;

        private SerializedDataParameter _direction;

        private SerializedDataParameter _center;
        private SerializedDataParameter _smoothness;
        private SerializedDataParameter _rounded;

        private SerializedDataParameter _controlTextureRgb;
        private SerializedDataParameter _controlIntensityR;
        private SerializedDataParameter _controlIntensityGb;
        private SerializedDataParameter _tiling;

        private SerializedDataParameter _chromaticAberrationIntensity;

        private SerializedDataParameter _vignetteIntensity;
        private SerializedDataParameter _vignetteSmoothness;
        private SerializedDataParameter _vignetteColor;
        private SerializedDataParameter _vignetteBlendMode;



        public override void OnEnable()
        {
            PropertyFetcher<MovieBasic> properties = new PropertyFetcher<MovieBasic>(serializedObject);

            _intensity = Unpack(properties.Find(x => x.intensity));

            _blurIntensity = Unpack(properties.Find(x => x.blurIntensity));
            _blurScale = Unpack(properties.Find(x => x.blurScale));
            _sampleCount = Unpack(properties.Find(x => x.sampleCount));
            _downSampleFactor = Unpack(properties.Find(x => x.downSamplingFactor));

            _controlMode = Unpack(properties.Find(x => x.controlMode));

            _direction = Unpack(properties.Find(x => x.direction));

            _center = Unpack(properties.Find(x => x.center));
            _smoothness = Unpack(properties.Find(x => x.smoothness));
            _rounded = Unpack(properties.Find(x => x.rounded));
            
            _controlTextureRgb = Unpack(properties.Find(x => x.controlTextureRgb));
            _controlIntensityR = Unpack(properties.Find(x => x.controlIntensityR));
            _controlIntensityGb = Unpack(properties.Find(x => x.controlIntensityGb));
            _tiling = Unpack(properties.Find(x => x.tiling));

            _chromaticAberrationIntensity = Unpack(properties.Find(x => x.chromaticAberrationIntensity));

            _vignetteIntensity = Unpack(properties.Find(x => x.vignetteIntensity));
            _vignetteSmoothness = Unpack(properties.Find(x => x.vignetteSmoothness));
            _vignetteColor = Unpack(properties.Find(x => x.vignetteColor));
            _vignetteBlendMode = Unpack(properties.Find(x => x.vignetteBlendMode));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(_intensity);

            PropertyField(_blurIntensity);
            PropertyField(_blurScale);
            PropertyField(_sampleCount);
            PropertyField(_downSampleFactor);

            PropertyField(_controlMode);
            switch ((MovieControlMode)_controlMode.value.enumValueIndex)
            {
                case MovieControlMode.None:
                    PropertyField(_direction);
                    break;
                case MovieControlMode.Fringe:
                    PropertyField(_center);
                    PropertyField(_smoothness);
                    PropertyField(_rounded);
                    break;
                case MovieControlMode.Texture:
                    PropertyField(_controlTextureRgb);
                    PropertyField(_controlIntensityR);
                    PropertyField(_controlIntensityGb);
                    PropertyField(_tiling);
                    break;
            }

            PropertyField(_chromaticAberrationIntensity);

            PropertyField(_vignetteIntensity);
            PropertyField(_vignetteSmoothness);
            PropertyField(_vignetteColor);
            PropertyField(_vignetteBlendMode);
        }
    }

#endif
}
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TakoLib.Urp.PostProcess
{
    /// <summary>
    /// 映画において典型的なエフェクト集。
    /// </summary>
    public class MovieBasicPass : CustomPostProcessPassBase
    {
        private LocalKeyword _keywordControlModeNone;
        private LocalKeyword _keywordControlModeFringe;
        private LocalKeyword _keywordControlModeTexture;
        private bool _lastBlitIsDestination;
        private RTHandle _tempTarget1;
        private RTHandle _tempTarget2;

        public MovieBasicPass(CustomPostProcessData data, Material material) : base(data, material)
        {
            _keywordControlModeNone = new LocalKeyword(material.shader, TakoLibUrpCommon.Keyword.ControlModeNone);
            _keywordControlModeFringe = new LocalKeyword(material.shader, TakoLibUrpCommon.Keyword.ControlModeFringe);
            _keywordControlModeTexture = new LocalKeyword(material.shader, TakoLibUrpCommon.Keyword.ControlModeTexture);
        }

        public override bool Execute(ref PostProcessParams parameters)
        {
            SetCommonParams(ref parameters);
            DoMovieBasic();
            return _lastBlitIsDestination;
        }
        private void DoMovieBasic()
        {
            MovieBasic movieBasic = _volumeStack.GetComponent<MovieBasic>();

            if (!movieBasic || !movieBasic.IsActive()) return;

            _lastBlitIsDestination = false;
            bool chromaticAberration = 0 < movieBasic.chromaticAberrationIntensity.value;
            bool blur = 0 < movieBasic.blurIntensity.value;
            bool vignette = 0 < movieBasic.vignetteIntensity.value;

            //common properties
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Intensity, movieBasic.intensity.value);

            //control texture properties
            switch (movieBasic.controlMode.value)
            {
                case MovieControlMode.None:
                    SetKeyword(MovieControlMode.None);
                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.Direction, movieBasic.direction.value);
                    break;

                case MovieControlMode.Fringe:
                    SetKeyword(MovieControlMode.Fringe);
                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.Center, movieBasic.center.value);
                    _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Rounded, movieBasic.rounded.value ? 1 : 0);
                    _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Smoothness, movieBasic.smoothness.value);
                    break;

                case MovieControlMode.Texture:
                    Debug.LogError($"{nameof(MovieBasic)}のTextureモードは未実装です。");
                    SetKeyword(MovieControlMode.Texture);
                    if (movieBasic.controlTextureRgb.value)
                    {
                        _cmd.SetGlobalTexture(TakoLibUrpCommon.ShaderId.ControlTextureRgb, movieBasic.controlTextureRgb.value);
                        _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.ControlIntensity,
                            new Vector3(movieBasic.controlIntensityR.value, movieBasic.controlIntensityGb.value.x, movieBasic.controlIntensityGb.value.y));
                        _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.ControlTiling, movieBasic.tiling.value);
                    }
                    break;
            }

            //chromatic aberration
            if (chromaticAberration)
            {
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.ChromaticAberrationIntensity, movieBasic.chromaticAberrationIntensity.value);
                Blitter.BlitCameraTexture(_cmd, _source, _destination, _material, 0);
                Swap(ref _source, ref _destination);
                _lastBlitIsDestination = true;
            }

            //blur
            if (blur)
            {
                //blur properties
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.BlurIntensity, movieBasic.blurIntensity.value);
                _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurScale, movieBasic.blurScale.value);
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.SampleCount, movieBasic.sampleCount.value);


                int divisionFactor = movieBasic.downSamplingFactor.value;
                if (1 < divisionFactor)
                {
                    RenderTextureDescriptor descriptor = _descriptor;
                    int division = BlurDivision(movieBasic.blurIntensity.value * movieBasic.intensity.value, divisionFactor);
                    descriptor.width /= division;
                    descriptor.height /= division;
                    RenderingUtils.ReAllocateIfNeeded(ref _tempTarget1, descriptor);
                    RenderingUtils.ReAllocateIfNeeded(ref _tempTarget2, descriptor);

                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, new Vector2(0.1f, 0));
                    Blitter.BlitCameraTexture(_cmd, _source, _tempTarget1, _material, 1);
                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, new Vector2(0, 0.1f));
                    Blitter.BlitCameraTexture(_cmd, _tempTarget1, _tempTarget2, _material, 1);
                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, new Vector2(0.07f, 0.07f));
                    Blitter.BlitCameraTexture(_cmd, _tempTarget2, _tempTarget1, _material, 1);
                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, new Vector2(0.07f, -0.07f));
                    Blitter.BlitCameraTexture(_cmd, _tempTarget1, _tempTarget2, _material, 1);

                    if (vignette) _source = _tempTarget2;
                    else Blitter.BlitCameraTexture(_cmd, _tempTarget2, _source);
                }
                else
                {
                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, new Vector2(0.1f, 0));
                    Blitter.BlitCameraTexture(_cmd, _source, _destination, _material, 1);
                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, new Vector2(0, 0.1f));
                    Blitter.BlitCameraTexture(_cmd, _destination, _source, _material, 1);
                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, new Vector2(0.07f, 0.07f));
                    Blitter.BlitCameraTexture(_cmd, _source, _destination, _material, 1);
                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, new Vector2(0.07f, -0.07f));
                    Blitter.BlitCameraTexture(_cmd, _destination, _source, _material, 1);
                }
            }

            //vignette
            if (vignette)
            {
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.VignetteIntensity, movieBasic.vignetteIntensity.value * 3f);
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.VignetteSmoothness, movieBasic.vignetteSmoothness.value * 5f);
                _cmd.SetGlobalColor(TakoLibUrpCommon.ShaderId.VignetteColor, movieBasic.vignetteColor.value);
                _cmd.SetGlobalInt(TakoLibUrpCommon.ShaderId.BlendMode, (int)movieBasic.vignetteBlendMode.value);

                //vignette
                Blitter.BlitCameraTexture(_cmd, _source, _destination, _material, 2);
                _lastBlitIsDestination = !_lastBlitIsDestination;
            }
        }

        private int BlurDivision(float intensity, int factor) => Mathf.CeilToInt(intensity * factor);

        private void Swap(ref RTHandle a, ref RTHandle b)
        {
            (a, b) = (b, a);
        }

        private void SetKeyword(MovieControlMode mode)
        {
            _cmd.SetKeyword(_material, _keywordControlModeNone, mode == MovieControlMode.None);
            _cmd.SetKeyword(_material, _keywordControlModeFringe, mode == MovieControlMode.Fringe);
            _cmd.SetKeyword(_material, _keywordControlModeTexture, mode == MovieControlMode.Texture);
        }
    }
}

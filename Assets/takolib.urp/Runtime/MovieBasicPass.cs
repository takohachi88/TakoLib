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
        private LocalKeyword _keywordVignette;
        private bool _lastBlitIsDestination;
        private RTHandle _tempTarget1;
        private RTHandle _tempTarget2;

        public MovieBasicPass(CustomPostProcessData data, Material material) : base(data, material)
        {
            _keywordControlModeNone = new LocalKeyword(material.shader, TakoLibUrpCommon.Keyword.ControlModeNone);
            _keywordControlModeFringe = new LocalKeyword(material.shader, TakoLibUrpCommon.Keyword.ControlModeFringe);
            _keywordControlModeTexture = new LocalKeyword(material.shader, TakoLibUrpCommon.Keyword.ControlModeTexture);
            _keywordVignette = new LocalKeyword(material.shader, TakoLibUrpCommon.Keyword.Vignette);
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
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Rounded, movieBasic.rounded.value ? 1 : 0);

            //control texture properties
            switch (movieBasic.controlMode.value)
            {
                case MovieControlMode.None:
                    SetControlKeyword(MovieControlMode.None);
                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.Direction, movieBasic.direction.value);
                    break;

                case MovieControlMode.Fringe:
                    SetControlKeyword(MovieControlMode.Fringe);
                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.Center, movieBasic.center.value);
                    _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Smoothness, movieBasic.smoothness.value);
                    break;

                case MovieControlMode.Texture:
                    Debug.LogError($"{nameof(MovieBasic)}のTextureモードは未実装です。");
                    SetControlKeyword(MovieControlMode.Texture);
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

            //vignette
            if (vignette)
            {
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.VignetteIntensity, movieBasic.vignetteIntensity.value * 3f);
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.VignetteSmoothness, movieBasic.vignetteSmoothness.value * 5f);
                _cmd.SetGlobalColor(TakoLibUrpCommon.ShaderId.VignetteColor, movieBasic.vignetteColor.value);
                _cmd.SetGlobalInt(TakoLibUrpCommon.ShaderId.BlendMode, (int)movieBasic.vignetteBlendMode.value);
            }

            //blur
            if (blur)
            {
                //blur properties
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.BlurIntensity, movieBasic.blurIntensity.value);
                _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurScale, movieBasic.blurScale.value);
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.SampleCount, movieBasic.sampleCount.value);
                _cmd.SetKeyword(_material, _keywordVignette, false);

                int divisionFactor = movieBasic.downSamplingFactor.value;
                int dimention = movieBasic.blurDimention.value;
                if (1 < divisionFactor && 1 < dimention)
                {
                    RenderTextureDescriptor descriptor = _descriptor;
                    int division = BlurDivision(movieBasic.blurIntensity.value * movieBasic.intensity.value, divisionFactor);
                    descriptor.width /= division;
                    descriptor.height /= division;
                    RenderingUtils.ReAllocateIfNeeded(ref _tempTarget1, descriptor);
                    RenderingUtils.ReAllocateIfNeeded(ref _tempTarget2, descriptor);

                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, Direction(dimention, 0, movieBasic.direction.value));
                    Blitter.BlitCameraTexture(_cmd, _source, _tempTarget1, _material, 1);

                    RTHandle source = _tempTarget1;
                    RTHandle destination = _tempTarget2;

                    for (int i = 1; i < dimention; i++)
                    {
                        _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, Direction(dimention, i, movieBasic.direction.value));
                        Blitter.BlitCameraTexture(_cmd, source, destination, _material, 1);
                        Swap(ref source, ref destination);
                    }

                    _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, Direction(dimention, dimention - 1, movieBasic.direction.value));

                    if (vignette) Blitter.BlitCameraTexture(_cmd, source, _source, _material, 2);
                    else Blitter.BlitCameraTexture(_cmd, source, _source);

                    Swap(ref _source, ref _destination);
                }
                else
                {
                    for (int i = 0; i < dimention; i++)
                    {
                        _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, Direction(dimention, i, movieBasic.direction.value));

                        //blurかつvignetteの場合は最後のblur内でvignetteをやってしまう。（blitの節約）
                        if (vignette && i == dimention - 1) _cmd.SetKeyword(_material, _keywordVignette, true);

                        Blitter.BlitCameraTexture(_cmd, _source, _destination, _material, 1);
                        Swap(ref _source, ref _destination);
                        _lastBlitIsDestination = !_lastBlitIsDestination;
                    }
                }
            }

            //vignette only
            if (vignette && !blur)
            {
                Blitter.BlitCameraTexture(_cmd, _source, _destination, _material, 2);
                _lastBlitIsDestination = !_lastBlitIsDestination;
            }
        }

        private static Vector2 Direction(int dimention, int index, Vector2 direction)
            => Quaternion.Euler(0, 0, 180f / dimention * index) * direction * 0.1f;

        private int BlurDivision(float intensity, int factor) => Mathf.CeilToInt(intensity * factor);

        private void Swap(ref RTHandle a, ref RTHandle b)
        {
            (a, b) = (b, a);
        }

        private void SetControlKeyword(MovieControlMode mode)
        {
            _cmd.SetKeyword(_material, _keywordControlModeNone, mode == MovieControlMode.None);
            _cmd.SetKeyword(_material, _keywordControlModeFringe, mode == MovieControlMode.Fringe);
            _cmd.SetKeyword(_material, _keywordControlModeTexture, mode == MovieControlMode.Texture);
        }
    }
}

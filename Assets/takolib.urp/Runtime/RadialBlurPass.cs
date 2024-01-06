using TakoLib.Common.Extensions;
using UnityEngine;
using UnityEngine.Rendering;

namespace TakoLib.Urp.PostProcess
{
    public class RadialBlurPass : CustomPostProcessPassBase
    {
        private LocalKeyword _keywordDither;
        private LocalKeyword _keywordNoiseGradientTexture;

        public RadialBlurPass(CustomPostProcessData data, Material material) : base(data, material)
        {
            _keywordDither = new LocalKeyword(material.shader, TakoLibUrpCommon.Keyword.Dither);
            _keywordNoiseGradientTexture = new LocalKeyword(material.shader, TakoLibUrpCommon.Keyword.NoiseGradientTexture);
        }

        public override bool Execute(ref PostProcessParams parameters)
        {
            SetCommonParams(ref parameters);
            DoRadialBlur();
            return false;
        }
        private void DoRadialBlur()
        {
            RadialBlur radialBlur = _volumeStack.GetComponent<RadialBlur>();

            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Intensity, radialBlur.intensity.value);
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.BlurIntensity, radialBlur.blurIntensity.value);
            _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.Center, radialBlur.center.value);
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.SampleCount, radialBlur.sampleCount.value);
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.ChromaticAberrationIntensity, radialBlur.chromaticAberrationIntensity.value);
            _cmd.SetKeyword(_material, _keywordDither, radialBlur.dither.value);
            _cmd.SetKeyword(_material, _keywordNoiseGradientTexture, radialBlur.noiseGradientTexture.value);
            if (radialBlur.noiseGradientTexture.value)
            {
                _cmd.SetGlobalTexture(TakoLibUrpCommon.ShaderId.NoiseGradientTexture, radialBlur.noiseGradientTexture.value);
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.NoiseTiling, radialBlur.noiseTiling.value);
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.NoiseTiling, radialBlur.noiseTiling.value);
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.NoiseIntensity, radialBlur.noiseIntensity.value);
            }

            if (0 < radialBlur.chromaticAberrationIntensity.value)
            {
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.ChromaticAberrationLimit, radialBlur.chromaticAberrationLimit.value);
                Blitter.BlitCameraTexture(_cmd, _source, _destination, _material, 1);
            }
            else
            {
                Blitter.BlitCameraTexture(_cmd, _source, _destination);
            }
            Blitter.BlitCameraTexture(_cmd, _destination, _source, _material, 0);
        }
    }
}

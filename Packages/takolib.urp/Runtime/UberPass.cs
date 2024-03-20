using TakoLib.Common.Extensions;
using UnityEngine;
using UnityEngine.Rendering;

namespace TakoLib.Urp.PostProcess
{
    public class UberPass : CustomPostProcessPassBase
    {
        private LocalKeyword keywordVignette;

        public UberPass(CustomPostProcessData data, Material material) : base(data, material)
        {
            keywordVignette = new LocalKeyword(material.shader, TakoLibUrpCommon.Keyword.Vignette);
        }

        public override bool Execute(ref PostProcessParams parameters)
        {
            SetCommonParams(ref parameters);
            DoUber();
            return true;
        }
        private void DoUber()
        {
            Mosaic mosaic = _volumeStack.GetComponent<Mosaic>();
            Posterization posterization = _volumeStack.GetComponent<Posterization>();
            Nega nega = _volumeStack.GetComponent<Nega>();
            AdvancedVignette advancedVignette = _volumeStack.GetComponent<AdvancedVignette>();

            bool useMosaic = mosaic && mosaic.IsActive();
            bool usePosterization = posterization && posterization.IsActive();
            bool useNega = nega && nega.IsActive();
            bool useVignette = advancedVignette && advancedVignette.IsActive();
            
            _cmd.SetKeyword(_material, keywordVignette, useVignette);

            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Nega, useNega.ToInt());

            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.PosterizationIntensity, posterization.intensity.value);
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.NegaIntensity, nega.intensity.value);
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.VignetteIntensity, advancedVignette.intensity.value * 3f);

            if (useMosaic)
            {
                float t = mosaic.intensity.value;
                t = 1 - t;
                t = 1 - t * t * t * t * t;
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.MosaicIntensity, t);
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.MosaicCellDensity, mosaic.cellDensity.value);
            }
            else _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.MosaicIntensity, 0);

            if (usePosterization)
            {
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.ToneCount, posterization.toneCount.value);
            }
            if (useVignette)
            {
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.VignetteSmoothness, advancedVignette.smoothness.value * 5f);
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Rounded, advancedVignette.rounded.value ? 1 : 0);
                _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.VignetteColor, advancedVignette.color.value);
                _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.VignetteCenter, advancedVignette.center.value);
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.BlendMode, (int)advancedVignette.blendMode.value);
            }

            Blitter.BlitCameraTexture(_cmd, _source, _destination, _material, 0);
        }
    }
}

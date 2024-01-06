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
            AdvancedVignette advancedVignette = _volumeStack.GetComponent<AdvancedVignette>();

            bool useVignette = advancedVignette || advancedVignette.IsActive();
            _cmd.SetKeyword(_material, keywordVignette, useVignette);
            if (useVignette)
            {
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.VignetteIntensity, advancedVignette.intensity.value * 3f);
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Smoothness, advancedVignette.smoothness.value * 5f);
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Rounded, advancedVignette.rounded.value ? 1 : 0);
                _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.VignetteColor, advancedVignette.color.value);
                _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.VignetteCenter, advancedVignette.center.value);
                _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.BlendMode, (int)advancedVignette.blendMode.value);
            }

            Blitter.BlitCameraTexture(_cmd, _source, _destination, _material, 0);

        }
    }
}

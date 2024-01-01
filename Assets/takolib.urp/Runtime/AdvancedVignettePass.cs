using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace TakoLib.Urp.PostProcess
{
    public class AdvancedVignettePass : CustomPostProcessPassBase<AdvancedVignette>
    {
        public AdvancedVignettePass(CustomPostProcessData data, Material material) : base(data, material) { }

        public override void Execute(ref PostProcessParams<AdvancedVignette> parameters)
        {
            SetCommonParams(ref parameters);
            DoAdvancedVignette(parameters.cmd, parameters.volumeComponent, parameters.source, parameters.destination, parameters.aspectRatio);
        }
        private void DoAdvancedVignette(CommandBuffer cmd, AdvancedVignette advancedVignette, RTHandle source, RTHandle destination, float aspectRatio)
        {
            cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Intensity, advancedVignette.intensity.value * 3f);
            cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Smoothness, advancedVignette.smoothness.value * 5f);
            cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Roundness, advancedVignette.rounded.value ? aspectRatio : 1f);
            cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.Color, advancedVignette.color.value);
            cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.Center, advancedVignette.center.value);
            cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.BlendMode, (int)advancedVignette.blendMode.value);

            Blitter.BlitCameraTexture(cmd, source, destination, _material, 0);
            Blitter.BlitCameraTexture(cmd, destination, source);
        }
    }
}

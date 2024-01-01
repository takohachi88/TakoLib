using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace TakoLib.Urp.PostProcess
{
    public class RadialBlurPass : CustomPostProcessPassBase<RadialBlur>
    {
        public RadialBlurPass(CustomPostProcessData data, Material material) : base(data, material) { }

        public override void Execute(ref PostProcessParams<RadialBlur> parameters)
        {
            SetCommonParams(ref parameters);
            DoRadialBlur(parameters.cmd, parameters.volumeComponent, parameters.source, parameters.destination);
        }
        private void DoRadialBlur(CommandBuffer cmd, RadialBlur radialBlur, RTHandle source, RTHandle destination)
        {
            cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Intensity, radialBlur.intensity.value);
            cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.Center, radialBlur.center.value);
            cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.SampleCount, radialBlur.sampleCount.value);

            Blitter.BlitCameraTexture(cmd, source, destination, _material, 0);
            Blitter.BlitCameraTexture(cmd, destination, source);
        }
    }
}

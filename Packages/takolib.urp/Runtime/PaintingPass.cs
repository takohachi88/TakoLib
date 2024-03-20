using UnityEngine;
using UnityEngine.Rendering;

namespace TakoLib.Urp.PostProcess
{
    public class PaintingPass : CustomPostProcessPassBase
    {
        public PaintingPass(CustomPostProcessData data, Material material) : base(data, material)
        {
        }

        public override bool Execute(ref PostProcessParams parameters)
        {
            SetCommonParams(ref parameters);
            DoSnnFiltering();
            return true;
        }
        private void DoSnnFiltering()
        {
            Painting painting = _volumeStack.GetComponent<Painting>();

            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Intensity, painting.intensity.value * 0.01f);
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.SampleCount, painting.sampleCount.value);

            Blitter.BlitCameraTexture(_cmd, _source, _destination, _material, 0);
        }
    }
}

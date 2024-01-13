using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TakoLib.Urp.PostProcess
{
    [Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/Painting", typeof(UniversalRenderPipeline))]
    public class Painting : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);
        public NoInterpClampedIntParameter sampleCount = new NoInterpClampedIntParameter(3, 2, 8);

        public bool IsActive() => 0 < intensity.value;
        public bool IsTileCompatible() => true;
    }
}
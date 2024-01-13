using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TakoLib.Urp.PostProcess
{
    [Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/Posterization", typeof(UniversalRenderPipeline))]
    public class Posterization : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);
        public NoInterpClampedFloatParameter threshold = new NoInterpClampedFloatParameter(0, 0, 1);
        public MinIntParameter toneCount = new MinIntParameter(3, 1);

        public bool IsActive() => 0 < intensity.value;
        public bool IsTileCompatible() => true;
    }
}
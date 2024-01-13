using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TakoLib.Urp.PostProcess
{
    [Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/Mosaic", typeof(UniversalRenderPipeline))]
    public class Mosaic : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);

        public MinIntParameter cellDensity = new MinIntParameter(20, 1);

        public bool IsActive() => 0 < intensity.value;
        public bool IsTileCompatible() => true;
    }
}
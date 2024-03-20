using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TakoLib.Urp.PostProcess
{
    [Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/Diffusion", typeof(UniversalRenderPipeline))]
    public class Diffusion : VolumeComponent, IPostProcessComponent
    {
        public MinFloatParameter intensity = new(0, 0);
        public NoInterpMinFloatParameter threshold = new(0.5f, 0);
        public NoInterpClampedFloatParameter radius = new(0.05f, 0.01f, 0.05f);
        public BlendModeParameter blendMode = new(BlendMode.Alpha);

        public bool IsActive() => 0 < intensity.value;
        public bool IsTileCompatible() => true;
    }
}
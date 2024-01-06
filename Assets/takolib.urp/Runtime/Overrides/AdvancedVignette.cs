using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

namespace TakoLib.Urp.PostProcess
{
    [Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/Advanced Vignette", typeof(UniversalRenderPipeline))]
    public class AdvancedVignette : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);
        public NoInterpClampedFloatParameter smoothness = new NoInterpClampedFloatParameter(0, 0, 1);
        public BoolParameter rounded = new BoolParameter(false);
        public NoInterpColorParameter color = new NoInterpColorParameter(new Color(0, 0.4f, 0.4f));
        public NoInterpVector2Parameter center = new NoInterpVector2Parameter(new Vector2(0.5f, 0.5f));
        public BlendModeParameter blendMode = new BlendModeParameter(BlendMode.Multiply);

        public bool IsActive() => 0 < intensity.value;
        public bool IsTileCompatible() => true;
    }
}
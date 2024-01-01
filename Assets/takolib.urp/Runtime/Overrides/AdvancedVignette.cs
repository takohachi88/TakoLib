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
        public ClampedFloatParameter smoothness = new ClampedFloatParameter(0, 0, 1);
        public BoolParameter rounded = new BoolParameter(false);
        public ColorParameter color = new ColorParameter(new Color(0, 0.4f, 0.4f));
        public NoInterpVector2Parameter center = new NoInterpVector2Parameter(new Vector2(0.5f, 0.5f));
        public BlendModeParameter blendMode = new BlendModeParameter(BlendMode.Multiply);

        public bool IsActive() => 0 < intensity.value;
        public bool IsTileCompatible() => true;
    }
}

public enum BlendMode
{
    Alpha,
    Additive,
    Multiply,
    Nega,
}

[Serializable]
public sealed class BlendModeParameter : VolumeParameter<BlendMode>
{
    public BlendModeParameter(BlendMode value, bool overrideState = false) : base(value, overrideState) { }
}

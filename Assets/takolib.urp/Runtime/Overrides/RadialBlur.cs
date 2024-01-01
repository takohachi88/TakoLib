using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

[Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/Radial Blur", typeof(UniversalRenderPipeline))]
public class RadialBlur : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);
    public NoInterpVector2Parameter center = new NoInterpVector2Parameter(new Vector2(0.5f, 0.5f));
    public ClampedIntParameter sampleCount = new ClampedIntParameter(5, 2, 15);

    public bool IsActive() => 0 < intensity.value;
    public bool IsTileCompatible() => true;
}

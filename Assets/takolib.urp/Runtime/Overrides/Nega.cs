using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/Nega", typeof(UniversalRenderPipeline))]
public class Nega : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);

    public bool IsActive() => 0 < intensity.value;
    public bool IsTileCompatible() => true;
}

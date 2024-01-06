using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

[Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/Radial Blur", typeof(UniversalRenderPipeline))]
public class RadialBlur : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);
    public NoInterpVector2Parameter center = new NoInterpVector2Parameter(new Vector2(0.5f, 0.5f));
    
    [Header("Blur")]
    public ClampedFloatParameter blurIntensity = new ClampedFloatParameter(0.5f, 0, 1);
    public ClampedIntParameter sampleCount = new ClampedIntParameter(4, 2, 15);
    public BoolParameter dither = new BoolParameter(true);

    [Header("Noise")]
    public ClampedFloatParameter noiseIntensity = new ClampedFloatParameter(0, 0, 1);
    public TextureParameter noiseGradientTexture = new TextureParameter(null);
    public MinFloatParameter noiseTiling = new MinFloatParameter(1, Mathf.Epsilon);

    [Header("Chromatic Aberration")]
    public ClampedFloatParameter chromaticAberrationIntensity = new ClampedFloatParameter(0, 0, 1);
    public ClampedFloatParameter chromaticAberrationLimit = new ClampedFloatParameter(0.1f, 0, 1);

    public bool IsActive() => 0 < intensity.value;
    public bool IsTileCompatible() => true;
}

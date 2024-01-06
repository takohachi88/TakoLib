using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

namespace TakoLib.Urp.PostProcess
{
    [Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/Smart DoF", typeof(UniversalRenderPipeline))]
    public class SmartDof : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter focusDistance = new ClampedFloatParameter(0, 0, 1);
        public ClampedFloatParameter focusWidth = new ClampedFloatParameter(0.005f, 0, 1);
        public ClampedFloatParameter focusSmooth = new ClampedFloatParameter(0.06f, 0, 1);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);
        public MinIntParameter sampleCount  = new MinIntParameter(6, 2);


        public bool IsActive() => 0 < intensity.value;
        public bool IsTileCompatible() => true;
    }
}

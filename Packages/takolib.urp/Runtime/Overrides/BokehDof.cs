using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

namespace TakoLib.Urp.PostProcess
{
    [Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/Bokeh DoF", typeof(UniversalRenderPipeline))]
    public class BokehDof : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enabled = new(false);
        public MinFloatParameter focusDistance = new(10f, 0.1f);
        public ClampedFloatParameter focalLength = new(50f, 1f, 300f);

        public ClampedFloatParameter aperture = new(5.6f, 1f, 32f);
        public ClampedIntParameter bladeCount = new(5, 3, 9);
        public ClampedFloatParameter bladeCurvature = new(1f, 0f, 1f);
        public ClampedFloatParameter bladeRotation = new(0f, -180f, 180f);

        public bool IsActive() => enabled.value;
        public bool IsTileCompatible() => true;
    }
}

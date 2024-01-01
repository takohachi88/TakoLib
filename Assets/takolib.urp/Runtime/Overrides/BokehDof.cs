using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

namespace TakoLib.Urp.PostProcess
{
    [Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/Smart DoF", typeof(UniversalRenderPipeline))]
    public class BokehDof : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enabled = new BoolParameter(false);
        public MinFloatParameter focusDistance = new MinFloatParameter(10f, 0.1f);
        public ClampedFloatParameter focalLength = new ClampedFloatParameter(50f, 1f, 300f);

        public ClampedFloatParameter aperture = new ClampedFloatParameter(5.6f, 1f, 32f);
        public ClampedIntParameter bladeCount = new ClampedIntParameter(5, 3, 9);
        public ClampedFloatParameter bladeCurvature = new ClampedFloatParameter(1f, 0f, 1f);
        public ClampedFloatParameter bladeRotation = new ClampedFloatParameter(0f, -180f, 180f);

        public bool IsActive() => enabled.value;
        public bool IsTileCompatible() => true;
    }
}

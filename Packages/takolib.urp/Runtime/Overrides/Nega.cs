using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TakoLib.Urp.PostProcess
{
    [Serializable, VolumeComponentMenuForRenderPipeline("TakoLib/Nega", typeof(UniversalRenderPipeline))]
    public class Nega : VolumeComponent, IPostProcessComponent
    {
        public MinFloatParameter intensity = new(0, 0);

        public bool IsActive() => 0 < intensity.value;
        public bool IsTileCompatible() => true;
    }
}
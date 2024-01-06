using System;
using UnityEngine.Rendering;

namespace TakoLib.Urp.PostProcess
{
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
}
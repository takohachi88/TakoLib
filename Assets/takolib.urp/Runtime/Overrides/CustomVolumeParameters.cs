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

    public enum MovieControlMode
    {
        None,
        Fringe,
        Texture,
    }

    [Serializable]
    public sealed class BlendModeParameter : VolumeParameter<BlendMode>
    {
        public BlendModeParameter(BlendMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class MovieControlModeParameter : VolumeParameter<MovieControlMode>
    {
        public MovieControlModeParameter(MovieControlMode value, bool overrideState = false) : base(value, overrideState) { }
    }
}
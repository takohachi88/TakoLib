using UnityEngine;
using UnityEngine.Rendering;

namespace TakoLib.Urp.PostProcess
{
    public abstract class CustomPostProcessPassBase<T> where T : VolumeComponent
    {
        protected RTHandle _source;
        protected RTHandle _destination;
        protected RTHandle _tempTarget1;
        protected RTHandle _tempTarget2;
        protected RenderTextureDescriptor _descriptor;

        protected CustomPostProcessData _data;
        protected Material _material;

        public CustomPostProcessPassBase(CustomPostProcessData data, Material material)
        {
            _data = data;
            _material = material;
        }

        protected void SetCommonParams(ref PostProcessParams<T> parameter)
        {
            _source = parameter.source;
            _destination = parameter.destination;
            _tempTarget1 = parameter.tempTarget1;
            _tempTarget2 = parameter.tempTarget2;
            _descriptor = parameter.descriptor;
        }

        public abstract void Execute(ref PostProcessParams<T> parameter);
    }

    public struct PostProcessParams<T> where T : VolumeComponent
    {
        public CommandBuffer cmd;
        public T volumeComponent;
        public RTHandle source;
        public RTHandle destination;
        public RTHandle tempTarget1;
        public RTHandle tempTarget2;
        public RenderTextureDescriptor descriptor;
        public float aspectRatio;
    }
}

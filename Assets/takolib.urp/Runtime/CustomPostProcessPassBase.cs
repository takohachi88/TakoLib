using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace TakoLib.Urp.PostProcess
{
    public abstract class CustomPostProcessPassBase
    {
        protected CommandBuffer _cmd;
        protected RTHandle _source;
        protected RTHandle _destination;
        protected RenderTextureDescriptor _descriptor;

        protected CustomPostProcessData _data;
        protected Material _material;
        protected VolumeStack _volumeStack;

        public CustomPostProcessPassBase(CustomPostProcessData data, Material material)
        {
            _data = data;
            _material = material;
        }

        protected void SetCommonParams(ref PostProcessParams parameter)
        {
            _cmd = parameter.cmd;
            _volumeStack = parameter.volumeStack;
            _source = parameter.source;
            _destination = parameter.destination;
            _descriptor = parameter.descriptor;
        }


        /// <summary>
        /// passを実行する。
        /// </summary>
        /// <param name="parameter">エフェクトに必要なパラメータ</param>
        /// <returns>最後のblit先がdestinationか</returns>
        public abstract bool Execute(ref PostProcessParams parameter);
    }

    public ref struct PostProcessParams
    {
        public VolumeStack volumeStack;
        public CommandBuffer cmd;
        public RTHandle source;
        public RTHandle destination;
        public RenderTextureDescriptor descriptor; //TODO: public ref にしたいがC#10までお預け。
    }
}

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Linq;

namespace TakoLib.Urp.PostProcess
{
    public class SmartDofPass : CustomPostProcessPassBase
    {
        private RTHandle _tempTarget1;
        private RTHandle _tempTarget2;
        private RTHandle _tempTarget3;
        public SmartDofPass(CustomPostProcessData data, Material material) : base(data, material) { }

        public override bool Execute(ref PostProcessParams parameters)
        {
            SetCommonParams(ref parameters);
            DoSmartDof();
            return true;
        }


        private void DoSmartDof()
        {
            SmartDof smartDof = _volumeStack.GetComponent<SmartDof>();

            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.FocusDistance, 1 - smartDof.focusDistance.value);
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.FocusWidth, smartDof.focusWidth.value);
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.FocusSmooth, smartDof.focusSmooth.value);
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Intensity, smartDof.intensity.value * 0.07f);
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.SampleCount, smartDof.sampleCount.value);

            _descriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
            RenderingUtils.ReAllocateIfNeeded(ref _tempTarget1, _descriptor);
            RenderingUtils.ReAllocateIfNeeded(ref _tempTarget2, _descriptor);
            RenderingUtils.ReAllocateIfNeeded(ref _tempTarget3, _descriptor);

            Blitter.BlitCameraTexture(_cmd, _source, _tempTarget1);//奥抽出
            Blitter.BlitCameraTexture(_cmd, _tempTarget1, _tempTarget2, _material, 2);//ボケ横
            Blitter.BlitCameraTexture(_cmd, _tempTarget2, _tempTarget1, _material, 3);//ボケ縦
            Blitter.BlitCameraTexture(_cmd, _source, _tempTarget2, _material, 1);//手前抽出
            Blitter.BlitCameraTexture(_cmd, _tempTarget2, _tempTarget3, _material, 2);//ボケ横
            Blitter.BlitCameraTexture(_cmd, _tempTarget3, _tempTarget2, _material, 3);//ボケ縦

            _cmd.SetGlobalTexture(TakoLibUrpCommon.ShaderId.Temptarget1, _tempTarget1);
            _cmd.SetGlobalTexture(TakoLibUrpCommon.ShaderId.TempTarget2, _tempTarget2);

            Blitter.BlitCameraTexture(_cmd, _source, _destination, _material, 4);//ブレンド
        }

    }
}

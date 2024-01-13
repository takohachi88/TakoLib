using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TakoLib.Urp.PostProcess
{
    public class DiffusionPass : CustomPostProcessPassBase
    {
        private const int _targetCount = 8;

        private RTHandle[] _tempTargets = new RTHandle[_targetCount];

        public DiffusionPass(CustomPostProcessData data, Material material) : base(data, material)
        {
        }

        private void AllocateTargets()
        {
            RenderTextureDescriptor descriptor = _descriptor;

            descriptor.width = (int)(descriptor.width * 0.5f);
            descriptor.height = (int)(descriptor.height * 0.5f);
            RenderingUtils.ReAllocateIfNeeded(ref _tempTargets[0], descriptor);

            for (int i = 1; i < _targetCount; i += 2)
            {
                descriptor.width = (int)(descriptor.width * 0.5f);
                descriptor.height = (int)(descriptor.height * 0.5f);
                RenderingUtils.ReAllocateIfNeeded(ref _tempTargets[i], descriptor);
                if (i != _targetCount - 1) RenderingUtils.ReAllocateIfNeeded(ref _tempTargets[i + 1], descriptor);
            }
        }

        public override bool Execute(ref PostProcessParams parameters)
        {
            SetCommonParams(ref parameters);
            DoDiffusion();
            return true;
        }

        private void DownSampleBlit(Vector2 direction, RTHandle source, RTHandle destination)
        {
            _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, direction);
            Blitter.BlitCameraTexture(_cmd, source, destination, _material, 1);
        }

        private void DoDiffusion()
        {
            Diffusion diffusion = _volumeStack.GetComponent<Diffusion>();

            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Intensity, diffusion.intensity.value);
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Threshold, diffusion.threshold.value);
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.BlendMode, (int)diffusion.blendMode.value);

            AllocateTargets();

            //抽出。
            Blitter.BlitCameraTexture(_cmd, _source, _tempTargets[0], _material, 0);

            //down
            for (int i = 0; i < _targetCount - 2; i += 2)
            {
                _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, new Vector2(diffusion.radius.value, 0));
                Blitter.BlitCameraTexture(_cmd, _tempTargets[i], _tempTargets[i + 1], _material, 1);

                _cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BlurDirection, new Vector2(0, diffusion.radius.value));
                Blitter.BlitCameraTexture(_cmd, _tempTargets[i + 1], _tempTargets[i + 2], _material, 1);
            }

            //up
            for (int i = _targetCount - 3; 2 <= i; i -= 2)
            {
                _cmd.SetGlobalTexture(TakoLibUrpCommon.ShaderId.DiffusionMipTexture, _tempTargets[i]);
                Blitter.BlitCameraTexture(_cmd, _tempTargets[i], _tempTargets[i - 2], _material, 2);
            }
            Blitter.BlitCameraTexture(_cmd, _tempTargets[1], _tempTargets[0], _material, 2);

            _cmd.SetGlobalTexture(TakoLibUrpCommon.ShaderId.DiffusionTexture, _tempTargets[0]);

            Blitter.BlitCameraTexture(_cmd, _source, _destination, _material, 3);
        }
    }
}

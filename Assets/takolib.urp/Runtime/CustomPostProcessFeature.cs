using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TakoLib.Urp.PostProcess
{
    public class CustomPostProcessFeature : ScriptableRendererFeature
    {
        [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        [SerializeField] private CustomPostProcessData data;

        private CustomPostProcessPass pass;


        public override void Create()
        {
            pass = new CustomPostProcessPass(data);
            pass.renderPassEvent = renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(pass);
        }
    }


    public class CustomPostProcessPass : ScriptableRenderPass
    {
        private RenderTextureDescriptor _descriptor;
        private RTHandle _tempTarget1;
        private RTHandle _tempTarget2;
        private RTHandle _destination;

        private BokehDofPass _smartDofPass;
        private RadialBlurPass _radialBlurPass;
        private AdvancedVignettePass _advancedVignettePass;

        private MaterialLibrary _materialLibrary;

        private CustomPostProcessData _data;

        private static ProfilingSampler _smartDofSampler = new ProfilingSampler(nameof(BokehDof));
        private static ProfilingSampler _radialBlurSampler = new ProfilingSampler(nameof(RadialBlur));
        private static ProfilingSampler _advancedVignetteSampler = new ProfilingSampler(nameof(AdvancedVignette));

        private bool destinationIsCameraColor = false;

        public CustomPostProcessPass(CustomPostProcessData data)
        {
            profilingSampler = new ProfilingSampler("Custom Post Process Pass");
            _data = data;
            _materialLibrary = new MaterialLibrary(_data.resources);

            _smartDofPass = new BokehDofPass(_data, _materialLibrary.smartDof);
            _radialBlurPass = new RadialBlurPass(_data, _materialLibrary.radialBlur);
            _advancedVignettePass = new AdvancedVignettePass(_data, _materialLibrary.advancedVignette);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _descriptor = TakoLibUrpCommon.PostProcessDescriptor(cameraTextureDescriptor.width, cameraTextureDescriptor.height, cameraTextureDescriptor.graphicsFormat);
            RenderingUtils.ReAllocateIfNeeded(ref _tempTarget1, _descriptor);
            RenderingUtils.ReAllocateIfNeeded(ref _tempTarget2, _descriptor);
            RenderingUtils.ReAllocateIfNeeded(ref _destination, _descriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Custom Post Process Pass");

            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            destinationIsCameraColor = false;
            float aspectRatio = _descriptor.width / (float)_descriptor.height;

            if (!source.rt)
            {
                //Debug.LogError("source is null");
                return;
            }

            VolumeStack stack = VolumeManager.instance.stack;
            BokehDof smartDof = stack.GetComponent<BokehDof>();
            RadialBlur radialBlur = stack.GetComponent<RadialBlur>();
            AdvancedVignette advancedVignette = stack.GetComponent<AdvancedVignette>();

            if (smartDof.IsActive())
            {
                using (new ProfilingScope(cmd, _smartDofSampler))
                {
                    PostProcessParams<BokehDof> parameters = new()
                    {
                        cmd = cmd,
                        volumeComponent = smartDof,
                        source = source,
                        destination = _destination,
                        tempTarget1 = _tempTarget1,
                        tempTarget2 = _tempTarget2,
                        descriptor = _descriptor,
                    };
                    _smartDofPass.Execute(ref parameters);
                    Swap(ref source, ref _destination);
                }
            }

            if (radialBlur.IsActive())
            {
                using (new ProfilingScope(cmd, _radialBlurSampler))
                {
                    PostProcessParams<RadialBlur> parameters = new()
                    {
                        cmd = cmd,
                        volumeComponent = radialBlur,
                        source = source,
                        destination = _destination,
                    };
                    _radialBlurPass.Execute(ref parameters);
                    Swap(ref source, ref _destination);
                }
            }

            if (advancedVignette.IsActive())
            {
                using (new ProfilingScope(cmd, _advancedVignetteSampler))
                {
                    PostProcessParams<AdvancedVignette> parameters = new()
                    {
                        cmd = cmd,
                        volumeComponent = advancedVignette,
                        source = source,
                        destination = _destination,
                        aspectRatio = aspectRatio,
                    };
                    _advancedVignettePass.Execute(ref parameters);
                    Swap(ref source, ref _destination);
                }
            }




            if (destinationIsCameraColor)
            {
                Blitter.BlitCameraTexture(cmd, source, _destination);
                Swap(ref source, ref _destination);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Swap(ref RTHandle source, ref RTHandle destination)
        {
            (source, _destination) = (_destination, source);
            destinationIsCameraColor = !destinationIsCameraColor;
        }

        private void DoAdvancedVignette(BokehDof smartDof, CommandBuffer cmd, RTHandle source, RTHandle destination)
        {

        }


        public class MaterialLibrary : IDisposable
        {
            public readonly Material smartDof;
            public readonly Material radialBlur;
            public readonly Material advancedVignette;

            public MaterialLibrary(CustomPostProcessData.CustomPostProcessResources resources)
            {
                Assert.IsNotNull(resources);
                smartDof = Load(resources.smartDof);
                radialBlur = Load(resources.radialBlur);
                advancedVignette = Load(resources.advancedVignette);
            }

            private Material Load(Shader shader)
            {
                if (!shader || !shader.isSupported)
                {
                    Debug.LogError("shader is null or not supported");
                    return null;
                }
                return CoreUtils.CreateEngineMaterial(shader);
            }

            public void Dispose()
            {
                CoreUtils.Destroy(smartDof);
                CoreUtils.Destroy(radialBlur);
                CoreUtils.Destroy(advancedVignette);
            }
        }
    }
}
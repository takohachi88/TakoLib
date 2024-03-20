using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TakoLib.Urp.PostProcess
{
    public class CustomPostProcessFeature : ScriptableRendererFeature
    {
        [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        [SerializeField] private CustomPostProcessData _data;

        private CustomPostProcessPass _pass;

        public override void Create()
        {
            if (!_data) return;
            _pass = new CustomPostProcessPass(_data);
            _pass.renderPassEvent = _renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            VolumeStack stack = VolumeManager.instance.stack;
            if (!_data || stack == null) return;
            _pass.Setup(stack);
            renderer.EnqueuePass(_pass);
        }
    }

    public class CustomPostProcessPass : ScriptableRenderPass
    {
        private RenderTextureDescriptor _descriptor;
        private RTHandle _destination;

        private BokehDofPass _bokehDofPass;
        private SmartDofPass _smartDofPass;
        private RadialBlurPass _radialBlurPass;
        private MovieBasicPass _movieBasicPass;
        private UberPass _uberPass;
        private PaintingPass _paintingPass;
        private DiffusionPass _diffusionPass;
        private TemporalBlurPass _temporalBlurPass;

        private MaterialLibrary _materialLibrary;

        private CustomPostProcessData _data;

        private static ProfilingSampler _bokehDofSampler = new ProfilingSampler(nameof(BokehDof));
        private static ProfilingSampler _smartDofSampler = new ProfilingSampler(nameof(SmartDof));
        private static ProfilingSampler _radialBlurSampler = new ProfilingSampler(nameof(RadialBlur));
        private static ProfilingSampler _movieBasicSampler = new ProfilingSampler(nameof(MovieBasic));
        private static ProfilingSampler _uberSampler = new ProfilingSampler("UberEffect");
        private static ProfilingSampler _paintingSampler = new ProfilingSampler(nameof(Painting));
        private static ProfilingSampler _diffusionSampler = new ProfilingSampler(nameof(Diffusion));
        private static ProfilingSampler _temporalBlurSampler = new ProfilingSampler(nameof(TemporalBlur));

        private bool destinationIsCameraColor = false;
        private VolumeStack _volumeStack;

        public void Setup(VolumeStack stack)
        {
            _volumeStack = stack;
        }

        public CustomPostProcessPass(CustomPostProcessData data)
        {
            profilingSampler = new ProfilingSampler("Custom Post Process Pass");
            _data = data;
            _materialLibrary = new MaterialLibrary(_data.resources);

            _bokehDofPass = new BokehDofPass(_data, _materialLibrary.bokehDof);
            _smartDofPass = new SmartDofPass(_data, _materialLibrary.smartDof);
            _radialBlurPass = new RadialBlurPass(_data, _materialLibrary.radialBlur);
            _movieBasicPass = new MovieBasicPass(_data, _materialLibrary.movieBasic);
            _uberPass = new UberPass(_data, _materialLibrary.uber);
            _paintingPass = new PaintingPass(_data, _materialLibrary.painting);
            _diffusionPass = new DiffusionPass(_data, _materialLibrary.diffusion);
            _temporalBlurPass = new TemporalBlurPass(_data, _materialLibrary.temporalBlur);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _descriptor = TakoLibUrpCommon.PostProcessDescriptor(cameraTextureDescriptor.width, cameraTextureDescriptor.height, cameraTextureDescriptor.graphicsFormat);
            RenderingUtils.ReAllocateIfNeeded(ref _destination, _descriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Custom Post Process Pass");

            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            destinationIsCameraColor = false;

            if (!source.rt)
            {
                //Debug.LogError("source is null");
                return;
            }

            float aspectRatio = _descriptor.width / (float)_descriptor.height;
            cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.AspectRatio, aspectRatio);

            //こことそれぞれのExecute内で計2回GetComponentしてしまうのを何とかしたい気持ちに駆られそうになるが、
            //VolumeStackのGetComponentの中身はdictionaryのTryGetValueなので十分軽くあまり問題ない。
            BokehDof bokehDof = _volumeStack.GetComponent<BokehDof>();
            SmartDof smartDof = _volumeStack.GetComponent<SmartDof>();
            RadialBlur radialBlur = _volumeStack.GetComponent<RadialBlur>();
            MovieBasic movieBasic = _volumeStack.GetComponent<MovieBasic>();
            Mosaic mosaic = _volumeStack.GetComponent<Mosaic>();
            Posterization posterization = _volumeStack.GetComponent<Posterization>();
            Nega nega = _volumeStack.GetComponent<Nega>();
            AdvancedVignette advancedVignette = _volumeStack.GetComponent<AdvancedVignette>();
            Painting painting = _volumeStack.GetComponent<Painting>();
            Diffusion diffusion = _volumeStack.GetComponent<Diffusion>();
            TemporalBlur temporalBlur = _volumeStack.GetComponent<TemporalBlur>();

            PostProcessParams parameters = new()
            {
                volumeStack = _volumeStack,
                cmd = cmd,
                source = source,
                destination = _destination,
                descriptor = _descriptor,
            };

            if (bokehDof.IsActive())
            {
                using (new ProfilingScope(cmd, _bokehDofSampler))
                {
                    if (_bokehDofPass.Execute(ref parameters)) Swap(ref source, ref _destination);
                }
            }

            if (smartDof.IsActive())
            {
                using (new ProfilingScope(cmd, _smartDofSampler))
                {
                    parameters.source = source;
                    parameters.destination = _destination;
                    if (_smartDofPass.Execute(ref parameters)) Swap(ref source, ref _destination);
                }
            }

            if (radialBlur.IsActive())
            {
                using (new ProfilingScope(cmd, _radialBlurSampler))
                {
                    parameters.source = source;
                    parameters.destination = _destination;
                    if (_radialBlurPass.Execute(ref parameters)) Swap(ref source, ref _destination);
                }
            }

            if (movieBasic.IsActive())
            {
                using (new ProfilingScope(cmd, _movieBasicSampler))
                {
                    parameters.source = source;
                    parameters.destination = _destination;
                    if (_movieBasicPass.Execute(ref parameters)) Swap(ref source, ref _destination);
                }
            }

            if (mosaic.IsActive() || posterization.IsActive() || nega.IsActive() || advancedVignette.IsActive())
            {
                using (new ProfilingScope(cmd, _uberSampler))
                {
                    parameters.source = source;
                    parameters.destination = _destination;
                    if (_uberPass.Execute(ref parameters)) Swap(ref source, ref _destination);
                }
            }

            if (painting.IsActive())
            {
                using (new ProfilingScope(cmd, _paintingSampler))
                {
                    parameters.source = source;
                    parameters.destination = _destination;
                    if (_paintingPass.Execute(ref parameters)) Swap(ref source, ref _destination);
                }
            }

            if (diffusion.IsActive())
            {
                using (new ProfilingScope(cmd, _diffusionSampler))
                {
                    parameters.source = source;
                    parameters.destination = _destination;
                    if (_diffusionPass.Execute(ref parameters)) Swap(ref source, ref _destination);
                }
            }

            if (temporalBlur.IsActive())
            {
                using (new ProfilingScope(cmd, _temporalBlurSampler))
                {
                    parameters.source = source;
                    parameters.destination = _destination;
                    if (_temporalBlurPass.Execute(ref parameters)) Swap(ref source, ref _destination);
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
            public readonly Material bokehDof;
            public readonly Material smartDof;
            public readonly Material radialBlur;
            public readonly Material movieBasic;
            public readonly Material uber;
            public readonly Material painting;
            public readonly Material diffusion;
            public readonly Material temporalBlur;

            public MaterialLibrary(CustomPostProcessData.CustomPostProcessResources resources)
            {
                Assert.IsNotNull(resources);
                bokehDof = Load(resources.bokehDof);
                smartDof = Load(resources.smartDof);
                radialBlur = Load(resources.radialBlur);
                movieBasic = Load(resources.movieBasic);
                uber = Load(resources.uber);
                painting = Load(resources.painting);
                diffusion = Load(resources.diffusion);
                temporalBlur = Load(resources.temporalBlur);
            }

            private Material Load(Shader shader)
            {
                //AssertはUnityの処理が止まらなくなり操作不能に陥るため不可。
                if (!shader || !shader.isSupported)
                {
                    Debug.LogError("shader is null or not supported");
                    //return null;
                }
                return CoreUtils.CreateEngineMaterial(shader);
            }

            public void Dispose()
            {
                CoreUtils.Destroy(bokehDof);
                CoreUtils.Destroy(smartDof);
                CoreUtils.Destroy(radialBlur);
                CoreUtils.Destroy(movieBasic);
                CoreUtils.Destroy(uber);
                CoreUtils.Destroy(painting);
                CoreUtils.Destroy(diffusion);
                CoreUtils.Destroy(temporalBlur);
            }
        }
    }
}
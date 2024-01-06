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
        private RTHandle _destination;

        private BokehDofPass _bokehDofPass;
        private SmartDofPass _smartDofPass;
        private RadialBlurPass _radialBlurPass;
        private MovieBasicPass _movieBasicPass;
        private UberPass _uberPass;
        private MaterialLibrary _materialLibrary;

        private CustomPostProcessData _data;

        private static ProfilingSampler _bokehDofSampler = new ProfilingSampler(nameof(BokehDof));
        private static ProfilingSampler _smartDofSampler = new ProfilingSampler(nameof(SmartDof));
        private static ProfilingSampler _radialBlurSampler = new ProfilingSampler(nameof(RadialBlur));
        private static ProfilingSampler _movieBasicSampler = new ProfilingSampler(nameof(MovieBasic));
        private static ProfilingSampler _uberSampler = new ProfilingSampler("UberEffect");

        private bool destinationIsCameraColor = false;

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

            VolumeStack stack = VolumeManager.instance.stack;
            //こことそれぞれのExecute内で計2回GetComponentしてしまうのを何とかしたい気持ちに駆られそうになるが、
            //VolumeStackのGetComponentの中身はdictionaryのTryGetValueなので十分軽くあまり問題ない。
            BokehDof bokehDof = stack.GetComponent<BokehDof>();
            SmartDof smartDof = stack.GetComponent<SmartDof>();
            RadialBlur radialBlur = stack.GetComponent<RadialBlur>();
            AdvancedVignette advancedVignette = stack.GetComponent<AdvancedVignette>();
            MovieBasic movieBasic = stack.GetComponent<MovieBasic>();

            if (bokehDof.IsActive())
            {
                using (new ProfilingScope(cmd, _bokehDofSampler))
                {
                    PostProcessParams parameters = new()
                    {
                        cmd = cmd,
                        source = source,
                        destination = _destination,
                        descriptor = _descriptor,
                    };
                    if (_bokehDofPass.Execute(ref parameters)) Swap(ref source, ref _destination);
                }
            }

            if (smartDof.IsActive())
            {
                using (new ProfilingScope(cmd, _smartDofSampler))
                {
                    PostProcessParams parameters = new()
                    {
                        volumeStack = stack,
                        cmd = cmd,
                        source = source,
                        destination = _destination,
                        descriptor = _descriptor,
                    };
                    if (_smartDofPass.Execute(ref parameters)) Swap(ref source, ref _destination);
                }
            }


            if (radialBlur.IsActive())
            {
                using (new ProfilingScope(cmd, _radialBlurSampler))
                {
                    PostProcessParams parameters = new()
                    {
                        volumeStack = stack,
                        cmd = cmd,
                        source = source,
                        destination = _destination,
                    };
                    if (_radialBlurPass.Execute(ref parameters)) Swap(ref source, ref _destination);
                }
            }

            if (movieBasic.IsActive())
            {
                using (new ProfilingScope(cmd, _movieBasicSampler))
                {
                    PostProcessParams parameters = new()
                    {
                        volumeStack = stack,
                        cmd = cmd,
                        source = source,
                        destination = _destination,
                        descriptor = _descriptor,
                    };
                    if (_movieBasicPass.Execute(ref parameters)) Swap(ref source, ref _destination);
                }
            }

            if (advancedVignette.IsActive())
            {
                using (new ProfilingScope(cmd, _uberSampler))
                {
                    PostProcessParams parameters = new()
                    {
                        volumeStack = stack,
                        cmd = cmd,
                        source = source,
                        destination = _destination,
                    };
                    if (_uberPass.Execute(ref parameters)) Swap(ref source, ref _destination);
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

            public MaterialLibrary(CustomPostProcessData.CustomPostProcessResources resources)
            {
                Assert.IsNotNull(resources);
                bokehDof = Load(resources.bokehDof);
                smartDof = Load(resources.smartDof);
                radialBlur = Load(resources.radialBlur);
                movieBasic = Load(resources.movieBasic);
                uber = Load(resources.uber);
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
            }
        }
    }
}
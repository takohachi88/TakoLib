using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace TakoLib.Urp.PostProcess
{
    public class SmartDofPass : CustomPostProcessPassBase<SmartDof>
    {
        private RTHandle _fullCoCTexture;

        private Vector4[] _bokehKernel;
        private int _bokehHash;
        private float _bokehMaxRadius;
        private float _bokehRcpAspect;

        public SmartDofPass(CustomPostProcessData data, Material material) : base(data, material) { }

        public override void Execute(ref PostProcessParams<SmartDof> parameters)
        {
            SetCommonParams(ref parameters);
            DoSmartDof(parameters.cmd, parameters.volumeComponent, parameters.source, parameters.destination);
        }


        private void DoSmartDof(CommandBuffer cmd, SmartDof smartDof, RTHandle source, RTHandle destination)
        {
            int downSample = 2;
            Material material = _material;
            int wh = _descriptor.width / downSample;
            int hh = _descriptor.height / downSample;

            float F = smartDof.focalLength.value / 1000f;
            float A = smartDof.focalLength.value / smartDof.aperture.value;
            float P = smartDof.focusDistance.value;
            float maxCoC = (A * F) / (P - F);
            const float radiusInPixels = 14;
            float maxRadius = Mathf.Min(0.05f, radiusInPixels / _descriptor.height);
            float rcpAspect = 1f / (wh / (float)hh);

            CoreUtils.SetKeyword(material, TakoLibUrpCommon.Keyword.UseFastSrgbLinearConversion, true);
            //フォーカス距離、最大CoC、画面サイズに合わせた最大CoC半径、画面のアスペクト比
            cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.CoCParams, new Vector4(P, maxCoC, maxRadius, rcpAspect));

            int hash = smartDof.GetHashCode();
            if (hash != _bokehHash || maxRadius != _bokehMaxRadius || rcpAspect != _bokehRcpAspect)
            {
                _bokehHash = hash;
                _bokehMaxRadius = maxRadius;
                _bokehRcpAspect = rcpAspect;
                PrepareBokehKernel(maxRadius, rcpAspect, smartDof.bladeCount.value, smartDof.bladeCurvature.value, smartDof.bladeRotation.value);
            }

            cmd.SetGlobalVectorArray(TakoLibUrpCommon.ShaderId.BokehKernel, _bokehKernel);

            RenderTextureDescriptor cocDescriptor = TakoLibUrpCommon.PostProcessDescriptor(_descriptor.width, _descriptor.height, GraphicsFormat.R8_UNorm);
            RenderTextureDescriptor tempDescriptor = TakoLibUrpCommon.PostProcessDescriptor(wh, hh, GraphicsFormat.R16G16B16A16_SFloat);

            RenderingUtils.ReAllocateIfNeeded(ref _fullCoCTexture, cocDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp);
            RenderingUtils.ReAllocateIfNeeded(ref _tempTarget1, tempDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp);
            RenderingUtils.ReAllocateIfNeeded(ref _tempTarget2, tempDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp);

            TakoLibUrpCommon.SetSourceSize(cmd, _descriptor);
            cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.DownSampleScaleFactor, new Vector4(1f / downSample, 1f / downSample, downSample, downSample));
            float uvMargin = (1f / _descriptor.height) * downSample;
            cmd.SetGlobalVector(TakoLibUrpCommon.ShaderId.BokehConstants, new Vector4(uvMargin, uvMargin * 2f));

            //CoC
            Blitter.BlitCameraTexture(cmd, source, _fullCoCTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, 0);
            cmd.SetGlobalTexture(TakoLibUrpCommon.ShaderId.FullCoCTexture, _fullCoCTexture);

            //Downscale, prefilter color + coc
            Blitter.BlitCameraTexture(cmd, source, _tempTarget1, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, 1);

            //bokeh blur
            Blitter.BlitCameraTexture(cmd, _tempTarget1, _tempTarget2, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, 2);

            //post-filtering
            Blitter.BlitCameraTexture(cmd, _tempTarget2, _tempTarget1, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, 3);

            //composite
            cmd.SetGlobalTexture(TakoLibUrpCommon.ShaderId.DofTexture, _tempTarget1);
            Blitter.BlitCameraTexture(cmd, source, destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, 4);
        }


        private void PrepareBokehKernel(float maxRadius, float rcpAspect, int bladeCount, float bladeCurvature, float bladeRotation)
        {
            const int Rings = 4;
            const int PointsPerRing = 7;

            // Check the existing array
            if (_bokehKernel == null) _bokehKernel = new Vector4[42];

            // Fill in sample points (concentric circles transformed to rotated N-Gon)
            int idx = 0;
            float curvature = 1f - bladeCurvature;
            float rotation = bladeRotation * Mathf.Deg2Rad;
            const float PI = Mathf.PI;
            const float TWO_PI = Mathf.PI * 2f;

            for (int ring = 1; ring < Rings; ring++)
            {
                float bias = 1f / PointsPerRing;
                float radius = (ring + bias) / (Rings - 1f + bias);
                int points = ring * PointsPerRing;

                for (int point = 0; point < points; point++)
                {
                    // Angle on ring
                    float phi = 2f * PI * point / points;

                    // Transform to rotated N-Gon
                    // Adapted from "CryEngine 3 Graphics Gems" [Sousa13]
                    float nt = Mathf.Cos(PI / bladeCount);
                    float dt = Mathf.Cos(phi - (TWO_PI / bladeCount) * Mathf.Floor((bladeCount * phi + Mathf.PI) / TWO_PI));
                    float r = radius * Mathf.Pow(nt / dt, curvature);
                    float u = r * Mathf.Cos(phi - rotation);
                    float v = r * Mathf.Sin(phi - rotation);

                    float uRadius = u * maxRadius;
                    float vRadius = v * maxRadius;
                    float uRadiusPowTwo = uRadius * uRadius;
                    float vRadiusPowTwo = vRadius * vRadius;
                    float kernelLength = Mathf.Sqrt(uRadiusPowTwo + vRadiusPowTwo);
                    float uRCP = uRadius * rcpAspect;

                    _bokehKernel[idx] = new Vector4(uRadius, vRadius, kernelLength, uRCP);
                    idx++;
                }
            }
        }
    }
}

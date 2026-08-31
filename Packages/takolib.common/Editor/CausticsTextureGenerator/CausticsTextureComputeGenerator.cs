#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace TakoLibEditor.Common
{
    /// <summary>
    /// ComputeShaderを使用してコースティクスの1フレームを生成する。
    /// </summary>
    internal sealed class CausticsTextureComputeGenerator : IDisposable
    {
        public const string ComputeShaderPath = "Packages/takolib.common/Editor/CausticsTextureGenerator/CausticsTextureGenerator.compute";

        private const int WaveStride = sizeof(float) * 8;
        private const float FixedPointScale = 65536f;

        private readonly ComputeShader _shader;
        private readonly CausticsTextureSettings _settings;
        private readonly int _sampleWidth;
        private readonly int _sampleHeight;
        private readonly int _pixelCount;
        private readonly int _clearKernel;
        private readonly int _accumulateKernel;
        private readonly int _resolveKernel;
        private readonly int _blurHorizontalKernel;
        private readonly int _blurVerticalKernel;
        private readonly int _toneMapKernel;
        private readonly ComputeBuffer _waves;
        private readonly ComputeBuffer _accumulation;
        private readonly ComputeBuffer _densityA;
        private readonly ComputeBuffer _densityB;
        private readonly RenderTexture _output;
        private bool _disposed;

        public CausticsTextureComputeGenerator(ComputeShader source, CausticsTextureSettings settings)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            _settings = settings?.Copy() ?? throw new ArgumentNullException(nameof(settings));
            _shader = UnityEngine.Object.Instantiate(source);
            _sampleWidth = _settings.Width * _settings.Supersampling;
            _sampleHeight = _settings.Height * _settings.Supersampling;
            _pixelCount = _settings.Width * _settings.Height;
            try
            {
                _clearKernel = _shader.FindKernel("ClearAccumulation");
                _accumulateKernel = _shader.FindKernel("AccumulateCaustics");
                _resolveKernel = _shader.FindKernel("ResolveDensity");
                _blurHorizontalKernel = _shader.FindKernel("BlurHorizontal");
                _blurVerticalKernel = _shader.FindKernel("BlurVertical");
                _toneMapKernel = _shader.FindKernel("ToneMap");
                CausticsTextureGenerator.GpuWave[] waveData = CausticsTextureGenerator.CreateGpuWaves(_settings);
                _waves = new ComputeBuffer(waveData.Length, WaveStride);
                _waves.SetData(waveData);
                _accumulation = new ComputeBuffer(_pixelCount * 3, sizeof(uint));
                _densityA = new ComputeBuffer(_pixelCount, sizeof(float) * 4);
                _densityB = new ComputeBuffer(_pixelCount, sizeof(float) * 4);
                _output = new RenderTexture(_settings.Width, _settings.Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear) { enableRandomWrite = true, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
                _output.Create();
                SetConstantParameters(waveData.Length);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public Color32[] GenerateFrame(int frameIndex)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CausticsTextureComputeGenerator));
            if (frameIndex < 0 || frameIndex >= _settings.FrameCount)
                throw new ArgumentOutOfRangeException(nameof(frameIndex));

            float time = (float)frameIndex / _settings.FrameCount;
            float loopAngle = Mathf.PI * 2f * time;
            _shader.SetVector("_LoopPosition", new Vector4(Mathf.Cos(loopAngle), Mathf.Sin(loopAngle), 0f, 0f));
            DispatchClear();
            DispatchAccumulate();
            DispatchResolve();
            if (_settings.BlurRadius > 0)
                DispatchBlur();
            DispatchToneMap();
            return ReadOutput();
        }

        private void SetConstantParameters(int waveCount)
        {
            _shader.SetInt("_Width", _settings.Width);
            _shader.SetInt("_Height", _settings.Height);
            _shader.SetInt("_SampleWidth", _sampleWidth);
            _shader.SetInt("_SampleHeight", _sampleHeight);
            _shader.SetInt("_PixelCount", _pixelCount);
            _shader.SetInt("_WaveCount", waveCount);
            _shader.SetInt("_BlurRadius", _settings.BlurRadius);
            _shader.SetInt("_AlphaFromIntensity", _settings.AlphaFromIntensity ? 1 : 0);
            _shader.SetFloat("_SampleEnergy", 1f / (_settings.Supersampling * _settings.Supersampling));
            _shader.SetFloat("_FixedPointScale", FixedPointScale);
            _shader.SetFloat("_AnimationPhaseScale", Mathf.PI * 2f * _settings.AnimationSpeed);
            _shader.SetFloat("_RefractionStrength", _settings.RefractionStrength);
            _shader.SetFloat("_ChromaticAberration", _settings.ChromaticAberration);
            _shader.SetFloat("_BlackPoint", _settings.BlackPoint);
            _shader.SetFloat("_Exposure", _settings.Exposure);
            _shader.SetFloat("_Contrast", _settings.Contrast);
            _shader.SetVector("_Tint", _settings.Tint);
            _shader.SetBuffer(_accumulateKernel, "_Waves", _waves);
            _shader.SetBuffer(_accumulateKernel, "_Accumulation", _accumulation);
        }

        private void DispatchClear()
        {
            _shader.SetBuffer(_clearKernel, "_Accumulation", _accumulation);
            _shader.Dispatch(_clearKernel, Mathf.CeilToInt(_pixelCount * 3f / 64f), 1, 1);
        }

        private void DispatchAccumulate()
        {
            _shader.Dispatch(_accumulateKernel, Mathf.CeilToInt(_sampleWidth / 8f), Mathf.CeilToInt(_sampleHeight / 8f), 1);
        }

        private void DispatchResolve()
        {
            _shader.SetBuffer(_resolveKernel, "_Accumulation", _accumulation);
            _shader.SetBuffer(_resolveKernel, "_DensityOutput", _densityA);
            _shader.Dispatch(_resolveKernel, Mathf.CeilToInt(_settings.Width / 8f), Mathf.CeilToInt(_settings.Height / 8f), 1);
        }

        private void DispatchBlur()
        {
            _shader.SetBuffer(_blurHorizontalKernel, "_DensityInput", _densityA);
            _shader.SetBuffer(_blurHorizontalKernel, "_DensityOutput", _densityB);
            _shader.Dispatch(_blurHorizontalKernel, Mathf.CeilToInt(_settings.Width / 8f), Mathf.CeilToInt(_settings.Height / 8f), 1);
            _shader.SetBuffer(_blurVerticalKernel, "_DensityInput", _densityB);
            _shader.SetBuffer(_blurVerticalKernel, "_DensityOutput", _densityA);
            _shader.Dispatch(_blurVerticalKernel, Mathf.CeilToInt(_settings.Width / 8f), Mathf.CeilToInt(_settings.Height / 8f), 1);
        }

        private void DispatchToneMap()
        {
            _shader.SetBuffer(_toneMapKernel, "_DensityInput", _densityA);
            _shader.SetTexture(_toneMapKernel, "_Output", _output);
            _shader.Dispatch(_toneMapKernel, Mathf.CeilToInt(_settings.Width / 8f), Mathf.CeilToInt(_settings.Height / 8f), 1);
        }

        private Color32[] ReadOutput()
        {
            AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(_output, 0, TextureFormat.RGBA32);
            request.WaitForCompletion();
            if (request.hasError)
                throw new InvalidOperationException("Failed to read the generated caustics texture from the GPU.");
            Texture2D readback = new(_settings.Width, _settings.Height, TextureFormat.RGBA32, false, true);
            try
            {
                readback.LoadRawTextureData(request.GetData<byte>());
                readback.Apply(false, false);
                return readback.GetPixels32();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _waves?.Dispose();
            _accumulation?.Dispose();
            _densityA?.Dispose();
            _densityB?.Dispose();
            if (_output != null)
            {
                _output.Release();
                UnityEngine.Object.DestroyImmediate(_output);
            }
            if (_shader != null)
                UnityEngine.Object.DestroyImmediate(_shader);
        }
    }
}

#endif

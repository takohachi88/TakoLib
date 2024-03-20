using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TakoLib.Urp.PostProcess
{
    public class TemporalBlurPass : CustomPostProcessPassBase
    {
        private RTHandle _previousFrame;
        private float _intensity;
        private float _time;

        public TemporalBlurPass(CustomPostProcessData data, Material material) : base(data, material)
        {
        }

        public override bool Execute(ref PostProcessParams parameters)
        {
            SetCommonParams(ref parameters);
            return DoTemporalBlur();
        }
        private bool DoTemporalBlur()
        {
            TemporalBlur temporalBlur = _volumeStack.GetComponent<TemporalBlur>();

            RenderingUtils.ReAllocateIfNeeded(ref _previousFrame, _descriptor);

            //初期化。
            if (_previousFrame?.rt == null)
            {
                _intensity = temporalBlur.intensity.value;
                Blitter.BlitCameraTexture(_cmd, _source, _previousFrame);
                return false;
            }

            bool timeMode = temporalBlur.intervalMode == TemporalBlurIntervalMode.Time;
            bool frameCountMode = temporalBlur.intervalMode == TemporalBlurIntervalMode.FrameCount;

            //intensityを少しずつ減らしてフェードアウト。
            _intensity = Mathf.Clamp01(_intensity - temporalBlur.fadeOut.value * 0.05f * (timeMode ? Time.deltaTime * 100f : 1));
            _cmd.SetGlobalFloat(TakoLibUrpCommon.ShaderId.Intensity, _intensity);
            Blitter.BlitCameraTexture(_cmd, _source, _destination, _material, 0);

            _time += Time.deltaTime;

            if (frameCountMode && Time.frameCount % temporalBlur.intervalFrameCount.value == 0)
            {
                _intensity = temporalBlur.intensity.value;
                Blitter.BlitCameraTexture(_cmd, _destination, _previousFrame);
                _cmd.SetGlobalTexture(TakoLibUrpCommon.ShaderId.PreviousBlitTexture, _previousFrame);
            }
            else if (timeMode && temporalBlur.intervalTime.value <= _time)
            {
                _intensity = temporalBlur.intensity.value;
                Blitter.BlitCameraTexture(_cmd, _destination, _previousFrame);
                _cmd.SetGlobalTexture(TakoLibUrpCommon.ShaderId.PreviousBlitTexture, _previousFrame);
                _time = 0;
            }

            return true;
        }
    }
}

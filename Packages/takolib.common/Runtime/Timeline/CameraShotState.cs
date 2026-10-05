using UnityEngine;

namespace TakoLib.Common.Timeline
{
    public struct CameraShotState
    {
        public CameraKeyChannels channels;
        public Vector3 position;
        public Quaternion rotation;
        public float fieldOfView;
        public Vector2 lensShift;
        public Vector3 shakePosition;
        public Vector3 shakeRotation;
    }

    public sealed class CameraShotBlend
    {
        private CameraShotState _state;
        private float _positionWeight;
        private float _rotationWeight;
        private float _fovWeight;
        private float _shiftWeight;
        private float _shakeWeight;

        public void Clear()
        {
            _state = new CameraShotState { rotation = Quaternion.identity };
            _positionWeight = _rotationWeight = _fovWeight = _shiftWeight = _shakeWeight = 0;
        }

        public void Add(CameraShotState state, float weight)
        {
            if (weight <= 0) return;
            if ((state.channels & CameraKeyChannels.Position) != 0)
            {
                _positionWeight += weight;
                _state.position = Vector3.Lerp(_state.position, state.position, weight / _positionWeight);
            }
            if ((state.channels & CameraKeyChannels.Rotation) != 0)
            {
                _rotationWeight += weight;
                _state.rotation = Quaternion.Slerp(_state.rotation, state.rotation, weight / _rotationWeight);
            }
            if ((state.channels & CameraKeyChannels.FieldOfView) != 0)
            {
                _fovWeight += weight;
                _state.fieldOfView = Mathf.Lerp(_state.fieldOfView, state.fieldOfView, weight / _fovWeight);
            }
            if ((state.channels & CameraKeyChannels.LensShift) != 0)
            {
                _shiftWeight += weight;
                _state.lensShift = Vector2.Lerp(_state.lensShift, state.lensShift, weight / _shiftWeight);
            }
            if ((state.channels & CameraKeyChannels.Shake) != 0)
            {
                _shakeWeight += weight;
                _state.shakePosition += state.shakePosition * weight;
                _state.shakeRotation += state.shakeRotation * weight;
            }
            _state.channels |= state.channels;
        }

        public CameraShotState Resolve(CameraShotState baseline)
        {
            var result = new CameraShotState
            {
                channels = _state.channels,
                position = Vector3.Lerp(baseline.position, _state.position, Mathf.Clamp01(_positionWeight)),
                rotation = Quaternion.Slerp(baseline.rotation, _state.rotation, Mathf.Clamp01(_rotationWeight)),
                fieldOfView = Mathf.Lerp(baseline.fieldOfView, _state.fieldOfView, Mathf.Clamp01(_fovWeight)),
                lensShift = Vector2.Lerp(baseline.lensShift, _state.lensShift, Mathf.Clamp01(_shiftWeight)),
                shakePosition = _state.shakePosition / Mathf.Max(1, _shakeWeight),
                shakeRotation = _state.shakeRotation / Mathf.Max(1, _shakeWeight)
            };
            result.position += result.rotation * result.shakePosition;
            result.rotation *= Quaternion.Euler(result.shakeRotation);
            return result;
        }
    }
}

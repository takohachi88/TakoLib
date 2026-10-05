using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Splines;
using UnityEngine.Timeline;

namespace TakoLib.Common.Timeline
{
    [TrackColor(0.2f, 0.65f, 0.8f)]
    [TrackClipType(typeof(CameraShotClip))]
    [TrackBindingType(typeof(Camera))]
    public sealed class CameraShotTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<CameraShotMixer>.Create(graph, inputCount);
            mixer.GetBehaviour().Initialize(graph.GetResolver() as PlayableDirector);
            return mixer;
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            var camera = director.GetGenericBinding(this) as Camera;
            if (camera == null) return;
            driver.AddFromName<Transform>(camera.gameObject, "m_LocalPosition.x");
            driver.AddFromName<Transform>(camera.gameObject, "m_LocalPosition.y");
            driver.AddFromName<Transform>(camera.gameObject, "m_LocalPosition.z");
            driver.AddFromName<Transform>(camera.gameObject, "m_LocalRotation.x");
            driver.AddFromName<Transform>(camera.gameObject, "m_LocalRotation.y");
            driver.AddFromName<Transform>(camera.gameObject, "m_LocalRotation.z");
            driver.AddFromName<Transform>(camera.gameObject, "m_LocalRotation.w");
            driver.AddFromName(camera, "field of view");
            driver.AddFromName(camera, "m_FocalLength");
            driver.AddFromName(camera, "m_LensShift.x");
            driver.AddFromName(camera, "m_LensShift.y");
            driver.AddFromName(camera, "m_projectionMatrixMode");
        }
    }

    public sealed class CameraShotBehaviour : PlayableBehaviour
    {
        public CameraShotClip clip;
        public SplineContainer path;
        private CameraShotSampler _sampler;

        public void Initialize(CameraShotClip shot, SplineContainer spline)
        {
            clip = shot;
            path = spline;
            _sampler?.Dispose();
            _sampler = new CameraShotSampler();
            // 初期化時にキャッシュを作り、最初の再生フレームでの確保を避ける。
            _sampler.TryEvaluateState(clip, path, 0, out _);
        }

        public bool Evaluate(double time, out CameraShotState state)
        {
            state = default;
            if (_sampler == null) return false;
            return _sampler.TryEvaluateState(clip, path, time, out state);
        }
        public override void OnPlayableDestroy(Playable playable)
        {
            _sampler?.Dispose();
            _sampler = null;
        }
    }

    public sealed class CameraShotMixer : PlayableBehaviour
    {
        private Camera _camera;
        private Vector3 _position;
        private Quaternion _rotation;
        private PlayableDirector _director;
        private float _fieldOfView;
        private float _focalLength;
        private Vector2 _lensShift;
        private bool _physicalCamera;
        private readonly CameraShotBlend _blend = new CameraShotBlend();

        public void Initialize(PlayableDirector director)
        {
            _director = director;
            if (_director != null) _director.stopped += OnDirectorStopped;
        }

        private void OnDirectorStopped(PlayableDirector director) => Restore();

        private void Restore()
        {
            if (_camera == null) return;
            _camera.transform.SetLocalPositionAndRotation(_position, _rotation);
            _camera.usePhysicalProperties = _physicalCamera;
            _camera.focalLength = _focalLength;
            _camera.fieldOfView = _fieldOfView;
            _camera.lensShift = _lensShift;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var camera = playerData as Camera;
            if (camera != _camera)
            {
                Restore();
                _camera = camera;
                if (camera != null)
                {
                    _position = camera.transform.localPosition;
                    _rotation = camera.transform.localRotation;
                    _fieldOfView = camera.fieldOfView;
                    _focalLength = camera.focalLength;
                    _lensShift = camera.lensShift;
                    _physicalCamera = camera.usePhysicalProperties;
                }
            }
            if (_camera == null) return;
            _blend.Clear();
            bool hasInput = false;
            for (int i = 0; i < playable.GetInputCount(); ++i)
            {
                if (playable.GetInputWeight(i) <= 0) continue;
                var input = (ScriptPlayable<CameraShotBehaviour>)playable.GetInput(i);
                if (input.GetBehaviour().Evaluate(input.GetTime(), out var state))
                {
                    _blend.Add(state, playable.GetInputWeight(i));
                    hasInput = true;
                }
            }
            Restore();
            if (!hasInput) return;
            var result = _blend.Resolve(new CameraShotState
            {
                position = _camera.transform.position, rotation = _camera.transform.rotation,
                fieldOfView = _fieldOfView, lensShift = _lensShift
            });
            _camera.transform.SetPositionAndRotation(result.position, result.rotation);
            if ((result.channels & CameraKeyChannels.LensShift) != 0)
            {
                _camera.usePhysicalProperties = true;
                _camera.lensShift = result.lensShift;
            }
            if ((result.channels & (CameraKeyChannels.FieldOfView | CameraKeyChannels.LensShift)) != 0)
            {
                _camera.fieldOfView = result.fieldOfView;
                if (_camera.usePhysicalProperties)
                    _camera.focalLength = Camera.FieldOfViewToFocalLength(result.fieldOfView, _camera.sensorSize.y);
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_director != null) _director.stopped -= OnDirectorStopped;
            Restore();
            _camera = null;
        }
    }
}

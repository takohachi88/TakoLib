using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace TakoLib.Common.Timeline.Editor
{
    [CustomEditor(typeof(CameraShotClip))]
    public sealed class CameraShotClipInspector : UnityEditor.Editor
    {
        private VisualElement _root;
        private VisualElement _details;
        private CameraKeyStrip _keyStrip;
        private Button _deleteKeyButton;
        private int _selectedIndex;
        private CameraPassageKey _selected;
        private bool _refreshQueued;
        private SplineContainer _displayedPath;
        private Button _addKeyButton;
        private CameraKeyChannels _newChannels = CameraKeyChannels.Position | CameraKeyChannels.Rotation;
        private CameraShotClip Shot => (CameraShotClip)target;
        private TimelineClip Context => TimelineEditor.selectedClip?.asset == target ? TimelineEditor.selectedClip : null;
        private SplineContainer Path => TimelineEditor.inspectedDirector == null ? null : Shot.path.Resolve(TimelineEditor.inspectedDirector);

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndo;
            SceneView.duringSceneGui += DrawScene;
            Spline.Changed += OnSplineChanged;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
            SceneView.duringSceneGui -= DrawScene;
            Spline.Changed -= OnSplineChanged;
        }

        private void OnUndo()
        {
            _selected = Shot.keys.Count == 0 ? null : Shot.keys[Mathf.Clamp(_selectedIndex, 0, Shot.keys.Count - 1)];
            Build();
            Refresh();
        }

        private void OnSplineChanged(Spline spline, int index, SplineModification modification)
        {
            if (Path == null || Path.Spline != spline || _refreshQueued) return;
            _refreshQueued = true;
            EditorApplication.delayCall += () =>
            {
                _refreshQueued = false;
                if (this == null || target == null) return;
                Build();
                Refresh();
            };
        }

        public override VisualElement CreateInspectorGUI()
        {
            _root = new VisualElement();
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/takolib.common/Editor/Timeline/CameraKeyEditor.uss");
            if (sheet != null) _root.styleSheets.Add(sheet);
            Build();
            _root.schedule.Execute(() =>
            {
                if (target == null) return;
                if (Path != _displayedPath) { Build(); Refresh(); }
                var clip = Context;
                var director = TimelineEditor.inspectedDirector;
                _addKeyButton?.SetEnabled(clip != null && director != null && director.time >= clip.start && director.time <= clip.end);
            }).Every(200);
            return _root;
        }

        private void Build()
        {
            if (_root == null || target == null) return;
            _root.Clear();
            _addKeyButton = null;
            _displayedPath = Path;
            serializedObject.Update();
            var mode = new EnumField("位置の指定方法", Shot.positionMode);
            mode.RegisterValueChangedCallback(evt =>
            {
                Change("Change camera position mode", () =>
                {
                    if ((CameraPositionMode)evt.newValue == CameraPositionMode.Direct && Path != null)
                        foreach (var key in Shot.keys)
                        {
                            int index = CameraKnotIdentity.Find(Path.Spline, key.knotId);
                            if (index >= 0) key.position = Path.transform.TransformPoint(Path.Spline[index].Position);
                        }
                    Shot.positionMode = (CameraPositionMode)evt.newValue;
                });
                _root.schedule.Execute(Build);
            });
            _root.Add(mode);
            if (Shot.positionMode == CameraPositionMode.Spline)
            {
                var pathField = new PropertyField(serializedObject.FindProperty("path"), "Spline Container");
                _root.Add(pathField);
                pathField.Bind(serializedObject);
                pathField.RegisterValueChangeCallback(_ => _root.schedule.Execute(() => { Build(); Refresh(); }));
            }
            _root.Add(new HelpBox("時間はClip内の秒、位置・回転はワールド座標です。キーごとに記録する項目を選べます。Lens Shiftはプレビュー中にPhysical Cameraを有効にします。", HelpBoxMessageType.Info));
            if (Context == null || TimelineEditor.inspectedDirector == null)
            {
                _root.Add(new HelpBox("TimelineでCamera Shot Clipを選択してください。", HelpBoxMessageType.Warning));
                return;
            }
            var path = Path;
            if (Shot.positionMode == CameraPositionMode.Spline && (path == null || path.Splines.Count != 1 || path.Spline.Closed || path.Spline.Count < 2))
            {
                _root.Add(new HelpBox("2個以上のKnotを持つ、開いたSplineを1本含むSpline Containerを指定してください。", HelpBoxMessageType.Warning));
            }
            var camera = TimelineEditor.inspectedDirector.GetGenericBinding(Context.GetParentTrack()) as Camera;
            if (camera == null) _root.Add(new HelpBox("Trackに出力用Cameraを割り当ててください。", HelpBoxMessageType.Warning));
            var channels = new EnumFlagsField("追加するキーの項目", _newChannels);
            channels.RegisterValueChangedCallback(evt => _newChannels = (CameraKeyChannels)evt.newValue & CameraKeyChannels.All);
            _root.Add(channels);
            var shakeOptions = new Foldout { text = "手振れの共通設定", value = false };
            var seed = new IntegerField("Seed") { value = Shot.shakeSeed };
            seed.RegisterValueChangedCallback(evt => Change("Change shake seed", () => Shot.shakeSeed = evt.newValue));
            shakeOptions.Add(seed);
            var phase = new DoubleField("Phase Offset") { value = Shot.shakePhaseOffset, isDelayed = true, tooltip = "手振れパターンの開始位置。通常は0で構いません。強さや速さは変わりません。" };
            phase.RegisterValueChangedCallback(evt =>
            {
                if (double.IsNaN(evt.newValue) || double.IsInfinity(evt.newValue)) { phase.SetValueWithoutNotify(Shot.shakePhaseOffset); return; }
                Change("Change shake phase", () => Shot.shakePhaseOffset = evt.newValue);
            });
            shakeOptions.Add(phase);
            var mute = new Toggle("手振れを一時的に無効化") { value = Shot.muteShake };
            mute.RegisterValueChangedCallback(evt => Change("Mute camera shake", () => Shot.muteShake = evt.newValue));
            shakeOptions.Add(mute);
            _root.Add(shakeOptions);
            if (_selected == null || !Shot.keys.Contains(_selected))
                _selected = Shot.keys.FirstOrDefault();
            _keyStrip = new CameraKeyStrip(Shot, Context, SelectKey, Refresh, () => _root.schedule.Execute(Build));
            _root.Add(_keyStrip);
            var toolbar = new VisualElement();
            toolbar.AddToClassList("camera-key-toolbar");
            toolbar.Add(new Button(() => SelectAdjacent(-1)) { text = "前のキー", name = "previousKey" });
            toolbar.Add(new Button(() => SelectAdjacent(1)) { text = "次のキー", name = "nextKey" });
            var spacer = new VisualElement();
            spacer.AddToClassList("camera-key-spacer");
            toolbar.Add(spacer);
            _addKeyButton = new Button(AddKey) { text = "+", name = "addKey", tooltip = "再生ヘッドの時刻に追加。同時刻にキーがある場合は選択した項目を記録。" };
            _addKeyButton.AddToClassList("camera-key-action");
            toolbar.Add(_addKeyButton);
            _deleteKeyButton = new Button(DeleteSelectedKey) { text = "-", name = "deleteKey", tooltip = "選択中のキーを削除（Undo可能）" };
            _deleteKeyButton.AddToClassList("camera-key-action");
            toolbar.Add(_deleteKeyButton);
            _root.Add(toolbar);
            _details = new VisualElement();
            _root.Add(_details);
            BuildSelectedKey();
        }

        private void BuildSelectedKey()
        {
            if (_details == null) return;
            _details.Clear();
            _keyStrip?.SetSelected(_selected);
            _deleteKeyButton?.SetEnabled(_selected != null);
            _selectedIndex = _selected == null ? 0 : Shot.keys.IndexOf(_selected);
            _root.Q<Button>("previousKey")?.SetEnabled(_selectedIndex > 0);
            _root.Q<Button>("nextKey")?.SetEnabled(_selected != null && _selectedIndex < Shot.keys.Count - 1);
            if (_selected == null)
            {
                _details.Add(new Label("+でキーを追加してください。"));
                return;
            }
            var path = Path;
            var key = _selected;
            if (key != null)
            {
                int index = path == null ? -1 : CameraKnotIdentity.Find(path.Spline, key.knotId);
                bool pathPosition = Shot.positionMode == CameraPositionMode.Spline && (key.channels & CameraKeyChannels.Position) != 0;
                var foldout = new VisualElement { name = "selectedKeyDetails" };
                var title = new Label($"Key {Shot.keys.IndexOf(key) + 1} / {Shot.keys.Count}  |  {key.time:0.###} s" + (pathPosition ? $"  |  Knot {index}" : ""));
                title.AddToClassList("camera-key-heading");
                foldout.Add(title);
                _details.Add(foldout);
                var time = new DoubleField("Clip内の時刻 (秒)") { name = "keyTime", value = key.time, isDelayed = true };
                time.RegisterValueChangedCallback(evt =>
                {
                    if (double.IsNaN(evt.newValue) || double.IsInfinity(evt.newValue) || evt.newValue < 0 ||
                        Shot.keys.Any(k => k != key && Math.Abs(k.time - evt.newValue) < 0.000001))
                    { time.SetValueWithoutNotify(key.time); return; }
                    Change("Change camera key time", () => { key.time = evt.newValue; Shot.keys.Sort((a, b) => a.time.CompareTo(b.time)); });
                    _root.schedule.Execute(Build);
                });
                foldout.Add(time);
                var keyChannels = new EnumFlagsField("記録する項目", key.channels);
                keyChannels.RegisterValueChangedCallback(evt => { Change("Change key channels", () => key.channels = (CameraKeyChannels)evt.newValue & CameraKeyChannels.All); _root.schedule.Execute(Build); });
                foldout.Add(keyChannels);
                if (pathPosition && path != null)
                {
                var knot = new IntegerField("Knot番号 (0から)") { value = index, isDelayed = true };
                knot.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue < 0 || evt.newValue >= path.Spline.Count) { knot.SetValueWithoutNotify(index); return; }
                    Undo.RecordObject(path, "Assign camera passage knot");
                    Change("Assign camera passage knot", () => key.knotId = CameraKnotIdentity.GetOrCreate(path.Spline, evt.newValue));
                    MarkPath(path);
                    _root.schedule.Execute(Build);
                });
                foldout.Add(knot);
                }
                else if ((key.channels & CameraKeyChannels.Position) != 0)
                {
                    var position = new Vector3Field("位置") { value = key.position };
                    position.RegisterValueChangedCallback(evt => Change("Move camera key", () => key.position = evt.newValue));
                    foldout.Add(position);
                }
                if ((key.channels & CameraKeyChannels.Rotation) != 0)
                {
                var rotation = new Vector3Field("カメラの回転 (度)") { value = key.eulerAngles };
                rotation.RegisterValueChangedCallback(evt => Change("Rotate camera key", () => key.eulerAngles = evt.newValue));
                foldout.Add(rotation);
                }
                if ((key.channels & CameraKeyChannels.FieldOfView) != 0)
                    AddFloat(foldout, "FoV (度)", key.fieldOfView, value => key.fieldOfView = value, 1, 179);
                if ((key.channels & CameraKeyChannels.LensShift) != 0)
                {
                    var shift = new Vector2Field("Lens Shift") { value = key.lensShift };
                    shift.RegisterValueChangedCallback(evt => Change("Change lens shift", () => key.lensShift = evt.newValue));
                    foldout.Add(shift);
                }
                if ((key.channels & CameraKeyChannels.Shake) != 0)
                {
                    var positionScale = new Vector3Field("位置の揺れ幅 (m)") { value = key.shakePositionScale };
                    positionScale.RegisterValueChangedCallback(evt => Change("Change shake scale", () => key.shakePositionScale = evt.newValue));
                    foldout.Add(positionScale);
                    var rotationScale = new Vector3Field("回転の揺れ幅 (度)") { value = key.shakeRotationScale };
                    rotationScale.RegisterValueChangedCallback(evt => Change("Change shake scale", () => key.shakeRotationScale = evt.newValue));
                    foldout.Add(rotationScale);
                    AddFloat(foldout, "Frequency (Hz)", key.shakeFrequency, value => key.shakeFrequency = value, 0, 1000);
                    AddFloat(foldout, "Scale", key.shakeScale, value => key.shakeScale = value, 0, float.MaxValue);
                }
                var ease = new EnumField("次のキーへの補間", key.ease);
                ease.RegisterValueChangedCallback(evt => { Change("Change camera easing", () => key.ease = (EasingType)evt.newValue); _root.schedule.Execute(Build); });
                foldout.Add(ease);
                if (key.ease == EasingType.Custom)
                {
                    var curve = new CurveField("補間カーブ (0～1)") { value = key.curve };
                    curve.RegisterValueChangedCallback(evt => Change("Change camera curve", () => key.curve = evt.newValue));
                    foldout.Add(curve);
                }
                foldout.Add(new Button(() => SelectKey(key)) { text = "この時刻へ移動 / Sceneで編集" });
                if ((key.channels & CameraKeyChannels.Rotation) != 0) foldout.Add(new Button(() =>
                {
                    var scene = SceneView.lastActiveSceneView;
                    if (scene == null) return;
                    Change("Capture view rotation", () => key.eulerAngles = scene.rotation.eulerAngles);
                    Build();
                }) { text = "Scene Viewの向きだけを取り込む" });
                if (Shot.positionMode == CameraPositionMode.Direct && (key.channels & CameraKeyChannels.Position) != 0)
                    foldout.Add(new Button(() =>
                    {
                        var scene = SceneView.lastActiveSceneView;
                        if (scene == null) return;
                        Change("Capture view position", () => key.position = scene.camera.transform.position);
                        Build();
                    }) { text = "Scene Viewの位置を取り込む" });
                if (pathPosition && index < 0) foldout.Add(new HelpBox("対応Knotがありません。Knot番号を選び直すまでClipは評価されません。", HelpBoxMessageType.Error));
            }
        }

        private void AddKey()
        {
            var clip = Context;
            var director = TimelineEditor.inspectedDirector;
            var path = Path;
            if (clip == null || director == null || _newChannels == CameraKeyChannels.None) return;
            double time = (director.time - clip.start) * clip.timeScale + clip.clipIn;
            if (director.time < clip.start || director.time > clip.end || time < 0) return;
            var existing = Shot.keys.FirstOrDefault(k => Math.Abs(k.time - time) < 0.000001);
            bool pathPosition = Shot.positionMode == CameraPositionMode.Spline && (_newChannels & CameraKeyChannels.Position) != 0;
            if (pathPosition && (path == null || path.Splines.Count != 1 || path.Spline.Closed || path.Spline.Count < 2)) return;
            var camera = director.GetGenericBinding(clip.GetParentTrack()) as Camera;
            var key = existing ?? new CameraPassageKey { time = time, channels = CameraKeyChannels.None };
            int knotId = key.knotId;
            if (pathPosition && (existing == null || (existing.channels & CameraKeyChannels.Position) == 0))
            {
                Undo.RecordObject(path, "Add camera passage key");
                int knotIndex = Mathf.Min(Shot.keys.Count(k => (k.channels & CameraKeyChannels.Position) != 0), path.Spline.Count - 1);
                knotId = CameraKnotIdentity.GetOrCreate(path.Spline, knotIndex);
                MarkPath(path);
            }
            Change("Record camera key", () =>
            {
                key.channels |= _newChannels;
                if ((_newChannels & CameraKeyChannels.Position) != 0)
                {
                    key.knotId = knotId;
                    if (camera != null) key.position = camera.transform.position;
                }
                if (camera != null)
                {
                    if ((_newChannels & CameraKeyChannels.Rotation) != 0) key.eulerAngles = camera.transform.eulerAngles;
                    if ((_newChannels & CameraKeyChannels.FieldOfView) != 0) key.fieldOfView = camera.fieldOfView;
                    if ((_newChannels & CameraKeyChannels.LensShift) != 0) key.lensShift = camera.lensShift;
                }
                if (existing == null) Shot.keys.Add(key);
                Shot.keys.Sort((a, b) => a.time.CompareTo(b.time));
            });
            _selected = key;
            Build();
        }

        private void SelectKey(CameraPassageKey key)
        {
            var clip = Context;
            if (clip == null) return;
            _selected = key;
            BuildSelectedKey();
            TimelineEditor.GetWindow().playbackControls.SetCurrentTime(clip.start + (key.time - clip.clipIn) / clip.timeScale);
            Refresh();
        }

        private void SelectAdjacent(int direction)
        {
            if (Shot.keys.Count == 0) return;
            int index = Mathf.Clamp(Shot.keys.IndexOf(_selected) + direction, 0, Shot.keys.Count - 1);
            SelectKey(Shot.keys[index]);
        }

        private void DeleteSelectedKey()
        {
            if (_selected == null || !Shot.keys.Contains(_selected)) return;
            int index = Shot.keys.IndexOf(_selected);
            Change("Delete camera key", () => Shot.keys.Remove(_selected));
            _selected = Shot.keys.Count == 0 ? null : Shot.keys[Mathf.Min(index, Shot.keys.Count - 1)];
            Build();
        }

        private void Change(string label, Action action)
        {
            Undo.RecordObject(Shot, label);
            action();
            EditorUtility.SetDirty(Shot);
            Refresh();
        }

        private void AddFloat(VisualElement parent, string label, float value, Action<float> setValue, float min, float max)
        {
            var field = new FloatField(label) { value = value };
            field.RegisterValueChangedCallback(evt =>
            {
                if (float.IsNaN(evt.newValue) || float.IsInfinity(evt.newValue)) { field.SetValueWithoutNotify(evt.previousValue); return; }
                float clamped = Mathf.Clamp(evt.newValue, min, max);
                Change(label, () => setValue(clamped));
                field.SetValueWithoutNotify(clamped);
            });
            parent.Add(field);
        }

        private static void MarkPath(SplineContainer path)
        {
            EditorUtility.SetDirty(path);
            PrefabUtility.RecordPrefabInstancePropertyModifications(path);
        }

        private static void Refresh()
        {
            CameraShotSampler.InvalidateCaches();
            TimelineEditor.Refresh(RefreshReason.ContentsModified);
            TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate);
            SceneView.RepaintAll();
        }

        private void DrawScene(SceneView scene)
        {
            if (target == null || _selected == null || !Shot.keys.Contains(_selected) || Context == null) return;
            var path = Path;
            bool hasPosition = (_selected.channels & CameraKeyChannels.Position) != 0;
            bool pathPosition = Shot.positionMode == CameraPositionMode.Spline && hasPosition;
            int index = path == null ? -1 : CameraKnotIdentity.Find(path.Spline, _selected.knotId);
            if (pathPosition && index < 0) return;
            Vector3 position = pathPosition ? path.transform.TransformPoint(path.Spline[index].Position) : _selected.position;
            if (!hasPosition)
            {
                var camera = TimelineEditor.inspectedDirector.GetGenericBinding(Context.GetParentTrack()) as Camera;
                if (camera != null) position = camera.transform.position;
            }
            Quaternion rotation = Quaternion.Euler(_selected.eulerAngles);
            Handles.Label(position, $"Camera key {_selected.time:0.###} s / Knot {index}");
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = hasPosition ? Handles.PositionHandle(position, Quaternion.identity) : position;
            if (EditorGUI.EndChangeCheck())
            {
                if (pathPosition)
                {
                    Undo.RecordObject(path, "Move camera path knot");
                    var knot = path.Spline[index];
                    knot.Position = path.transform.InverseTransformPoint(newPosition);
                    path.Spline[index] = knot;
                    MarkPath(path);
                    Refresh();
                }
                else Change("Move camera key", () => _selected.position = newPosition);
            }
            EditorGUI.BeginChangeCheck();
            Quaternion newRotation = (_selected.channels & CameraKeyChannels.Rotation) != 0 ? Handles.RotationHandle(rotation, position) : rotation;
            if (EditorGUI.EndChangeCheck())
            {
                Change("Rotate camera passage", () => _selected.eulerAngles = newRotation.eulerAngles);
                _root?.schedule.Execute(Build);
            }
            Handles.ArrowHandleCap(0, position, rotation, HandleUtility.GetHandleSize(position), EventType.Repaint);
        }
    }
}

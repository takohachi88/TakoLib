using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace TakoLib.Common.Timeline.Editor
{
    [CustomEditor(typeof(VolumeWeightClip))]
    public sealed class VolumeWeightClipInspector : UnityEditor.Editor
    {
        private VisualElement _root;
        private VolumeWeightClip Clip => (VolumeWeightClip)target;
        private TimelineClip Context => TimelineEditor.selectedClip?.asset == target ? TimelineEditor.selectedClip : null;
        private void OnEnable() { Undo.undoRedoPerformed += Build; }
        private void OnDisable() { Undo.undoRedoPerformed -= Build; }

        public override VisualElement CreateInspectorGUI()
        {
            _root = new VisualElement();
            Build();
            return _root;
        }

        private void Change(string name, Action action)
        {
            Undo.RecordObject(Clip, name);
            action();
            EditorUtility.SetDirty(Clip);
            TimelineEditor.Refresh(RefreshReason.ContentsModified);
            TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate);
        }

        private void Build()
        {
            if (_root == null || target == null) return;
            _root.Clear();
            _root.Add(new HelpBox("TrackにVolumeを割り当てます。Weightだけを変更し、Profileは変更しません。Local Volumeの距離やPriority、各項目のOverrideも最終結果に影響します。", HelpBoxMessageType.Info));
            _root.Add(new Button(() =>
            {
                var context = Context;
                var director = TimelineEditor.inspectedDirector;
                if (context == null || director == null || director.time < context.start || director.time > context.end) return;
                double time = (director.time - context.start) * context.timeScale + context.clipIn;
                if (Clip.keys.Any(k => Math.Abs(k.time - time) < 0.000001)) return;
                var volume = director.GetGenericBinding(context.GetParentTrack()) as Volume;
                float weight = Clip.TryEvaluate(time, out var current) ? current : volume == null ? 1 : volume.weight;
                Change("Add volume weight key", () => { Clip.keys.Add(new VolumeWeightKey { time = time, weight = weight }); Clip.keys.Sort((a, b) => a.time.CompareTo(b.time)); });
                Build();
            }) { text = "現在時刻にWeightキーを追加" });
            foreach (var key in Clip.keys)
            {
                var foldout = new Foldout { text = $"{key.time:0.###} s / Weight {key.weight:0.###}", value = true };
                var time = new DoubleField("Clip内の時刻 (秒)") { value = key.time, isDelayed = true };
                time.RegisterValueChangedCallback(evt =>
                {
                    if (double.IsNaN(evt.newValue) || double.IsInfinity(evt.newValue) || evt.newValue < 0 || Clip.keys.Any(k => k != key && Math.Abs(k.time - evt.newValue) < 0.000001))
                    { time.SetValueWithoutNotify(key.time); return; }
                    Change("Move volume key", () => { key.time = evt.newValue; Clip.keys.Sort((a, b) => a.time.CompareTo(b.time)); });
                    _root.schedule.Execute(Build);
                });
                foldout.Add(time);
                var weight = new Slider("Weight", 0, 1) { value = key.weight, showInputField = true };
                weight.RegisterValueChangedCallback(evt => Change("Change volume weight", () => key.weight = Mathf.Clamp01(evt.newValue)));
                foldout.Add(weight);
                var ease = new EnumField("次のキーへの補間", key.ease);
                ease.RegisterValueChangedCallback(evt => { Change("Change volume easing", () => key.ease = (EasingType)evt.newValue); _root.schedule.Execute(Build); });
                foldout.Add(ease);
                if (key.ease == EasingType.Custom)
                {
                    var curve = new CurveField("補間カーブ (0～1)") { value = key.curve };
                    curve.RegisterValueChangedCallback(evt => Change("Change volume curve", () => key.curve = evt.newValue));
                    foldout.Add(curve);
                }
                foldout.Add(new Button(() =>
                {
                    if (Context == null) return;
                    TimelineEditor.GetWindow().playbackControls.SetCurrentTime(Context.start + (key.time - Context.clipIn) / Context.timeScale);
                    TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate);
                }) { text = "この時刻へ移動" });
                foldout.Add(new Button(() => { Change("Delete volume key", () => Clip.keys.Remove(key)); Build(); }) { text = "キーを削除" });
                _root.Add(foldout);
            }
        }
    }

    [CustomTimelineEditor(typeof(VolumeWeightClip))]
    public sealed class VolumeWeightClipEditor : ClipEditor
    {
        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);
            if (!((VolumeWeightClip)clip.asset).TryEvaluate(0, out _)) options.errorText = "キーの時刻・Weightを確認してください。";
            return options;
        }
    }
}

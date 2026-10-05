using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace TakoLib.Common.Timeline.Editor
{
    public sealed class CameraKeyStrip : VisualElement
    {
        private readonly CameraShotClip _shot;
        private readonly TimelineClip _clip;
        private readonly List<(CameraPassageKey key, Label marker)> _markers = new List<(CameraPassageKey, Label)>();
        private readonly VisualElement _strip;
        private readonly VisualElement _playhead;
        private CameraPassageKey _selected;

        public CameraKeyStrip(CameraShotClip shot, TimelineClip clip, Action<CameraPassageKey> select, Action changed, Action finished)
        {
            _shot = shot;
            _clip = clip;
            var hint = new Label("キーを選択して下で編集 / ドラッグで時刻変更 / 背景でスクラブ / Shiftでスナップ解除");
            hint.AddToClassList("camera-key-hint");
            Add(hint);
            _strip = new VisualElement { name = "cameraKeyStrip" };
            _strip.style.height = 40;
            _strip.style.backgroundColor = new Color(0.12f, 0.16f, 0.19f);
            _strip.style.marginTop = 4;
            _strip.style.marginBottom = 4;
            Add(_strip);
            _playhead = new VisualElement { pickingMode = PickingMode.Ignore };
            _playhead.style.position = Position.Absolute;
            _playhead.style.width = 1;
            _playhead.style.top = 0;
            _playhead.style.bottom = 0;
            _playhead.style.backgroundColor = Color.white;
            _strip.Add(_playhead);
            foreach (var key in shot.keys)
            {
                var marker = new Label("◆") { tooltip = $"{key.time:0.###} s / {key.channels}" };
                marker.style.position = Position.Absolute;
                marker.style.top = 10;
                marker.style.width = 16;
                marker.AddToClassList("camera-key-marker");
                marker.usageHints = UsageHints.DynamicTransform;
                bool dragging = false;
                int undoGroup = -1;
                marker.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0) return;
                    select(key);
                    Undo.IncrementCurrentGroup();
                    undoGroup = Undo.GetCurrentGroup();
                    Undo.RegisterCompleteObjectUndo(shot, "Move camera key time");
                    dragging = true;
                    marker.CapturePointer(evt.pointerId);
                    evt.StopPropagation();
                });
                marker.RegisterCallback<PointerMoveEvent>(evt =>
                {
                    if (!dragging || !marker.HasPointerCapture(evt.pointerId)) return;
                    double time = TimeAt(_strip.WorldToLocal(evt.position).x);
                    double fps = TimelineEditor.inspectedAsset == null ? 30 : TimelineEditor.inspectedAsset.editorSettings.frameRate;
                    if (!evt.shiftKey) time = Math.Round(time * fps) / fps;
                    time = Math.Max(clip.clipIn, Math.Min(clip.clipIn + clip.duration * clip.timeScale, time));
                    if (!shot.keys.Any(k => k != key && Math.Abs(k.time - time) < 0.000001))
                    {
                        key.time = time;
                        shot.keys.Sort((a, b) => a.time.CompareTo(b.time));
                        EditorUtility.SetDirty(shot);
                        changed();
                        select(key);
                        UpdatePositions();
                    }
                    evt.StopPropagation();
                });
                marker.RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (!dragging) return;
                    dragging = false;
                    marker.ReleasePointer(evt.pointerId);
                    Undo.CollapseUndoOperations(undoGroup);
                    finished();
                    evt.StopPropagation();
                });
                marker.RegisterCallback<PointerCaptureOutEvent>(_ =>
                {
                    if (!dragging) return;
                    dragging = false;
                    Undo.CollapseUndoOperations(undoGroup);
                    finished();
                });
                _strip.Add(marker);
                _markers.Add((key, marker));
            }
            _strip.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                var window = TimelineEditor.GetWindow();
                if (window == null) return;
                double local = TimeAt(evt.localPosition.x);
                window.playbackControls.SetCurrentTime(clip.start + (local - clip.clipIn) / clip.timeScale);
                TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate);
            });
            _strip.RegisterCallback<GeometryChangedEvent>(_ => UpdatePositions());
            schedule.Execute(UpdatePositions).Every(100);
        }

        public void SetSelected(CameraPassageKey key)
        {
            _selected = key;
            foreach (var pair in _markers)
                pair.marker.EnableInClassList("camera-key-marker--selected", pair.key == key);
        }

        private double TimeAt(float x)
        {
            float width = Mathf.Max(1, _strip.contentRect.width - 16);
            return _clip.clipIn + Mathf.Clamp01((x - 8) / width) * _clip.duration * _clip.timeScale;
        }

        private float XAt(double time)
        {
            double span = Math.Max(0.000001, _clip.duration * _clip.timeScale);
            return 8 + (float)((time - _clip.clipIn) / span) * Mathf.Max(1, _strip.contentRect.width - 16);
        }

        private void UpdatePositions()
        {
            if (_shot == null) return;
            foreach (var pair in _markers)
            {
                bool visible = pair.key.time >= _clip.clipIn && pair.key.time <= _clip.clipIn + _clip.duration * _clip.timeScale;
                pair.marker.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                pair.marker.style.translate = new Translate(XAt(pair.key.time) - 8, 0);
                pair.marker.text = (_shot.keys.IndexOf(pair.key) + 1).ToString();
                pair.marker.tooltip = $"{pair.key.time:0.###} s / {pair.key.channels}";
            }
            var director = TimelineEditor.inspectedDirector;
            double local = director == null ? 0 : (director.time - _clip.start) * _clip.timeScale + _clip.clipIn;
            _playhead.style.translate = new Translate(XAt(local), 0);
            _playhead.style.display = local >= _clip.clipIn && local <= _clip.clipIn + _clip.duration * _clip.timeScale ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

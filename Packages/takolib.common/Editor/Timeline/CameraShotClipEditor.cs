using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace TakoLib.Common.Timeline.Editor
{
    [CustomTimelineEditor(typeof(CameraShotClip))]
    public sealed class CameraShotClipEditor : ClipEditor
    {
        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var shot = (CameraShotClip)clip.asset;
            double span = region.endTime - region.startTime;
            if (span <= 0) return;
            foreach (var key in shot.keys)
            {
                if (key.time < region.startTime || key.time > region.endTime) continue;
                float x = region.position.x + (float)((key.time - region.startTime) / span) * region.position.width;
                EditorGUI.DrawRect(new Rect(x - 1, region.position.y + 2, 2, region.position.height - 4), new Color(0.6f, 0.95f, 1));
            }
        }

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);
            var shot = (CameraShotClip)clip.asset;
            options.tooltip = "通過キーはInspectorで編集できます。位置と向きのSceneハンドルはキーを選択すると表示されます。";
            if (shot.keys.Count == 0) options.errorText = "通過キーを追加してください。";
            return options;
        }
    }
}

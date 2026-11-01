#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace TakoLibEditor.Common
{
    internal sealed class TextureGeneratorPreview : IDisposable
    {
        private TextureGeneratorRecipe recipe;
        private TextureGeneratorTone tone = new TextureGeneratorTone();
        private string generationKey, toneKey, error;
        private Color32[] pixels, volume;
        private Vector3Int volumeSize;
        private int width, height, channel, slice = 32;
        private bool linear = true, pending, tiles, started;
        public bool HasStarted => started;
        private double due;
        private Texture2D display;
        private int[,] before, after;
        public void SetRecipe(TextureGeneratorRecipe data)
        {
            recipe = data.Clone(); recipe.Normalize();
            string key = JsonUtility.ToJson(recipe.noise) + "|" + recipe.generate3D + "|" +
                (recipe.generate3D ? recipe.volumeSize.ToString() : recipe.size.ToString());
            if (generationKey != key)
            {
                generationKey = key; volume = null; pending = started; error = null;
                due = EditorApplication.timeSinceStartup + 0.2;
            }
            SetTone(recipe.tone);
        }
        public void SetTone(TextureGeneratorTone value)
        {
            string key = JsonUtility.ToJson(value);
            if (key == toneKey) return;
            toneKey = key; tone = value; BuildDisplay();
        }
        public void Request() { started = true; generationKey = null; if (recipe != null) SetRecipe(recipe); }
        public bool Tick()
        {
            if (!pending || recipe == null || EditorApplication.timeSinceStartup < due ||
                EditorApplication.isCompiling || EditorApplication.isUpdating) return false;
            pending = false;
            try
            {
                if (recipe.generate3D)
                {
                    volumeSize = recipe.volumeSize;
                    volume = TextureGeneratorEngine.Generate3DPixels(volumeSize.x, volumeSize.y, volumeSize.z,
                        recipe.noise, p => EditorUtility.DisplayCancelableProgressBar("Texture Preview", "3Dデータを生成中…", p));
                    ShowSlice();
                }
                else
                {
                    float scale = Mathf.Min(1, 256f / Mathf.Max(recipe.size.x, recipe.size.y));
                    Texture2D raw = TextureGeneratorEngine.Generate(Mathf.Max(1, Mathf.RoundToInt(recipe.size.x * scale)),
                        Mathf.Max(1, Mathf.RoundToInt(recipe.size.y * scale)), recipe.noise);
                    try { SetPixels(raw.GetPixels32(), raw.width, raw.height, true); }
                    finally { UnityEngine.Object.DestroyImmediate(raw); }
                }
            }
            catch (OperationCanceledException) { error = "生成をキャンセルしました。再生成ボタンで再試行できます。"; }
            catch (Exception e) { error = e.Message; Debug.LogException(e); }
            finally { EditorUtility.ClearProgressBar(); }
            return true;
        }
        public void SetPixels(Color32[] values, int w, int h, bool isLinear)
        {
            pixels = values; width = w; height = h; linear = isLinear; BuildDisplay();
        }
        private void ShowSlice()
        {
            if (volume == null) return;
            slice = Mathf.Clamp(slice, 0, volumeSize.z - 1);
            var values = new Color32[volumeSize.x * volumeSize.y];
            Array.Copy(volume, slice * values.Length, values, 0, values.Length);
            SetPixels(values, volumeSize.x, volumeSize.y, true);
        }
        private static int[,] Histogram(Color32[] values)
        {
            var bins = new int[5, 256];
            foreach (Color32 p in values)
            {
                bins[0, p.r]++; bins[0, p.g]++; bins[0, p.b]++;
                bins[1, p.r]++; bins[2, p.g]++; bins[3, p.b]++; bins[4, p.a]++;
            }
            return bins;
        }
        private void BuildDisplay()
        {
            if (pixels == null) return;
            before = Histogram(pixels);
            Color32[] adjusted = tone.Apply((Color32[])pixels.Clone());
            after = Histogram(adjusted);
            if (channel > 0 && channel < 4)
                for (int i = 0; i < adjusted.Length; i++)
                {
                    Color32 p = adjusted[i]; byte v = channel == 1 ? p.r : channel == 2 ? p.g : p.b;
                    adjusted[i] = new Color32(v, v, v, 255);
                }
            if (display != null) UnityEngine.Object.DestroyImmediate(display);
            display = new Texture2D(width, height, TextureFormat.RGBA32, false, linear)
                { hideFlags = HideFlags.HideAndDontSave, filterMode = recipe == null ? FilterMode.Bilinear : recipe.Filter };
            display.SetPixels32(adjusted); display.Apply(false, false);
        }
        public void DrawHistogram()
        {
            EditorGUILayout.LabelField("画素値の分布（灰色: 調整前 / 色付き: 調整後）", EditorStyles.miniLabel);
            Rect rect = GUILayoutUtility.GetRect(1, 70, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.08f, 0.08f));
            if (before == null || after == null) return;
            int maximum = 1;
            for (int i = 0; i < 256; i++) maximum = Mathf.Max(maximum, Mathf.Max(before[channel, i], after[channel, i]));
            Color color = channel == 1 ? Color.red : channel == 2 ? Color.green : channel == 3 ? Color.cyan : Color.yellow;
            color.a = 0.65f;
            for (int i = 0; i < 256; i++)
            {
                float x = rect.x + rect.width * i / 256;
                float a = (rect.height - 1) * before[channel, i] / maximum;
                float b = (rect.height - 1) * after[channel, i] / maximum;
                EditorGUI.DrawRect(new Rect(x, rect.yMax - a, Mathf.Max(1, rect.width / 256), a), Color.gray);
                EditorGUI.DrawRect(new Rect(x, rect.yMax - b, Mathf.Max(1, rect.width / 256), b), color);
            }
        }
        public void Draw(bool isVolume, Action generate, string generateLabel, bool canGenerate = true)
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                bool expanded;
                using (new EditorGUILayout.HorizontalScope())
                {
                    expanded = TextureGeneratorGUI.AreaFoldout("Preview", "プレビュー");
                    if (expanded)
                        using (new EditorGUI.DisabledScope(!canGenerate))
                            if (GUILayout.Button(generateLabel, GUILayout.Width(130))) generate();
                }
                if (!expanded) return;
                EditorGUI.BeginChangeCheck();
                channel = GUILayout.Toolbar(channel, new[] { "RGB", "R", "G", "B", "A" });
                if (EditorGUI.EndChangeCheck()) BuildDisplay();
                tiles = EditorGUILayout.Toggle("3 × 3 タイル", tiles);
                if (isVolume && recipe != null)
                {
                    EditorGUI.BeginChangeCheck();
                    slice = EditorGUILayout.IntSlider("Zスライス", slice, 0, recipe.volumeSize.z - 1);
                    if (EditorGUI.EndChangeCheck()) ShowSlice();
                    EditorGUILayout.LabelField("分布も選択中のZ断面です。圧縮前のデータを表示します。", EditorStyles.miniLabel);
                }
                if (pending) EditorGUILayout.LabelField("プレビュー更新待ち…", EditorStyles.miniLabel);
                if (error != null) EditorGUILayout.HelpBox(error, MessageType.Info);
                if (display == null) return;
                float w = Mathf.Clamp(EditorGUIUtility.currentViewWidth * 2f / 3f - 25, 100, 420);
                float h = Mathf.Min(280, w * display.height / display.width); w = h * display.width / display.height;
                Rect area = GUILayoutUtility.GetRect(w, h, GUILayout.ExpandWidth(false));
                int count = tiles ? 3 : 1;
                for (int y = 0; y < count; y++) for (int x = 0; x < count; x++)
                {
                    Rect tile = new Rect(area.x + x * w / count, area.y + y * h / count, w / count, h / count);
                    if (channel == 4) EditorGUI.DrawTextureAlpha(tile, display, ScaleMode.StretchToFill);
                    else GUI.DrawTexture(tile, display, ScaleMode.StretchToFill, false);
                }
            }
        }
        public void Clear()
        {
            if (display != null) UnityEngine.Object.DestroyImmediate(display);
            display = null; pixels = volume = null; before = after = null;
        }
        public void Dispose() { pending = false; Clear(); }
    }
}
#endif
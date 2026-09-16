#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TakoLibEditor.Common
{
    public sealed class TextureGeneratorWindow : EditorWindow
    {
        [SerializeField] private TextureGenerationSettings _settings = new TextureGenerationSettings();
        [SerializeField] private int _mode;
        [SerializeField] private Vector2Int _size = new Vector2Int(512, 512);
        [SerializeField] private Texture2D _source;
        [SerializeField] private bool _horizontal = true, _vertical = true, _tilePreview;
        [SerializeField] private int _previewChannel;
        [SerializeField] private bool _blendSeams, _toneEnabled;
        [SerializeField] private float _blendWidth = 0.15f, _blendHeight = 0.15f;
        [SerializeField] private float _solidWidth, _solidHeight;
        [SerializeField] private AnimationCurve _tone = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _toneR = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _toneG = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _toneB = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _toneA = AnimationCurve.Linear(0, 0, 1, 1);
        private Vector2 _scroll;
        private Texture2D _preview, _display;
        private bool _previewPending;
        private double _previewAt;
        private string _previewError;
        [SerializeField] private int _histogramChannel;
        private int[,] _inputHistogram, _outputHistogram;

        [MenuItem("Tools/TakoLib/Texture Generator")]
        private static void Open() => GetWindow<TextureGeneratorWindow>("Texture Generator").Show();

        [MenuItem("Assets/TakoLib/Offset Texture 50%", false, 2101)]
        private static void OpenOffset()
        {
            var window = GetWindow<TextureGeneratorWindow>("Texture Generator");
            window._mode = 1;
            window._source = Selection.activeObject as Texture2D;
            window.ClearPreview();
            window.QueuePreview();
            window.Show();
        }

        [MenuItem("Assets/TakoLib/Offset Texture 50%", true)]
        private static bool ValidateOffset() => Selection.activeObject is Texture2D;
        private void OnEnable()
        {
            minSize = new Vector2(410, 580);
            EditorApplication.update += UpdatePreview;
            QueuePreview();
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdatePreview;
            _previewPending = false;
            ClearPreview();
        }

        private void QueuePreview()
        {
            _previewError = null;
            _previewPending = true;
            _previewAt = EditorApplication.timeSinceStartup + 0.2;
            if (_mode == 1 && _source == null)
            {
                _previewPending = false;
                ClearPreview();
            }
            Repaint();
        }

        private void UpdatePreview()
        {
            if (!_previewPending || EditorApplication.timeSinceStartup < _previewAt ||
                EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            _previewPending = false;
            Run(false);
            Repaint();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUI.BeginChangeCheck();
            _mode = GUILayout.Toolbar(_mode, new[] { "生成", "50% オフセット" });
            EditorGUILayout.Space();
            if (_mode == 0) DrawGeneration();
            else
            {
                _source = (Texture2D)EditorGUILayout.ObjectField("元テクスチャ", _source, typeof(Texture2D), false);
                _horizontal = EditorGUILayout.Toggle("左右に50%移動", _horizontal);
                _vertical = EditorGUILayout.Toggle("上下に50%移動", _vertical);
                _blendSeams = EditorGUILayout.Toggle("フェードでシームレス化", _blendSeams);
                if (_blendSeams)
                {
                    EditorGUILayout.LabelField("縦帯（左右方向）", EditorStyles.boldLabel);
                    _solidWidth = EditorGUILayout.Slider("100%領域の幅 (UV)", _solidWidth, 0f, 1f);
                    _blendWidth = EditorGUILayout.Slider("片側のフェード幅 (UV)",
                        Mathf.Min(_blendWidth, (1f - _solidWidth) * 0.5f), 0f, (1f - _solidWidth) * 0.5f);
                    EditorGUILayout.LabelField("横帯（上下方向）", EditorStyles.boldLabel);
                    _solidHeight = EditorGUILayout.Slider("100%領域の幅 (UV)", _solidHeight, 0f, 1f);
                    _blendHeight = EditorGUILayout.Slider("片側のフェード幅 (UV)",
                        Mathf.Min(_blendHeight, (1f - _solidHeight) * 0.5f), 0f, (1f - _solidHeight) * 0.5f);
                    EditorGUILayout.HelpBox("100%領域の幅は中央の帯全体の幅です。その両側に指定幅のフェードを付け、100%から0%へ滑らかに重ねます。\n" +
                        "全体の幅 = 100%領域の幅 + フェード幅 × 2（最大1 UV）。フェード幅0では境界が急に切り替わります。\n" +
                        "交差部分は縦横のオフセットを組み合わせて合成します。\n" +
                        "両方向をシームレスにするには左右・上下の移動を両方有効にしてください。", MessageType.Info);
                }
                EditorGUILayout.HelpBox("端を循環させて移動します。Read/Write設定は不要です。元画像は変更しません。\n" +
                    "偶数サイズでは左・右（上・下）どちらに移動しても同じ結果になります。奇数サイズは半分を切り捨て、右・上へ移動します。オフセット自体は継ぎ目を除去しません。", MessageType.Info);
                if (_source != null) EditorGUILayout.LabelField("出力サイズ", $"{_source.width} × {_source.height}");
            }
            if (EditorGUI.EndChangeCheck()) QueuePreview();
            DrawToneCurves();

            using (new EditorGUI.DisabledScope(_mode == 1 && _source == null))
            {
                if (GUILayout.Button("プレビューを再生成", GUILayout.Height(28))) QueuePreview();
                if (GUILayout.Button("PNGを保存…", GUILayout.Height(28))) Run(true);
            }
            EditorGUILayout.Space();
            if (_previewPending) EditorGUILayout.LabelField("プレビュー更新待ち…", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(_previewError)) EditorGUILayout.HelpBox(_previewError, MessageType.Error);
            _tilePreview = EditorGUILayout.Toggle("3 × 3 タイルで確認", _tilePreview);
            EditorGUI.BeginChangeCheck();
            _previewChannel = GUILayout.Toolbar(_previewChannel, new[] { "RGB", "R", "G", "B", "A" });
            if (EditorGUI.EndChangeCheck()) UpdateDisplay();
            if (_display != null)
            {
                float width = Mathf.Max(100, position.width - 38);
                float height = Mathf.Min(420, width * _display.height / _display.width);
                width = height * _display.width / _display.height;
                width *= 2f / 3f;
                height *= 2f / 3f;
                Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false));
                int tiles = _tilePreview ? 3 : 1;
                for (int y = 0; y < tiles; y++)
                for (int x = 0; x < tiles; x++)
                    GUI.DrawTexture(new Rect(rect.x + x * rect.width / tiles, rect.y + y * rect.height / tiles,
                        rect.width / tiles, rect.height / tiles), _display, ScaleMode.StretchToFill, false);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawGeneration()
        {
            _settings.pattern = (GeneratedPattern)EditorGUILayout.EnumPopup("パターン", _settings.pattern);
            _size = EditorGUILayout.Vector2IntField("解像度", _size);
            _size.x = Mathf.Clamp(_size.x, 16, 4096);
            _size.y = Mathf.Clamp(_size.y, 16, 4096);
            _settings.columns = EditorGUILayout.IntSlider("横のセル数", _settings.columns, 1, 128);
            _settings.rows = EditorGUILayout.IntSlider("縦のセル数", _settings.rows, 2, 128);
            _settings.seed = EditorGUILayout.IntField("シード", _settings.seed);
            bool separateSeeds = EditorGUILayout.Toggle("RGBA別のシード", _settings.separateSeeds);
            if (separateSeeds && !_settings.separateSeeds)
                _settings.redSeed = _settings.greenSeed = _settings.blueSeed = _settings.alphaSeed = _settings.seed;
            _settings.separateSeeds = separateSeeds;
            if (_settings.separateSeeds)
            {
                _settings.redSeed = EditorGUILayout.IntField("R シード", _settings.redSeed);
                _settings.greenSeed = EditorGUILayout.IntField("G シード", _settings.greenSeed);
                _settings.blueSeed = EditorGUILayout.IntField("B シード", _settings.blueSeed);
                _settings.alphaSeed = EditorGUILayout.IntField("A シード", _settings.alphaSeed);
                EditorGUILayout.HelpBox("VoronoiのUVをR/Gに組み合わせる場合、RとGのシードを揃えてください。Hexagon/Triangleの形状と中心位置はシードに依存しません。", MessageType.None);
            }
            _settings.seamless = EditorGUILayout.Toggle("シームレス生成", _settings.seamless);
            if (_settings.pattern == GeneratedPattern.Fractal || _settings.pattern == GeneratedPattern.Billow ||
                _settings.pattern == GeneratedPattern.Ridged)
            {
                _settings.octaves = EditorGUILayout.IntSlider("オクターブ", _settings.octaves, 1, 8);
                _settings.persistence = EditorGUILayout.Slider("高周波の強さ", _settings.persistence, 0, 1);
            }
            if (_settings.pattern == GeneratedPattern.Hexagon || _settings.pattern == GeneratedPattern.Triangle)
                EditorGUILayout.HelpBox("シームレス時、縦のセル数は偶数に切り上げます。正六角形・正三角形の比率は「幅/高さ = 横セル数/(縦セル数 × 0.866)」です。", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("RGBA チャンネル割り当て", EditorStyles.boldLabel);
            _settings.red = (GeneratedChannel)EditorGUILayout.EnumPopup("R", _settings.red);
            _settings.green = (GeneratedChannel)EditorGUILayout.EnumPopup("G", _settings.green);
            _settings.blue = (GeneratedChannel)EditorGUILayout.EnumPopup("B", _settings.blue);
            _settings.alpha = (GeneratedChannel)EditorGUILayout.EnumPopup("A", _settings.alpha);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("グレースケール"))
                {
                    _settings.red = _settings.green = _settings.blue = GeneratedChannel.Value;
                    _settings.alpha = GeneratedChannel.One;
                    GUI.changed = true;
                }
                if (GUILayout.Button("UV + セルID"))
                {
                    _settings.red = GeneratedChannel.U; _settings.green = GeneratedChannel.V;
                    _settings.blue = GeneratedChannel.CellId; _settings.alpha = GeneratedChannel.One;
                    GUI.changed = true;
                }
            }
            if (_settings.pattern == GeneratedPattern.Voronoi || _settings.pattern == GeneratedPattern.Hexagon ||
                _settings.pattern == GeneratedPattern.Triangle)
            {
                if (GUILayout.Button("モザイク用 セル中心UV (RG)"))
                {
                    _settings.red = GeneratedChannel.CenterU;
                    _settings.green = GeneratedChannel.CenterV;
                    _settings.blue = GeneratedChannel.Zero;
                    _settings.alpha = GeneratedChannel.One;
                    _settings.greenSeed = _settings.redSeed;
                    _toneEnabled = false;
                    GUI.changed = true;
                }
                EditorGUILayout.HelpBox("CenterU/CenterVはセル全体を中心点の画像UVで塗ります。RGを画像サンプリングのUVに接続してください。Voronoiは生成点、Triangleは重心を使います。", MessageType.None);
            }
            EditorGUILayout.HelpBox("Value: ノイズ値（Voronoi/Hexagonは中心からの距離、Triangleは辺からの距離）\n" +
                "U/V: セル内座標、CellId: セルごとの乱数、Zero/One: 0/1。TriangleのU/Vは重心座標の2成分です。\n" +
                "生成PNGはリニアのRGBAデータです。プレビューは最大256pxで、保存時に指定解像度で生成します。", MessageType.None);
        }

        private void DrawToneCurves()
        {
            DrawHistogram();
            EditorGUI.BeginChangeCheck();
            _toneEnabled = EditorGUILayout.Toggle("トーンカーブを適用", _toneEnabled);
            if (_toneEnabled)
            {
                Rect range = new Rect(0, 0, 1, 1);
                _tone = EditorGUILayout.CurveField("RGB 共通", _tone, Color.white, range, GUILayout.Height(72));
                _toneR = EditorGUILayout.CurveField("R", _toneR, Color.red, range, GUILayout.Height(44));
                _toneG = EditorGUILayout.CurveField("G", _toneG, Color.green, range, GUILayout.Height(44));
                _toneB = EditorGUILayout.CurveField("B", _toneB, Color.cyan, range, GUILayout.Height(44));
                _toneA = EditorGUILayout.CurveField("A", _toneA, Color.gray, range, GUILayout.Height(44));
                if (GUILayout.Button("カーブをリセット"))
                {
                    _tone = AnimationCurve.Linear(0, 0, 1, 1);
                    _toneR = AnimationCurve.Linear(0, 0, 1, 1);
                    _toneG = AnimationCurve.Linear(0, 0, 1, 1);
                    _toneB = AnimationCurve.Linear(0, 0, 1, 1);
                    _toneA = AnimationCurve.Linear(0, 0, 1, 1);
                    GUI.changed = true;
                }
                EditorGUILayout.HelpBox("横軸=入力、縦軸=出力。RGB共通→各チャンネルの順に適用します。Aは専用カーブのみ適用します。\n" +
                    "カーブ編集はプレビューに即時反映し、PNGにも適用します。UVを座標として使う場合は無効にしてください。", MessageType.None);
            }
            if (EditorGUI.EndChangeCheck()) UpdateDisplay();
        }

        private void DrawHistogram()
        {
            EditorGUILayout.LabelField("トーンカーブ・画素値の分布", EditorStyles.boldLabel);
            _histogramChannel = GUILayout.Toolbar(_histogramChannel, new[] { "RGB", "R", "G", "B", "A" });
            Rect rect = GUILayoutUtility.GetRect(1, 88, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(0.08f, 0.08f, 0.08f));
                for (int i = 1; i < 4; i++)
                    EditorGUI.DrawRect(new Rect(rect.x + rect.width * i / 4, rect.y, 1, rect.height), new Color(0.2f, 0.2f, 0.2f));
                if (_inputHistogram != null && _outputHistogram != null)
                {
                    int maximum = 1;
                    for (int bin = 0; bin < 256; bin++)
                        maximum = Mathf.Max(maximum, Mathf.Max(_inputHistogram[_histogramChannel, bin], _outputHistogram[_histogramChannel, bin]));
                    Color color = _histogramChannel == 1 ? new Color(1, 0.3f, 0.3f, 0.7f) :
                        _histogramChannel == 2 ? new Color(0.3f, 1, 0.3f, 0.7f) :
                        _histogramChannel == 3 ? new Color(0.3f, 0.6f, 1, 0.7f) : new Color(1, 0.8f, 0.3f, 0.7f);
                    for (int bin = 0; bin < 256; bin++)
                    {
                        float x = rect.x + rect.width * bin / 256;
                        float width = Mathf.Max(1, rect.width / 256);
                        float inputHeight = (rect.height - 2) * _inputHistogram[_histogramChannel, bin] / maximum;
                        float outputHeight = (rect.height - 2) * _outputHistogram[_histogramChannel, bin] / maximum;
                        EditorGUI.DrawRect(new Rect(x, rect.yMax - inputHeight, width, inputHeight), new Color(0.55f, 0.55f, 0.55f));
                        EditorGUI.DrawRect(new Rect(x, rect.yMax - outputHeight, width, outputHeight), color);
                    }
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("0", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label("0.5", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label("1", EditorStyles.miniLabel);
            }
            EditorGUILayout.LabelField("灰色: 調整前 / 色付き: 現在の出力 / 縦軸: 画素数", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("プレビューから集計（RGBは3チャンネルを合算）", EditorStyles.miniLabel);
        }

        private static int[,] Histogram(Color32[] pixels)
        {
            var bins = new int[5, 256];
            foreach (Color32 pixel in pixels)
            {
                bins[0, pixel.r]++; bins[0, pixel.g]++; bins[0, pixel.b]++;
                bins[1, pixel.r]++; bins[2, pixel.g]++; bins[3, pixel.b]++; bins[4, pixel.a]++;
            }
            return bins;
        }

        private static float EvaluateCurve(AnimationCurve curve, float value)
            => Mathf.Clamp01(curve == null || curve.length == 0 ? value : curve.Evaluate(value));

        private Color32[] AdjustTone(Color32[] pixels)
        {
            if (!_toneEnabled) return pixels;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color p = pixels[i];
                pixels[i] = new Color(EvaluateCurve(_toneR, EvaluateCurve(_tone, p.r)),
                    EvaluateCurve(_toneG, EvaluateCurve(_tone, p.g)),
                    EvaluateCurve(_toneB, EvaluateCurve(_tone, p.b)), EvaluateCurve(_toneA, p.a));
            }
            return pixels;
        }

        private void Run(bool save)
        {
            if (_mode == 1 && _source == null) return;
            string path = null;
            if (save)
            {
                path = EditorUtility.SaveFilePanelInProject("Save Texture", _mode == 0 ? "GeneratedTexture" : _source.name + "_Offset", "png", "保存先を選択");
                if (string.IsNullOrEmpty(path)) return;
                if (_mode == 1 && string.Equals(path, AssetDatabase.GetAssetPath(_source), StringComparison.OrdinalIgnoreCase))
                {
                    EditorUtility.DisplayDialog("保存先", "元画像とは別のファイル名を指定してください。", "OK");
                    return;
                }
            }
            Texture2D generated = null;
            try
            {
                float scale = save ? 1 : Mathf.Min(1, 256f / Mathf.Max(_size.x, _size.y));
                generated = _mode == 0
                    ? TextureGeneratorEngine.Generate(Mathf.Max(1, Mathf.RoundToInt(_size.x * scale)),
                        Mathf.Max(1, Mathf.RoundToInt(_size.y * scale)), _settings,
                        progress => save && EditorUtility.DisplayCancelableProgressBar("Texture Generator", "生成中…", progress))
                    : TextureGeneratorEngine.Offset(_source, _horizontal, _vertical, _blendSeams, _blendWidth, _blendHeight,
                        save ? 0 : 256, _solidWidth, _solidHeight);
                if (save)
                {
                    if (_toneEnabled)
                    {
                        generated.SetPixels32(AdjustTone(generated.GetPixels32()));
                        generated.Apply();
                    }
                    File.WriteAllBytes(Path.Combine(Directory.GetParent(Application.dataPath).FullName, path), generated.EncodeToPNG());
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = _mode == 1 && _source.isDataSRGB;
                    importer.alphaSource = TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency = false;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.maxTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(generated.width, generated.height));
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false;
                    importer.filterMode = _mode == 0 &&
                        (_settings.red == GeneratedChannel.CenterU || _settings.red == GeneratedChannel.CenterV ||
                         _settings.green == GeneratedChannel.CenterU || _settings.green == GeneratedChannel.CenterV ||
                         _settings.blue == GeneratedChannel.CenterU || _settings.blue == GeneratedChannel.CenterV ||
                         _settings.alpha == GeneratedChannel.CenterU || _settings.alpha == GeneratedChannel.CenterV)
                        ? FilterMode.Point : FilterMode.Bilinear;
                    importer.wrapMode = _mode == 1 || _settings.seamless ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                    importer.SaveAndReimport();
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                }
                else
                {
                    ClearPreview();
                    _preview = generated;
                    generated = null;
                    UpdateDisplay();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (save) EditorUtility.DisplayDialog("Texture Generator", exception.Message, "OK");
                else _previewError = exception.Message;
            }
            finally
            {
                if (generated != null) DestroyImmediate(generated);
                if (save) EditorUtility.ClearProgressBar();
            }
        }

        private void UpdateDisplay()
        {
            if (_display != null) DestroyImmediate(_display);
            if (_preview == null) return;
            Color32[] pixels = _preview.GetPixels32();
            _inputHistogram = Histogram(pixels);
            pixels = AdjustTone(pixels);
            _outputHistogram = Histogram(pixels);
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                byte value = _previewChannel == 1 ? p.r : _previewChannel == 2 ? p.g : _previewChannel == 3 ? p.b : p.a;
                pixels[i] = _previewChannel == 0 ? new Color32(p.r, p.g, p.b, 255) : new Color32(value, value, value, 255);
            }
            _display = new Texture2D(_preview.width, _preview.height, TextureFormat.RGBA32, false, !_preview.isDataSRGB)
                { hideFlags = HideFlags.HideAndDontSave };
            _display.SetPixels32(pixels);
            _display.Apply();
            Repaint();
        }

        private void ClearPreview()
        {
            if (_preview != null) DestroyImmediate(_preview);
            if (_display != null) DestroyImmediate(_display);
            _preview = _display = null;
            _inputHistogram = _outputHistogram = null;
        }
    }
}
#endif

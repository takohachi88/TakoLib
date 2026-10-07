#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TakoLibEditor.Common
{
    /// <summary>
    /// コースティクスの生成設定だけを保存する。PNG生成は明示的な出力操作時に行う。
    /// </summary>
    [CreateAssetMenu(fileName = "New Caustics Texture", menuName = "2D/Caustics Texture")]
    public sealed class CausticsTexturePreset : ScriptableObject
    {
        private const long MaximumFramePixels = 32L * 1024L * 1024L;
        private const int MaximumAtlasSize = 16384;
        [SerializeField] private CausticsTextureSettings _settings = new();
        [SerializeField, Min(1)] private int _atlasColumns = 4;
        [SerializeField, Tooltip("有効にするとアトラスPNGのみを出力します。無効の場合、Spriteが無効なら連番PNGも出力し、Spriteが有効ならアトラスをMultipleモードで分割します。")] private bool _atlasOnly = true;
        [SerializeField] private FilterMode _filterMode = FilterMode.Bilinear;
        [SerializeField] private TextureImporterFormat _format = TextureImporterFormat.RGB24;
        [SerializeField] private bool _sprite;
        [SerializeField, Tooltip("保存ダイアログで指定した前回のファイル名です。ダイアログの初期ファイル名にはScriptableObject名を使用します。")] private string _baseFileName = string.Empty;
        [SerializeField, Tooltip("Assetsフォルダからの相対パスです。名前を付けて保存する際の初期フォルダとして使用します。")] private string _directory = string.Empty;
        public CausticsTextureSettings Settings => _settings;
        public int AtlasColumns => Mathf.Max(1, _atlasColumns);
        public bool AtlasOnly => _atlasOnly;

        /// <summary>
        /// Inspectorの入力値を安全な範囲に収めた設定を返す。
        /// </summary>
        public CausticsTextureSettings CreateNormalizedSettings()
        {
            CausticsTextureSettings settings = _settings?.Copy() ?? new CausticsTextureSettings();
            settings.Width = Mathf.Clamp(settings.Width, 32, 2048);
            settings.Height = Mathf.Clamp(settings.Height, 32, 2048);
            settings.FrameCount = Mathf.Clamp(settings.FrameCount, 1, 256);
            settings.Supersampling = Mathf.Clamp(settings.Supersampling, 1, 4);
            settings.WaveCount = Mathf.Clamp(settings.WaveCount, 4, 32);
            settings.PatternScale = Mathf.Clamp(settings.PatternScale, 1, 12);
            settings.AnimationSpeed = Mathf.Clamp(settings.AnimationSpeed, 0f, 2f);
            settings.RefractionStrength = Mathf.Clamp(settings.RefractionStrength, 0f, 0.25f);
            settings.ChromaticAberration = Mathf.Clamp(settings.ChromaticAberration, 0f, 0.5f);
            settings.BlurRadius = Mathf.Clamp(settings.BlurRadius, 0, 16);
            settings.BlackPoint = Mathf.Clamp(settings.BlackPoint, 0f, 2f);
            settings.Exposure = Mathf.Clamp(settings.Exposure, 0.1f, 20f);
            settings.Contrast = Mathf.Clamp(settings.Contrast, 0.1f, 4f);
            return settings;
        }

        public static string ValidateSettings(CausticsTextureSettings settings, int atlasColumns)
        {
            string validationError = settings.Validate();
            if (validationError != null)
                return validationError;

            atlasColumns = Mathf.Clamp(atlasColumns, 1, settings.FrameCount);
            int atlasRows = Mathf.CeilToInt((float)settings.FrameCount / atlasColumns);
            int atlasWidth = settings.Width * atlasColumns;
            int atlasHeight = settings.Height * atlasRows;
            int maximumTextureSize = Mathf.Min(MaximumAtlasSize, SystemInfo.maxTextureSize);
            if (atlasWidth > maximumTextureSize || atlasHeight > maximumTextureSize)
            {
                return $"Atlas size {atlasWidth} × {atlasHeight} exceeds the maximum " +
                       $"texture size of {maximumTextureSize}.";
            }

            long framePixels = (long)settings.Width * settings.Height * settings.FrameCount;
            if (framePixels > MaximumFramePixels)
            {
                return $"The sequence contains {framePixels:N0} pixels. Reduce the resolution " +
                       $"or frame count below the {MaximumFramePixels:N0}-pixel safety limit.";
            }

            return null;
        }

        private static void CopyFrameToAtlas(
            Color32[] framePixels,
            int frameIndex,
            int frameWidth,
            int frameHeight,
            int atlasColumns,
            int atlasRows,
            Color32[] atlasPixels,
            int atlasWidth)
        {
            int column = frameIndex % atlasColumns;
            int topDownRow = frameIndex / atlasColumns;
            int atlasRow = atlasRows - 1 - topDownRow;
            for (int y = 0; y < frameHeight; y++)
            {
                int sourceOffset = y * frameWidth;
                int destinationOffset = (atlasRow * frameHeight + y) * atlasWidth + column * frameWidth;
                Array.Copy(framePixels, sourceOffset, atlasPixels, destinationOffset, frameWidth);
            }
        }

        private static string ValidateOutput(string baseName, string directory)
        {
            if (string.IsNullOrWhiteSpace(baseName) || baseName == "." || baseName == ".." || baseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return "Base File Name must be a valid file name without a directory.";
            if (string.IsNullOrWhiteSpace(directory)) return null;
            try
            {
                string assetsRoot = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, directory));
                if (Path.IsPathRooted(directory) || !(path + Path.DirectorySeparatorChar).StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
                    return "Directory must be a relative path inside Assets.";
            }
            catch (Exception) { return "Directory is not a valid path."; }
            return null;
        }

        private static Texture2D CreatePngTexture(int width, int height, Color32[] pixels, bool linear)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false, linear);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                return texture;
            }
            catch { DestroyImmediate(texture); throw; }
        }

        private static byte[] EncodePng(int width, int height, Color32[] pixels, bool linear)
        {
            Texture2D texture = CreatePngTexture(width, height, pixels, linear);
            try { return texture.EncodeToPNG(); }
            finally { DestroyImmediate(texture); }
        }

        /// <summary>
        /// 設定アセットには画像を保存せず、要求されたときだけPNGを生成する。
        /// </summary>
        public void ExportPngFiles()
        {
            CausticsTextureSettings settings = CreateNormalizedSettings();
            int columns = Mathf.Clamp(_atlasColumns, 1, settings.FrameCount);
            string error = ValidateOutput(name, _directory) ?? ValidateSettings(settings, columns);
            if (error != null) throw new InvalidOperationException(error);
            if (!Enum.IsDefined(typeof(TextureImporterFormat), _format))
                throw new InvalidOperationException("Choose a supported texture import format.");

            string initialDirectory = string.IsNullOrWhiteSpace(_directory) ? Application.dataPath : Path.GetFullPath(Path.Combine(Application.dataPath, _directory));
            if (!Directory.Exists(initialDirectory)) initialDirectory = Application.dataPath;
            string atlasPath = EditorUtility.SaveFilePanel("Export Caustics PNG", initialDirectory, name, "png");
            if (string.IsNullOrEmpty(atlasPath)) return;
            atlasPath = Path.GetFullPath(atlasPath);
            string outputDirectory = Path.GetDirectoryName(atlasPath);
            string baseName = Path.GetFileNameWithoutExtension(atlasPath);
            error = ValidateOutput(baseName, string.Empty);
            if (error != null) throw new InvalidOperationException(error);
            string assetsRoot = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            outputDirectory = Path.GetFullPath(outputDirectory);
            if (!(outputDirectory + Path.DirectorySeparatorChar).StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Select a directory inside Assets so texture import settings can be applied.");

            string relativeDirectory = outputDirectory.Length == assetsRoot.Length - 1 ? string.Empty : outputDirectory.Substring(assetsRoot.Length).Replace('\\', '/');
            if (_directory != relativeDirectory || _baseFileName != baseName)
            {
                Undo.RecordObject(this, "Set Caustics Output Path");
                _directory = relativeDirectory;
                _baseFileName = baseName;
                EditorUtility.SetDirty(this);
            }
            int rows = Mathf.CeilToInt((float)settings.FrameCount / columns);
            int atlasWidth = settings.Width * columns;
            int atlasHeight = settings.Height * rows;
            List<string> paths = new() { atlasPath };
            bool exportFrames = !_atlasOnly && !_sprite;
            if (exportFrames)
                for (int i = 0; i < settings.FrameCount; i++) paths.Add(Path.Combine(outputDirectory, $"{baseName}_{i:D3}.png"));
            if (paths.Any(File.Exists) && !EditorUtility.DisplayDialog("Overwrite Existing Files?", "One or more PNG files already exist and will be overwritten.", "Overwrite", "Cancel")) return;

            // キャンセル時に既存画像を変更しないよう、全フレームの生成後に書き出す。
            List<byte[]> framePngs = new();
            Color32[] atlasPixels = new Color32[atlasWidth * atlasHeight];
            try
            {
                using (CausticsFrameGenerator generator = new(settings))
                {
                    for (int i = 0; i < settings.FrameCount; i++)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar("Generate Caustics PNG", $"Frame {i + 1} / {settings.FrameCount}", (float)i / settings.FrameCount)) return;
                        Color32[] pixels = generator.GenerateFrame(i);
                        CopyFrameToAtlas(pixels, i, settings.Width, settings.Height, columns, rows, atlasPixels, atlasWidth);
                        if (exportFrames) framePngs.Add(EncodePng(settings.Width, settings.Height, pixels, settings.Linear));
                    }
                }
                byte[] atlasPng = EncodePng(atlasWidth, atlasHeight, atlasPixels, settings.Linear);
                Directory.CreateDirectory(outputDirectory);
                File.WriteAllBytes(atlasPath, atlasPng);
                for (int i = 0; i < framePngs.Count; i++) File.WriteAllBytes(paths[i + 1], framePngs[i]);
                foreach (string path in paths)
                {
                    string assetPath = "Assets/" + Path.GetFullPath(path).Substring(assetsRoot.Length).Replace('\\', '/');
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                    ConfigurePngImporter(assetPath, settings, _format, path == atlasPath, columns, rows);
                }
                Debug.Log($"Exported {paths.Count} caustics PNG file(s) to {outputDirectory}.", this);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private void ConfigurePngImporter(string path, CausticsTextureSettings settings, TextureImporterFormat format, bool atlas, int columns, int rows)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"PNG importer could not be loaded: {path}");
            importer.textureType = _sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.spriteImportMode = _sprite ? (atlas && !_atlasOnly ? SpriteImportMode.Multiple : SpriteImportMode.Single) : SpriteImportMode.None;
            importer.filterMode = _filterMode;
            importer.sRGBTexture = !settings.Linear;
            importer.mipmapEnabled = settings.GenerateMipmaps;
            importer.alphaIsTransparency = settings.AlphaFromIntensity;
            importer.npotScale = TextureImporterNPOTScale.None;
            TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
            platform.maxTextureSize = Mathf.NextPowerOfTwo(atlas ? Mathf.Max(settings.Width * columns, settings.Height * rows) : Mathf.Max(settings.Width, settings.Height));
            platform.format = format;
            importer.SetPlatformTextureSettings(platform);
            importer.SaveAndReimport();
            if (_sprite && atlas && !_atlasOnly) SliceAtlas(importer, settings, columns, rows);
        }

        private static void SliceAtlas(TextureImporter importer, CausticsTextureSettings settings, int columns, int rows)
        {
            SpriteDataProviderFactories factories = new();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Sprite editing is not supported by the PNG importer.");
            provider.InitSpriteEditorDataProvider();
            ISpriteFrameEditCapability capability = provider.GetDataProvider<ISpriteFrameEditCapability>();
            if (capability == null || !capability.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite))
                throw new InvalidOperationException("The PNG importer does not support sprite slicing.");
            Dictionary<string, GUID> ids = provider.GetSpriteRects().GroupBy(rect => rect.name).ToDictionary(group => group.Key, group => group.First().spriteID);
            SpriteRect[] rects = new SpriteRect[settings.FrameCount];
            for (int i = 0; i < rects.Length; i++)
            {
                string frameName = $"Frame_{i:D3}";
                rects[i] = new SpriteRect
                {
                    name = frameName,
                    spriteID = ids.TryGetValue(frameName, out GUID id) ? id : GUID.Generate(),
                    rect = new Rect(i % columns * settings.Width, (rows - 1 - i / columns) * settings.Height, settings.Width, settings.Height),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                };
            }
            provider.SetSpriteRects(rects);
            ISpriteNameFileIdDataProvider names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        /// <summary>
        /// GPUが利用できない、または生成に失敗した場合にはCPUへ切り替える。
        /// </summary>
        private sealed class CausticsFrameGenerator : IDisposable
        {
            private readonly CausticsTextureSettings _settings;
            private CausticsTextureComputeGenerator _compute;
            public CausticsFrameGenerator(CausticsTextureSettings settings)
            {
                _settings = settings;
                if (!SystemInfo.supportsComputeShaders) return;
                ComputeShader shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(CausticsTextureComputeGenerator.ComputeShaderPath);
                if (shader == null) return;
                try { _compute = new CausticsTextureComputeGenerator(shader, settings); }
                catch (Exception exception) { Debug.LogWarning($"GPU initialization failed; CPU generation will be used: {exception.Message}"); }
            }
            public Color32[] GenerateFrame(int frame)
            {
                if (_compute != null)
                {
                    try { return _compute.GenerateFrame(frame); }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"GPU generation failed; CPU generation will be used: {exception.Message}");
                        Dispose();
                    }
                }
                return CausticsTextureGenerator.GenerateFrame(_settings, frame);
            }
            public void Dispose() { _compute?.Dispose(); _compute = null; }
        }

        [CustomEditor(typeof(CausticsTexturePreset))]
        public sealed class CausticsTexturePresetEditor : UnityEditor.Editor
        {
            private SerializedProperty _settingsProperty;
            private SerializedProperty _atlasColumnsProperty;
            private SerializedProperty _atlasOnlyProperty;
            private SerializedProperty _filterModeProperty;
            private SerializedProperty _formatProperty;
            private SerializedProperty _spriteProperty;
            private int _previewFrame;

            private Texture2D _previewTexture;
            private Color32[][] _previewFrames;
            private int _displayedPreviewFrame = -1;
            private bool _previewEnabled;
            private string _previewSettingsJson;
            private double _previewUpdateTime = -1d;
            private SerializedProperty _baseFileNameProperty;
            private SerializedProperty _directoryProperty;
            private CausticsTexturePreset Preset => (CausticsTexturePreset)target;
            private void OnEnable()
            {
                Undo.undoRedoPerformed += OnUndoRedo;
                EditorApplication.update += UpdatePreview;
            }
            private void OnDisable()
            {
                Undo.undoRedoPerformed -= OnUndoRedo;
                EditorApplication.update -= UpdatePreview;
                ClearPreview();
            }
            private void OnUndoRedo() { RequestPreviewUpdate(); Repaint(); }
            private void RequestPreviewUpdate()
            {
                if (!_previewEnabled || target == null) return;
                string settingsJson = JsonUtility.ToJson(Preset.CreateNormalizedSettings());
                if (_previewSettingsJson == settingsJson) return;
                _previewSettingsJson = settingsJson;
                ClearPreview();
                // 連続したスライダー操作では最後の変更から少し待ち、不要な再生成をまとめる。
                _previewUpdateTime = Preset.Settings.FrameCount > 1 ? EditorApplication.timeSinceStartup + 0.3d : -1d;
            }
            private void UpdatePreview()
            {
                if (target == null || _previewUpdateTime < 0d || EditorApplication.timeSinceStartup < _previewUpdateTime) return;
                GeneratePreview();
                Repaint();
            }
            private void ClearPreview()
            {
                if (_previewTexture != null) DestroyImmediate(_previewTexture);
                _previewTexture = null;
                _previewFrames = null;
                _displayedPreviewFrame = -1;
            }

            private void EnsureProperties()
            {
                if (_settingsProperty != null)
                    return;

                _baseFileNameProperty = serializedObject.FindProperty(nameof(_baseFileName));
                _directoryProperty = serializedObject.FindProperty(nameof(_directory));
                _settingsProperty = serializedObject.FindProperty(nameof(CausticsTexturePreset._settings));
                _atlasColumnsProperty = serializedObject.FindProperty(nameof(CausticsTexturePreset._atlasColumns));
                _atlasOnlyProperty = serializedObject.FindProperty(nameof(CausticsTexturePreset._atlasOnly));
                _filterModeProperty = serializedObject.FindProperty(nameof(CausticsTexturePreset._filterMode));
                _formatProperty = serializedObject.FindProperty(nameof(CausticsTexturePreset._format));
                _spriteProperty = serializedObject.FindProperty(nameof(CausticsTexturePreset._sprite));
            }

            public override void OnInspectorGUI()
            {
                EnsureProperties();
                serializedObject.Update();

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(_baseFileNameProperty, new GUIContent("Last Export File Name", _baseFileNameProperty.tooltip));
                EditorGUILayout.PropertyField(_directoryProperty, new GUIContent("Directory", _directoryProperty.tooltip));
                DrawSequenceSettings();
                EditorGUILayout.Space(8f);
                DrawSimulationSettings();
                EditorGUILayout.Space(8f);
                DrawAppearanceSettings();
                EditorGUILayout.Space(8f);
                DrawTextureSettings();
                if (serializedObject.ApplyModifiedProperties()) RequestPreviewUpdate();

                if (FindSetting(nameof(CausticsTextureSettings.FrameCount)).intValue > 1)
                {
                    EditorGUILayout.Space(8f);
                    DrawInlinePreview();
                }

                string validationError = GetValidationError();
                if (validationError != null)
                    EditorGUILayout.HelpBox(validationError, MessageType.Error);

                EditorGUILayout.Space(8f);

                using (new EditorGUI.DisabledScope(validationError != null))
                {
                    if (GUILayout.Button("Export PNG...", GUILayout.Height(26f)))
                    {
                        try { Preset.ExportPngFiles(); }
                        catch (Exception exception) { Debug.LogException(exception, Preset); EditorUtility.DisplayDialog("Export Caustics PNG", exception.Message, "OK"); }
                    }
                }
            }

            private void DrawInlinePreview()
            {
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                int frameCount = Mathf.Max(1, FindSetting(nameof(CausticsTextureSettings.FrameCount)).intValue);
                _previewFrame = EditorGUILayout.IntSlider("Frame", Mathf.Clamp(_previewFrame, 0, frameCount - 1), 0, frameCount - 1);
                if (GUILayout.Button("Generate Preview")) GeneratePreview();
                if (_previewUpdateTime >= 0d) EditorGUILayout.LabelField("Updating preview...", EditorStyles.centeredGreyMiniLabel);
                if (_previewTexture == null) return;
                float width = Mathf.Max(64f, EditorGUIUtility.currentViewWidth - 42f);
                float height = Mathf.Min(320f, width * _previewTexture.height / _previewTexture.width);
                Rect rect = FitAspectRect(EditorGUILayout.GetControlRect(false, height), (float)_previewTexture.width / _previewTexture.height);
                Event current = Event.current;
                if (current.type == EventType.ScrollWheel && rect.Contains(current.mousePosition) && current.delta.y != 0f)
                {
                    _previewFrame = Mathf.Clamp(_previewFrame + (current.delta.y > 0f ? 1 : -1), 0, _previewFrames.Length - 1);
                    current.Use();
                    Repaint();
                }
                if (_displayedPreviewFrame != _previewFrame)
                {
                    _previewTexture.SetPixels32(_previewFrames[_previewFrame]);
                    _previewTexture.Apply(false, false);
                    _displayedPreviewFrame = _previewFrame;
                }
                GUI.DrawTexture(rect, _previewTexture, ScaleMode.ScaleToFit, true);
            }

            private void GeneratePreview()
            {
                _previewEnabled = true;
                _previewUpdateTime = -1d;
                ClearPreview();
                try
                {
                    CausticsTextureSettings settings = Preset.CreateNormalizedSettings();
                    _previewSettingsJson = JsonUtility.ToJson(settings);
                    _previewFrame = Mathf.Clamp(_previewFrame, 0, settings.FrameCount - 1);
                    if ((long)settings.Width * settings.Height * settings.FrameCount > MaximumFramePixels)
                        throw new InvalidOperationException($"Preview exceeds the {MaximumFramePixels:N0}-pixel safety limit. Reduce resolution or frame count.");
                    Color32[][] frames = new Color32[settings.FrameCount][];
                    using CausticsFrameGenerator generator = new(settings);
                    for (int i = 0; i < frames.Length; i++)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar("Generate Caustics Preview", $"Frame {i + 1} / {frames.Length}", (float)i / frames.Length))
                        {
                            _previewEnabled = false;
                            return;
                        }
                        frames[i] = generator.GenerateFrame(i);
                    }
                    // 生成済み画素を保持し、フレーム切り替えでは再計算せず表示用Textureだけを更新する。
                    _previewFrames = frames;
                    _previewTexture = CreatePngTexture(settings.Width, settings.Height, frames[_previewFrame], settings.Linear);
                    _previewTexture.hideFlags = HideFlags.HideAndDontSave;
                    _displayedPreviewFrame = _previewFrame;
                }
                catch (Exception exception) { ClearPreview(); Debug.LogException(exception, Preset); }
                finally { EditorUtility.ClearProgressBar(); }
            }

            private static Rect FitAspectRect(Rect rect, float aspect)
            {
                float availableAspect = rect.width / Mathf.Max(1f, rect.height);
                if (availableAspect > aspect)
                {
                    float width = rect.height * aspect;
                    rect.x += (rect.width - width) * 0.5f;
                    rect.width = width;
                }
                else
                {
                    float height = rect.width / Mathf.Max(0.0001f, aspect);
                    rect.y += (rect.height - height) * 0.5f;
                    rect.height = height;
                }

                return rect;
            }

            private void DrawSequenceSettings()
            {
                EditorGUILayout.LabelField("Sequence and Atlas", EditorStyles.boldLabel);
                SerializedProperty width = FindSetting(nameof(CausticsTextureSettings.Width));
                SerializedProperty height = FindSetting(nameof(CausticsTextureSettings.Height));
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Resolution");
                    width.intValue = EditorGUILayout.IntField(width.intValue);
                    GUILayout.Label("×", GUILayout.Width(14f));
                    height.intValue = EditorGUILayout.IntField(height.intValue);
                }
                width.intValue = Mathf.Clamp(width.intValue, 32, 2048);
                height.intValue = Mathf.Clamp(height.intValue, 32, 2048);

                SerializedProperty frameCount = FindSetting(nameof(CausticsTextureSettings.FrameCount));
                frameCount.intValue = EditorGUILayout.IntSlider("Frame Count", frameCount.intValue, 1, 256);
                _atlasColumnsProperty.intValue = EditorGUILayout.IntSlider("Atlas Columns", Mathf.Clamp(_atlasColumnsProperty.intValue, 1, frameCount.intValue), 1, frameCount.intValue);
                EditorGUILayout.PropertyField(_atlasOnlyProperty, new GUIContent("Atlas Only", _atlasOnlyProperty.tooltip));

                int rows = Mathf.CeilToInt((float)frameCount.intValue / _atlasColumnsProperty.intValue);
                EditorGUILayout.LabelField(
                    "Atlas Size",
                    $"{width.intValue * _atlasColumnsProperty.intValue} × " +
                    $"{height.intValue * rows} px");
            }

            private void DrawSimulationSettings()
            {
                EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);
                DrawIntSlider(nameof(CausticsTextureSettings.Supersampling), "Supersampling", 1, 4);
                DrawIntSlider(nameof(CausticsTextureSettings.WaveCount), "Wave Count", 4, 32);
                DrawIntSlider(nameof(CausticsTextureSettings.PatternScale), "Pattern Scale", 1, 12);
                EditorGUILayout.PropertyField(FindSetting(nameof(CausticsTextureSettings.Seed)), new GUIContent("Seed"));
                SerializedProperty animationSpeed = FindSetting(nameof(CausticsTextureSettings.AnimationSpeed));
                animationSpeed.floatValue = EditorGUILayout.Slider(new GUIContent("Animation Speed", animationSpeed.tooltip), animationSpeed.floatValue, 0f, 2f);
                DrawSlider(nameof(CausticsTextureSettings.RefractionStrength), "Refraction Strength", 0f, 0.25f);
            }

            private void DrawAppearanceSettings()
            {
                EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
                DrawSlider(nameof(CausticsTextureSettings.BlackPoint), "Black Point", 0f, 2f);
                DrawSlider(nameof(CausticsTextureSettings.Exposure), "Exposure", 0.1f, 20f);
                DrawSlider(nameof(CausticsTextureSettings.Contrast), "Contrast", 0.1f, 4f);
                DrawSlider(nameof(CausticsTextureSettings.ChromaticAberration), "Chromatic Aberration", 0f, 0.5f);
                DrawIntSlider(nameof(CausticsTextureSettings.BlurRadius), "Periodic Blur", 0, 16);
                EditorGUILayout.PropertyField(FindSetting(nameof(CausticsTextureSettings.Tint)), new GUIContent("Tint"));
                EditorGUILayout.PropertyField(FindSetting(nameof(CausticsTextureSettings.AlphaFromIntensity)), new GUIContent("Intensity to Alpha"));
            }

            private void DrawTextureSettings()
            {
                EditorGUILayout.LabelField("Texture", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_filterModeProperty, new GUIContent("Filter Mode"));
                EditorGUILayout.PropertyField(_formatProperty, new GUIContent("Format"));
                EditorGUILayout.PropertyField(_spriteProperty, new GUIContent("Sprite", _spriteProperty.tooltip));
                EditorGUILayout.PropertyField(FindSetting(nameof(CausticsTextureSettings.Linear)), new GUIContent("Linear Texture"));
                EditorGUILayout.PropertyField(FindSetting(nameof(CausticsTextureSettings.GenerateMipmaps)), new GUIContent("Generate Mipmaps"));

            }

            private SerializedProperty FindSetting(string propertyName)
            {
                return _settingsProperty.FindPropertyRelative(propertyName);
            }

            private void DrawIntSlider(string propertyName, string label, int minimum, int maximum)
            {
                SerializedProperty property = FindSetting(propertyName);
                property.intValue = EditorGUILayout.IntSlider(label, property.intValue, minimum, maximum);
            }

            private void DrawSlider(string propertyName, string label, float minimum, float maximum)
            {
                SerializedProperty property = FindSetting(propertyName);
                property.floatValue = EditorGUILayout.Slider(label, property.floatValue, minimum, maximum);
            }

            private string GetValidationError()
            {
                CausticsTextureSettings settings = new()
                {
                    Width = FindSetting(nameof(CausticsTextureSettings.Width)).intValue,
                    Height = FindSetting(nameof(CausticsTextureSettings.Height)).intValue,
                    FrameCount = FindSetting(nameof(CausticsTextureSettings.FrameCount)).intValue,
                    Supersampling = FindSetting(nameof(CausticsTextureSettings.Supersampling)).intValue,
                    WaveCount = FindSetting(nameof(CausticsTextureSettings.WaveCount)).intValue,
                    PatternScale = FindSetting(nameof(CausticsTextureSettings.PatternScale)).intValue,
                    AnimationSpeed = FindSetting(nameof(CausticsTextureSettings.AnimationSpeed)).floatValue,
                    RefractionStrength = FindSetting(nameof(CausticsTextureSettings.RefractionStrength)).floatValue,
                    ChromaticAberration = FindSetting(nameof(CausticsTextureSettings.ChromaticAberration)).floatValue,
                    BlurRadius = FindSetting(nameof(CausticsTextureSettings.BlurRadius)).intValue,
                    BlackPoint = FindSetting(nameof(CausticsTextureSettings.BlackPoint)).floatValue,
                    Exposure = FindSetting(nameof(CausticsTextureSettings.Exposure)).floatValue,
                    Contrast = FindSetting(nameof(CausticsTextureSettings.Contrast)).floatValue,
                };
                if (!Enum.IsDefined(typeof(TextureImporterFormat), (TextureImporterFormat)_formatProperty.intValue))
                    return "The selected format is not supported by the PNG importer.";
                return ValidateOutput(Preset.name, _directoryProperty.stringValue) ?? ValidateSettings(settings, _atlasColumnsProperty.intValue);
            }

        }
    }
}

#endif


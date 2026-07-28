#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TakoLibEditor.Common
{
    /// <summary>
    /// ループ可能なコースティクス連番を生成し、PNGとして出力するエディターウィンドウ。
    /// </summary>
    public sealed class CausticsTextureGeneratorWindow : EditorWindow
    {
        private const string MenuPath = "Tools/TakoLib/Caustics Texture Generator";
        private const long MaximumFramePixels = 32L * 1024L * 1024L;

        [SerializeField] private CausticsTextureSettings _settings = new();
        [SerializeField] private CausticsTexturePreset _preset;
        [SerializeField] private int _atlasColumns = 4;
        [SerializeField] private string _fileName = "Caustics";
        [SerializeField] private string _outputDirectory = string.Empty;
        [SerializeField] private bool _atlasOnly;
        [SerializeField] private int _previewFrame;
        [SerializeField] private Vector2 _scrollPosition;

        private Texture2D _previewTexture;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            CausticsTextureGeneratorWindow window =
                GetWindow<CausticsTextureGeneratorWindow>("Caustics Generator");
            window.minSize = new Vector2(430f, 650f);
            window.Show();
        }

        private void OnDisable()
        {
            DestroyPreview();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Loopable Caustics Texture", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generates a periodic refracted-light simulation. Texture edges tile seamlessly, " +
                "and the final frame advances into frame 0 at the same interval as all other frames.",
                MessageType.Info);

            DrawPresetSettings();
            EditorGUILayout.Space(8f);
            DrawOutputSettings();
            EditorGUILayout.Space(8f);
            DrawSequenceSettings();
            EditorGUILayout.Space(8f);
            DrawSimulationSettings();
            EditorGUILayout.Space(8f);
            DrawAppearanceSettings();
            EditorGUILayout.Space(8f);
            DrawPreview();
            EditorGUILayout.Space(8f);
            DrawGenerateButton();

            EditorGUILayout.EndScrollView();
        }

        private void DrawPresetSettings()
        {
            EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
            _preset = (CausticsTexturePreset)EditorGUILayout.ObjectField(
                new GUIContent(
                    "Preset Asset",
                    "A reusable project asset containing generation and atlas settings."),
                _preset,
                typeof(CausticsTexturePreset),
                false);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_preset == null))
                {
                    if (GUILayout.Button("Load"))
                        LoadPreset();
                    if (GUILayout.Button("Update"))
                        UpdatePreset();
                }

                if (GUILayout.Button("Save As New..."))
                    SavePresetAsNew();
            }

            EditorGUILayout.HelpBox(
                "Presets include texture, sequence, atlas, simulation, appearance, output, and import settings. " +
                "The output directory is stored as a path relative to the Assets folder.",
                MessageType.None);
        }

        private void DrawOutputSettings()
        {
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            _fileName = EditorGUILayout.TextField("Base File Name", _fileName);
            _atlasOnly = EditorGUILayout.Toggle(
                new GUIContent(
                    "Atlas Only",
                    "Outputs only the combined atlas PNG and skips individual sequence PNG files."),
                _atlasOnly);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(
                    new GUIContent(
                        "Directory",
                        "Output directory relative to the project's Assets folder."));
                GUILayout.Label("Assets/", GUILayout.Width(46f));
                _outputDirectory = EditorGUILayout.TextField(_outputDirectory);
                if (GUILayout.Button("Browse", GUILayout.Width(70f)))
                {
                    string selected = EditorUtility.OpenFolderPanel(
                        "Select Caustics Output Directory",
                        GetDirectoryPickerStartPath(),
                        string.Empty);
                    if (!string.IsNullOrEmpty(selected))
                        SetOutputDirectoryFromAbsolutePath(selected);
                }
            }

            _settings.Linear = EditorGUILayout.Toggle(
                new GUIContent(
                    "Linear Texture",
                    "Imports generated textures with sRGB disabled when output is inside Assets."),
                _settings.Linear);
            _settings.GenerateMipmaps = EditorGUILayout.Toggle("Generate Mipmaps", _settings.GenerateMipmaps);
        }

        private void DrawSequenceSettings()
        {
            EditorGUILayout.LabelField("Sequence and Atlas", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Resolution");
                _settings.Width = EditorGUILayout.IntField(_settings.Width);
                GUILayout.Label("×", GUILayout.Width(14f));
                _settings.Height = EditorGUILayout.IntField(_settings.Height);
            }

            _settings.Width = Mathf.Clamp(_settings.Width, 32, 2048);
            _settings.Height = Mathf.Clamp(_settings.Height, 32, 2048);
            _settings.FrameCount = EditorGUILayout.IntSlider("Frame Count", _settings.FrameCount, 1, 256);
            _atlasColumns = EditorGUILayout.IntSlider(
                new GUIContent("Atlas Columns", "Frames are arranged left-to-right, then top-to-bottom."),
                Mathf.Clamp(_atlasColumns, 1, _settings.FrameCount),
                1,
                _settings.FrameCount);

            int rows = Mathf.CeilToInt((float)_settings.FrameCount / _atlasColumns);
            long framePixels = (long)_settings.Width * _settings.Height * _settings.FrameCount;
            EditorGUILayout.LabelField(
                "Atlas Size",
                $"{_settings.Width * _atlasColumns} × {_settings.Height * rows} px");
            EditorGUILayout.LabelField(
                "Sequence Memory",
                $"{framePixels * 4.0 / (1024.0 * 1024.0):0.0} MiB (+ atlas working memory)");
            EditorGUILayout.HelpBox(
                _atlasOnly
                    ? "File: Name_Atlas.png. Atlas frame 0 is at the top-left."
                    : "Files: Name_000.png … and Name_Atlas.png. Atlas frame 0 is at the top-left.",
                MessageType.None);
        }

        private void DrawSimulationSettings()
        {
            EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);
            _settings.Supersampling = EditorGUILayout.IntSlider(
                new GUIContent(
                    "Supersampling",
                    "Samples per axis. 2 is recommended; 3–4 improves thin highlights but takes longer."),
                _settings.Supersampling,
                1,
                4);
            _settings.WaveCount = EditorGUILayout.IntSlider("Wave Count", _settings.WaveCount, 4, 32);
            _settings.PatternScale = EditorGUILayout.IntSlider(
                new GUIContent(
                    "Pattern Scale",
                    "Approximate number of large caustic cells across one tile."),
                _settings.PatternScale,
                1,
                12);
            _settings.Seed = EditorGUILayout.IntField("Seed", _settings.Seed);
            _settings.RefractionStrength = EditorGUILayout.Slider(
                new GUIContent(
                    "Refraction Strength",
                    "Higher values produce tighter folds and brighter caustic lines."),
                _settings.RefractionStrength,
                0f,
                0.25f);
        }

        private void DrawAppearanceSettings()
        {
            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
            _settings.BlackPoint = EditorGUILayout.Slider(
                new GUIContent(
                    "Black Point",
                    "Removes the unfocused background illumination."),
                _settings.BlackPoint,
                0f,
                2f);
            _settings.Exposure = EditorGUILayout.Slider("Exposure", _settings.Exposure, 0.1f, 10f);
            _settings.Contrast = EditorGUILayout.Slider("Contrast", _settings.Contrast, 0.1f, 4f);
            _settings.ChromaticAberration = EditorGUILayout.Slider(
                new GUIContent(
                    "Chromatic Aberration",
                    "Separates RGB refraction strengths. Set to 0 for monochrome caustics."),
                _settings.ChromaticAberration,
                0f,
                0.25f);
            _settings.BlurRadius = EditorGUILayout.IntSlider(
                new GUIContent(
                    "Periodic Blur",
                    "Gaussian blur wraps across all texture edges, preserving tiling."),
                _settings.BlurRadius,
                0,
                16);
            _settings.Tint = EditorGUILayout.ColorField(
                new GUIContent("Tint", "Multiplies the generated RGB light."),
                _settings.Tint,
                true,
                false,
                true);
            _settings.AlphaFromIntensity = EditorGUILayout.Toggle(
                new GUIContent(
                    "Intensity to Alpha",
                    "Uses the brightest RGB channel as alpha instead of opaque alpha."),
                _settings.AlphaFromIntensity);
        }

        private void DrawPreview()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            _previewFrame = EditorGUILayout.IntSlider(
                "Frame",
                Mathf.Clamp(_previewFrame, 0, _settings.FrameCount - 1),
                0,
                _settings.FrameCount - 1);

            if (GUILayout.Button("Generate Preview", GUILayout.Height(24f)))
                GeneratePreview();

            if (_previewTexture != null)
            {
                float availableWidth = Mathf.Max(64f, position.width - 40f);
                float aspect = (float)_previewTexture.height / _previewTexture.width;
                Rect previewRect = GUILayoutUtility.GetRect(
                    availableWidth,
                    Mathf.Min(availableWidth * aspect, 420f),
                    GUILayout.ExpandWidth(true));
                EditorGUI.DrawPreviewTexture(previewRect, _previewTexture, null, ScaleMode.ScaleToFit);
            }
        }

        private void DrawGenerateButton()
        {
            string validationError = ValidateExport();
            if (validationError != null)
                EditorGUILayout.HelpBox(validationError, MessageType.Error);

            using (new EditorGUI.DisabledScope(validationError != null))
            {
                if (GUILayout.Button("Generate PNG Sequence and Atlas", GUILayout.Height(34f)))
                    GenerateAndExport();
            }
        }

        private void GeneratePreview()
        {
            string validationError = _settings.Validate();
            if (validationError != null)
            {
                EditorUtility.DisplayDialog("Invalid Settings", validationError, "OK");
                return;
            }

            try
            {
                CausticsTextureSettings previewSettings = _settings.Copy();
                float scale = Mathf.Min(1f, 512f / Mathf.Max(previewSettings.Width, previewSettings.Height));
                previewSettings.Width = Mathf.Max(32, Mathf.RoundToInt(previewSettings.Width * scale));
                previewSettings.Height = Mathf.Max(32, Mathf.RoundToInt(previewSettings.Height * scale));

                EditorUtility.DisplayProgressBar("Caustics Preview", "Simulating refracted light...", 0.5f);
                Color32[] pixels = CausticsTextureGenerator.GenerateFrame(previewSettings, _previewFrame);

                DestroyPreview();
                _previewTexture = new Texture2D(
                    previewSettings.Width,
                    previewSettings.Height,
                    TextureFormat.RGBA32,
                    false,
                    previewSettings.Linear)
                {
                    name = "Caustics Preview",
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                _previewTexture.SetPixels32(pixels);
                _previewTexture.Apply(false, true);
                Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Preview Failed", exception.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void LoadPreset()
        {
            if (_preset == null)
                return;

            _settings = _preset.CreateSettingsCopy();
            _atlasColumns = Mathf.Clamp(_preset.AtlasColumns, 1, _settings.FrameCount);
            _fileName = _preset.BaseFileName;
            _outputDirectory = _preset.OutputDirectory;
            _atlasOnly = _preset.AtlasOnly;
            _previewFrame = Mathf.Clamp(_previewFrame, 0, _settings.FrameCount - 1);
            DestroyPreview();
            Repaint();
        }

        private void UpdatePreset()
        {
            if (_preset == null)
                return;

            Undo.RecordObject(_preset, "Update Caustics Preset");
            _preset.Store(
                _settings,
                _atlasColumns,
                _fileName,
                NormalizeAssetsRelativeDirectory(_outputDirectory),
                _atlasOnly);
            EditorUtility.SetDirty(_preset);
            AssetDatabase.SaveAssetIfDirty(_preset);
        }

        private void SavePresetAsNew()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Caustics Preset",
                "CausticsPreset",
                "asset",
                "Choose where to save the caustics preset.");
            if (string.IsNullOrEmpty(path))
                return;

            CausticsTexturePreset preset = CreateInstance<CausticsTexturePreset>();
            preset.Store(
                _settings,
                _atlasColumns,
                _fileName,
                NormalizeAssetsRelativeDirectory(_outputDirectory),
                _atlasOnly);
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            _preset = preset;
            EditorGUIUtility.PingObject(preset);
        }

        private void GenerateAndExport()
        {
            string validationError = ValidateExport();
            if (validationError != null)
            {
                EditorUtility.DisplayDialog("Invalid Settings", validationError, "OK");
                return;
            }

            string outputDirectory = ResolveOutputDirectory();
            int digits = Mathf.Max(3, (_settings.FrameCount - 1).ToString().Length);
            List<string> outputPaths = BuildOutputPaths(outputDirectory, digits);
            bool hasExistingFiles = outputPaths.Exists(File.Exists);
            if (hasExistingFiles && !EditorUtility.DisplayDialog(
                    "Overwrite Existing Files?",
                    "One or more sequence or atlas files already exist and will be overwritten.",
                    "Overwrite",
                    "Cancel"))
            {
                return;
            }

            Color32[][] frames = new Color32[_settings.FrameCount][];
            try
            {
                for (int frame = 0; frame < frames.Length; frame++)
                {
                    bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                        "Generating Caustics",
                        $"Simulating frame {frame + 1} / {frames.Length}",
                        (float)frame / frames.Length);
                    if (cancelled)
                        return;

                    frames[frame] = CausticsTextureGenerator.GenerateFrame(_settings, frame);
                }

                Directory.CreateDirectory(outputDirectory);
                if (!_atlasOnly)
                    WriteSequence(frames, outputDirectory, digits);
                WriteAtlas(frames, outputDirectory);
                ImportGeneratedTextures(outputPaths);

                string generatedDescription = _atlasOnly
                    ? "one loopable atlas"
                    : $"{frames.Length} loopable frames and one atlas";
                Debug.Log(
                    $"[{nameof(CausticsTextureGeneratorWindow)}] Generated " +
                    $"{generatedDescription} at \"{outputDirectory}\".");
                EditorUtility.DisplayDialog(
                    "Caustics Generated",
                    $"Generated {generatedDescription}.\n\n{outputDirectory}",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Generation Failed", exception.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void WriteSequence(Color32[][] frames, string outputDirectory, int digits)
        {
            Texture2D texture = new(
                _settings.Width,
                _settings.Height,
                TextureFormat.RGBA32,
                false,
                _settings.Linear);
            try
            {
                for (int frame = 0; frame < frames.Length; frame++)
                {
                    EditorUtility.DisplayProgressBar(
                        "Writing Caustics",
                        $"Encoding frame {frame + 1} / {frames.Length}",
                        (float)frame / (frames.Length + 1));
                    texture.SetPixels32(frames[frame]);
                    texture.Apply(false, false);
                    string path = Path.Combine(
                        outputDirectory,
                        $"{SanitizeFileName(_fileName)}_{frame.ToString($"D{digits}")}.png");
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
            }
            finally
            {
                DestroyImmediate(texture);
            }
        }

        private void WriteAtlas(Color32[][] frames, string outputDirectory)
        {
            int rows = Mathf.CeilToInt((float)frames.Length / _atlasColumns);
            int atlasWidth = _settings.Width * _atlasColumns;
            int atlasHeight = _settings.Height * rows;
            Color32[] atlasPixels = new Color32[atlasWidth * atlasHeight];

            for (int frame = 0; frame < frames.Length; frame++)
            {
                int column = frame % _atlasColumns;
                int topDownRow = frame / _atlasColumns;
                int atlasRow = rows - 1 - topDownRow;
                for (int y = 0; y < _settings.Height; y++)
                {
                    int sourceOffset = y * _settings.Width;
                    int destinationOffset =
                        (atlasRow * _settings.Height + y) * atlasWidth +
                        column * _settings.Width;
                    Array.Copy(
                        frames[frame],
                        sourceOffset,
                        atlasPixels,
                        destinationOffset,
                        _settings.Width);
                }
            }

            EditorUtility.DisplayProgressBar(
                "Writing Caustics",
                "Encoding atlas",
                (float)frames.Length / (frames.Length + 1));
            Texture2D atlas = new(
                atlasWidth,
                atlasHeight,
                TextureFormat.RGBA32,
                false,
                _settings.Linear);
            try
            {
                atlas.SetPixels32(atlasPixels);
                atlas.Apply(false, false);
                string path = Path.Combine(
                    outputDirectory,
                    $"{SanitizeFileName(_fileName)}_Atlas.png");
                File.WriteAllBytes(path, atlas.EncodeToPNG());
            }
            finally
            {
                DestroyImmediate(atlas);
            }
        }

        private void ImportGeneratedTextures(IEnumerable<string> paths)
        {
            List<string> assetPaths = new();
            foreach (string path in paths)
            {
                string assetPath = FileUtil.GetProjectRelativePath(path.Replace('\\', '/'));
                if (!string.IsNullOrEmpty(assetPath))
                    assetPaths.Add(assetPath);
            }

            if (assetPaths.Count == 0)
                return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string assetPath in assetPaths)
            {
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                    continue;

                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = _settings.GenerateMipmaps;
                importer.sRGBTexture = !_settings.Linear;
                importer.alphaSource = _settings.AlphaFromIntensity
                    ? TextureImporterAlphaSource.FromInput
                    : TextureImporterAlphaSource.None;
                importer.npotScale = TextureImporterNPOTScale.None;

                int requiredSize = Mathf.NextPowerOfTwo(
                    Mathf.Max(
                        importer.assetPath.EndsWith("_Atlas.png", StringComparison.OrdinalIgnoreCase)
                            ? _settings.Width * _atlasColumns
                            : _settings.Width,
                        importer.assetPath.EndsWith("_Atlas.png", StringComparison.OrdinalIgnoreCase)
                            ? _settings.Height * Mathf.CeilToInt((float)_settings.FrameCount / _atlasColumns)
                            : _settings.Height));
                importer.maxTextureSize = Mathf.Min(16384, requiredSize);
                importer.SaveAndReimport();
            }

            if (assetPaths.Count > 0)
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPaths[^1]);
        }

        private List<string> BuildOutputPaths(string outputDirectory, int digits)
        {
            string safeName = SanitizeFileName(_fileName);
            List<string> paths = new(_atlasOnly ? 1 : _settings.FrameCount + 1);
            if (!_atlasOnly)
            {
                for (int frame = 0; frame < _settings.FrameCount; frame++)
                {
                    paths.Add(Path.Combine(
                        outputDirectory,
                        $"{safeName}_{frame.ToString($"D{digits}")}.png"));
                }
            }
            paths.Add(Path.Combine(outputDirectory, $"{safeName}_Atlas.png"));
            return paths;
        }

        private string ValidateExport()
        {
            string validationError = _settings.Validate();
            if (validationError != null)
                return validationError;
            if (string.IsNullOrWhiteSpace(_fileName))
                return "Base file name is required.";
            if (string.IsNullOrWhiteSpace(_outputDirectory))
                return "Output directory is required.";
            if (!TryResolveOutputDirectory(_outputDirectory, out _))
                return "Output directory must be a relative path inside the project's Assets folder.";

            string safeName = SanitizeFileName(_fileName);
            if (string.IsNullOrWhiteSpace(safeName))
                return "Base file name must contain at least one valid file-name character.";

            int rows = Mathf.CeilToInt((float)_settings.FrameCount / _atlasColumns);
            int atlasWidth = _settings.Width * _atlasColumns;
            int atlasHeight = _settings.Height * rows;
            int maximumTextureSize = Mathf.Min(16384, SystemInfo.maxTextureSize);
            if (atlasWidth > maximumTextureSize || atlasHeight > maximumTextureSize)
            {
                return $"Atlas size {atlasWidth} × {atlasHeight} exceeds the current " +
                       $"maximum texture size of {maximumTextureSize}. Change Atlas Columns or resolution.";
            }

            long framePixels = (long)_settings.Width * _settings.Height * _settings.FrameCount;
            if (framePixels > MaximumFramePixels)
            {
                return $"The sequence contains {framePixels:N0} pixels. Reduce resolution or frame count " +
                       $"below the {MaximumFramePixels:N0}-pixel safety limit.";
            }

            return null;
        }

        private string ResolveOutputDirectory()
        {
            return TryResolveOutputDirectory(_outputDirectory, out string outputDirectory)
                ? outputDirectory
                : Application.dataPath;
        }

        private string GetDirectoryPickerStartPath()
        {
            return TryResolveOutputDirectory(_outputDirectory, out string outputDirectory)
                ? outputDirectory
                : Application.dataPath;
        }

        private void SetOutputDirectoryFromAbsolutePath(string selectedDirectory)
        {
            string assetsDirectory = Path.GetFullPath(Application.dataPath);
            string selected = Path.GetFullPath(selectedDirectory);
            string relativePath = Path.GetRelativePath(assetsDirectory, selected)
                .Replace('\\', '/');

            if (!TryResolveOutputDirectory(relativePath, out _))
            {
                EditorUtility.DisplayDialog(
                    "Invalid Output Directory",
                    "Select the Assets folder or one of its subdirectories.",
                    "OK");
                return;
            }

            _outputDirectory = relativePath;
        }

        private static bool TryResolveOutputDirectory(
            string assetsRelativePath,
            out string outputDirectory)
        {
            outputDirectory = null;
            if (string.IsNullOrWhiteSpace(assetsRelativePath) ||
                Path.IsPathRooted(assetsRelativePath))
            {
                return false;
            }

            try
            {
                string assetsDirectory = Path.GetFullPath(Application.dataPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string candidate = Path.GetFullPath(
                    Path.Combine(assetsDirectory, assetsRelativePath));
                bool isAssetsDirectory = string.Equals(
                    candidate,
                    assetsDirectory,
                    StringComparison.OrdinalIgnoreCase);
                bool isAssetsChild = candidate.StartsWith(
                    assetsDirectory + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase);
                if (!isAssetsDirectory && !isAssetsChild)
                    return false;

                outputDirectory = candidate;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string NormalizeAssetsRelativeDirectory(string assetsRelativePath)
        {
            if (!TryResolveOutputDirectory(assetsRelativePath, out string outputDirectory))
                return string.Empty;

            return Path.GetRelativePath(Application.dataPath, outputDirectory)
                .Replace('\\', '/');
        }

        private static string SanitizeFileName(string fileName)
        {
            string result = fileName ?? string.Empty;
            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
                result = result.Replace(invalidCharacter.ToString(), string.Empty);
            return result.Trim();
        }

        private void DestroyPreview()
        {
            if (_previewTexture == null)
                return;
            DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }
    }
}

#endif

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace TakoLibEditor.Common
{
    /// <summary>
    /// 専用ファイルをコースティクスのアトラステクスチャとして読み込むImporter。
    /// 設定はImporterに保存され、生成済みTexture2Dがメインアセットになる。
    /// </summary>
    [ScriptedImporter(2, Extension)]
    public sealed class CausticsTextureImporter : ScriptedImporter
    {
        public const string Extension = "causticstexture";

        private const string MenuPath = "Assets/Create/2D/Caustics Texture";
        private const long MaximumFramePixels = 32L * 1024L * 1024L;
        private const int MaximumAtlasSize = 16384;

        [SerializeField] private CausticsTextureSettings _settings = new();
        [SerializeField, Min(1)] private int _atlasColumns = 4;
        [SerializeField, Tooltip(
            "メインアセットはループ可能なコースティクスアトラスです。" +
            "有効にすると各フレームのTexture2Dサブアセットを省略し、" +
            "無効にすると各フレームをサブアセットとして展開します。")]
        private bool _atlasOnly = true;
        [SerializeField] private TextureWrapMode _wrapMode = TextureWrapMode.Repeat;
        [SerializeField] private FilterMode _filterMode = FilterMode.Bilinear;
        [SerializeField] private TextureFormat _format = TextureFormat.RGB24;
        [SerializeField, Tooltip(
            "Atlas Onlyが有効な場合はアトラス全体を1つのSpriteにし、" +
            "無効な場合は各フレームをMultiple相当のSpriteサブアセットにします。")]
        private bool _sprite;

        public CausticsTextureSettings Settings => _settings;
        public int AtlasColumns => Mathf.Max(1, _atlasColumns);
        public bool AtlasOnly => _atlasOnly;

        [MenuItem(MenuPath, true)]
        private static bool CreateAssetValidate()
        {
            return AssetDatabase.IsValidFolder(GetSelectedFolder());
        }

        [MenuItem(MenuPath)]
        private static void CreateAsset()
        {
            string folderPath = GetSelectedFolder();
            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{folderPath}/New Caustics Texture.{Extension}");
            CreateCausticsTextureAction action =
                ScriptableObject.CreateInstance<CreateCausticsTextureAction>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                default,
                action,
                path,
                null,
                null);
        }

        private static string GetSelectedFolder()
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(selectedPath))
                return "Assets";
            if (AssetDatabase.IsValidFolder(selectedPath))
                return selectedPath;

            return (Path.GetDirectoryName(selectedPath) ?? "Assets").Replace('\\', '/');
        }

        public override void OnImportAsset(AssetImportContext context)
        {
            CausticsTextureSettings settings = CreateNormalizedSettings();
            int atlasColumns = Mathf.Clamp(_atlasColumns, 1, settings.FrameCount);
            string validationError = ValidateSettings(settings, atlasColumns);
            if (validationError != null)
                throw new InvalidOperationException(validationError);

            int atlasRows = Mathf.CeilToInt((float)settings.FrameCount / atlasColumns);
            int atlasWidth = settings.Width * atlasColumns;
            int atlasHeight = settings.Height * atlasRows;
            Color32[] atlasPixels = new Color32[atlasWidth * atlasHeight];
            int frameDigits = Mathf.Max(3, (settings.FrameCount - 1).ToString().Length);

            for (int frameIndex = 0; frameIndex < settings.FrameCount; frameIndex++)
            {
                Color32[] framePixels =
                    CausticsTextureGenerator.GenerateFrame(settings, frameIndex);
                CopyFrameToAtlas(
                    framePixels,
                    frameIndex,
                    settings.Width,
                    settings.Height,
                    atlasColumns,
                    atlasRows,
                    atlasPixels,
                    atlasWidth);

                if (!_atlasOnly && !_sprite)
                {
                    Texture2D frameTexture = CreateTexture(
                        context,
                        settings.Width,
                        settings.Height,
                        framePixels,
                        settings,
                        $"Frame_{frameIndex.ToString($"D{frameDigits}")}");
                    context.AddObjectToAsset(frameTexture.name, frameTexture);
                }
            }

            string atlasName = Path.GetFileNameWithoutExtension(context.assetPath);
            Texture2D atlas = CreateTexture(
                context,
                atlasWidth,
                atlasHeight,
                atlasPixels,
                settings,
                atlasName);
            context.AddObjectToAsset("Atlas", atlas);

            if (_sprite && _atlasOnly)
            {
                Sprite sprite = Sprite.Create(
                    atlas,
                    new Rect(0f, 0f, atlas.width, atlas.height),
                    new Vector2(0.5f, 0.5f));
                sprite.name = atlasName;
                context.AddObjectToAsset("Sprite", sprite);
                context.SetMainObject(sprite);
            }
            else if (_sprite)
            {
                AddMultipleSprites(
                    context,
                    atlas,
                    settings,
                    atlasColumns,
                    atlasRows,
                    frameDigits);
                context.SetMainObject(atlas);
            }
            else
            {
                context.SetMainObject(atlas);
            }

            if (!SystemInfo.SupportsTextureFormat(_format))
            {
                context.LogImportWarning(
                    $"Texture format {_format} is not supported by the current graphics device. " +
                    "The asset may not be previewable on this platform.");
            }
        }

        private static void AddMultipleSprites(
            AssetImportContext context,
            Texture2D atlas,
            CausticsTextureSettings settings,
            int atlasColumns,
            int atlasRows,
            int frameDigits)
        {
            for (int frameIndex = 0; frameIndex < settings.FrameCount; frameIndex++)
            {
                int column = frameIndex % atlasColumns;
                int topDownRow = frameIndex / atlasColumns;
                int atlasRow = atlasRows - 1 - topDownRow;
                string frameName = $"Frame_{frameIndex.ToString($"D{frameDigits}")}";
                Sprite sprite = Sprite.Create(
                    atlas,
                    new Rect(
                        column * settings.Width,
                        atlasRow * settings.Height,
                        settings.Width,
                        settings.Height),
                    new Vector2(0.5f, 0.5f));
                sprite.name = frameName;
                context.AddObjectToAsset(frameName, sprite);
            }
        }

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

        public static string ValidateSettings(
            CausticsTextureSettings settings,
            int atlasColumns)
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

        private Texture2D CreateTexture(
            AssetImportContext context,
            int width,
            int height,
            Color32[] pixels,
            CausticsTextureSettings settings,
            string textureName)
        {
            Texture2D texture = new(
                width,
                height,
                TextureFormat.RGBA32,
                settings.GenerateMipmaps,
                settings.Linear)
            {
                name = textureName,
                wrapMode = _wrapMode,
                filterMode = _filterMode,
                alphaIsTransparency = settings.AlphaFromIntensity,
            };
            texture.SetPixels32(pixels);
            texture.Apply(settings.GenerateMipmaps, false);
            ConvertTextureFormat(context, texture);
            return texture;
        }

        private void ConvertTextureFormat(AssetImportContext context, Texture2D texture)
        {
            if (texture.format == _format)
                return;

            try
            {
                EditorUtility.CompressTexture(
                    texture,
                    _format,
                    TextureCompressionQuality.Normal);
            }
            catch (Exception exception)
            {
                context.LogImportError(
                    $"Failed to convert '{texture.name}' to {_format}: {exception.Message}");
            }

            if (texture.format != _format)
            {
                context.LogImportError(
                    $"'{texture.name}' could not be converted to {_format}. " +
                    $"Its current format is {texture.format}.");
            }
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
                int destinationOffset =
                    (atlasRow * frameHeight + y) * atlasWidth + column * frameWidth;
                Array.Copy(
                    framePixels,
                    sourceOffset,
                    atlasPixels,
                    destinationOffset,
                    frameWidth);
            }
        }

        /// <summary>
        /// コースティクスImporterの設定とPNG出力を提供するInspector。
        /// </summary>
        [CustomEditor(typeof(CausticsTextureImporter))]
        public sealed class CausticsTextureImporterEditor : ScriptedImporterEditor
        {
            private readonly struct PngExportItem
            {
                public readonly Texture2D Texture;
                public readonly RectInt? Region;
                public readonly string Path;

                public PngExportItem(Texture2D texture, RectInt? region, string path)
                {
                    Texture = texture;
                    Region = region;
                    Path = path;
                }
            }

            private SerializedProperty _settingsProperty;
            private SerializedProperty _atlasColumnsProperty;
            private SerializedProperty _atlasOnlyProperty;
            private SerializedProperty _wrapModeProperty;
            private SerializedProperty _filterModeProperty;
            private SerializedProperty _formatProperty;
            private SerializedProperty _spriteProperty;
            private int _previewFrame;

            private CausticsTextureImporter Importer =>
                (CausticsTextureImporter)target;

            private void EnsureProperties()
            {
                if (_settingsProperty != null)
                    return;

                _settingsProperty = serializedObject.FindProperty(
                    nameof(CausticsTextureImporter._settings));
                _atlasColumnsProperty = serializedObject.FindProperty(
                    nameof(CausticsTextureImporter._atlasColumns));
                _atlasOnlyProperty = serializedObject.FindProperty(
                    nameof(CausticsTextureImporter._atlasOnly));
                _wrapModeProperty = serializedObject.FindProperty(
                    nameof(CausticsTextureImporter._wrapMode));
                _filterModeProperty = serializedObject.FindProperty(
                    nameof(CausticsTextureImporter._filterMode));
                _formatProperty = serializedObject.FindProperty(
                    nameof(CausticsTextureImporter._format));
                _spriteProperty = serializedObject.FindProperty(
                    nameof(CausticsTextureImporter._sprite));
            }

            public override void OnInspectorGUI()
            {
                EnsureProperties();
                serializedObject.Update();

                DrawSequenceSettings();
                EditorGUILayout.Space(8f);
                DrawSimulationSettings();
                EditorGUILayout.Space(8f);
                DrawAppearanceSettings();
                EditorGUILayout.Space(8f);
                DrawTextureSettings();

                if (FindSetting(nameof(CausticsTextureSettings.FrameCount)).intValue > 1)
                {
                    EditorGUILayout.Space(8f);
                    DrawInlinePreview();
                }

                string validationError = GetValidationError();
                if (validationError != null)
                    EditorGUILayout.HelpBox(validationError, MessageType.Error);

                serializedObject.ApplyModifiedProperties();
                ApplyRevertGUI();
                EditorGUILayout.Space(8f);

                using (new EditorGUI.DisabledScope(validationError != null))
                {
                    if (GUILayout.Button("Export PNG...", GUILayout.Height(26f)))
                        ExportPngFiles();
                }
            }

            private void DrawInlinePreview()
            {
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                int frameCount = Mathf.Max(
                    1,
                    FindSetting(nameof(CausticsTextureSettings.FrameCount)).intValue);
                int atlasColumns = Mathf.Clamp(
                    _atlasColumnsProperty.intValue,
                    1,
                    frameCount);
                int atlasRows = Mathf.CeilToInt((float)frameCount / atlasColumns);
                _previewFrame = Mathf.Clamp(_previewFrame, 0, frameCount - 1);
                _previewFrame = EditorGUILayout.IntSlider(
                    "Frame",
                    _previewFrame,
                    0,
                    frameCount - 1);

                Texture2D atlas = LoadAtlasTexture();
                if (atlas == null)
                {
                    EditorGUILayout.HelpBox(
                        "Apply the settings to generate the preview.",
                        MessageType.None);
                    return;
                }

                int column = _previewFrame % atlasColumns;
                int topDownRow = _previewFrame / atlasColumns;
                int atlasRow = atlasRows - 1 - topDownRow;
                Rect textureCoordinates = new(
                    (float)column / atlasColumns,
                    (float)atlasRow / atlasRows,
                    1f / atlasColumns,
                    1f / atlasRows);
                int frameWidth = Mathf.Max(
                    1,
                    FindSetting(nameof(CausticsTextureSettings.Width)).intValue);
                int frameHeight = Mathf.Max(
                    1,
                    FindSetting(nameof(CausticsTextureSettings.Height)).intValue);
                float availableWidth = Mathf.Max(64f, EditorGUIUtility.currentViewWidth - 42f);
                float previewHeight = Mathf.Min(
                    320f,
                    availableWidth * frameHeight / frameWidth);
                Rect layoutRect = EditorGUILayout.GetControlRect(false, previewHeight);
                Rect previewRect = FitAspectRect(layoutRect, (float)frameWidth / frameHeight);
                EditorGUI.DrawRect(previewRect, new Color(0.12f, 0.12f, 0.12f, 1f));
                GUI.DrawTextureWithTexCoords(previewRect, atlas, textureCoordinates, true);
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

            private Texture2D LoadAtlasTexture()
            {
                return AssetDatabase.LoadAllAssetsAtPath(Importer.assetPath)
                    .OfType<Texture2D>()
                    .FirstOrDefault(texture => !texture.name.StartsWith("Frame_"));
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

                SerializedProperty frameCount =
                    FindSetting(nameof(CausticsTextureSettings.FrameCount));
                frameCount.intValue = EditorGUILayout.IntSlider(
                    "Frame Count",
                    frameCount.intValue,
                    1,
                    256);
                _atlasColumnsProperty.intValue = EditorGUILayout.IntSlider(
                    "Atlas Columns",
                    Mathf.Clamp(_atlasColumnsProperty.intValue, 1, frameCount.intValue),
                    1,
                    frameCount.intValue);
                EditorGUILayout.PropertyField(
                    _atlasOnlyProperty,
                    new GUIContent("Atlas Only", _atlasOnlyProperty.tooltip));

                int rows = Mathf.CeilToInt(
                    (float)frameCount.intValue / _atlasColumnsProperty.intValue);
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
                EditorGUILayout.PropertyField(
                    FindSetting(nameof(CausticsTextureSettings.Seed)),
                    new GUIContent("Seed"));
                SerializedProperty animationSpeed = FindSetting(nameof(CausticsTextureSettings.AnimationSpeed));
                animationSpeed.floatValue = EditorGUILayout.Slider(new GUIContent("Animation Speed", animationSpeed.tooltip), animationSpeed.floatValue, 0f, 2f);
                DrawSlider(
                    nameof(CausticsTextureSettings.RefractionStrength),
                    "Refraction Strength",
                    0f,
                    0.25f);
            }

            private void DrawAppearanceSettings()
            {
                EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
                DrawSlider(nameof(CausticsTextureSettings.BlackPoint), "Black Point", 0f, 2f);
                DrawSlider(nameof(CausticsTextureSettings.Exposure), "Exposure", 0.1f, 20f);
                DrawSlider(nameof(CausticsTextureSettings.Contrast), "Contrast", 0.1f, 4f);
                DrawSlider(nameof(CausticsTextureSettings.ChromaticAberration), "Chromatic Aberration", 0f, 0.5f);
                DrawIntSlider(nameof(CausticsTextureSettings.BlurRadius), "Periodic Blur", 0, 16);
                EditorGUILayout.PropertyField(
                    FindSetting(nameof(CausticsTextureSettings.Tint)),
                    new GUIContent("Tint"));
                EditorGUILayout.PropertyField(
                    FindSetting(nameof(CausticsTextureSettings.AlphaFromIntensity)),
                    new GUIContent("Intensity to Alpha"));
            }

            private void DrawTextureSettings()
            {
                EditorGUILayout.LabelField("Texture", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_wrapModeProperty, new GUIContent("Wrap Mode"));
                EditorGUILayout.PropertyField(_filterModeProperty, new GUIContent("Filter Mode"));
                EditorGUILayout.PropertyField(_formatProperty, new GUIContent("Format"));
                EditorGUILayout.PropertyField(
                    _spriteProperty,
                    new GUIContent("Sprite", _spriteProperty.tooltip));
                EditorGUILayout.PropertyField(
                    FindSetting(nameof(CausticsTextureSettings.Linear)),
                    new GUIContent("Linear Texture"));
                EditorGUILayout.PropertyField(
                    FindSetting(nameof(CausticsTextureSettings.GenerateMipmaps)),
                    new GUIContent("Generate Mipmaps"));

                var format = (TextureFormat)_formatProperty.intValue;
                if (!SystemInfo.SupportsTextureFormat(format))
                {
                    EditorGUILayout.HelpBox(
                        $"{format} is not supported by the current graphics device.",
                        MessageType.Warning);
                }
            }

            private SerializedProperty FindSetting(string propertyName)
            {
                return _settingsProperty.FindPropertyRelative(propertyName);
            }

            private void DrawIntSlider(
                string propertyName,
                string label,
                int minimum,
                int maximum)
            {
                SerializedProperty property = FindSetting(propertyName);
                property.intValue = EditorGUILayout.IntSlider(
                    label,
                    property.intValue,
                    minimum,
                    maximum);
            }

            private void DrawSlider(
                string propertyName,
                string label,
                float minimum,
                float maximum)
            {
                SerializedProperty property = FindSetting(propertyName);
                property.floatValue = EditorGUILayout.Slider(
                    label,
                    property.floatValue,
                    minimum,
                    maximum);
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
                return ValidateSettings(settings, _atlasColumnsProperty.intValue);
            }

            private void ExportPngFiles()
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                string assetDirectory = Path.GetDirectoryName(Importer.assetPath) ?? "Assets";
                string initialDirectory = projectRoot == null
                    ? Application.dataPath
                    : Path.GetFullPath(Path.Combine(projectRoot, assetDirectory));
                string outputDirectory = EditorUtility.OpenFolderPanel(
                    "Export Caustics PNG",
                    initialDirectory,
                    string.Empty);
                if (string.IsNullOrEmpty(outputDirectory))
                    return;

                string baseName = Path.GetFileNameWithoutExtension(Importer.assetPath);
                List<PngExportItem> outputs = BuildExportList(
                    outputDirectory,
                    baseName);
                bool hasExistingFile = outputs.Any(output => File.Exists(output.Path));
                if (hasExistingFile && !EditorUtility.DisplayDialog(
                        "Overwrite Existing Files?",
                        "One or more PNG files already exist and will be overwritten.",
                        "Overwrite",
                        "Cancel"))
                {
                    return;
                }

                try
                {
                    for (int i = 0; i < outputs.Count; i++)
                    {
                        PngExportItem output = outputs[i];
                        EditorUtility.DisplayProgressBar(
                            "Export Caustics PNG",
                            Path.GetFileName(output.Path),
                            (float)i / outputs.Count);
                        File.WriteAllBytes(output.Path, EncodePng(output));
                    }
                    AssetDatabase.Refresh();
                    Debug.Log(
                        $"[{nameof(CausticsTextureImporter)}] Exported {outputs.Count} PNG file(s) " +
                        $"to \"{outputDirectory}\".");
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            private List<PngExportItem> BuildExportList(
                string outputDirectory,
                string baseName)
            {
                UnityEngine.Object[] assets =
                    AssetDatabase.LoadAllAssetsAtPath(Importer.assetPath);
                Texture2D atlas = LoadAtlasTexture();
                if (atlas == null)
                    throw new InvalidOperationException("The imported atlas texture could not be loaded.");

                List<PngExportItem> outputs = new()
                {
                    new(
                        atlas,
                        null,
                        Path.Combine(outputDirectory, $"{baseName}_Atlas.png")),
                };

                Texture2D[] frameTextures = assets
                    .OfType<Texture2D>()
                    .Where(texture => texture != atlas && texture.name.StartsWith("Frame_"))
                    .OrderBy(texture => texture.name)
                    .ToArray();
                foreach (Texture2D frame in frameTextures)
                {
                    string frameNumber = frame.name.Substring("Frame_".Length);
                    outputs.Add(new PngExportItem(
                        frame,
                        null,
                        Path.Combine(outputDirectory, $"{baseName}_{frameNumber}.png")));
                }

                if (frameTextures.Length == 0)
                {
                    IEnumerable<Sprite> frameSprites = assets
                        .OfType<Sprite>()
                        .Where(sprite => sprite.name.StartsWith("Frame_"))
                        .OrderBy(sprite => sprite.name);
                    foreach (Sprite frame in frameSprites)
                    {
                        string frameNumber = frame.name.Substring("Frame_".Length);
                        Rect rect = frame.rect;
                        outputs.Add(new PngExportItem(
                            atlas,
                            new RectInt(
                                Mathf.RoundToInt(rect.x),
                                Mathf.RoundToInt(rect.y),
                                Mathf.RoundToInt(rect.width),
                                Mathf.RoundToInt(rect.height)),
                            Path.Combine(outputDirectory, $"{baseName}_{frameNumber}.png")));
                    }
                }

                return outputs;
            }

            private byte[] EncodePng(PngExportItem output)
            {
                if (!output.Region.HasValue)
                    return output.Texture.EncodeToPNG();

                RectInt region = output.Region.Value;
                Color32[] sourcePixels = output.Texture.GetPixels32();
                Color32[] framePixels = new Color32[region.width * region.height];
                for (int y = 0; y < region.height; y++)
                {
                    Array.Copy(
                        sourcePixels,
                        (region.y + y) * output.Texture.width + region.x,
                        framePixels,
                        y * region.width,
                        region.width);
                }

                bool linear = Importer.Settings?.Linear ?? true;
                Texture2D frameTexture = new(
                    region.width,
                    region.height,
                    TextureFormat.RGBA32,
                    false,
                    linear);
                try
                {
                    frameTexture.SetPixels32(framePixels);
                    frameTexture.Apply(false, false);
                    return frameTexture.EncodeToPNG();
                }
                finally
                {
                    DestroyImmediate(frameTexture);
                }
            }
        }
    }

    /// <summary>
    /// Projectビューで名前を決定したあとに専用ファイルを作成する。
    /// </summary>
    internal sealed class CreateCausticsTextureAction : AssetCreationEndAction
    {
        public override void Action(EntityId entityId, string pathName, string resourceFile)
        {
            File.WriteAllText(pathName, "{}");
            AssetDatabase.ImportAsset(pathName);
            AssetDatabase.Refresh();
            ProjectWindowUtil.ShowCreatedAsset(AssetDatabase.LoadMainAssetAtPath(pathName));
        }
    }
}

#endif

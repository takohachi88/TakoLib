#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.ProjectWindowCallback;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace TakoLibEditor.Common
{
    /// <summary>
    /// Imports a texture whose output channels are assembled from channels of other textures.
    /// Source texture references are importer settings only and are not retained by the output asset.
    /// </summary>
    [ScriptedImporter(2, Extension)]
    public sealed class ChannelTextureImporter : ScriptedImporter
    {
        private const string Extension = "channeltexture";
        private const string MenuPath = "Assets/Create/2D/Channel Texture";
        private const string ComputeShaderPath = "Packages/takolib.common/Editor/ChannelTextureImporter.compute";

        public enum SourceChannel
        {
            R,
            G,
            B,
            A,
            Zero,
            One,
        }

        [Serializable]
        private sealed class ChannelSource
        {
            [SerializeField] private Texture2D _texture;
            [SerializeField] private SourceChannel _channel;

            public Texture2D Texture => _texture;
            public SourceChannel Channel => _channel;

            public ChannelSource(SourceChannel channel)
            {
                _channel = channel;
            }
        }

        [Flags]
        private enum OutputChannels
        {
            None = 0,
            R = 1 << 0,
            G = 1 << 1,
            B = 1 << 2,
            A = 1 << 3,
            RGBA = R | G | B | A,
        }

        [SerializeField] private ChannelSource _red = new(SourceChannel.Zero);
        [SerializeField] private ChannelSource _green = new(SourceChannel.Zero);
        [SerializeField] private ChannelSource _blue = new(SourceChannel.Zero);
        [SerializeField] private ChannelSource _alpha = new(SourceChannel.Zero);
        [SerializeField] private Vector2Int _size = new(256, 256);
        [SerializeField] private TextureFormat _format = TextureFormat.RGBA32;
        [SerializeField] private TextureCompressionQuality _compressionQuality = TextureCompressionQuality.Normal;
        [SerializeField] private bool _linear = true;
        [SerializeField] private bool _sprite;

        public Texture2D RedSourceTexture => _red?.Texture;
        public Texture2D GreenSourceTexture => _green?.Texture;
        public Texture2D BlueSourceTexture => _blue?.Texture;
        public Texture2D AlphaSourceTexture => _alpha?.Texture;
        public SourceChannel RedSourceChannel => _red?.Channel ?? SourceChannel.Zero;
        public SourceChannel GreenSourceChannel => _green?.Channel ?? SourceChannel.Zero;
        public SourceChannel BlueSourceChannel => _blue?.Channel ?? SourceChannel.Zero;
        public SourceChannel AlphaSourceChannel => _alpha?.Channel ?? SourceChannel.Zero;
        public Vector2Int Size => _size;
        public TextureFormat Format => _format;
        public TextureCompressionQuality CompressionQuality => _compressionQuality;
        public bool Linear => _linear;
        public bool IsSprite => _sprite;

        [MenuItem(MenuPath, true)]
        private static bool CreateAssetValidate()
        {
            return AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
        }

        [MenuItem(MenuPath)]
        private static void CreateAsset()
        {
            string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/channel_.{Extension}");
            var endAction = ScriptableObject.CreateInstance<CreateChannelTextureAction>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(default, endAction, path, null, null);
        }

        public override void OnImportAsset(AssetImportContext context)
        {
            EnsureSettings();

            _size.x = Mathf.Max(1, _size.x);
            _size.y = Mathf.Max(1, _size.y);

            RegisterSourceDependencies(context);

            Texture2D texture = GenerateTexture(context);
            texture.name = Path.GetFileNameWithoutExtension(context.assetPath);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            AddOutputAssets(context, texture);
        }

        private void EnsureSettings()
        {
            _red ??= new ChannelSource(SourceChannel.Zero);
            _green ??= new ChannelSource(SourceChannel.Zero);
            _blue ??= new ChannelSource(SourceChannel.Zero);
            _alpha ??= new ChannelSource(SourceChannel.Zero);
        }

        private void RegisterSourceDependencies(AssetImportContext context)
        {
            var registeredPaths = new HashSet<string>();
            RegisterSourceDependency(context, _red, registeredPaths);
            RegisterSourceDependency(context, _green, registeredPaths);
            RegisterSourceDependency(context, _blue, registeredPaths);
            RegisterSourceDependency(context, _alpha, registeredPaths);
        }

        private static void RegisterSourceDependency(
            AssetImportContext context,
            ChannelSource source,
            HashSet<string> registeredPaths)
        {
            if (!UsesTexture(source.Channel) || source.Texture == null)
            {
                return;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source.Texture);
            if (!string.IsNullOrEmpty(sourcePath)
                && sourcePath != context.assetPath
                && registeredPaths.Add(sourcePath))
            {
                context.DependsOnArtifact(sourcePath);
            }
        }

        private Texture2D GenerateTexture(AssetImportContext context)
        {
            TextureFormat intermediateFormat = RequiresFloatIntermediate(_format)
                ? TextureFormat.RGBAFloat
                : TextureFormat.RGBA32;

            ComputeShader computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputeShaderPath);
            if (computeShader == null)
            {
                context.LogImportError($"Channel packing ComputeShader was not found at '{ComputeShaderPath}'.");
                return CreateBlankTexture(intermediateFormat);
            }

            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsAsyncGPUReadback)
            {
                context.LogImportError("The current graphics device must support ComputeShader and AsyncGPUReadback.");
                return CreateBlankTexture(intermediateFormat);
            }

            int kernel = computeShader.FindKernel("CSMain");
            var renderTexture = new RenderTexture(
                _size.x,
                _size.y,
                0,
                RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            Texture2D texture;
            try
            {
                renderTexture.Create();
                computeShader.SetInts("_OutputSize", _size.x, _size.y);
                computeShader.SetInt("_EncodeSrgb", _linear ? 0 : 1);
                computeShader.SetTexture(kernel, "_Result", renderTexture);
                ConfigureComputeChannel(context, computeShader, kernel, _red, OutputChannels.R, "R", "Red");
                ConfigureComputeChannel(context, computeShader, kernel, _green, OutputChannels.G, "G", "Green");
                ConfigureComputeChannel(context, computeShader, kernel, _blue, OutputChannels.B, "B", "Blue");
                ConfigureComputeChannel(context, computeShader, kernel, _alpha, OutputChannels.A, "A", "Alpha");

                computeShader.Dispatch(
                    kernel,
                    Mathf.CeilToInt(_size.x / 8f),
                    Mathf.CeilToInt(_size.y / 8f),
                    1);

                AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(renderTexture, 0, intermediateFormat);
                request.WaitForCompletion();

                texture = new Texture2D(_size.x, _size.y, intermediateFormat, false, _linear);
                if (request.hasError)
                {
                    context.LogImportError("Failed to read back the texture generated by the ComputeShader.");
                    texture.SetPixels(new Color[_size.x * _size.y]);
                }
                else
                {
                    texture.LoadRawTextureData(request.GetData<byte>());
                }

                texture.Apply(false, false);
            }
            finally
            {
                renderTexture.Release();
                DestroyImmediate(renderTexture);
            }

            ConvertTextureFormat(context, texture);
            return texture;
        }

        private Texture2D CreateBlankTexture(TextureFormat intermediateFormat)
        {
            var texture = new Texture2D(_size.x, _size.y, intermediateFormat, false, _linear);
            texture.SetPixels(new Color[_size.x * _size.y]);
            texture.Apply(false, false);
            return texture;
        }

        private void ConvertTextureFormat(AssetImportContext context, Texture2D texture)
        {
            try
            {
                if (texture.format != _format)
                {
                    EditorUtility.CompressTexture(texture, _format, _compressionQuality);
                }
            }
            catch (Exception exception)
            {
                context.LogImportError($"Failed to convert the generated texture to {_format}: {exception.Message}");
            }

            if (texture.format != _format)
            {
                context.LogImportError(
                    $"The generated texture could not be converted to {_format}. Its current format is {texture.format}.");
            }

            if (!SystemInfo.SupportsTextureFormat(_format))
            {
                context.LogImportWarning(
                    $"Texture format {_format} is not supported by the current graphics device. The asset may not be previewable on this platform.");
            }
        }

        private void ConfigureComputeChannel(
            AssetImportContext context,
            ComputeShader computeShader,
            int kernel,
            ChannelSource source,
            OutputChannels outputChannel,
            string propertySuffix,
            string outputChannelName)
        {
            SourceChannel effectiveChannel = source.Channel;
            Texture2D sourceTexture = source.Texture;
            if ((GetOutputChannels(_format) & outputChannel) == 0)
            {
                effectiveChannel = SourceChannel.Zero;
            }
            else if (UsesTexture(effectiveChannel) && sourceTexture == null)
            {
                effectiveChannel = SourceChannel.Zero;
            }
            else if (UsesTexture(effectiveChannel)
                && AssetDatabase.GetAssetPath(sourceTexture) == context.assetPath)
            {
                context.LogImportError(
                    $"{outputChannelName} output cannot use the generated texture itself as a source. Zero is used instead.");
                effectiveChannel = SourceChannel.Zero;
            }

            Texture2D textureToBind = UsesTexture(effectiveChannel) ? sourceTexture : Texture2D.blackTexture;
            computeShader.SetTexture(kernel, $"_Source{propertySuffix}", textureToBind);
            computeShader.SetInt($"_Channel{propertySuffix}", (int)effectiveChannel);
        }

        private void AddOutputAssets(AssetImportContext context, Texture2D texture)
        {
            context.AddObjectToAsset("Texture", texture);

            if (!_sprite)
            {
                context.SetMainObject(texture);
                return;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            sprite.name = texture.name;

            context.AddObjectToAsset("Sprite", sprite);
            context.SetMainObject(sprite);
        }

        private static bool UsesTexture(SourceChannel channel)
        {
            return channel is SourceChannel.R or SourceChannel.G or SourceChannel.B or SourceChannel.A;
        }

        private static OutputChannels GetOutputChannels(TextureFormat format)
        {
            string formatName = format.ToString();
            if (formatName.StartsWith("PVRTC_RGB", StringComparison.Ordinal)
                && !formatName.StartsWith("PVRTC_RGBA", StringComparison.Ordinal))
            {
                return OutputChannels.R | OutputChannels.G | OutputChannels.B;
            }

            return format switch
            {
                TextureFormat.Alpha8 => OutputChannels.A,

                TextureFormat.R8 or
                TextureFormat.R16 or
                TextureFormat.RHalf or
                TextureFormat.RFloat or
                TextureFormat.BC4 or
                TextureFormat.EAC_R or
                TextureFormat.EAC_R_SIGNED or
                TextureFormat.R8_SIGNED or
                TextureFormat.R16_SIGNED => OutputChannels.R,

                TextureFormat.RG16 or
                TextureFormat.RG32 or
                TextureFormat.RGHalf or
                TextureFormat.RGFloat or
                TextureFormat.BC5 or
                TextureFormat.EAC_RG or
                TextureFormat.EAC_RG_SIGNED or
                TextureFormat.RG16_SIGNED or
                TextureFormat.RG32_SIGNED => OutputChannels.R | OutputChannels.G,

                TextureFormat.RGB24 or
                TextureFormat.RGB48 or
                TextureFormat.RGB565 or
                TextureFormat.RGB9e5Float or
                TextureFormat.YUY2 or
                TextureFormat.DXT1 or
                TextureFormat.DXT1Crunched or
                TextureFormat.BC6H or
                TextureFormat.ETC_RGB4 or
                TextureFormat.ETC2_RGB or
                TextureFormat.ETC_RGB4Crunched or
                TextureFormat.RGB24_SIGNED or
                TextureFormat.RGB48_SIGNED => OutputChannels.R | OutputChannels.G | OutputChannels.B,

                _ => OutputChannels.RGBA,
            };
        }

        private static bool IsCompressedFormat(TextureFormat format)
        {
            string formatName = format.ToString();
            return formatName.StartsWith("DXT", StringComparison.Ordinal)
                || formatName.StartsWith("BC", StringComparison.Ordinal)
                || formatName.StartsWith("PVRTC", StringComparison.Ordinal)
                || formatName.StartsWith("ETC", StringComparison.Ordinal)
                || formatName.StartsWith("EAC", StringComparison.Ordinal)
                || formatName.StartsWith("ASTC", StringComparison.Ordinal);
        }

        private static bool RequiresFloatIntermediate(TextureFormat format)
        {
            return format is TextureFormat.R16
                or TextureFormat.RG32
                or TextureFormat.RGB48
                or TextureFormat.RGBA64
                or TextureFormat.R8_SIGNED
                or TextureFormat.RG16_SIGNED
                or TextureFormat.RGB24_SIGNED
                or TextureFormat.RGBA32_SIGNED
                or TextureFormat.R16_SIGNED
                or TextureFormat.RG32_SIGNED
                or TextureFormat.RGB48_SIGNED
                or TextureFormat.RGBA64_SIGNED
                or TextureFormat.RHalf
                or TextureFormat.RGHalf
                or TextureFormat.RGBAHalf
                or TextureFormat.RFloat
                or TextureFormat.RGFloat
                or TextureFormat.RGBAFloat
                or TextureFormat.RGB9e5Float
                or TextureFormat.BC6H
                or TextureFormat.ASTC_HDR_4x4
                or TextureFormat.ASTC_HDR_5x5
                or TextureFormat.ASTC_HDR_6x6
                or TextureFormat.ASTC_HDR_8x8
                or TextureFormat.ASTC_HDR_10x10
                or TextureFormat.ASTC_HDR_12x12;
        }

        [CustomEditor(typeof(ChannelTextureImporter))]
        public sealed class ChannelTextureImporterEditor : ScriptedImporterEditor
        {
            private static readonly string[] ChannelPropertyNames = { "_red", "_green", "_blue", "_alpha" };
            private static readonly string[] ChannelLabels = { "R Output", "G Output", "B Output", "A Output" };
            private static readonly Color RedColor = new(1f, 0.45f, 0.45f);
            private static readonly Color GreenColor = new(0.45f, 1f, 0.45f);
            private static readonly Color BlueColor = new(0.45f, 0.65f, 1f);
            private static readonly Color[] ChannelColors = { RedColor, GreenColor, BlueColor, Color.white };

            private readonly struct ChannelValue
            {
                public readonly UnityEngine.Object Texture;
                public readonly int Channel;

                public ChannelValue(UnityEngine.Object texture, int channel)
                {
                    Texture = texture;
                    Channel = channel;
                }
            }

            private ReorderableList _channelList;
            private List<int> _channelIndices;
            private OutputChannels _listedOutputChannels;

            protected override void Apply()
            {
                serializedObject.Update();

                foreach (string propertyName in ChannelPropertyNames)
                {
                    SerializedProperty sourceProperty = serializedObject.FindProperty(propertyName);
                    SerializedProperty textureProperty = sourceProperty.FindPropertyRelative("_texture");
                    SerializedProperty channelProperty = sourceProperty.FindPropertyRelative("_channel");
                    var sourceChannel = (SourceChannel)channelProperty.enumValueIndex;

                    if (UsesTexture(sourceChannel) && textureProperty.objectReferenceValue == null)
                    {
                        channelProperty.enumValueIndex = (int)SourceChannel.Zero;
                    }
                }

                serializedObject.ApplyModifiedProperties();
                base.Apply();
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                SerializedProperty formatProperty = serializedObject.FindProperty("_format");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_size"));
                EditorGUILayout.PropertyField(formatProperty);

                var format = (TextureFormat)formatProperty.intValue;
                if (IsCompressedFormat(format))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_compressionQuality"));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("_linear"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_sprite"));

                if (!SystemInfo.SupportsTextureFormat(format))
                {
                    EditorGUILayout.HelpBox(
                        $"{format} is not supported by the current graphics device.",
                        MessageType.Warning);
                }

                EditorGUILayout.Space();

                OutputChannels outputChannels = GetOutputChannels(format);
                EnsureChannelList(outputChannels);
                _channelList.DoLayoutList();

                serializedObject.ApplyModifiedProperties();
                ApplyRevertGUI();
            }

            private void EnsureChannelList(OutputChannels outputChannels)
            {
                if (_channelList != null && _listedOutputChannels == outputChannels)
                {
                    return;
                }

                _listedOutputChannels = outputChannels;
                _channelIndices = GetActiveChannelIndices(outputChannels);
                _channelList = new ReorderableList(
                    _channelIndices,
                    typeof(int),
                    _channelIndices.Count > 1,
                    true,
                    false,
                    false)
                {
                    drawHeaderCallback = rect => EditorGUI.LabelField(
                        rect,
                        "Output Channels (drag to reorder assignments)"),
                    drawElementCallback = DrawChannelElement,
                    elementHeightCallback = GetChannelElementHeight,
                    onReorderCallbackWithDetails = ReorderChannels,
                };
            }

            private void DrawChannelElement(Rect rect, int listIndex, bool isActive, bool isFocused)
            {
                int channelIndex = _channelIndices[listIndex];
                SerializedProperty sourceProperty = serializedObject.FindProperty(ChannelPropertyNames[channelIndex]);
                SerializedProperty textureProperty = sourceProperty.FindPropertyRelative("_texture");
                SerializedProperty channelProperty = sourceProperty.FindPropertyRelative("_channel");

                const float labelWidth = 75f;
                const float spacing = 2f;
                float lineHeight = EditorGUIUtility.singleLineHeight;

                Rect colorRect = new(rect.x, rect.y + 1f, 3f, lineHeight - 2f);
                EditorGUI.DrawRect(colorRect, ChannelColors[channelIndex]);

                Rect labelRect = new(rect.x + 7f, rect.y, labelWidth, lineHeight);
                EditorGUI.LabelField(labelRect, ChannelLabels[channelIndex], EditorStyles.boldLabel);

                Rect channelRect = new(
                    labelRect.xMax,
                    rect.y,
                    rect.xMax - labelRect.xMax,
                    lineHeight);
                Texture2D sourceTexture = textureProperty.objectReferenceValue as Texture2D;
                List<SourceChannel> selectableChannels = GetSelectableSourceChannels(sourceTexture);
                var sourceChannel = (SourceChannel)channelProperty.enumValueIndex;
                int selectedIndex = selectableChannels.IndexOf(sourceChannel);
                if (selectedIndex < 0)
                {
                    sourceChannel = SourceChannel.Zero;
                    channelProperty.enumValueIndex = (int)sourceChannel;
                    selectedIndex = selectableChannels.IndexOf(sourceChannel);
                }

                string[] channelNames = selectableChannels.ConvertAll(channel => channel.ToString()).ToArray();
                int newSelectedIndex = EditorGUI.Popup(channelRect, selectedIndex, channelNames);
                sourceChannel = selectableChannels[newSelectedIndex];
                channelProperty.enumValueIndex = (int)sourceChannel;

                if (UsesTexture(sourceChannel))
                {
                    Rect textureRect = new(
                        labelRect.x,
                        rect.y + lineHeight + spacing,
                        rect.xMax - labelRect.x,
                        lineHeight);
                    EditorGUI.PropertyField(textureRect, textureProperty, new GUIContent("Source Texture"));

                    sourceTexture = textureProperty.objectReferenceValue as Texture2D;
                    if (sourceTexture != null)
                    {
                        Rect informationRect = new(
                            labelRect.x,
                            textureRect.yMax + spacing,
                            rect.xMax - labelRect.x,
                            lineHeight);
                        string colorSpace = sourceTexture.isDataSRGB ? "Gamma" : "Linear";
                        EditorGUI.LabelField(
                            informationRect,
                            $"{colorSpace}  |  {sourceTexture.format}  |  {sourceTexture.width} × {sourceTexture.height}",
                            EditorStyles.miniLabel);
                    }
                }
            }

            private float GetChannelElementHeight(int listIndex)
            {
                int channelIndex = _channelIndices[listIndex];
                SerializedProperty sourceProperty = serializedObject.FindProperty(ChannelPropertyNames[channelIndex]);
                SerializedProperty channelProperty = sourceProperty.FindPropertyRelative("_channel");
                var sourceChannel = (SourceChannel)channelProperty.enumValueIndex;

                float lineHeight = EditorGUIUtility.singleLineHeight;
                if (!UsesTexture(sourceChannel))
                {
                    return lineHeight + 4f;
                }

                SerializedProperty textureProperty = sourceProperty.FindPropertyRelative("_texture");
                bool hasSourceTexture = textureProperty.objectReferenceValue != null;
                return hasSourceTexture ? lineHeight * 3f + 8f : lineHeight * 2f + 6f;
            }

            private void ReorderChannels(ReorderableList list, int oldIndex, int newIndex)
            {
                List<int> activeChannelIndices = GetActiveChannelIndices(_listedOutputChannels);
                var values = new List<ChannelValue>(activeChannelIndices.Count);
                foreach (int channelIndex in activeChannelIndices)
                {
                    SerializedProperty sourceProperty = serializedObject.FindProperty(ChannelPropertyNames[channelIndex]);
                    values.Add(new ChannelValue(
                        sourceProperty.FindPropertyRelative("_texture").objectReferenceValue,
                        sourceProperty.FindPropertyRelative("_channel").enumValueIndex));
                }

                ChannelValue movedValue = values[oldIndex];
                values.RemoveAt(oldIndex);
                values.Insert(newIndex, movedValue);

                for (int i = 0; i < activeChannelIndices.Count; i++)
                {
                    SerializedProperty sourceProperty = serializedObject.FindProperty(ChannelPropertyNames[activeChannelIndices[i]]);
                    sourceProperty.FindPropertyRelative("_texture").objectReferenceValue = values[i].Texture;
                    sourceProperty.FindPropertyRelative("_channel").enumValueIndex = values[i].Channel;
                }

                _channelIndices.Clear();
                _channelIndices.AddRange(activeChannelIndices);
                GUI.changed = true;
                Repaint();
            }

            private static List<int> GetActiveChannelIndices(OutputChannels outputChannels)
            {
                var indices = new List<int>(4);
                if ((outputChannels & OutputChannels.R) != 0) indices.Add(0);
                if ((outputChannels & OutputChannels.G) != 0) indices.Add(1);
                if ((outputChannels & OutputChannels.B) != 0) indices.Add(2);
                if ((outputChannels & OutputChannels.A) != 0) indices.Add(3);
                return indices;
            }

            private static List<SourceChannel> GetSelectableSourceChannels(Texture2D sourceTexture)
            {
                OutputChannels channels = sourceTexture != null
                    ? GetOutputChannels(sourceTexture.format)
                    : OutputChannels.RGBA;
                var selectableChannels = new List<SourceChannel>(6);

                if ((channels & OutputChannels.R) != 0) selectableChannels.Add(SourceChannel.R);
                if ((channels & OutputChannels.G) != 0) selectableChannels.Add(SourceChannel.G);
                if ((channels & OutputChannels.B) != 0) selectableChannels.Add(SourceChannel.B);
                if ((channels & OutputChannels.A) != 0) selectableChannels.Add(SourceChannel.A);
                selectableChannels.Add(SourceChannel.Zero);
                selectableChannels.Add(SourceChannel.One);
                return selectableChannels;
            }
        }
    }

    public sealed class CreateChannelTextureAction : AssetCreationEndAction
    {
        public override void Action(EntityId entityId, string pathName, string resourceFile)
        {
            File.WriteAllBytes(pathName, new byte[1]);
            AssetDatabase.ImportAsset(pathName);
            ProjectWindowUtil.ShowCreatedAsset(AssetDatabase.LoadMainAssetAtPath(pathName));
        }
    }
}

#endif

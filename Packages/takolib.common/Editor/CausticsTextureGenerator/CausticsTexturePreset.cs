#if UNITY_EDITOR

using UnityEngine;

namespace TakoLibEditor.Common
{
    /// <summary>
    /// コースティクス生成設定を保持し、プロジェクト内で再利用するためのアセット。
    /// 出力先と一時的なプレビュー状態は保存せず、エディターウィンドウ側で管理する。
    /// </summary>
    [CreateAssetMenu(
        fileName = "CausticsPreset",
        menuName = "TakoLib/Caustics Texture Preset")]
    public sealed class CausticsTexturePreset : ScriptableObject
    {
        [SerializeField] private CausticsTextureSettings _settings = new();
        [SerializeField, Min(1)] private int _atlasColumns = 4;
        [SerializeField, Tooltip("出力ファイル名の基礎部分。")]
        private string _baseFileName = "Caustics";
        [SerializeField, Tooltip("Assetsフォルダーからの相対出力パス。")]
        private string _outputDirectory = string.Empty;
        [SerializeField] private bool _atlasOnly;

        public CausticsTextureSettings CreateSettingsCopy()
        {
            return _settings?.Copy() ?? new CausticsTextureSettings();
        }

        public int AtlasColumns => Mathf.Max(1, _atlasColumns);
        public string BaseFileName => _baseFileName ?? string.Empty;
        public string OutputDirectory => _outputDirectory ?? string.Empty;
        public bool AtlasOnly => _atlasOnly;

        public void Store(
            CausticsTextureSettings settings,
            int atlasColumns,
            string baseFileName,
            string outputDirectory,
            bool atlasOnly)
        {
            _settings = settings?.Copy() ?? new CausticsTextureSettings();
            _atlasColumns = Mathf.Clamp(atlasColumns, 1, _settings.FrameCount);
            _baseFileName = baseFileName ?? string.Empty;
            _outputDirectory = (outputDirectory ?? string.Empty).Replace('\\', '/');
            _atlasOnly = atlasOnly;
        }

        private void OnValidate()
        {
            _settings ??= new CausticsTextureSettings();
            _atlasColumns = Mathf.Clamp(_atlasColumns, 1, Mathf.Max(1, _settings.FrameCount));
            _baseFileName ??= string.Empty;
            _outputDirectory = (_outputDirectory ?? string.Empty).Replace('\\', '/');
        }
    }
}

#endif

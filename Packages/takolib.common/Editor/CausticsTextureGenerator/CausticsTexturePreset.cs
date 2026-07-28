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

        public CausticsTextureSettings CreateSettingsCopy()
        {
            return _settings?.Copy() ?? new CausticsTextureSettings();
        }

        public int AtlasColumns => Mathf.Max(1, _atlasColumns);

        public void Store(CausticsTextureSettings settings, int atlasColumns)
        {
            _settings = settings?.Copy() ?? new CausticsTextureSettings();
            _atlasColumns = Mathf.Clamp(atlasColumns, 1, _settings.FrameCount);
        }

        private void OnValidate()
        {
            _settings ??= new CausticsTextureSettings();
            _atlasColumns = Mathf.Clamp(_atlasColumns, 1, Mathf.Max(1, _settings.FrameCount));
        }
    }
}

#endif

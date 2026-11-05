#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace TakoLibEditor.Common
{
    internal sealed class TextureGeneratorInspectorPanel : IDisposable
    {
        private readonly TextureGeneratorPreset data;
        private readonly SerializedObject properties;
        private readonly TextureGeneratorPreview preview = new TextureGeneratorPreview();
        private readonly Action repaint;
        public TextureGeneratorInspectorPanel(TextureGeneratorPreset data, Action repaint)
        {
            this.data = data; this.repaint = repaint;
            properties = new SerializedObject(data); preview.SetRecipe(data.recipe);
            EditorApplication.update += Tick;
        }
        private void Tick() { if (preview.Tick()) repaint(); }
        private static void Run(Action action)
        {
            try { action(); }
            catch (ExitGUIException) { throw; }
            catch (Exception e) { Debug.LogException(e); EditorUtility.DisplayDialog("Texture Generator", e.Message, "OK"); }
        }
        public void Draw(bool allowSavePreset = false)
        {
            if (data == null) return;
            properties.Update();
            TextureGeneratorGUI.DrawRecipe(properties.FindProperty("recipe"), preview.DrawHistogram);
            properties.ApplyModifiedProperties();
            preview.SetRecipe(data.recipe);
            EditorGUILayout.Space();

            preview.Draw(data.recipe.generate3D, preview.Request, preview.HasStarted ? "プレビューを再生成" : "プレビューを生成");
            EditorGUILayout.Space();
            if (allowSavePreset && GUILayout.Button("Presetとして保存…"))
                Run(() => TextureGeneratorAssetUtility.SavePreset(data.recipe));
            if (data.recipe.generate3D)
                EditorGUILayout.LabelField("Zスライスを並べたPNGを、標準Importerの3D形状で取り込みます。", EditorStyles.miniLabel);
            if (GUILayout.Button(data.recipe.generate3D ? "3D PNGを出力…" : "PNGを出力…"))
                Run(() => TextureGeneratorAssetUtility.ExportPNG(data.recipe));
        }
        public void Dispose() { EditorApplication.update -= Tick; preview.Dispose(); }
    }

    [CustomEditor(typeof(TextureGeneratorPreset))]
    public sealed class TextureGeneratorPresetEditor : Editor
    {
        private TextureGeneratorInspectorPanel panel;
        private void OnEnable() => panel = new TextureGeneratorInspectorPanel((TextureGeneratorPreset)target, Repaint);
        private void OnDisable() => panel?.Dispose();
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("生成設定を保存するプリセットです。2D・3DともにPNGへ出力し、Unity標準のTextureImporterで取り込みます。PNGはこのSOを参照しません。", MessageType.Info);
            panel?.Draw();
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TakoLibEditor.Common
{
    public sealed class TextureGeneratorWindow : EditorWindow
    {
        [SerializeField] private TextureGeneratorRecipe recipe = new TextureGeneratorRecipe();
        [SerializeField] private Vector2 scroll;
        private TextureGeneratorPreset working;
        private TextureGeneratorInspectorPanel panel;

        [MenuItem("Tools/TakoLib/Texture Generator")]
        private static void Open() => GetWindow<TextureGeneratorWindow>("Texture Generator").Show();

        private void OnEnable()
        {
            minSize = new Vector2(410, 580);
            working = CreateInstance<TextureGeneratorPreset>();
            working.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            working.recipe = recipe;
            panel = new TextureGeneratorInspectorPanel(working, Repaint);
        }

        private void OnDisable()
        {
            if (working != null) recipe = working.recipe;
            panel?.Dispose();
            panel = null;
            if (working != null) DestroyImmediate(working);
            working = null;
        }

        private void OnGUI()
        {
            if (panel == null) return;
            using (var viewport = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = viewport.scrollPosition;
                panel.Draw(allowSavePreset: true);
            }
            recipe = working.recipe;
        }
    }
}
#endif
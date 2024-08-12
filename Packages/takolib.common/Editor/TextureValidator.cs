#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Linq;
using TakoLib.Common.Extensions;

namespace TakoLibEditor.Common
{

    public class TextureValidator : EditorWindow
    {
        private const string MENU_NAME_TEXTURE_VALIDATOR = "Assets/Tools/テクスチャの4の倍数チェック";

        private static TextureValidator _window = null;

        private Texture2D[] _filteredTextures;

        //検索の際に除外する文字列。
        private string _filter = "Atlas";
        private string _prevFilter;

        private Vector2 _scrollPosition;

        private Vector2Int _scrollArea = new Vector2Int(600, 500);

        [MenuItem(MENU_NAME_TEXTURE_VALIDATOR)]
        private static void Run()
        {
            _window = CreateInstance<TextureValidator>();
            _window.titleContent = new GUIContent("テクスチャの4の倍数チェック");
            _window.minSize = _window._scrollArea;
            _window.Setup();
            _window.ShowUtility();
        }

        private void Setup()
        {
            AssetDatabase.Refresh();

            _filteredTextures = AssetDatabase.FindAssets("t: texture", new[] { "Assets" })
                .Select(x => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(x)))
                .Where(x => x)
                .Where(x => !AssetDatabase.GetAssetPath(x).Contains(_filter))//検索フィルター。
                .Where(x => x.width % 4 != 0 || x.height % 4 != 0)//サイズが4の番数でないもの。
                .ToArray();
        }


        private void OnGUI()
        {
            EditorGUILayout.LabelField("4の倍数でないテクスチャを検索するツールです");
            EditorGUILayout.Space();

            if (GUILayout.Button("再検索", GUILayout.Width(60), GUILayout.Height(20))) Setup();
            EditorGUILayout.Space();

            _filter = EditorGUILayout.TextField("除外するキーワード：", _filter);
            if (_prevFilter != _filter) Setup();
            _prevFilter = _filter;

            if (_filteredTextures.IsNullOrEmpty())
            {
                EditorGUILayout.HelpBox("テクスチャがありませんでした。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"見つかったテクスチャ：{_filteredTextures.Length} 枚");
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("4の倍数でないテクスチャ一覧", EditorStyles.boldLabel);

            int sizeLabelWidth = 100;
            int textureLabelWidth = 1000;
            int height = 20;

            EditorGUI.indentLevel++;

            Rect currentRect = GUILayoutUtility.GetRect(0, height);
            Rect sizeRect = new Rect(currentRect.x, currentRect.y, sizeLabelWidth, height);
            Rect textureRect = new Rect(currentRect.x + sizeLabelWidth, currentRect.y, textureLabelWidth, height);

            EditorGUI.LabelField(sizeRect, "サイズ");
            EditorGUI.LabelField(textureRect, "テクスチャ");

            Rect scrollRect = GUILayoutUtility.GetRect(_scrollArea.x, _scrollArea.y);
            Rect scrollViewRect = new Rect(scrollRect.x, scrollRect.y, sizeLabelWidth + textureLabelWidth, height * _filteredTextures.Length);

            EditorGUI.DrawRect(scrollRect, new Color(0, 0, 0, 0.2f));

            using (var scroll = new GUI.ScrollViewScope(scrollRect, _scrollPosition, scrollViewRect))
            {
                _scrollPosition = scroll.scrollPosition;

                foreach (Texture2D texture in _filteredTextures)
                {
                    if (!texture) continue;

                    sizeRect.y += height;
                    textureRect.y += height;

                    string widthText = texture.width % 4 == 0 ? texture.width.ToString() : texture.width.ToString().TagColor(Color.red).TagBold();
                    string heightText = texture.height % 4 == 0 ? texture.height.ToString() : texture.height.ToString().TagColor(Color.red).TagBold();

                    EditorGUI.LabelField(sizeRect, $"( {widthText}, {heightText} )", TakoLibEditorUtility.StyleRichTextLabel);
                    if (GUI.Button(textureRect, $"{AssetDatabase.GetAssetPath(texture)}", EditorStyles.label))
                    {
                        Selection.activeObject = texture;
                    }
                }
            }
        }
    }
}

#endif
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TakoLibEditor.Common
{
    public static class TakoLibEditorUtility
    {
        /// <summary>
        /// 型Tのスクリプトファイルのパスを取得する。
        /// </summary>
        public static string GetScriptFilePath<T>() where T : ScriptableObject
        {
            //C#ではC#ファイルのパスを取得する方法は無い。
            //そこでやや強引だがUnityのScriptableObjectの機能を利用して取得する。
            ScriptableObject instance = ScriptableObject.CreateInstance(typeof(T));
            MonoScript monoScript = MonoScript.FromScriptableObject(instance);
            string scriptPath = AssetDatabase.GetAssetPath(monoScript);

            UnityEngine.Object.DestroyImmediate(instance);

            return Path.GetDirectoryName(scriptPath);
        }


        public static GUIStyle StyleRichTextLabel => new GUIStyle(EditorStyles.label)
        {
            richText = true,
        };

        public static GUIStyle StyleRichTextWrapLabel => new GUIStyle(EditorStyles.label)
        {
            richText = true,
            wordWrap = true,
        };

        /// <summary>
        /// インスペクタに仕切り線を描く。
        /// </summary>
        /// <param name="space">仕切り線の上下の空白</param>
        public static void DrawSeparator(float space = 0) => DrawSeparator(Color.gray, space);

        /// <summary>
        /// インスペクタに仕切り線を描く。
        /// </summary>
        /// <param name="color">仕切り線の色</param>
        /// <param name="space">仕切り線の上下の空白</param>
        public static void DrawSeparator(Color color, float space = 0)
        {
            EditorGUILayout.Space(space);
            Rect rect = GUILayoutUtility.GetRect(0, 1);
            Color defaultColor = Handles.color;
            Handles.color = color;
            Handles.DrawLine(rect.position, new Vector3(rect.position.x + rect.width, rect.y));
            Handles.color = defaultColor;
            EditorGUILayout.Space(space);
        }
    }
}
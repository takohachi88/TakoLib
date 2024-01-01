using System.IO;
using UnityEditor;
using UnityEngine;

namespace TakoLib.Common.Editor
{
    public static class TakoLibEditor
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

            Object.DestroyImmediate(instance);

            return Path.GetDirectoryName(scriptPath);
        }

        /// <summary>
        /// takolib.commonリポジトリのrootパス。
        /// </summary>
        internal static string GetBasePath() => GetScriptFilePath<TakoLibCommonRoot>();

        public static GUIStyle StyleRichTextLabel => new GUIStyle(EditorStyles.label)
        {
            richText = true,
        };
    }
}
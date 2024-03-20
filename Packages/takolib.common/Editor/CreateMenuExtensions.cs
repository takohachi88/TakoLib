using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TakoLib.Common.Editor
{
    /// <summary>
    /// Createメニューに項目を追加する。
    /// テンプレートファイルはこのC#スクリプトと同じディレクトリに置く。
    /// ScriptableObjectを継承しているのはテンプレートファイルを取得する際の実装上の都合によるもので、実際にScriptableObjectとして用いるわけではない。
    /// </summary>
    public class CreateMenuExtensions<T> : ScriptableObject where T : CreateMenuExtensions<T>
    {

        protected const string MenuItemRoot = "Assets/Create/";

        /// <summary>
        /// Createメニューの階層（シェーダー）。
        /// </summary>
        protected const string MenuItemShaderRoot = "Assets/Create/Shader/";

        /// <summary>
        /// Createメニューの階層（C#）。
        /// </summary>
        protected const string MenuItemCsRoot = "Assets/Create/Custom C# Scripts/";

        protected enum FileType
        {
            Shader, // シェーダーファイル。
            Cs, // C#スクリプト。
        }

        /// <summary>
        /// FileTypeに対応する拡張子。
        /// </summary>
        protected static readonly Dictionary<FileType, string> ExtensionConversion = new()
        {
            [FileType.Shader] = "shader",
            [FileType.Cs] = "cs",
        };

        /// <summary>
        /// このC#スクリプトファイルがあるディレクトリを取得する。
        /// </summary>
        /// <returns>このC#スクリプトファイルのディレクトリ</returns>
        private static string GetFilePath() => TakoLibEditor.GetScriptFilePath<T>();


        /// <summary>
        /// ファイルを生成する。
        /// </summary>
        /// <param name="fileType">ファイルの種類</param>
        /// <param name="fileName">ファイルの名前</param>
        /// <param name="defaultNewFileName">生成されるファイルのデフォルトの名前</param>
        protected static void CreateFile(FileType fileType, string fileName, string defaultNewFileName)
        {

            string extension = ExtensionConversion[fileType];
            string templatePath = $"{GetFilePath()}/{fileName}.{extension}.txt";
            string defaultNewFileFullName = $"{defaultNewFileName}.{extension}";

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, defaultNewFileFullName);
        }
    }
}

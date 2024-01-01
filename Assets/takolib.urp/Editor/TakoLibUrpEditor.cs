using TakoLib.Common.Editor;

namespace TakoLib.Urp.Editor
{
    public static class TakoLibUrpEditor
    {
        /// <summary>
        /// takolib.urpリポジトリのrootパス。
        /// </summary>
        internal static string GetBasePath() => TakoLibEditor.GetScriptFilePath<TakoLibUrpRoot>();
    }
}
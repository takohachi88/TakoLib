#if UNITY_EDITOR

using UnityEngine;

namespace TakoLib.Urp.Editor
{
    /// <summary>
    /// Packageを開発する場合、開発側と使用側とでパスが変わってしまう。
    /// そのため、このスクリプトファイルの場所をtakolib.urpリポジトリのRootとし、TakoLibEditor.GetUrpPath()で取得する。
    /// </summary>
    public class TakoLibUrpRoot : ScriptableObject
    {
    }
}

#endif
#if UNITY_EDITOR

using UnityEngine;

namespace TakoLib.Common.Editor
{
    /// <summary>
    /// Packageを開発する場合、開発側と使用側とでパスが変わってしまう。
    /// そのため、このスクリプトファイルの場所をCommonリポジトリのRootとし、TakoLibCommonEditor.GetBasePath()で取得する。
    /// </summary>
    public class TakoLibCommonRoot : ScriptableObject
    {
    }
}

#endif
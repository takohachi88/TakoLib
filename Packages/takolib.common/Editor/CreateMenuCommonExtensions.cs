#if UNITY_EDITOR

using TakoLib.Common.Editor;
using UnityEditor;

public class CreateMenuCommonExtensions : CreateMenuExtensions<CreateMenuCommonExtensions>
{
    [MenuItem(MenuItemRoot + "C# Script (MonoBehaviour Basic)", false, 80)]
    private static void CreateMonoBehaviourBasic() => CreateFile(FileType.Cs, "MonoBehaviourBasic", "new Custom MonoBehaviour Basic");

    [MenuItem(MenuItemRoot + "C# Script (MonoBehaviour Advanced)", false, 80)]
    private static void CreateMonoBehaviourAdvanced() => CreateFile(FileType.Cs, "MonoBehaviourAdvanced", "new Custom MonoBehaviour Advanced");
}

#endif
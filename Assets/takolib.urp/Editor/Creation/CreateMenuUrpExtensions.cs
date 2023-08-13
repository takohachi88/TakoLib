using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TakoLibrary.Common.Editor;
using UnityEditor;

public class CreateMenuUrpExtensions : CreateMenuExtensions<CreateMenuUrpExtensions>
{
    /// <summary>
    /// URPのミニマムなシェーダーを作成する。
    /// </summary>
    [MenuItem(MenuItemShaderRoot + "URP Minimum Shader")]
    private static void CreateUrpMinimumShader() => CreateFile(FileType.Shader, "UrpMinimum", "new URP Minimum");


    /// <summary>
    /// URPのいろんなオプション付きシェーダーを作成する。
    /// </summary>
    [MenuItem(MenuItemShaderRoot + "URP Advanced Shader")]
    private static void CreateUrpAdvancedShader() => CreateFile(FileType.Shader, "UrpAdvanced", "new URP Unlit Advanced");

    /// <summary>
    /// URPのVFX（Shuriken）用シェーダーを作成する。
    /// </summary>
    [MenuItem(MenuItemShaderRoot + "URP VFX Shader")]
    private static void CreateUrpVfxShader() => CreateFile(FileType.Shader, "UrpVfx", "new URP Unlit Vfx");
}

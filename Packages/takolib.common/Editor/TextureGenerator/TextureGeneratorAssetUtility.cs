#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TakoLibEditor.Common
{
    public static class TextureGeneratorAssetUtility
    {
        public static string Absolute(string path) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
        [MenuItem("Assets/Create/TakoLib/Texture Generator Preset", false, 2100)]
        private static void CreatePresetMenu() => SavePreset(new TextureGeneratorRecipe());
        public static void SavePreset(TextureGeneratorRecipe recipe)
        {
            string path = EditorUtility.SaveFilePanelInProject("プリセットを保存", "Texture Generator Preset", "asset", "保存先を選択", "Assets");
            if (string.IsNullOrEmpty(path)) return;
            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
            TextureGeneratorPreset preset;
            if (existing != null)
            {
                preset = existing as TextureGeneratorPreset;
                if (preset == null)
                {
                    EditorUtility.DisplayDialog("保存先", "このファイルはTexture Generatorプリセットではありません。別の保存先を選択してください。", "OK");
                    return;
                }
                Undo.RecordObject(preset, "Save Texture Generator Preset");
                preset.recipe = recipe.Clone();
                EditorUtility.SetDirty(preset);
            }
            else
            {
                preset = ScriptableObject.CreateInstance<TextureGeneratorPreset>();
                preset.recipe = recipe.Clone();
                AssetDatabase.CreateAsset(preset, path);
            }
            AssetDatabase.SaveAssetIfDirty(preset);
            Selection.activeObject = preset;
            EditorGUIUtility.PingObject(preset);
        }
        public static Color32[] GeneratePixels(TextureGeneratorRecipe recipe)
        {
            recipe.Normalize();
            if (recipe.generate3D) return TextureGeneratorEngine.Generate3DPixels(recipe.volumeSize.x, recipe.volumeSize.y, recipe.volumeSize.z, recipe.noise);
            Texture2D temporary = TextureGeneratorEngine.Generate(recipe.size.x, recipe.size.y, recipe.noise);
            try { return temporary.GetPixels32(); }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }
        }
        public static Vector2Int GetVolumePNGLayout(Vector3Int size)
        {
            if (size.x < 1 || size.y < 1 || size.z < 1) throw new ArgumentOutOfRangeException(nameof(size));
            int columns = 1, bestSide = int.MaxValue;
            // Exact divisors avoid adding blank slices to the imported volume.
            for (int candidate = 1; candidate <= size.z; candidate++)
            {
                if (size.z % candidate != 0) continue;
                int side = Mathf.Max(size.x * candidate, size.y * (size.z / candidate));
                if (side < bestSide) { bestSide = side; columns = candidate; }
            }
            return new Vector2Int(columns, size.z / columns);
        }
        public static Color32[] PackVolumePNG(Color32[] voxels, Vector3Int size, Vector2Int grid)
        {
            if (voxels == null || (long)size.x * size.y * size.z != voxels.Length ||
                grid.x < 1 || grid.y < 1 || (long)grid.x * grid.y != size.z)
                throw new ArgumentException("3DデータとPNGの分割数が一致しません。");
            int width = checked(size.x * grid.x), height = checked(size.y * grid.y);
            var atlas = new Color32[checked(width * height)];
            for (int z = 0; z < size.z; z++)
            {
                int tileX = z % grid.x;
                int tileY = grid.y - 1 - z / grid.x;
                for (int y = 0; y < size.y; y++)
                    Array.Copy(voxels, (z * size.y + y) * size.x, atlas,
                        (tileY * size.y + y) * width + tileX * size.x, size.x);
            }
            return atlas;
        }
        public static void ExportPNG(TextureGeneratorRecipe source)
        {
            TextureGeneratorRecipe recipe = source.Clone(); recipe.Normalize();
            Vector2Int grid = recipe.generate3D ? GetVolumePNGLayout(recipe.volumeSize) : Vector2Int.one;
            int width = recipe.generate3D ? recipe.volumeSize.x * grid.x : recipe.size.x;
            int height = recipe.generate3D ? recipe.volumeSize.y * grid.y : recipe.size.y;
            int largestSide = Mathf.Max(width, height);
            int limit = Mathf.Min(16384, SystemInfo.maxTextureSize);
            if (largestSide > limit)
                throw new InvalidOperationException($"PNGのサイズが{width}×{height}になり、最大{limit}pxを超えます。XY解像度を下げるか、奥行きを分割しやすい数にしてください。");
            int maxSize = Mathf.Clamp(Mathf.NextPowerOfTwo(largestSide), 32, 16384);
            string path = EditorUtility.SaveFilePanelInProject("PNGを保存", "GeneratedTexture", "png", "保存先を選択", "Assets");
            if (string.IsNullOrEmpty(path)) return;
            Texture2D texture = null;
            try
            {
                Color32[] pixels = recipe.tone.Apply(GeneratePixels(recipe));
                if (recipe.generate3D) pixels = PackVolumePNG(pixels, recipe.volumeSize, grid);
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                texture.SetPixels32(pixels); texture.Apply(false, false);
                File.WriteAllBytes(Absolute(path), texture.EncodeToPNG());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default; importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = false;
                importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = recipe.noise.seamless ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.filterMode = recipe.Filter;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.textureShape = recipe.generate3D ? TextureImporterShape.Texture3D : TextureImporterShape.Texture2D;
                settings.flipbookColumns = grid.x; settings.flipbookRows = grid.y;
                importer.SetTextureSettings(settings);
                var defaults = importer.GetDefaultPlatformTextureSettings();
                defaults.maxTextureSize = maxSize;
                importer.SetPlatformTextureSettings(defaults);
                importer.SaveAndReimport();
                EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(path));
            }
            finally { if (texture != null) UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
#endif

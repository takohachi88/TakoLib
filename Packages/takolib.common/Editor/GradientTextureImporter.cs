using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TakoLibEditor.Common
{
	/// <summary>
	/// gradient textureをUnity上で作成する機能。
	/// </summary>
	[ScriptedImporter(1, "gradienttexture")]
	public class GradientTextureImporter : ScriptedImporter
	{
		[MenuItem("Assets/Create/2D/Gradient Texture", true)]
		private static bool CreateAssetValidate() => AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));

		[MenuItem("Assets/Create/2D/Gradient Texture")]
		private static void CreateAsset()
		{
			Texture2D texture = new(16, 1);
			texture.Apply();

			string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/gradient_.gradienttexture");
			File.WriteAllBytes(path, texture.EncodeToPNG());
			AssetDatabase.Refresh();
		}

		[SerializeField]
		private Gradient _gradient = new()
		{
			colorKeys = new GradientColorKey[]
			{
				new GradientColorKey(Color.red, 0f),
				new GradientColorKey(Color.blue, 1f)
			},
		};
		[SerializeField] private Vector2Int _size = new(16, 1);
		[SerializeField] private TextureWrapMode _wrapMode = TextureWrapMode.Clamp;
		[SerializeField] private FilterMode _filterMode = FilterMode.Bilinear;
		[SerializeField] private TextureFormat _format = TextureFormat.RGBA32;
		[SerializeField] private bool _linear = false;

		public override void OnImportAsset(AssetImportContext ctx)
		{
			if (_size.x <= 1 || _size.y <= 0 || _gradient == null) return;

			Texture2D texture = new(_size.x, _size.y, _format, false, _linear);
			texture.wrapMode = _wrapMode;
			texture.filterMode = _filterMode;

			for (int x = 0; x < _size.x; x++)
			{
				for (int y = 0; y < _size.y; y++)
				{
					texture.SetPixel(x, y, _gradient.Evaluate((float)x / (_size.x - 1)));
				}
			}
			texture.Apply();

			ctx.AddObjectToAsset("GT", texture);
			ctx.SetMainObject(texture);
		}
	}
}

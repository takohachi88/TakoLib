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
		private const string MENU_PATH = "Assets/Create/2D/Gradient Texture";
		[MenuItem(MENU_PATH, true)]
		private static bool CreateAssetValidate() => AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));

		[MenuItem(MENU_PATH)]
		private static void CreateAsset()
		{
			Texture2D texture = new(16, 1);
			texture.Apply();

			string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/gradient_.gradienttexture");
			File.WriteAllBytes(path, texture.EncodeToPNG());
			AssetDatabase.Refresh();
		}

		private enum SpecifyMode
		{
			Curve,
			Gradient,
		}

		[SerializeField] private SpecifyMode _colorSpecifyMode = SpecifyMode.Gradient;

		[SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		[SerializeField, GradientUsage(true)]
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

		public override void OnImportAsset(AssetImportContext context)
		{
			if (_size.x <= 1 || _size.y <= 0 || _gradient == null) return;

			Texture2D texture = new(_size.x, _size.y, _format, false, _linear);
			texture.wrapMode = _wrapMode;
			texture.filterMode = _filterMode;

			for (int x = 0; x < _size.x; x++)
			{
				for (int y = 0; y < _size.y; y++)
				{
					switch (_colorSpecifyMode)
					{
						case SpecifyMode.Gradient:
							texture.SetPixel(x, y, _gradient.Evaluate((float)x / (_size.x - 1)));
							break;
						case SpecifyMode.Curve:
							float curve = _curve.Evaluate((float)x / (_size.x - 1));
							texture.SetPixel(x, y, new Color(curve, curve, curve, curve));
							break;
					}
                }
			}
			texture.Apply();

			context.AddObjectToAsset("Gradient Texture", texture);
			context.SetMainObject(texture);
		}

		[CustomEditor(typeof(GradientTextureImporter))]
		public class GradientTextureImporterEditor : ScriptedImporterEditor
		{
			private GradientTextureImporter _target;

            public override void OnInspectorGUI()
            {
				_target = target as GradientTextureImporter;

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._colorSpecifyMode)));

				switch (_target._colorSpecifyMode)
				{
					case SpecifyMode.Gradient:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._gradient)));
                        break;
					case SpecifyMode.Curve:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._curve)));
                        break;
				}
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._size)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._wrapMode)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._filterMode)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._format)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._linear)));

                ApplyRevertGUI();
            }
		}
	}
}

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

		[SerializeField] private AnimationCurve _curve;

		[SerializeField, GradientUsage(true)]
		private Gradient _gradient;
		[SerializeField] private Vector2Int _size = new(16, 1);
		[SerializeField] private bool _vertical = false;
		[SerializeField] private TextureWrapMode _wrapMode = TextureWrapMode.Clamp;
		[SerializeField] private FilterMode _filterMode = FilterMode.Bilinear;
		[SerializeField] private TextureFormat _format = TextureFormat.RGBA32;
		[SerializeField] private bool _linear = false;
		[SerializeField] private bool _sprite = false;

		public override void OnImportAsset(AssetImportContext context)
		{
			_size.x = Mathf.Max(_vertical ? 1 : 2, _size.x);
			_size.y = Mathf.Max(_vertical ? 2 : 1, _size.y);

			if (_gradient == null)
			{
				_gradient = new()
				{
					colorKeys = new GradientColorKey[]
					{
						new GradientColorKey(Color.red, 0f),
						new GradientColorKey(Color.blue, 1f)
					},
				};
			}

			if (_curve == null)
			{
				_curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
			}

			Texture2D texture = new(_size.x, _size.y, _format, false, _linear);
            texture.name = "Texture2D";
			texture.wrapMode = _wrapMode;
			texture.filterMode = _filterMode;

			for (int x = 0; x < _size.x; x++)
			{
				for (int y = 0; y < _size.y; y++)
				{
					float progress = _vertical ? (float)y / (_size.y - 1) : (float)x / (_size.x - 1);
		            switch (_colorSpecifyMode)
					{
						case SpecifyMode.Gradient:
							texture.SetPixel(x, y, _gradient.Evaluate(progress));
							break;
						case SpecifyMode.Curve:
							float curve = _curve.Evaluate(progress);
							texture.SetPixel(x, y, new Color(curve, curve, curve, curve));
							break;
					}
                }
			}
			texture.Apply();

			if (_sprite)
			{
                Sprite sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f));
                context.AddObjectToAsset("Sprite", sprite);
                context.AddObjectToAsset("Texture", texture);//Texture本体の保持も必要。Spriteからはあくまで参照されているだけ。
                context.SetMainObject(sprite);
            }
			else
			{
                context.AddObjectToAsset("Texture", texture);
                context.SetMainObject(texture);
            }
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
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._vertical)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._wrapMode)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._filterMode)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._format)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._linear)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._sprite)));

                ApplyRevertGUI();
            }
		}
	}
}

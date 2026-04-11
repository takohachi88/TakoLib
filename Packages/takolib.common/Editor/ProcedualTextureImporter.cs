#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace TakoLibEditor.Common
{
	/// <summary>
	/// gradient textureをUnity上で作成する機能。
	/// </summary>
	[ScriptedImporter(1, "procedualtexture")]
	public class ProcedualTextureImporter : ScriptedImporter
	{
		private const string MENU_PATH = "Assets/Create/2D/Procedual Texture";
		[MenuItem(MENU_PATH, true)]
		private static bool CreateAssetValidate() => AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));

		[MenuItem(MENU_PATH)]
		private static void CreateAsset()
		{
			Texture2D texture = new(16, 1);
			texture.Apply();

			string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{ProcedualTextureImporterSettings.instance.DefaultFileName}.procedualtexture");
			//名前を付けて保存する動作のため。
            var endAction = ScriptableObject.CreateInstance<CreateGradientTextureAction>();
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(default, endAction, path, null, null);
		}

		private enum SpecifyMode
		{
			Curve,
			Gradient,
			Shader,
		}

		[SerializeField] private SpecifyMode _colorSpecifyMode = SpecifyMode.Gradient;

		[SerializeField] private AnimationCurve _curveR;
		[SerializeField] private AnimationCurve _curveG;
		[SerializeField] private AnimationCurve _curveB;
		[SerializeField] private AnimationCurve _curveA;

		[SerializeField, GradientUsage(true)]
		private Gradient _gradient;

		[SerializeField, TextArea(10, 15)]
		private string _shaderCode;

		[Serializable]
		public class ShaderMessage
		{
			public string Message;
			public int Severity;
		}
		[SerializeField, HideInInspector] private ShaderMessage[] _shaderMessages;

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

			if (_curveR == null) _curveR = AnimationCurve.EaseInOut(0, 1, 1, 1);
			if (_curveG == null) _curveG = AnimationCurve.EaseInOut(0, 1, 1, 1);
			if (_curveB == null) _curveB = AnimationCurve.EaseInOut(0, 1, 1, 1);
			if (_curveA == null) _curveA = AnimationCurve.EaseInOut(0, 0, 1, 1);

			Texture2D texture = new(_size.x, _size.y, _format, false, _linear);
			texture.name = "Texture2D";
			texture.wrapMode = _wrapMode;
			texture.filterMode = _filterMode;

			float CalculateProgress(int x, int y) => _vertical ? (float)y / (_size.y - 1) : (float)x / (_size.x - 1);

			switch (_colorSpecifyMode)
			{
				case SpecifyMode.Gradient:
					for (int x = 0; x < _size.x; x++)
					{
						for (int y = 0; y < _size.y; y++)
						{
							float progress = CalculateProgress(x, y);
							texture.SetPixel(x, y, _gradient.Evaluate(progress));
						}
					}
					break;
				case SpecifyMode.Curve:
					for (int x = 0; x < _size.x; x++)
					{
						for (int y = 0; y < _size.y; y++)
						{
							float progress = CalculateProgress(x, y);
							float curveR = _curveR.Evaluate(progress);
							float curveG = _curveG.Evaluate(progress);
							float curveB = _curveB.Evaluate(progress);
							float curveA = _curveA.Evaluate(progress);
							texture.SetPixel(x, y, new Color(curveR, curveG, curveB, curveA));
						}
					}
					break;
				case SpecifyMode.Shader:
					texture = GenerateTextureFromShader(texture);
					break;
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

		private Texture2D GenerateTextureFromShader(Texture2D texture)
		{
            Shader shader = ShaderUtil.CreateShaderAsset(_shaderCode);
			Material material = new Material(shader);

			RenderTexture rt = RenderTexture.GetTemporary(_size.x, _size.y, 0, RenderTextureFormat.ARGB32);
			rt.wrapMode = _wrapMode;
			rt.filterMode = _filterMode;
			rt.Create();
			Graphics.Blit(null, rt, material);

			RenderTexture previous = RenderTexture.active;
			RenderTexture.active = rt;

			texture.ReadPixels(new Rect(0, 0, _size.x, _size.y), 0, 0);

			RenderTexture.active = previous;

			//シェーダーのコンパイルエラーを取得して保持しておき、エディタ拡張のほうで表示する。
			_shaderMessages = ShaderUtil
				.GetShaderMessages(shader)
				.Select(message => new ShaderMessage
				{
					Message = $"({message.line}) {message.message}",
					Severity = (int)message.severity,
				})
				.ToArray();

			RenderTexture.ReleaseTemporary(rt);
			DestroyImmediate(material);
			DestroyImmediate(shader);

			return texture;
		}

		[CustomEditor(typeof(ProcedualTextureImporter))]
		public class GradientTextureImporterEditor : ScriptedImporterEditor
		{
			private ProcedualTextureImporter _target;

			public override void OnInspectorGUI()
			{
				_target = target as ProcedualTextureImporter;

				serializedObject.Update();

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._colorSpecifyMode)));
				EditorGUI.indentLevel++;
				EditorGUILayout.BeginVertical(GUI.skin.box);

                switch (_target._colorSpecifyMode)
				{
					case SpecifyMode.Gradient:
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._gradient)));
						break;
					case SpecifyMode.Curve:
						Color color = GUI.backgroundColor;
						GUI.backgroundColor = Color.red;
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._curveR)));
						GUI.backgroundColor = Color.green;
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._curveG)));
						GUI.backgroundColor = Color.blue;
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._curveB)));
						GUI.backgroundColor = Color.white;
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._curveA)));
						GUI.backgroundColor = color;
						break;
					case SpecifyMode.Shader:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._shaderCode)));
                        //空欄の場合はProjectSettingsのテンプレートを使用する。
                        using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(_target._shaderCode)))
                        {
                            //インデントをボタンに適用する。
                            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                            rect = EditorGUI.IndentedRect(rect);
                            rect.size = new(200, 20);
                            if (GUI.Button(rect, "Apply template"))
                            {
                                _target._shaderCode = ProcedualTextureImporterSettings.instance.ShaderTemplate;
                            }
                        }
                        //シェーダーにコンパイルエラーがある場合はエラー内容を表示する。
                        foreach (ShaderMessage message in _target._shaderMessages)
						{
							EditorGUILayout.HelpBox(message.Message, message.Severity switch
							{
								0 => MessageType.Error,
								1 => MessageType.Warning,
								_ => MessageType.None,
							});
						}
						break;
				}

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;

				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._size)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._vertical)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._wrapMode)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._filterMode)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._format)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._linear)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._sprite)));

				ApplyRevertGUI();

				EditorGUILayout.Space();

				if (GUILayout.Button("Export as texture asset", GUILayout.Width(200), GUILayout.Height(20)))
				{
					string filePath = EditorUtility.SaveFilePanel("Export Procedual Texture", Application.dataPath, string.Empty, "png");
					if (string.IsNullOrEmpty(filePath)) return;
					string assetPath = filePath.Replace(Application.dataPath, "Assets");
					Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(_target.assetPath);
					if (!texture)
					{
						Debug.LogError($"[{nameof(ProcedualTextureImporter)}] Failed to load texture.");
						return;
					}
					File.WriteAllBytes(filePath, texture.EncodeToPNG());
					AssetDatabase.Refresh();
					Debug.Log($"[{nameof(ProcedualTextureImporter)}] Export completed. ({filePath})");
				}

				serializedObject.ApplyModifiedProperties();
			}
		}
	}

    /// <summary>
    /// Shaderの初期設定をProjectSettingsに保持する。
    /// </summary>
    [FilePath("ProjectSettings/ProcedualTextureImporter.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ProcedualTextureImporterSettings : ScriptableSingleton<ProcedualTextureImporterSettings>
    {
		private static readonly string SHADER_TEMPLATE = $@"// ProjectSettings/Procedual Texture Importer/Shader Template has been applied.
Shader ""Hidden/ProcedualTextureGenerated""
{{
    SubShader
    {{
        Tags {{ ""RenderType"" = ""Opaque"" }}
        Pass
        {{
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            struct Attributes
            {{
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            }};

            struct Varyings
            {{
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            }};

            Varyings vert (Attributes input)
            {{
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }}

            half4 frag(Varyings input) : SV_Target
            {{
                return half4(input.uv, 0, 1);
            }}

            ENDHLSL
        }}
    }}
}}";

		[SerializeField] private string _defaultFileName = "gradient_";
		[SerializeField, TextArea(10, 15)] private string _shaderTemplate;

		public string DefaultFileName => _defaultFileName;
        public string ShaderTemplate => _shaderTemplate;

        public void RevertToDefault()
		{
			_shaderTemplate = SHADER_TEMPLATE;
        }

        public void SaveSettings()
        {
            Save(true);
        }

        private void OnValidate()
        {
            Save(false);
        }
    }

	/// <summary>
	/// ProjectSettingsのUI。
	/// </summary>
    public static class ProcedualTextureImporterSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
			return new SettingsProvider($"Project/Procedual Texture Importer", SettingsScope.Project)
			{
				guiHandler = (searchContext) =>
				{
					var settings = ProcedualTextureImporterSettings.instance;
					SerializedObject serializedObject = new(settings);

					if (GUILayout.Button("Save Settings", GUILayout.Width(140))) settings.SaveSettings();

					EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultFileName"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("_shaderTemplate"), GUILayout.MinHeight(300));

					serializedObject.ApplyModifiedProperties();

					if (GUILayout.Button("Revert To Default", GUILayout.Width(140)))
					{
						settings.RevertToDefault();
					}
				},
			};
        }
    }

	/// <summary>
	/// .procedualtextureファイルを作成後に名前を編集するフェーズを設けるために必要なクラス。
	/// </summary>
    public class CreateGradientTextureAction : AssetCreationEndAction
    {
        public override void Action(EntityId entityId, string pathName, string resourceFile)
        {
			File.WriteAllBytes(pathName, new byte[1]);
            AssetDatabase.ImportAsset(pathName);
            AssetDatabase.Refresh();
            ProjectWindowUtil.ShowCreatedAsset(AssetDatabase.LoadMainAssetAtPath(pathName));
        }
    }
}

#endif
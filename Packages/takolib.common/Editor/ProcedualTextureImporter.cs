#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TakoLibEditor.Common
{
	/// <summary>
	/// procedual textureをUnity上で作成する機能。
	/// </summary>
	[ScriptedImporter(3, "procedualtexture")]
	public class ProcedualTextureImporter : ScriptedImporter, ISpriteEditorDataProvider, ISpriteNameFileIdDataProvider, ITextureDataProvider, ISpriteFrameEditCapability
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
			string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/procedual_.procedualtexture");
			//名前を付けて保存する動作のため。
            var endAction = ScriptableObject.CreateInstance<CreateProcedualTextureAction>();
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(default, endAction, path, null, null);
		}

		private enum SpecifyMode
		{
			Curve = 0,
			Gradient = 1,
			Shader = 2,
			MultipleGradients = 3,
		}

		[SerializeField] private SpecifyMode _colorSpecifyMode = SpecifyMode.Gradient;

		[SerializeField] private AnimationCurve _curveR;
		[SerializeField] private AnimationCurve _curveG;
		[SerializeField] private AnimationCurve _curveB;
		[SerializeField] private AnimationCurve _curveA;

		[SerializeField, GradientUsage(true)]
		private Gradient _gradient;
		[SerializeField, GradientUsage(true)]
		private Gradient[] _gradients;

		private static readonly string DefaultShaderCode = @"Shader ""Hidden/TakoLib/ProcedualTexture""
{
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl""

            float4x4 unity_MatrixVP;
            float4x4 unity_ObjectToWorld;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.positionOS));
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(input.uv, 0, 1);
            }

            ENDHLSL
        }
    }
}";

		[SerializeField, TextArea(10, 15)]
		private string _shaderCode = DefaultShaderCode;

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
		[SerializeField] private SpriteImportMode _spriteMode = SpriteImportMode.Single;
		[SerializeField] private float _pixelsPerUnit = 100f;
		[SerializeField] private SpriteRect _singleSpriteRect;
		[SerializeField] private SpriteRect[] _multipleSpriteRects = Array.Empty<SpriteRect>();

		public override void OnImportAsset(AssetImportContext context)
		{
			bool isMultipleGradients = _colorSpecifyMode == SpecifyMode.MultipleGradients;
			bool isVertical = !isMultipleGradients && _vertical;
			_size.x = Mathf.Max(isVertical ? 1 : 2, _size.x);
			_size.y = Mathf.Max(isVertical ? 2 : 1, _size.y);

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

			if (!CanGenerateTexture())
			{
				Texture2D invalidTexture = new(1, 1, TextureFormat.RGBA32, false, _linear);
				invalidTexture.name = "Texture2D";
				invalidTexture.SetPixel(0, 0, Color.clear);
				invalidTexture.Apply();
				context.LogImportError("Multiple Gradients requires at least one non-null Gradient.");
				context.AddObjectToAsset("Texture", invalidTexture);
				context.SetMainObject(invalidTexture);
				return;
			}

			Vector2Int outputSize = GetOutputTextureSize();
			Texture2D texture = new(outputSize.x, outputSize.y, _format, false, _linear);
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
				case SpecifyMode.MultipleGradients:
					for (int gradientIndex = 0; gradientIndex < _gradients.Length; gradientIndex++)
					{
						Gradient gradient = _gradients[gradientIndex];
						int yOffset = (_gradients.Length - gradientIndex - 1) * _size.y;
						for (int x = 0; x < _size.x; x++)
						{
							Color color = gradient.Evaluate((float)x / (_size.x - 1));
							for (int y = 0; y < _size.y; y++)
								texture.SetPixel(x, yOffset + y, color);
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

			context.AddObjectToAsset("Texture", texture);//Spriteから参照されるTexture本体もサブアセットとして保持する。

			if (!_sprite)
			{
				context.SetMainObject(texture);
				return;
			}

			EnsureSpriteEditorData();
			if (_spriteMode == SpriteImportMode.Multiple)
			{
				foreach (SpriteRect spriteRect in _multipleSpriteRects)
				{
					Sprite sprite = CreateSprite(texture, spriteRect);
					if (!sprite) continue;

					context.AddObjectToAsset($"Sprite_{spriteRect.spriteID}", sprite);
				}
				context.SetMainObject(texture);
			}
			else
			{
				Sprite sprite = CreateSprite(texture, _singleSpriteRect);
				context.AddObjectToAsset("Sprite", sprite);
				context.SetMainObject(sprite);
			}
		}

		private bool CanGenerateTexture()
		{
			return _colorSpecifyMode != SpecifyMode.MultipleGradients
				|| (_gradients != null && _gradients.Length > 0 && _gradients.All(gradient => gradient != null));
		}

		private Vector2Int GetOutputTextureSize()
		{
			if (_colorSpecifyMode != SpecifyMode.MultipleGradients)
				return _size;

			int gradientCount = _gradients?.Length ?? 0;
			long outputHeight = (long)_size.y * gradientCount;
			return new Vector2Int(_size.x, (int)Math.Min(outputHeight, int.MaxValue));
		}

		private void EnsureSpriteEditorData()
		{
			_pixelsPerUnit = Mathf.Max(0.01f, _pixelsPerUnit);
			if (_spriteMode != SpriteImportMode.Single && _spriteMode != SpriteImportMode.Multiple)
				_spriteMode = SpriteImportMode.Single;

			if (_singleSpriteRect == null)
			{
				_singleSpriteRect = new SpriteRect
				{
					name = "Sprite",
					alignment = SpriteAlignment.Center,
					pivot = new Vector2(0.5f, 0.5f),
					spriteID = GUID.Generate(),
				};
			}

			Vector2Int outputSize = GetOutputTextureSize();
			_singleSpriteRect.rect = new Rect(0, 0, outputSize.x, outputSize.y);
			if (string.IsNullOrEmpty(_singleSpriteRect.name)) _singleSpriteRect.name = "Sprite";
			if (_singleSpriteRect.spriteID.Empty()) _singleSpriteRect.spriteID = GUID.Generate();

			_multipleSpriteRects ??= Array.Empty<SpriteRect>();
			HashSet<GUID> spriteIds = new();
			for (int i = 0; i < _multipleSpriteRects.Length; i++)
			{
				SpriteRect spriteRect = _multipleSpriteRects[i];
				if (spriteRect == null)
				{
					spriteRect = CreateDefaultSpriteRect($"Sprite_{i}", new Rect(0, 0, outputSize.x, outputSize.y));
					_multipleSpriteRects[i] = spriteRect;
				}
				if (string.IsNullOrEmpty(spriteRect.name)) spriteRect.name = $"Sprite_{i}";
				GUID spriteId = spriteRect.spriteID;
				if (spriteId.Empty() || !spriteIds.Add(spriteId))
				{
					spriteRect.spriteID = GUID.Generate();
					spriteIds.Add(spriteRect.spriteID);
				}
			}
		}

		private Sprite CreateSprite(Texture2D texture, SpriteRect spriteRect)
		{
			Rect rect = ClampSpriteRect(spriteRect.rect, texture.width, texture.height);
			if (rect.width <= 0 || rect.height <= 0) return null;

			Vector4 border = ClampBorder(spriteRect.border, rect.size);
			Sprite sprite = Sprite.Create(
				texture,
				rect,
				spriteRect.pivot,
				_pixelsPerUnit,
				0,
				SpriteMeshType.FullRect,
				border);
			sprite.name = spriteRect.name;
			return sprite;
		}

		private static Rect ClampSpriteRect(Rect rect, int textureWidth, int textureHeight)
		{
			float xMin = Mathf.Clamp(rect.xMin, 0, textureWidth);
			float yMin = Mathf.Clamp(rect.yMin, 0, textureHeight);
			float xMax = Mathf.Clamp(rect.xMax, xMin, textureWidth);
			float yMax = Mathf.Clamp(rect.yMax, yMin, textureHeight);
			return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
		}

		private static Vector4 ClampBorder(Vector4 border, Vector2 rectSize)
		{
			border = new Vector4(
				Mathf.Max(0, border.x),
				Mathf.Max(0, border.y),
				Mathf.Max(0, border.z),
				Mathf.Max(0, border.w));

			float horizontal = border.x + border.z;
			if (horizontal > rectSize.x && horizontal > 0)
			{
				float scale = rectSize.x / horizontal;
				border.x *= scale;
				border.z *= scale;
			}

			float vertical = border.y + border.w;
			if (vertical > rectSize.y && vertical > 0)
			{
				float scale = rectSize.y / vertical;
				border.y *= scale;
				border.w *= scale;
			}
			return border;
		}

		private static SpriteRect CreateDefaultSpriteRect(string name, Rect rect)
		{
			return new SpriteRect
			{
				name = name,
				rect = rect,
				alignment = SpriteAlignment.Center,
				pivot = new Vector2(0.5f, 0.5f),
				spriteID = GUID.Generate(),
			};
		}

		private static SpriteRect CopySpriteRect(SpriteRect source)
		{
			if (source == null) return null;
			return new SpriteRect
			{
				name = source.name,
				rect = source.rect,
				alignment = source.alignment,
				pivot = source.pivot,
				border = source.border,
				customData = source.customData,
				spriteID = source.spriteID,
			};
		}

		SpriteImportMode ISpriteEditorDataProvider.spriteImportMode => _sprite ? _spriteMode : SpriteImportMode.None;
		float ISpriteEditorDataProvider.pixelsPerUnit => _pixelsPerUnit;
		UnityEngine.Object ISpriteEditorDataProvider.targetObject => this;

		SpriteRect[] ISpriteEditorDataProvider.GetSpriteRects()
		{
			EnsureSpriteEditorData();
			SpriteRect[] source = _spriteMode == SpriteImportMode.Multiple
				? _multipleSpriteRects
				: new[] { _singleSpriteRect };
			return source.Select(CopySpriteRect).ToArray();
		}

		void ISpriteEditorDataProvider.SetSpriteRects(SpriteRect[] spriteRects)
		{
			spriteRects ??= Array.Empty<SpriteRect>();
			if (_spriteMode == SpriteImportMode.Multiple)
			{
				_multipleSpriteRects = spriteRects
					.Where(spriteRect => spriteRect != null)
					.Select(CopySpriteRect)
					.ToArray();
			}
			else if (spriteRects.Length > 0 && spriteRects[0] != null)
			{
				_singleSpriteRect = CopySpriteRect(spriteRects[0]);
			}
		}

		void ISpriteEditorDataProvider.Apply()
		{
			EditorUtility.SetDirty(this);
			AssetDatabase.WriteImportSettingsIfDirty(assetPath);
		}

		void ISpriteEditorDataProvider.InitSpriteEditorDataProvider() => EnsureSpriteEditorData();

		T ISpriteEditorDataProvider.GetDataProvider<T>() where T : class
		{
			return this as T;
		}

		bool ISpriteEditorDataProvider.HasDataProvider(Type type)
		{
			return type == typeof(ITextureDataProvider)
				|| type == typeof(ISpriteNameFileIdDataProvider)
				|| type == typeof(ISpriteFrameEditCapability)
				|| type.IsAssignableFrom(GetType());
		}

		IEnumerable<SpriteNameFileIdPair> ISpriteNameFileIdDataProvider.GetNameFileIdPairs()
		{
			SpriteRect[] spriteRects = ((ISpriteEditorDataProvider)this).GetSpriteRects();
			return spriteRects.Select(spriteRect => new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID));
		}

		void ISpriteNameFileIdDataProvider.SetNameFileIdPairs(IEnumerable<SpriteNameFileIdPair> nameFileIdPairs)
		{
			if (nameFileIdPairs == null) return;
			Dictionary<GUID, string> names = nameFileIdPairs.ToDictionary(pair => pair.GetFileGUID(), pair => pair.name);
			SpriteRect[] spriteRects = _spriteMode == SpriteImportMode.Multiple
				? _multipleSpriteRects
				: new[] { _singleSpriteRect };
			foreach (SpriteRect spriteRect in spriteRects)
			{
				if (spriteRect != null && names.TryGetValue(spriteRect.spriteID, out string spriteName))
					spriteRect.name = spriteName;
			}
		}

		Texture2D ITextureDataProvider.texture => LoadGeneratedTexture();
		Texture2D ITextureDataProvider.previewTexture => LoadGeneratedTexture();

		void ITextureDataProvider.GetTextureActualWidthAndHeight(out int width, out int height)
		{
			Vector2Int outputSize = GetOutputTextureSize();
			width = outputSize.x;
			height = outputSize.y;
		}

		Texture2D ITextureDataProvider.GetReadableTexture2D() => LoadGeneratedTexture();

		EditCapability ISpriteFrameEditCapability.GetEditCapability()
		{
			return new EditCapability(EEditCapability.All);
		}

		void ISpriteFrameEditCapability.SetEditCapability(EditCapability editCapability)
		{
			// Procedual TextureではSprite Editorの全編集機能を常に許可する。
		}

		private Texture2D LoadGeneratedTexture()
		{
			return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Texture2D>().FirstOrDefault();
		}

		private Texture2D GenerateTextureFromShader(Texture2D texture)
		{
            Shader shader = ShaderUtil.CreateShaderAsset(_shaderCode);
			Material material = new Material(shader);

			RenderTextureFormat renderTextureFormat = _format switch
			{
				TextureFormat.RGBAFloat => RenderTextureFormat.ARGBFloat,
				TextureFormat.RGBAHalf => RenderTextureFormat.ARGBHalf,
				_ => RenderTextureFormat.ARGB32,
			};
			RenderTexture rt = RenderTexture.GetTemporary(_size.x, _size.y, 0, renderTextureFormat);
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
		public class ProcedualTextureImporterEditor : ScriptedImporterEditor
		{
			private ProcedualTextureImporter _target;

			protected override bool CanApply()
			{
				return base.CanApply()
					&& target is ProcedualTextureImporter importer
					&& importer.CanGenerateTexture();
			}

			public override void OnInspectorGUI()
			{
				_target = target as ProcedualTextureImporter;

				serializedObject.Update();

				SerializedProperty colorSpecifyModeProperty = serializedObject.FindProperty(nameof(_target._colorSpecifyMode));
				EditorGUILayout.PropertyField(colorSpecifyModeProperty);
				SpecifyMode colorSpecifyMode = (SpecifyMode)colorSpecifyModeProperty.intValue;
				EditorGUI.indentLevel++;
				EditorGUILayout.BeginVertical(GUI.skin.box);

				switch (colorSpecifyMode)
				{
					case SpecifyMode.Gradient:
						EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._gradient)));
						break;
					case SpecifyMode.MultipleGradients:
						SerializedProperty gradientsProperty = serializedObject.FindProperty(nameof(_target._gradients));
						EditorGUILayout.PropertyField(gradientsProperty, true);
						if (gradientsProperty.arraySize == 0)
							EditorGUILayout.HelpBox("Add at least one non-null Gradient to enable Apply and Export.", MessageType.Warning);
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
                                _target._shaderCode = DefaultShaderCode;
                            }
                        }
                        //シェーダーにコンパイルエラーがある場合はエラー内容を表示する。
						foreach (ShaderMessage message in _target._shaderMessages ?? Array.Empty<ShaderMessage>())
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

				SerializedProperty sizeProperty = serializedObject.FindProperty(nameof(_target._size));
				EditorGUILayout.PropertyField(sizeProperty);
				if (colorSpecifyMode == SpecifyMode.MultipleGradients)
				{
					SerializedProperty gradientsProperty = serializedObject.FindProperty(nameof(_target._gradients));
					Vector2Int size = sizeProperty.vector2IntValue;
					int width = Mathf.Max(2, size.x);
					int heightPerGradient = Mathf.Max(1, size.y);
					long resultHeight = (long)heightPerGradient * gradientsProperty.arraySize;
					EditorGUILayout.LabelField("Result Size", $"{width} x {resultHeight}");
				}
				else
				{
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._vertical)));
				}
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._wrapMode)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._filterMode)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._format)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._linear)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._sprite)));
				if (_target._sprite)
				{
					SerializedProperty spriteMode = serializedObject.FindProperty(nameof(_target._spriteMode));
					spriteMode.intValue = EditorGUILayout.IntPopup(
						"Sprite Mode",
						spriteMode.intValue,
						new[] { "Single", "Multiple" },
						new[] { (int)SpriteImportMode.Single, (int)SpriteImportMode.Multiple });
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(_target._pixelsPerUnit)), new GUIContent("Pixels Per Unit"));
				}

				serializedObject.ApplyModifiedProperties();
				ApplyRevertGUI();

				if (_target._sprite)
				{
					bool hasModified = HasModified();
					GUIContent spriteEditorContent = hasModified
						? new GUIContent("Sprite Editor", "Apply the importer settings before opening Sprite Editor.")
						: new GUIContent("Sprite Editor", "Edit the Sprite border, pivot, and slices.");
					using (new EditorGUI.DisabledScope(hasModified))
					{
						if (GUILayout.Button(spriteEditorContent))
						{
							string spriteAssetPath = _target.assetPath;
							EditorApplication.delayCall += () => OpenSpriteEditor(spriteAssetPath);
						}
					}
				}

				EditorGUILayout.Space();

				using (new EditorGUI.DisabledScope(!_target.CanGenerateTexture()))
				{
					if (GUILayout.Button("Export as PNG", GUILayout.Width(200), GUILayout.Height(20)))
						ExportTexture("png");

					GUIContent exportExrContent = new(
						"Export as EXR",
						"Exports a ZIP-compressed 32-bit float EXR. Use RGBAHalf or RGBAFloat as the texture Format to retain HDR values above 1.");
					if (GUILayout.Button(exportExrContent, GUILayout.Width(200), GUILayout.Height(20)))
						ExportTexture("exr");
				}

			}

			private void ExportTexture(string extension)
			{
				string filePath = EditorUtility.SaveFilePanel(
					"Export Procedual Texture",
					Application.dataPath,
					string.Empty,
					extension);
				if (string.IsNullOrEmpty(filePath)) return;

				Texture2D texture = _target.LoadGeneratedTexture();
				if (!texture)
				{
					Debug.LogError($"[{nameof(ProcedualTextureImporter)}] Failed to load texture.");
					return;
				}

				byte[] imageData = extension == "exr"
					? texture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat | Texture2D.EXRFlags.CompressZIP)
					: texture.EncodeToPNG();
				File.WriteAllBytes(filePath, imageData);
				AssetDatabase.Refresh();
				Debug.Log($"[{nameof(ProcedualTextureImporter)}] {extension.ToUpperInvariant()} export completed. ({filePath})");
			}

			private static void OpenSpriteEditor(string assetPath)
			{
				UnityEngine.Object spriteAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
				if (!spriteAsset) return;

				Selection.activeObject = spriteAsset;
				if (!EditorApplication.ExecuteMenuItem("Window/2D/Sprite Editor"))
					Debug.LogError($"[{nameof(ProcedualTextureImporter)}] Failed to open Sprite Editor.");
			}
		}
	}

	/// <summary>
	/// .procedualtextureファイルを作成後に名前を編集するフェーズを設けるために必要なクラス。
	/// </summary>
    public class CreateProcedualTextureAction : AssetCreationEndAction
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

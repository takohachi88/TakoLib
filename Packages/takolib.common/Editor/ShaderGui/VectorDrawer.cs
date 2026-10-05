using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TakoLibEditor.Common
{
    /// <summary>Shared component editing for ShaderLab Vector properties.</summary>
    public abstract class VectorDrawer : MaterialPropertyDrawer
    {
        private static readonly string[] Labels = { "X", "Y", "Z" };
        private readonly int _components;
        protected VectorDrawer(int components)
        {
            _components = components;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            if (prop.propertyType != ShaderPropertyType.Vector)
            {
                EditorGUI.LabelField(position, label, "Requires a Vector property.");
                return;
            }

            bool mixed = EditorGUI.showMixedValue;
            float labelWidth = EditorGUIUtility.labelWidth;
            int indent = EditorGUI.indentLevel;
            MaterialEditor.BeginProperty(position, prop);
            editor.BeginAnimatedCheck(position, prop);
            try
            {
                position.height = EditorGUIUtility.singleLineHeight;
                // MaterialEditor can leave almost the entire row allocated to the label.
                // Reserve room for each component before splitting the remaining rect.
                float inputWidth = _components * 60f + (_components - 1) * 4f;
                EditorGUIUtility.labelWidth = Mathf.Max(1f, Mathf.Min(labelWidth, position.width * 0.45f, position.width - inputWidth));
                position = EditorGUI.PrefixLabel(position, new GUIContent(label));
                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = 14;

                var properties = new MaterialProperty[prop.targets.Length];
                for (int i = 0; i < properties.Length; i++)
                    properties[i] = MaterialEditor.GetMaterialProperty(new[] { prop.targets[i] }, prop.name);

                float width = (position.width - (_components - 1) * 4) / _components;
                for (int component = 0; component < _components; component++)
                {
                    Rect field = position;
                    field.x += component * (width + 4);
                    field.width = width;

                    float value = properties[0].vectorValue[component];
                    EditorGUI.showMixedValue = false;
                    foreach (MaterialProperty target in properties) EditorGUI.showMixedValue |= !target.vectorValue[component].Equals(value);

                    EditorGUI.BeginChangeCheck();
                    float edited = EditorGUI.FloatField(field, Labels[component], value);
                    if (!EditorGUI.EndChangeCheck()) continue;

                    editor.RegisterPropertyChangeUndo(label);
                    foreach (MaterialProperty target in properties)
                    {
                        Vector4 vector = target.vectorValue;
                        vector[component] = edited;
                        target.vectorValue = vector;
                    }
                }
            }
            finally
            {
                EditorGUI.showMixedValue = mixed;
                EditorGUIUtility.labelWidth = labelWidth;
                EditorGUI.indentLevel = indent;
                editor.EndAnimatedCheck();
                MaterialEditor.EndProperty();
            }
        }
    }
}

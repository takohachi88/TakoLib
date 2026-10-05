using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TakoLibEditor.Common
{
    /// <summary>Use [MinMax(lower, upper)] on a Vector: X is minimum, Y is maximum.</summary>
    public sealed class MinMaxDrawer : MaterialPropertyDrawer
    {
        private readonly float _lower;
        private readonly float _upper;

        public MinMaxDrawer() : this(0, 1) { }

        public MinMaxDrawer(float lower, float upper)
        {
            _lower = Mathf.Min(lower, upper);
            _upper = Mathf.Max(lower, upper);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            if (prop.propertyType != ShaderPropertyType.Vector)
            {
                EditorGUI.LabelField(position, label, "[MinMax] requires a Vector property.");
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
                // Keep the label, numeric endpoints and slider on the same row.
                EditorGUIUtility.labelWidth = Mathf.Max(1f, Mathf.Min(labelWidth, position.width * 0.45f, position.width - 180f));
                position = EditorGUI.PrefixLabel(position, new GUIContent(label));
                EditorGUI.indentLevel = 0;

                var properties = new MaterialProperty[prop.targets.Length];
                bool minMixed = false;
                bool maxMixed = false;
                Vector4 first = prop.vectorValue;
                for (int i = 0; i < properties.Length; i++)
                {
                    properties[i] = MaterialEditor.GetMaterialProperty(new[] { prop.targets[i] }, prop.name);
                    minMixed |= !properties[i].vectorValue.x.Equals(first.x);
                    maxMixed |= !properties[i].vectorValue.y.Equals(first.y);
                }

                float fieldWidth = Mathf.Min(60, position.width * 0.25f);
                Rect minRect = new(position.x, position.y, fieldWidth, position.height);
                Rect maxRect = new(position.xMax - fieldWidth, position.y, fieldWidth, position.height);
                Rect sliderRect = new(minRect.xMax + 6, position.y, Mathf.Max(0, position.width - 2 * fieldWidth - 12), position.height);

                EditorGUI.showMixedValue = minMixed;
                EditorGUI.BeginChangeCheck();
                float min = EditorGUI.FloatField(minRect, first.x);
                bool minChanged = EditorGUI.EndChangeCheck();
                EditorGUI.showMixedValue = maxMixed;
                EditorGUI.BeginChangeCheck();
                float max = EditorGUI.FloatField(maxRect, first.y);
                bool maxChanged = EditorGUI.EndChangeCheck();

                float sliderMin = Mathf.Clamp(min, _lower, _upper);
                float sliderMax = Mathf.Clamp(max, sliderMin, _upper);
                EditorGUI.showMixedValue = minMixed || maxMixed;
                EditorGUI.BeginChangeCheck();
                EditorGUI.MinMaxSlider(sliderRect, ref sliderMin, ref sliderMax, _lower, _upper);
                bool sliderChanged = EditorGUI.EndChangeCheck();
                if (!minChanged && !maxChanged && !sliderChanged) return;

                editor.RegisterPropertyChangeUndo(label);
                foreach (MaterialProperty target in properties)
                {
                    Vector4 vector = target.vectorValue;
                    if (sliderChanged)
                    {
                        vector.x = sliderMin;
                        vector.y = sliderMax;
                    }
                    else
                    {
                        vector.x = Mathf.Clamp(vector.x, _lower, _upper);
                        vector.y = Mathf.Clamp(vector.y, vector.x, _upper);
                        if (minChanged) vector.x = Mathf.Clamp(min, _lower, vector.y);
                        if (maxChanged) vector.y = Mathf.Clamp(max, vector.x, _upper);
                    }
                    target.vectorValue = vector;
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

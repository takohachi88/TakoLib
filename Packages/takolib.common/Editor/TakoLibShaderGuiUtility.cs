using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.Rendering;

namespace TakoLibEditor.Common
{
    public static class TakoLibShaderGuiUtility
    {
        private static Color tableColor = new Color(0, 0, 0, 0.2f);
        private static Color tableOutlineColor = Color.gray;
        private static float tableIndent = 5;
        private static float textIndentInTable = 5;
        private static ShaderTagId lightModeTag = new ShaderTagId("LightMode");

        public static void DrawPassTable(MaterialEditor materialEditor, Material material)
        {

            int passCount = material.passCount;
            float height = 17;
            float indexWidth = 28;

            EditorGUILayout.LabelField($"Passes : {passCount}");

            Rect tableRect = GUILayoutUtility.GetRect(0, (passCount + 1) * height);
            tableRect.x += tableIndent;
            Handles.DrawSolidRectangleWithOutline(tableRect, tableColor, tableOutlineColor);

            float nameWidth = (tableRect.width - indexWidth) * 0.5f;
            float tagWidth = nameWidth;

            Rect contentRect = tableRect;
            contentRect.height = height;
            contentRect.x += textIndentInTable;

            Color defaultColor = Handles.color;
            Handles.color = tableOutlineColor;
            float linePositionX = tableRect.x + indexWidth;
            Handles.DrawLine(new Vector3(linePositionX, contentRect.y), new Vector3(linePositionX, contentRect.y + tableRect.height));
            linePositionX += nameWidth;
            Handles.DrawLine(new Vector3(linePositionX, contentRect.y), new Vector3(linePositionX, contentRect.y + tableRect.height));
            linePositionX += tagWidth;
            Handles.DrawLine(new Vector3(linePositionX, contentRect.y), new Vector3(linePositionX, contentRect.y + tableRect.height));


            void DrawRow(string indexLabel, string nameLabel, string tagLabel)
            {
                Rect tempRect = contentRect;
                tempRect.x += textIndentInTable;
                tempRect.width = indexWidth;
                EditorGUI.LabelField(tempRect, indexLabel);

                tempRect.x += indexWidth;
                tempRect.width = nameWidth - textIndentInTable * 2f;
                EditorGUI.LabelField(tempRect, nameLabel);

                tempRect.x += nameWidth;
                tempRect.width = tagWidth - textIndentInTable * 2f;
                EditorGUI.LabelField(tempRect, tagLabel);
            }

            DrawRow(string.Empty, "Name", "LightMode");

            contentRect.y += height;
            Handles.DrawLine(new Vector3(tableRect.x, contentRect.y), new Vector3(tableRect.x + tableRect.width, contentRect.y));

            for (int i = 0; i < passCount; i++)
            {
                DrawRow(i.ToString(), material.GetPassName(i), material.shader.FindPassTagValue(i, lightModeTag).name);

                contentRect.y += height;
            }

            Handles.color = defaultColor;
        }

        public static void DrawKeywordTable(MaterialEditor materialEditor, Material material)
        {
            string[] keywords = material.enabledKeywords.Select(k => k.name).ToArray();
            int keywordCount = keywords.Length;
            float height = 17;

            EditorGUILayout.LabelField($"Enabled Keywords : {keywordCount}");

            if (keywordCount <= 0)
            {
                return;
            };

            Rect tableRect = GUILayoutUtility.GetRect(0, keywordCount * height);
            tableRect.x += tableIndent;
            Handles.DrawSolidRectangleWithOutline(tableRect, tableColor, tableOutlineColor);

            Rect contentRect = tableRect;
            contentRect.height = height;
            contentRect.x += textIndentInTable;

            Color defaultColor = Handles.color;
            Handles.color = tableOutlineColor;

            for (int i = 0; i < keywordCount; i++)
            {
                EditorGUI.LabelField(contentRect, keywords[i]);
                contentRect.y += height;
            }

            Handles.color = defaultColor;
        }

    }
}
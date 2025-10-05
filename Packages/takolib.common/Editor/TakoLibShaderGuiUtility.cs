using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.Rendering;

namespace TakoLibEditor.Common
{
    public static class TakoLibShaderGuiUtility
    {
        private static readonly Color TableColor = new (0, 0, 0, 0.2f);
        private static readonly Color TableOutlineColor = Color.gray;
        private static readonly float TableIndent = 5;
        private static readonly float TextIndentInTable = 3;
        private static readonly ShaderTagId LightModeTag = new("LightMode");

        public static void DrawPassTable(MaterialEditor materialEditor, Material material)
        {
            int passCount = material.passCount;
            float height = 17;
            float indexWidth = 24;
            float enabledWidth = 24;

            EditorGUILayout.LabelField($"Passes : {passCount}");

            Rect tableRect = GUILayoutUtility.GetRect(0, (passCount + 1) * height);
            tableRect.x += TableIndent;
            Handles.DrawSolidRectangleWithOutline(tableRect, TableColor, TableOutlineColor);

            float nameWidth = (tableRect.width - indexWidth - enabledWidth) * 0.5f;
            float tagWidth = nameWidth;

            Rect contentRect = tableRect;
            contentRect.height = height;
            contentRect.x += TextIndentInTable;

            Color defaultColor = Handles.color;
            Handles.color = TableOutlineColor;
            float linePositionX = tableRect.x + indexWidth;
            //縦線
            float lineScreenSpace = 50f / tableRect.height;
            Handles.DrawDottedLine(new(linePositionX, contentRect.y), new(linePositionX, contentRect.y + tableRect.height), lineScreenSpace);
            linePositionX += enabledWidth;
            Handles.DrawDottedLine(new(linePositionX, contentRect.y), new(linePositionX, contentRect.y + tableRect.height), lineScreenSpace);
            linePositionX += nameWidth;
            Handles.DrawDottedLine(new(linePositionX, contentRect.y), new(linePositionX, contentRect.y + tableRect.height), lineScreenSpace);
            linePositionX += tagWidth;
            Handles.DrawDottedLine(new(linePositionX, contentRect.y), new(linePositionX, contentRect.y + tableRect.height), lineScreenSpace);

            void DrawRow(string indexLabel, ref bool? enabled, string nameLabel, string tagLabel)
            {
                Rect tempRect = contentRect;
                tempRect.x += TextIndentInTable;
                tempRect.width = indexWidth;
                EditorGUI.LabelField(tempRect, indexLabel);

                tempRect.x += indexWidth;
                tempRect.width = enabledWidth;
                if (enabled.HasValue) enabled = EditorGUI.Toggle(tempRect, enabled.Value);

                tempRect.x += indexWidth;
                tempRect.width = nameWidth - TextIndentInTable * 2f;
                EditorGUI.LabelField(tempRect, nameLabel);

                tempRect.x += nameWidth;
                tempRect.width = tagWidth - TextIndentInTable * 2f;
                EditorGUI.LabelField(tempRect, tagLabel);
            }

            bool? passEnabled = null;

            DrawRow(string.Empty, ref passEnabled, "Name", "LightMode");

            contentRect.y += height;
            //ヘッダー部分の横線。
            Handles.DrawDottedLine(new(tableRect.x, contentRect.y), new(tableRect.x + tableRect.width, contentRect.y), 500f / tableRect.width);

            for (int i = 0; i < passCount; i++)
            {
                string passName = material.GetPassName(i);
                passEnabled = material.GetShaderPassEnabled(passName);
                DrawRow(i.ToString(), ref passEnabled, passName, material.shader.FindPassTagValue(i, LightModeTag).name);
                material.SetShaderPassEnabled(passName, passEnabled.Value);

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
            tableRect.x += TableIndent;
            Handles.DrawSolidRectangleWithOutline(tableRect, TableColor, TableOutlineColor);

            Rect contentRect = tableRect;
            contentRect.height = height;
            contentRect.x += TextIndentInTable;

            Color defaultColor = Handles.color;
            Handles.color = TableOutlineColor;

            for (int i = 0; i < keywordCount; i++)
            {
                EditorGUI.LabelField(contentRect, keywords[i]);
                contentRect.y += height;
            }

            Handles.color = defaultColor;
        }
    }
}
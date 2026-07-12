using System;
using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Modules;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Editor
{
    internal static class SdfxEditorSearch
    {
        private static SearchField searchField;

        public static bool HasQuery(string query)
            => !string.IsNullOrWhiteSpace(query);

        public static string DrawInlineField(string query, string placeholder)
        {
            searchField ??= new SearchField();
            var style = GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField;
            var newQuery = searchField.OnToolbarGUI(
                GUILayoutUtility.GetRect(GUIContent.none, style, GUILayout.ExpandWidth(true), GUILayout.Height(18f)),
                query);
            if (!HasQuery(newQuery) && !string.IsNullOrEmpty(placeholder))
            {
                var placeholderRect = GUILayoutUtility.GetLastRect();
                placeholderRect.xMin += 14f;
                EditorGUI.LabelField(placeholderRect, placeholder, EditorStyles.miniLabel);
            }

            return newQuery;
        }

        public static bool MatchesQuery(string query, params string[] fields)
        {
            if (!HasQuery(query))
            {
                return true;
            }

            var tokens = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var tokenMatched = false;
                foreach (var field in fields)
                {
                    if (!string.IsNullOrEmpty(field)
                        && field.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        tokenMatched = true;
                        break;
                    }
                }

                if (!tokenMatched)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ModuleMatches(ShaderModule module, string query, string categoryLabel)
        {
            if (!HasQuery(query))
            {
                return true;
            }

            var fields = new List<string>
            {
                module.Id,
                module.DisplayName,
                module.Description,
                categoryLabel
            };

            foreach (var prop in module.Properties)
            {
                fields.Add(prop.Name);
                fields.Add(prop.DisplayName);
            }

            return MatchesQuery(query, fields.ToArray());
        }

        public static bool ModulePropertyMatches(ModuleProperty property, string query)
            => MatchesQuery(query, property.Name, property.DisplayName);

        public static bool MaterialPropertyMatches(MaterialProperty property, string query)
            => property != null && MatchesQuery(query, property.name, property.displayName);
    }
}

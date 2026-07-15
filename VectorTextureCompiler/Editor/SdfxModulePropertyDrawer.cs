using System;
using System.Collections.Generic;
using System.Text;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Modules;
using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Editor
{
    internal static class SdfxModulePropertyDrawer
    {
        private static readonly Dictionary<string, ModuleProperty> CorePropertyMeta = BuildCorePropertyMeta();

        public static bool TryGetCoreMeta(string propertyName, out ModuleProperty meta)
            => CorePropertyMeta.TryGetValue(propertyName, out meta);

        public static void Draw(
            MaterialEditor editor,
            MaterialProperty[] properties,
            ModuleProperty meta,
            string search = "",
            bool sectionHeaderMatches = true)
        {
            var matProp = FindMaterialProperty(meta.Name, properties);
            if (matProp == null)
            {
                return;
            }

            if (SdfxEditorSearch.HasQuery(search)
                && !sectionHeaderMatches
                && !SdfxEditorSearch.MaterialPropertyMatches(matProp, search)
                && !SdfxEditorSearch.ModulePropertyMatches(meta, search))
            {
                return;
            }

            switch (meta.Kind)
            {
                case ModulePropertyKind.Enum:
                    DrawEnum(editor, matProp, meta);
                    break;
                case ModulePropertyKind.Range:
                    DrawRange(editor, matProp, meta);
                    break;
                default:
                    editor.ShaderProperty(matProp, meta.DisplayName);
                    break;
            }
        }

        public static string HumanizeLabel(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return raw;
            }

            var sb = new StringBuilder(raw.Length + 8);
            for (var i = 0; i < raw.Length; i++)
            {
                var c = raw[i];
                if (i > 0)
                {
                    var prev = raw[i - 1];
                    if ((char.IsUpper(c) && !char.IsUpper(prev) && !char.IsDigit(prev))
                        || (char.IsDigit(c) && !char.IsDigit(prev)))
                    {
                        sb.Append(' ');
                    }
                }

                if (c == '_' || c == '-')
                {
                    sb.Append(' ');
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static void DrawEnum(MaterialEditor editor, MaterialProperty matProp, ModuleProperty meta)
        {
            var labels = meta.EnumLabels;
            if (labels == null || labels.Length == 0)
            {
                editor.ShaderProperty(matProp, meta.DisplayName);
                return;
            }

            var displayLabels = new string[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                displayLabels[i] = HumanizeLabel(labels[i]);
            }

            var current = Mathf.Clamp(Mathf.RoundToInt(matProp.floatValue), 0, labels.Length - 1);

            EditorGUI.showMixedValue = matProp.hasMixedValue;
            EditorGUI.BeginChangeCheck();

            // Always use a dropdown. Toolbars get cramped for blend/mode enums with many options.
            var newIndex = EditorGUILayout.Popup(
                new GUIContent(meta.DisplayName, SdfxLanguage.ShaderGui.ModePopupTooltip),
                current,
                displayLabels);

            if (EditorGUI.EndChangeCheck())
            {
                editor.RegisterPropertyChangeUndo(meta.DisplayName);
                matProp.floatValue = Mathf.Clamp(newIndex, 0, labels.Length - 1);
                current = Mathf.RoundToInt(matProp.floatValue);
            }

            EditorGUI.showMixedValue = false;
            DrawEnumDescription(meta, current, displayLabels);
            EditorGUILayout.Space(2f);
        }

        private static void DrawEnumDescription(ModuleProperty meta, int index, string[] displayLabels)
        {
            string description = null;
            if (meta.EnumDescriptions != null
                && index >= 0
                && index < meta.EnumDescriptions.Length
                && !string.IsNullOrWhiteSpace(meta.EnumDescriptions[index]))
            {
                description = meta.EnumDescriptions[index];
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                description = SdfxLanguage.ShaderGui.ModeFallbackDescription(displayLabels[index]);
            }

            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
        }

        private static void DrawRange(MaterialEditor editor, MaterialProperty matProp, ModuleProperty meta)
        {
            EditorGUI.showMixedValue = matProp.hasMixedValue;
            EditorGUI.BeginChangeCheck();

            float value;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(new GUIContent(
                    meta.DisplayName,
                    SdfxLanguage.ShaderGui.RangeTooltip(meta.RangeMin, meta.RangeMax)));

                value = EditorGUILayout.Slider(matProp.floatValue, meta.RangeMin, meta.RangeMax);
                GUILayout.Label(
                    value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                    EditorStyles.miniLabel,
                    GUILayout.Width(44f));
            }

            if (EditorGUI.EndChangeCheck())
            {
                editor.RegisterPropertyChangeUndo(meta.DisplayName);
                matProp.floatValue = value;
            }

            EditorGUI.showMixedValue = false;
        }

        private static MaterialProperty FindMaterialProperty(string name, MaterialProperty[] properties)
        {
            if (properties == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            foreach (var prop in properties)
            {
                if (prop != null && prop.name == name)
                {
                    return prop;
                }
            }

            return null;
        }

        private static Dictionary<string, ModuleProperty> BuildCorePropertyMeta()
        {
            return new Dictionary<string, ModuleProperty>(StringComparer.Ordinal)
            {
                ["_BlendMode"] = ModuleProperty.Enum(
                    "_BlendMode",
                    SdfxLanguage.EditorWindow.CoreBlendModeDisplayName,
                    SdfxLanguage.EditorWindow.CompileBlendModeLabels,
                    descriptions: SdfxLanguage.EditorWindow.CompileBlendModeDescriptions),
                ["_RenderQueuePreset"] = ModuleProperty.Enum(
                    "_RenderQueuePreset",
                    SdfxLanguage.EditorWindow.CoreRenderQueueDisplayName,
                    SdfxLanguage.EditorWindow.RenderQueueLabels,
                    defaultIndex: 1,
                    descriptions: SdfxLanguage.EditorWindow.RenderQueueDescriptions),
                ["_VertexColorMode"] = ModuleProperty.Enum(
                    "_VertexColorMode",
                    "Vertex Color Mode",
                    new[] { "Multiply", "Add", "Override" },
                    descriptions: new[]
                    {
                        "Multiply albedo by mesh vertex color.",
                        "Add vertex color to albedo.",
                        "Replace albedo with vertex color."
                    })
            };
        }
    }
}

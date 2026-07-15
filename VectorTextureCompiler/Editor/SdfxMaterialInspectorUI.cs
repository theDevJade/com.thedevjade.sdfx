using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SDFX.VectorTextureCompiler.Core;
using SDFX.VectorTextureCompiler.Core.CodeGen;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Modules;
using SDFX.VectorTextureCompiler.Core.Presets;
using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Editor
{
    internal static class SdfxMaterialInspectorUI
    {
        public static void DrawStatusBar(Material material, int enabledCount, int totalCount)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    SdfxLanguage.ShaderGui.StatusModules(enabledCount, totalCount),
                    EditorStyles.miniLabel,
                    GUILayout.Width(120f));

                var blend = material.HasProperty("_BlendMode")
                    ? ((BlendModePreset)Mathf.RoundToInt(material.GetFloat("_BlendMode"))).ToString()
                    : SdfxLanguage.ShaderGui.StatusUnknown;
                EditorGUILayout.LabelField(SdfxLanguage.ShaderGui.StatusBlend(blend), EditorStyles.miniLabel);

                EditorGUILayout.LabelField(
                    SdfxLanguage.ShaderGui.StatusQueue(material.renderQueue),
                    EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();

                var compiled = FindCompiledAsset(material);
                using (new EditorGUI.DisabledScope(compiled == null))
                {
                    if (GUILayout.Button(SdfxLanguage.ShaderGui.RecompileButton, EditorStyles.miniButton, GUILayout.Width(88f)))
                    {
                        SdfxCompilerActions.TryRecompile(compiled, out var message);
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            Debug.Log(message);
                        }
                    }
                }

                if (GUILayout.Button(SdfxLanguage.ShaderGui.OpenCompilerButton, EditorStyles.miniButton, GUILayout.Width(110f)))
                {
                    VectorTextureCompilerWindow.Open();
                }
            }
        }

        public static void DrawPresetPanel(
            MaterialEditor materialEditor,
            Material material,
            IReadOnlyList<SdfxMaterialLookPreset> presets,
            ref int presetIndex,
            ref SdfxMaterialLookPreset customPreset)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(SdfxLanguage.ShaderGui.LookPresetHeader, EditorStyles.boldLabel);

                customPreset = (SdfxMaterialLookPreset)EditorGUILayout.ObjectField(
                    SdfxLanguage.ShaderGui.LookPresetAssetField,
                    customPreset,
                    typeof(SdfxMaterialLookPreset),
                    false);

                var labels = new string[presets.Count + 1];
                labels[0] = SdfxLanguage.ShaderGui.LookPresetNone;
                for (var i = 0; i < presets.Count; i++)
                {
                    labels[i + 1] = presets[i].DisplayName;
                }

                presetIndex = EditorGUILayout.Popup(SdfxLanguage.ShaderGui.LookPresetField, presetIndex, labels);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(SdfxLanguage.ShaderGui.LookPresetApply, GUILayout.Height(22f)))
                    {
                        var preset = ResolveSelectedPreset(presets, presetIndex, customPreset);
                        if (preset != null)
                        {
                            SdfxMaterialPresetApplier.Apply(material, preset, materialEditor);
                        }
                    }

                    if (GUILayout.Button(SdfxLanguage.ShaderGui.LookPresetSaveAs, GUILayout.Height(22f)))
                    {
                        var target = material;
                        EditorApplication.delayCall += () => SavePresetFromMaterial(target);
                    }

                    if (GUILayout.Button(SdfxLanguage.ShaderGui.LookPresetCapture, GUILayout.Height(22f)))
                    {
                        customPreset = SdfxMaterialPresetApplier.CaptureFromMaterial(
                            material,
                            material.name + " Look");
                    }
                }

                DrawBlendMismatchWarning(material, presets, presetIndex, customPreset);

                if (ResolveSelectedPreset(presets, presetIndex, customPreset) == null)
                {
                    EditorGUILayout.LabelField(SdfxLanguage.ShaderGui.LookPresetHint, EditorStyles.centeredGreyMiniLabel);
                }
            }
        }

        public static void DrawSearchBar(ref string search)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                search = SdfxEditorSearch.DrawInlineField(search, SdfxLanguage.ShaderGui.SearchPlaceholder);
                if (GUILayout.Button(SdfxLanguage.ShaderGui.SearchClear, EditorStyles.toolbarButton, GUILayout.Width(42f)))
                {
                    search = string.Empty;
                    GUI.FocusControl(null);
                }
            }
        }

        public static void DrawModuleToolbar(
            MaterialEditor materialEditor,
            Material material,
            IReadOnlyList<ShaderModule> modules,
            Action onLayoutChanged)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(SdfxLanguage.ShaderGui.ModulesEnableAll, EditorStyles.toolbarButton))
                {
                    SetAllModules(materialEditor, material, modules, true);
                    onLayoutChanged?.Invoke();
                }

                if (GUILayout.Button(SdfxLanguage.ShaderGui.ModulesDisableAll, EditorStyles.toolbarButton))
                {
                    SetAllModules(materialEditor, material, modules, false);
                    onLayoutChanged?.Invoke();
                }

                GUILayout.Space(8f);

                if (GUILayout.Button(SdfxLanguage.ShaderGui.ModulesExpandAll, EditorStyles.toolbarButton))
                {
                    SdfxShaderGUI.SetAllModuleFoldouts(modules, true);
                    onLayoutChanged?.Invoke();
                }

                if (GUILayout.Button(SdfxLanguage.ShaderGui.ModulesCollapseAll, EditorStyles.toolbarButton))
                {
                    SdfxShaderGUI.SetAllModuleFoldouts(modules, false);
                    onLayoutChanged?.Invoke();
                }
            }
        }

        public static void RequestRepaint(MaterialEditor materialEditor)
        {
            materialEditor?.Repaint();
            var editors = ActiveEditorTracker.sharedTracker.activeEditors;
            if (editors == null)
            {
                return;
            }

            for (var i = 0; i < editors.Length; i++)
            {
                editors[i]?.Repaint();
            }
        }

        public static void DrawBlendModeInfo(Material material)
        {
            if (!material.HasProperty("_BlendMode"))
            {
                return;
            }

            var preset = (BlendModePreset)Mathf.RoundToInt(material.GetFloat("_BlendMode"));
            CorePipeline.GetBlendFactors(preset, out var src, out var dst, out var zWrite);
            EditorGUILayout.HelpBox(
                SdfxLanguage.ShaderGui.BlendModeInfo(preset, src, dst, zWrite, material.renderQueue),
                MessageType.None);
        }

        public static void DrawModuleConflicts(IReadOnlyList<string> enabledIds, ShaderModule module)
        {
            if (module.ConflictIds == null || module.ConflictIds.Count == 0 || enabledIds == null)
            {
                return;
            }

            if (!enabledIds.Contains(module.Id))
            {
                return;
            }

            var conflicts = ShaderModuleRegistry.ValidateSelection(enabledIds);
            var relevant = conflicts
                .Where(w => w.IndexOf(module.DisplayName, StringComparison.OrdinalIgnoreCase) >= 0
                    || module.ConflictIds.Any(c => w.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
            if (relevant.Count == 0)
            {
                return;
            }

            foreach (var warning in relevant)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }

        public static void ResetModuleDefaults(MaterialEditor editor, Material material, ShaderModule module)
        {
            editor.RegisterPropertyChangeUndo(SdfxLanguage.ShaderGui.ResetModuleUndo(module.DisplayName));
            foreach (var prop in module.Properties)
            {
                if (!material.HasProperty(prop.Name))
                {
                    continue;
                }

                ApplyDefault(material, prop);
            }
        }

        public static void EnableOnlyModule(MaterialEditor editor, Material material, ShaderModule target, IReadOnlyList<ShaderModule> all)
        {
            editor.RegisterPropertyChangeUndo(SdfxLanguage.ShaderGui.SoloModuleUndo(target.DisplayName));
            foreach (var module in all)
            {
                if (!material.HasProperty(module.ToggleProperty))
                {
                    continue;
                }

                var enable = module.Id == target.Id;
                material.SetFloat(module.ToggleProperty, enable ? 1f : 0f);
                if (enable)
                {
                    material.EnableKeyword(module.Keyword);
                }
                else
                {
                    material.DisableKeyword(module.Keyword);
                }
            }
        }

        public static CompiledVectorTextureAsset FindCompiledAsset(Material material)
        {
            if (material == null)
            {
                return null;
            }

            CompiledVectorTextureAsset textureMatch = null;
            var prim = material.HasProperty("_PrimitiveDataTex")
                ? material.GetTexture("_PrimitiveDataTex")
                : null;

            var guids = AssetDatabase.FindAssets("t:CompiledVectorTextureAsset");
            foreach (var guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<CompiledVectorTextureAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null)
                {
                    continue;
                }

                if (asset.material == material)
                {
                    return asset;
                }

                if (textureMatch == null
                    && prim != null
                    && asset.primitiveDataTexture == prim)
                {
                    textureMatch = asset;
                }
            }

            return textureMatch;
        }

        public static int CountEnabledModules(Material material, IReadOnlyList<ShaderModule> modules)
        {
            var count = 0;
            foreach (var module in modules)
            {
                if (material.HasProperty(module.ToggleProperty)
                    && material.GetFloat(module.ToggleProperty) > 0.5f)
                {
                    count++;
                }
            }

            return count;
        }

        public static List<string> GetEnabledModuleIds(Material material, IReadOnlyList<ShaderModule> modules)
        {
            var ids = new List<string>();
            foreach (var module in modules)
            {
                if (material.HasProperty(module.ToggleProperty)
                    && material.GetFloat(module.ToggleProperty) > 0.5f)
                {
                    ids.Add(module.Id);
                }
            }

            return ids;
        }

        private static SdfxMaterialLookPreset ResolveSelectedPreset(
            IReadOnlyList<SdfxMaterialLookPreset> presets,
            int index,
            SdfxMaterialLookPreset customPreset)
        {
            if (customPreset != null)
            {
                return customPreset;
            }

            if (index > 0 && index <= presets.Count)
            {
                return presets[index - 1];
            }

            return null;
        }

        private static void DrawBlendMismatchWarning(
            Material material,
            IReadOnlyList<SdfxMaterialLookPreset> presets,
            int presetIndex,
            SdfxMaterialLookPreset customPreset)
        {
            var preset = ResolveSelectedPreset(presets, presetIndex, customPreset);
            if (preset == null || !material.HasProperty("_BlendMode"))
            {
                return;
            }

            var current = SdfxBlendStateSync.ReadBlendMode(material);
            if (current == preset.CompileBlendHint)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                SdfxLanguage.ShaderGui.BlendMismatch(preset.DisplayName, preset.CompileBlendHint, current),
                MessageType.Info);
        }

        private static void SavePresetFromMaterial(Material material)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                SdfxLanguage.ShaderGui.LookPresetSaveTitle,
                material.name + "Look",
                "asset",
                SdfxLanguage.ShaderGui.LookPresetSaveTitle);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var captured = SdfxMaterialPresetApplier.CaptureFromMaterial(
                material,
                System.IO.Path.GetFileNameWithoutExtension(path));
            AssetDatabase.CreateAsset(captured, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(captured);
        }

        private static void SetAllModules(MaterialEditor editor, Material material, IReadOnlyList<ShaderModule> modules, bool enabled)
        {
            editor.RegisterPropertyChangeUndo(enabled
                ? SdfxLanguage.ShaderGui.ModulesEnableAll
                : SdfxLanguage.ShaderGui.ModulesDisableAll);
            foreach (var module in modules)
            {
                if (!material.HasProperty(module.ToggleProperty))
                {
                    continue;
                }

                material.SetFloat(module.ToggleProperty, enabled ? 1f : 0f);
                if (enabled)
                {
                    material.EnableKeyword(module.Keyword);
                }
                else
                {
                    material.DisableKeyword(module.Keyword);
                }
            }
        }

        private static void ApplyDefault(Material material, ModuleProperty prop)
        {
            switch (prop.Kind)
            {
                case ModulePropertyKind.Color:
                    if (TryParseColor(prop.DefaultValue, out var color))
                    {
                        material.SetColor(prop.Name, color);
                    }

                    break;
                case ModulePropertyKind.Texture2D:
                    break;
                default:
                    if (float.TryParse(prop.DefaultValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    {
                        material.SetFloat(prop.Name, value);
                    }

                    break;
            }
        }

        private static bool TryParseColor(string defaultValue, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(defaultValue))
            {
                return false;
            }

            var trimmed = defaultValue.Trim('(', ')');
            var parts = trimmed.Split(',');
            if (parts.Length < 4)
            {
                return false;
            }

            if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var r)
                && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var g)
                && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var b)
                && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var a))
            {
                color = new Color(r, g, b, a);
                return true;
            }

            return false;
        }
    }
}

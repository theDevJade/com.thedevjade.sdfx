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
        public static void DrawStatusBar(
            Material material,
            int enabledCount,
            int totalCount,
            CompiledVectorTextureAsset compiled = null)
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

                compiled ??= FindCompiledAsset(material);
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

        public static void DrawModuleConflicts(IReadOnlyList<string> conflictWarnings, ShaderModule module)
        {
            if (conflictWarnings == null || conflictWarnings.Count == 0 || module == null)
            {
                return;
            }

            if (module.ConflictIds == null || module.ConflictIds.Count == 0)
            {
                return;
            }

            for (var i = 0; i < conflictWarnings.Count; i++)
            {
                var warning = conflictWarnings[i];
                if (warning.IndexOf(module.DisplayName, StringComparison.OrdinalIgnoreCase) < 0
                    && !module.ConflictIds.Any(c => warning.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    continue;
                }

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

        private static readonly Dictionary<int, CompiledVectorTextureAsset> CompiledAssetByMaterialId =
            new Dictionary<int, CompiledVectorTextureAsset>();

        private static bool compiledAssetHooksRegistered;
        private static List<(string path, CompiledVectorTextureAsset asset)> compiledAssetIndex;
        private static double compiledAssetIndexBuiltAt;

        public static void InvalidateCompiledAssetCache()
        {
            CompiledAssetByMaterialId.Clear();
            compiledAssetIndex = null;
        }

        public static CompiledVectorTextureAsset FindCompiledAsset(Material material)
        {
            EnsureCompiledAssetHooks();

            if (material == null)
            {
                return null;
            }

            var materialId = material.GetInstanceID();
            if (CompiledAssetByMaterialId.TryGetValue(materialId, out var cached))
            {
                return cached;
            }

            var found = FindCompiledAssetUncached(material);
            CompiledAssetByMaterialId[materialId] = found;
            return found;
        }

        private static void EnsureCompiledAssetHooks()
        {
            if (compiledAssetHooksRegistered)
            {
                return;
            }

            compiledAssetHooksRegistered = true;
            EditorApplication.projectChanged += InvalidateCompiledAssetCache;
        }

        private static CompiledVectorTextureAsset FindCompiledAssetUncached(Material material)
        {
            CompiledVectorTextureAsset textureMatch = null;
            var prim = material.HasProperty("_PrimitiveDataTex")
                ? material.GetTexture("_PrimitiveDataTex")
                : null;

            var index = GetOrBuildCompiledAssetIndex();
            for (var i = 0; i < index.Count; i++)
            {
                var asset = index[i].asset;
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

        private static List<(string path, CompiledVectorTextureAsset asset)> GetOrBuildCompiledAssetIndex()
        {
            var now = EditorApplication.timeSinceStartup;
            if (compiledAssetIndex != null && now - compiledAssetIndexBuiltAt < 5.0)
            {
                return compiledAssetIndex;
            }

            var guids = AssetDatabase.FindAssets("t:CompiledVectorTextureAsset");
            var index = new List<(string path, CompiledVectorTextureAsset asset)>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<CompiledVectorTextureAsset>(path);
                if (asset != null)
                {
                    index.Add((path, asset));
                }
            }

            compiledAssetIndex = index;
            compiledAssetIndexBuiltAt = now;
            return index;
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

        private static Vector2 metricsScroll;
        private const int MetricsMaxRows = 48;

        public static void DrawPrimitiveMetrics(Material material, CompiledVectorTextureAsset compiled)
        {
            EditorGUILayout.HelpBox(SdfxLanguage.ShaderGui.MetricsHelp, MessageType.None);

            if (compiled == null)
            {
                EditorGUILayout.HelpBox(SdfxLanguage.ShaderGui.MetricsNoCompiled, MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(SdfxLanguage.ShaderGui.MetricsProfileButton, GUILayout.Height(24f)))
                {
                    SdfxPrimitiveMetricsProfiler.Profile(compiled, material, sampleMaterialGpu: false);
                }

                using (new EditorGUI.DisabledScope(material == null))
                {
                    if (GUILayout.Button(SdfxLanguage.ShaderGui.MetricsProfileWithGpuButton, GUILayout.Height(24f)))
                    {
                        SdfxPrimitiveMetricsProfiler.Profile(compiled, material, sampleMaterialGpu: true);
                    }
                }
            }

            var session = SdfxPrimitiveMetricsProfiler.LastSession;
            if (session == null || session.Primitives == null || session.Primitives.Length == 0)
            {
                EditorGUILayout.LabelField(SdfxLanguage.ShaderGui.MetricsEmpty, EditorStyles.miniLabel);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(SdfxLanguage.ShaderGui.MetricsCopyButton, GUILayout.Height(22f)))
                {
                    EditorGUIUtility.systemCopyBuffer = SdfxPrimitiveMetricsProfiler.BuildClipboardReport(session);
                    Debug.Log(SdfxLanguage.ShaderGui.MetricsCopied);
                }
            }

            EditorGUILayout.LabelField(
                SdfxLanguage.ShaderGui.MetricsCapturedAt(session.CapturedAtUtc.ToString("HH:mm:ss")),
                EditorStyles.miniLabel);

            if (session.IsBenchmark && session.Passes > 0)
            {
                EditorGUILayout.LabelField(
                    SdfxLanguage.ShaderGui.MetricsBenchmarkPasses(session.Passes),
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.LabelField(
                SdfxLanguage.ShaderGui.MetricsTotalCpu(session.TotalCpuMilliseconds, session.Primitives.Length),
                EditorStyles.miniLabel);

            if (session.MaterialGpuMilliseconds >= 0.0)
            {
                EditorGUILayout.LabelField(
                    SdfxLanguage.ShaderGui.MetricsMaterialGpu(session.MaterialGpuMilliseconds),
                    EditorStyles.miniLabel);
                if (session.MaterialGpuMinMilliseconds >= 0.0 && session.MaterialGpuMaxMilliseconds >= 0.0)
                {
                    EditorGUILayout.LabelField(
                        SdfxLanguage.ShaderGui.MetricsMaterialGpuRange(
                            session.MaterialGpuMinMilliseconds,
                            session.MaterialGpuMaxMilliseconds),
                        EditorStyles.miniLabel);
                }
            }

            if (session.FrameTimingCpuMilliseconds >= 0.0 || session.FrameTimingGpuMilliseconds >= 0.0)
            {
                EditorGUILayout.LabelField(
                    SdfxLanguage.ShaderGui.MetricsFrameTiming(
                        session.FrameTimingCpuMilliseconds,
                        session.FrameTimingGpuMilliseconds),
                    EditorStyles.miniLabel);
            }

            if (!string.IsNullOrWhiteSpace(session.Status))
            {
                EditorGUILayout.LabelField(session.Status, EditorStyles.wordWrappedMiniLabel);
            }

            var showCount = Mathf.Min(MetricsMaxRows, session.Primitives.Length);
            if (showCount < session.Primitives.Length)
            {
                EditorGUILayout.LabelField(
                    SdfxLanguage.ShaderGui.MetricsShowingTop(showCount, session.Primitives.Length),
                    EditorStyles.miniLabel);
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(SdfxLanguage.ShaderGui.MetricsColIndex, EditorStyles.miniLabel, GUILayout.Width(36f));
                GUILayout.Label(SdfxLanguage.ShaderGui.MetricsColType, EditorStyles.miniLabel, GUILayout.Width(88f));
                GUILayout.Label(SdfxLanguage.ShaderGui.MetricsColEdges, EditorStyles.miniLabel, GUILayout.Width(48f));
                GUILayout.Label(SdfxLanguage.ShaderGui.MetricsColCpu, EditorStyles.miniLabel, GUILayout.Width(64f));
                GUILayout.Label(SdfxLanguage.ShaderGui.MetricsColShare, EditorStyles.miniLabel);
            }

            metricsScroll = EditorGUILayout.BeginScrollView(
                metricsScroll,
                GUILayout.MinHeight(80f),
                GUILayout.MaxHeight(220f));
            for (var i = 0; i < showCount; i++)
            {
                var row = session.Primitives[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(row.Index.ToString(), EditorStyles.miniLabel, GUILayout.Width(36f));
                    GUILayout.Label(row.Type.ToString(), EditorStyles.miniLabel, GUILayout.Width(88f));
                    GUILayout.Label(row.PathEdges.ToString(), EditorStyles.miniLabel, GUILayout.Width(48f));
                    GUILayout.Label(row.CpuMicroseconds.ToString("0.0"), EditorStyles.miniLabel, GUILayout.Width(64f));

                    var shareRect = GUILayoutUtility.GetRect(40f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
                    EditorGUI.ProgressBar(shareRect, Mathf.Clamp01(row.ShareOfTotal), $"{row.ShareOfTotal * 100f:0.0}%");
                }
            }

            EditorGUILayout.EndScrollView();
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

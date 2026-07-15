using System.Collections.Generic;
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
    public sealed class SdfxShaderGUI : ShaderGUI
    {
        private const string FoldoutKeyPrefix = "SDFX.ShaderGUI.Foldout.";
        private const string SearchKey = "SDFX.ShaderGUI.Search";
        private const string LookPresetIndexKey = "SDFX.ShaderGUI.LookPresetIndex";
        private const string CustomPresetKey = "SDFX.ShaderGUI.CustomPreset";

        private SdfxMaterialLookPreset customPreset;
        private int presetIndex;
        private string search = string.Empty;
        private bool guiStateLoaded;
        private string cachedModulesShaderName = string.Empty;
        private List<ShaderModule> cachedCompiledModules;

        public static string FoldoutKeyForModule(string moduleId) => FoldoutKeyPrefix + "module." + moduleId;

        public static void SetAllModuleFoldouts(IReadOnlyList<ShaderModule> modules, bool expanded)
        {
            SetFoldoutStatic("modules", expanded);
            foreach (var group in modules.GroupBy(m => m.Category))
            {
                SetFoldoutStatic("category." + group.Key, expanded);
            }

            foreach (var module in modules)
            {
                SetFoldoutStatic("module." + module.Id, expanded);
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            materialEditor.SetDefaultGUIWidths();
            EnsureGuiStateLoaded();

            var material = (Material)materialEditor.target;
            var compiledModules = GetCompiledModules(material, properties);
            var enabledCount = SdfxMaterialInspectorUI.CountEnabledModules(material, compiledModules);
            var compiledAsset = SdfxMaterialInspectorUI.FindCompiledAsset(material);

            DrawBanner(materialEditor, material);
            SdfxMaterialInspectorUI.DrawStatusBar(material, enabledCount, compiledModules.Count, compiledAsset);

            var presets = SdfxMaterialPresetApplier.LoadBuiltinPresets();
            EditorGUI.BeginChangeCheck();
            SdfxMaterialInspectorUI.DrawPresetPanel(materialEditor, material, presets, ref presetIndex, ref customPreset);
            if (EditorGUI.EndChangeCheck())
            {
                SessionState.SetInt(LookPresetIndexKey, presetIndex);
                SaveCustomPreset(customPreset);
            }

            EditorGUILayout.Space(4f);
            EditorGUI.BeginChangeCheck();
            SdfxMaterialInspectorUI.DrawSearchBar(ref search);
            if (EditorGUI.EndChangeCheck())
            {
                SessionState.SetString(SearchKey, search);
            }

            EditorGUILayout.Space(2f);
            DrawBaseSection(materialEditor, properties, search);
            DrawCorePipelineSection(materialEditor, properties, search);
            DrawModuleSections(materialEditor, material, properties, search, compiledModules);
            DrawBakedDataSection(materialEditor, material, properties, search, compiledAsset);
            DrawDebugSection(materialEditor, material, properties, search, compiledAsset);
            DrawAdvancedSection(materialEditor, properties, search);
            DrawShaderActions(material, compiledModules, compiledAsset);
        }

        private void EnsureGuiStateLoaded()
        {
            if (guiStateLoaded)
            {
                return;
            }

            guiStateLoaded = true;
            presetIndex = SessionState.GetInt(LookPresetIndexKey, 0);
            search = SessionState.GetString(SearchKey, string.Empty);
            customPreset ??= LoadCustomPreset();
        }


        private static void DrawBanner(MaterialEditor materialEditor, Material material)
        {
            SdfxBannerRenderer.Draw(materialEditor);
            EditorGUILayout.LabelField(material.shader.name, EditorStyles.centeredGreyMiniLabel);
        }


        private static void DrawBaseSection(MaterialEditor materialEditor, MaterialProperty[] properties, string search)
        {
            if (!SectionVisible(search, properties, SdfxLanguage.ShaderGui.BaseHeader, "_Color", "_BackgroundColor", "_Cull"))
            {
                return;
            }

            var headerMatches = SdfxEditorSearch.MatchesQuery(search, SdfxLanguage.ShaderGui.BaseHeader);
            if (!DrawSectionFoldout("base", SdfxLanguage.ShaderGui.BaseHeader, defaultOpen: true, forceOpen: SdfxEditorSearch.HasQuery(search)))
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                DrawPropertyIfPresent(materialEditor, properties, "_Color", search, headerMatches);
                DrawPropertyIfPresent(materialEditor, properties, "_BackgroundColor", search, headerMatches);
                DrawPropertyIfPresent(materialEditor, properties, "_Cull", search, headerMatches);
            }
        }


        private static void DrawCorePipelineSection(MaterialEditor materialEditor, MaterialProperty[] properties, string search)
        {
            if (!SectionVisible(
                    search,
                    properties,
                    SdfxLanguage.ShaderGui.CorePipelineHeader,
                    "_BlendMode",
                    "_Opacity",
                    "_Brightness",
                    "_StencilRef"))
            {
                return;
            }

            var headerMatches = SdfxEditorSearch.MatchesQuery(search, SdfxLanguage.ShaderGui.CorePipelineHeader, "blend", "opacity", "pipeline", "color", "stencil");
            if (!DrawSectionFoldout("corePipeline", SdfxLanguage.ShaderGui.CorePipelineHeader, defaultOpen: true, forceOpen: SdfxEditorSearch.HasQuery(search)))
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                if (DrawSubSection("core.rendering", SdfxLanguage.ShaderGui.SubRendering, search, headerMatches,
                        "_BlendMode", "_Opacity", "_AlphaClip", "_AlphaClipThreshold", "_RenderQueuePreset", "_QueueOffset", "_ZWrite", "_ZTest", "_DepthOffset", "_DepthOffsetUnits"))
                {
                    DrawModuleProperty(materialEditor, properties, "_BlendMode", search, headerMatches);
                    SyncBlendMode(materialEditor, properties);
                    SdfxMaterialInspectorUI.DrawBlendModeInfo((Material)materialEditor.target);
                    DrawPropertyIfPresent(materialEditor, properties, "_Opacity", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_AlphaClip", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_AlphaClipThreshold", search, headerMatches);
                    DrawModuleProperty(materialEditor, properties, "_RenderQueuePreset", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_QueueOffset", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_ZWrite", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_ZTest", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_DepthOffset", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_DepthOffsetUnits", search, headerMatches);
                }

                if (DrawSubSection("core.color", SdfxLanguage.ShaderGui.SubColorGrading, search, headerMatches,
                        "_Brightness", "_Contrast", "_Saturation", "_ColorBoost", "_Exposure"))
                {
                    DrawPropertyIfPresent(materialEditor, properties, "_Brightness", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_Contrast", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_Saturation", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_ColorBoost", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_Exposure", search, headerMatches);
                }

                if (DrawSubSection("core.vertex", SdfxLanguage.ShaderGui.SubVertexColor, search, headerMatches,
                        "_UseVertexColor", "_VertexColorMode"))
                {
                    DrawPropertyIfPresent(materialEditor, properties, "_UseVertexColor", search, headerMatches);
                    DrawModuleProperty(materialEditor, properties, "_VertexColorMode", search, headerMatches);
                }

                if (DrawSubSection("core.stencil", SdfxLanguage.ShaderGui.SubStencil, search, headerMatches,
                        "_StencilRef", "_StencilReadMask", "_StencilWriteMask", "_StencilComp", "_StencilPass", "_StencilFail", "_StencilZFail"))
                {
                    EditorGUILayout.HelpBox(SdfxLanguage.ShaderGui.StencilHelp, MessageType.Info);
                    DrawPropertyIfPresent(materialEditor, properties, "_StencilRef", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_StencilReadMask", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_StencilWriteMask", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_StencilComp", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_StencilPass", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_StencilFail", search, headerMatches);
                    DrawPropertyIfPresent(materialEditor, properties, "_StencilZFail", search, headerMatches);
                }
            }
        }

        private static void SyncBlendMode(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var blendProp = FindProperty("_BlendMode", properties, propertyIsMandatory: false);
            if (blendProp == null || blendProp.hasMixedValue)
            {
                return;
            }

            var last = SessionState.GetFloat("SDFX.ShaderGUI.LastBlend", -1f);
            if (!Mathf.Approximately(last, blendProp.floatValue))
            {
                SdfxBlendStateSync.OnBlendModePropertyChanged(materialEditor, blendProp);
                SessionState.SetFloat("SDFX.ShaderGUI.LastBlend", blendProp.floatValue);
            }
        }


        private static void DrawModuleSections(
            MaterialEditor materialEditor,
            Material material,
            MaterialProperty[] properties,
            string search,
            List<ShaderModule> compiledModules)
        {
            if (compiledModules.Count == 0)
            {
                return;
            }

            var hasModuleMatches = compiledModules.Any(m =>
                SdfxEditorSearch.ModuleMatches(m, search, SdfxEditorLabels.Category(m.Category)));
            if (!SectionVisible(search, properties, SdfxLanguage.ShaderGui.ModulesHeader)
                && !(SdfxEditorSearch.HasQuery(search) && hasModuleMatches))
            {
                return;
            }

            if (!DrawSectionFoldout(
                    "modules",
                    SdfxLanguage.ShaderGui.ModulesHeader,
                    defaultOpen: true,
                    forceOpen: SdfxEditorSearch.HasQuery(search) && hasModuleMatches))
            {
                return;
            }

            SdfxMaterialInspectorUI.DrawModuleToolbar(
                materialEditor,
                material,
                compiledModules,
                () => SdfxMaterialInspectorUI.RequestRepaint(materialEditor));

            var enabledIds = SdfxMaterialInspectorUI.GetEnabledModuleIds(material, compiledModules);
            var conflictWarnings = ShaderModuleRegistry.ValidateSelection(enabledIds);
            var visibleCount = 0;
            var drawnPropertyNames = new HashSet<string>();

            foreach (var group in compiledModules.GroupBy(m => m.Category).OrderBy(g => (int)g.Key))
            {
                var category = group.Key;
                var categoryLabel = SdfxEditorLabels.Category(category);
                var categoryModules = group.ToList();
                var visibleModules = categoryModules
                    .Where(m => SdfxEditorSearch.ModuleMatches(m, search, categoryLabel))
                    .ToList();
                if (visibleModules.Count == 0)
                {
                    continue;
                }

                visibleCount += visibleModules.Count;
                var enabledInCategory = 0;
                for (var i = 0; i < categoryModules.Count; i++)
                {
                    if (enabledIds.Contains(categoryModules[i].Id))
                    {
                        enabledInCategory++;
                    }
                }

                var categoryTitle = SdfxLanguage.ShaderGui.CategoryWithCount(categoryLabel, enabledInCategory, categoryModules.Count);
                var categoryKey = "category." + category;
                var categoryHasSearchMatch = SdfxEditorSearch.HasQuery(search)
                    && visibleModules.Any(m => SdfxEditorSearch.ModuleMatches(m, search, categoryLabel));
                if (!DrawSectionFoldout(
                        categoryKey,
                        categoryTitle,
                        defaultOpen: categoryHasSearchMatch,
                        forceOpen: categoryHasSearchMatch))
                {
                    continue;
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var module in visibleModules)
                    {
                        DrawModule(
                            materialEditor,
                            material,
                            properties,
                            module,
                            search,
                            categoryLabel,
                            enabledIds,
                            conflictWarnings,
                            compiledModules,
                            drawnPropertyNames);
                    }
                }
            }

            if (SdfxEditorSearch.HasQuery(search) && visibleCount == 0)
            {
                EditorGUILayout.HelpBox(SdfxLanguage.ShaderGui.SearchNoResults, MessageType.Info);
            }
        }

        private static void DrawModule(
            MaterialEditor materialEditor,
            Material material,
            MaterialProperty[] properties,
            ShaderModule module,
            string search,
            string categoryLabel,
            List<string> enabledIds,
            IReadOnlyList<string> conflictWarnings,
            List<ShaderModule> compiledModules,
            HashSet<string> drawnPropertyNames)
        {
            var toggleProp = FindProperty(module.ToggleProperty, properties, propertyIsMandatory: false);
            if (toggleProp == null)
            {
                return;
            }

            var headerMatches = SdfxEditorSearch.MatchesQuery(
                search,
                module.DisplayName,
                module.Description,
                categoryLabel,
                module.Id);
            var enabled = toggleProp.floatValue > 0.5f;
            var foldoutKey = "module." + module.Id;
            var open = GetFoldout(foldoutKey, defaultValue: false);
            var moduleMatchesSearch = headerMatches
                || module.Properties.Any(p => SdfxEditorSearch.ModulePropertyMatches(p, search));
            if (SdfxEditorSearch.HasQuery(search) && moduleMatchesSearch)
            {
                open = true;
            }

            const float toggleWidth = 22f;
            const float soloWidth = 36f;
            const float resetWidth = 44f;
            const float buttonGap = 4f;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                var buttonsWidth = soloWidth + resetWidth + buttonGap;
                var toggleRect = new Rect(headerRect.x, headerRect.y, toggleWidth, headerRect.height);
                var foldoutRect = new Rect(
                    headerRect.x + toggleWidth,
                    headerRect.y,
                    Mathf.Max(0f, headerRect.width - toggleWidth - buttonsWidth - buttonGap),
                    headerRect.height);
                var soloRect = new Rect(headerRect.xMax - buttonsWidth, headerRect.y, soloWidth, headerRect.height);
                var resetRect = new Rect(soloRect.xMax + buttonGap, headerRect.y, resetWidth, headerRect.height);

                EditorGUI.BeginChangeCheck();
                var newEnabled = EditorGUI.Toggle(toggleRect, enabled);
                if (EditorGUI.EndChangeCheck())
                {
                    materialEditor.RegisterPropertyChangeUndo(module.DisplayName);
                    toggleProp.floatValue = newEnabled ? 1f : 0f;
                    foreach (var target in materialEditor.targets.OfType<Material>())
                    {
                        if (newEnabled)
                        {
                            target.EnableKeyword(module.Keyword);
                        }
                        else
                        {
                            target.DisableKeyword(module.Keyword);
                        }
                    }

                    enabledIds = SdfxMaterialInspectorUI.GetEnabledModuleIds(material, compiledModules);
                    if (newEnabled)
                    {
                        open = true;
                    }

                    SdfxMaterialInspectorUI.RequestRepaint(materialEditor);
                }

                var newOpen = EditorGUI.Foldout(
                    foldoutRect,
                    open,
                    new GUIContent(module.DisplayName, module.Description),
                    true,
                    EditorStyles.foldout);
                if (newOpen != open)
                {
                    open = newOpen;
                    SetFoldout(foldoutKey, open);
                    SdfxMaterialInspectorUI.RequestRepaint(materialEditor);
                }

                if (GUI.Button(soloRect, SdfxLanguage.ShaderGui.ModuleSolo, EditorStyles.miniButton))
                {
                    SdfxMaterialInspectorUI.EnableOnlyModule(materialEditor, material, module, compiledModules);
                    open = true;
                    SetFoldout(foldoutKey, true);
                    SdfxMaterialInspectorUI.RequestRepaint(materialEditor);
                }

                if (GUI.Button(resetRect, SdfxLanguage.ShaderGui.ModuleReset, EditorStyles.miniButton))
                {
                    SdfxMaterialInspectorUI.ResetModuleDefaults(materialEditor, material, module);
                }

                if (!open)
                {
                    return;
                }

                EditorGUILayout.LabelField(module.Description, EditorStyles.wordWrappedMiniLabel);

                if (enabled)
                {
                    SdfxMaterialInspectorUI.DrawModuleConflicts(conflictWarnings, module);
                }

                using (new EditorGUI.DisabledScope(!enabled))
                {
                    EditorGUILayout.Space(2f);
                    foreach (var moduleProp in module.Properties)
                    {
                        if (!drawnPropertyNames.Add(moduleProp.Name))
                        {
                            continue;
                        }

                        if (!headerMatches
                            && SdfxEditorSearch.HasQuery(search)
                            && !SdfxEditorSearch.ModulePropertyMatches(moduleProp, search))
                        {
                            continue;
                        }

                        SdfxModulePropertyDrawer.Draw(materialEditor, properties, moduleProp, search, headerMatches);
                    }
                }
            }
        }


        private static void DrawBakedDataSection(
            MaterialEditor materialEditor,
            Material material,
            MaterialProperty[] properties,
            string search,
            CompiledVectorTextureAsset compiled)
        {
            if (!SectionVisible(
                    search,
                    properties,
                    SdfxLanguage.ShaderGui.BakedDataHeader,
                    "_PrimitiveDataTex",
                    "_GridLookupTex",
                    "_GridIndexTex",
                    "_PathDataTex"))
            {
                return;
            }

            var headerMatches = SdfxEditorSearch.MatchesQuery(
                search,
                SdfxLanguage.ShaderGui.BakedDataHeader,
                "baked",
                "data",
                "texture");
            var forceOpen = SdfxEditorSearch.HasQuery(search) && headerMatches;
            if (!DrawSectionFoldout("bakedData", SdfxLanguage.ShaderGui.BakedDataHeader, defaultOpen: false, forceOpen: forceOpen))
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.HelpBox(SdfxLanguage.ShaderGui.BakedDataHelp, MessageType.Info);

                if (compiled != null)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(SdfxLanguage.ShaderGui.CompiledAssetField, compiled, typeof(CompiledVectorTextureAsset), false);
                        if (GUILayout.Button(SdfxLanguage.ShaderGui.SelectSourceButton, GUILayout.Width(90f)))
                        {
                            Selection.activeObject = compiled;
                            EditorGUIUtility.PingObject(compiled);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(compiled.sourcePath))
                    {
                        EditorGUILayout.LabelField(SdfxLanguage.ShaderGui.SourcePathLabel, compiled.sourcePath, EditorStyles.miniLabel);
                    }
                }

                DrawPropertyIfPresent(materialEditor, properties, "_PrimitiveDataTex", search, headerMatches);
                DrawPropertyIfPresent(materialEditor, properties, "_GridLookupTex", search, headerMatches);
                DrawPropertyIfPresent(materialEditor, properties, "_GridIndexTex", search, headerMatches);
                DrawPropertyIfPresent(materialEditor, properties, "_PathDataTex", search, headerMatches);
            }
        }

        private static void DrawDebugSection(
            MaterialEditor materialEditor,
            Material material,
            MaterialProperty[] properties,
            string search,
            CompiledVectorTextureAsset compiled)
        {
            if (!SectionVisible(search, properties, SdfxLanguage.ShaderGui.DebugHeader, "_Debug", "_DebugHeatmap", "_DebugDistance")
                && !SdfxEditorSearch.MatchesQuery(search, SdfxLanguage.ShaderGui.MetricsHeader, "metrics", "profile", "primitive"))
            {
                return;
            }

            var headerMatches = SdfxEditorSearch.MatchesQuery(
                search,
                SdfxLanguage.ShaderGui.DebugHeader,
                SdfxLanguage.ShaderGui.MetricsHeader,
                "debug",
                "metrics",
                "profile");
            var forceOpen = SdfxEditorSearch.HasQuery(search) && headerMatches;
            if (!DrawSectionFoldout("debug", SdfxLanguage.ShaderGui.DebugHeader, defaultOpen: false, forceOpen: forceOpen))
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                DrawPropertyIfPresent(materialEditor, properties, "_Debug", search, headerMatches);
                DrawPropertyIfPresent(materialEditor, properties, "_DebugHeatmap", search, headerMatches);
                DrawPropertyIfPresent(materialEditor, properties, "_DebugDistance", search, headerMatches);

                EditorGUILayout.Space(6f);
                var metricsForceOpen = SdfxEditorSearch.HasQuery(search)
                    && SdfxEditorSearch.MatchesQuery(search, SdfxLanguage.ShaderGui.MetricsHeader, "metrics", "profile", "primitive");
                if (DrawSectionFoldout(
                        "debug.metrics",
                        SdfxLanguage.ShaderGui.MetricsHeader,
                        defaultOpen: false,
                        forceOpen: metricsForceOpen))
                {
                    SdfxMaterialInspectorUI.DrawPrimitiveMetrics(material, compiled);
                }
            }
        }

        private static void DrawAdvancedSection(MaterialEditor materialEditor, MaterialProperty[] properties, string search)
        {
            if (!SectionVisible(search, properties, SdfxLanguage.ShaderGui.AdvancedHeader, "advanced", "render queue", "instancing"))
            {
                return;
            }

            if (!DrawSectionFoldout("advanced", SdfxLanguage.ShaderGui.AdvancedHeader, defaultOpen: false, forceOpen: SdfxEditorSearch.HasQuery(search)))
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                materialEditor.RenderQueueField();
                materialEditor.EnableInstancingField();
                materialEditor.DoubleSidedGIField();
            }
        }

        private static void DrawShaderActions(
            Material material,
            IReadOnlyList<ShaderModule> compiledModules,
            CompiledVectorTextureAsset compiled)
        {
            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(SdfxLanguage.ShaderGui.ShaderActionsHeader, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(SdfxLanguage.ShaderGui.ShaderActionsHelp, MessageType.None);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(compiled == null))
                    {
                        if (GUILayout.Button(SdfxLanguage.ShaderGui.OptimizeButton, GUILayout.Height(28f)))
                        {
                            var enabledCount = SdfxMaterialInspectorUI.CountEnabledModules(material, compiledModules);
                            var confirm = EditorUtility.DisplayDialog(
                                SdfxLanguage.ShaderGui.OptimizeConfirmTitle,
                                SdfxLanguage.ShaderGui.OptimizeConfirmMessage(enabledCount, compiledModules.Count),
                                SdfxLanguage.ShaderGui.OptimizeButton,
                                SdfxLanguage.ShaderGui.CancelButton);
                            if (confirm)
                            {
                                var target = material;
                                EditorApplication.delayCall += () =>
                                {
                                    if (SdfxCompilerActions.TryOptimizeShader(target, out var message))
                                    {
                                        Debug.Log(message);
                                    }
                                    else if (!string.IsNullOrWhiteSpace(message))
                                    {
                                        EditorUtility.DisplayDialog(
                                            SdfxLanguage.ShaderGui.OptimizeConfirmTitle,
                                            message,
                                            SdfxLanguage.ShaderGui.OkButton);
                                    }
                                };
                            }
                        }

                        if (GUILayout.Button(SdfxLanguage.ShaderGui.RecompileShaderButton, GUILayout.Height(28f)))
                        {
                            var target = material;
                            var asset = compiled;
                            EditorApplication.delayCall += () => SdfxModulePickerWindow.Open(target, asset);
                        }
                    }
                }

                if (compiled == null)
                {
                    EditorGUILayout.HelpBox(SdfxLanguage.ShaderGui.NoCompiledAsset, MessageType.Warning);
                }
            }
        }


        private List<ShaderModule> GetCompiledModules(Material material, MaterialProperty[] properties)
        {
            var shaderName = material != null && material.shader != null ? material.shader.name : string.Empty;
            if (cachedCompiledModules != null
                && string.Equals(cachedModulesShaderName, shaderName, System.StringComparison.Ordinal))
            {
                return cachedCompiledModules;
            }

            cachedModulesShaderName = shaderName;
            cachedCompiledModules = ShaderModuleRegistry.All
                .Where(m => FindProperty(m.ToggleProperty, properties, propertyIsMandatory: false) != null)
                .ToList();
            return cachedCompiledModules;
        }

        private static bool DrawSubSection(
            string key,
            string title,
            string search,
            bool parentHeaderMatches,
            params string[] fieldNames)
        {
            if (SdfxEditorSearch.HasQuery(search) && !parentHeaderMatches)
            {
                var anyVisible = false;
                foreach (var field in fieldNames)
                {
                    if (SdfxEditorSearch.MatchesQuery(search, field, title))
                    {
                        anyVisible = true;
                        break;
                    }
                }

                if (!anyVisible)
                {
                    return false;
                }
            }

            return DrawSectionFoldout("sub." + key, title, defaultOpen: true, forceOpen: SdfxEditorSearch.HasQuery(search));
        }

        private static bool SectionVisible(string search, MaterialProperty[] properties, string sectionTitle, params string[] fields)
        {
            if (!SdfxEditorSearch.HasQuery(search))
            {
                return true;
            }

            var searchable = new List<string> { sectionTitle };
            foreach (var field in fields)
            {
                searchable.Add(field);
                if (field != null && field.StartsWith("_"))
                {
                    var prop = FindProperty(field, properties, propertyIsMandatory: false);
                    if (prop != null)
                    {
                        searchable.Add(prop.displayName);
                    }
                }
            }

            return SdfxEditorSearch.MatchesQuery(search, searchable.ToArray());
        }

        private static void DrawModuleProperty(
            MaterialEditor materialEditor,
            MaterialProperty[] properties,
            string name,
            string search = "",
            bool sectionHeaderMatches = true)
        {
            if (!SdfxModulePropertyDrawer.TryGetCoreMeta(name, out var meta))
            {
                DrawPropertyIfPresent(materialEditor, properties, name, search, sectionHeaderMatches);
                return;
            }

            SdfxModulePropertyDrawer.Draw(materialEditor, properties, meta, search, sectionHeaderMatches);
        }

        private static void DrawPropertyIfPresent(
            MaterialEditor materialEditor,
            MaterialProperty[] properties,
            string name,
            string search = "",
            bool sectionHeaderMatches = true)
        {
            var prop = FindProperty(name, properties, propertyIsMandatory: false);
            if (prop == null)
            {
                return;
            }

            if (SdfxEditorSearch.HasQuery(search)
                && !sectionHeaderMatches
                && !SdfxEditorSearch.MaterialPropertyMatches(prop, search))
            {
                return;
            }

            if (prop.type == MaterialProperty.PropType.Range)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(prop.displayName);
                    EditorGUI.BeginChangeCheck();
                    var value = EditorGUILayout.Slider(prop.floatValue, prop.rangeLimits.x, prop.rangeLimits.y);
                    if (EditorGUI.EndChangeCheck())
                    {
                        materialEditor.RegisterPropertyChangeUndo(prop.displayName);
                        prop.floatValue = value;
                    }

                    GUILayout.Label(
                        value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                        EditorStyles.miniLabel,
                        GUILayout.Width(44f));
                }

                return;
            }

            materialEditor.ShaderProperty(prop, prop.displayName);
        }

        private static bool DrawSectionFoldout(string key, string title, bool defaultOpen, bool forceOpen = false)
        {
            var open = GetFoldout(key, defaultOpen);
            if (forceOpen)
            {
                open = true;
            }

            var newOpen = EditorGUILayout.Foldout(open, title, toggleOnLabelClick: true, EditorStyles.foldoutHeader);
            if (newOpen != open)
            {
                SetFoldout(key, newOpen);
            }

            return newOpen;
        }

        private static readonly Dictionary<string, bool> FoldoutMemory = new Dictionary<string, bool>();

        private static bool GetFoldout(string key, bool defaultValue)
        {
            var fullKey = FoldoutKeyPrefix + key;
            if (FoldoutMemory.TryGetValue(fullKey, out var cached))
            {
                return cached;
            }

            var value = SessionState.GetBool(fullKey, defaultValue);
            FoldoutMemory[fullKey] = value;
            return value;
        }

        private static void SetFoldout(string key, bool value)
            => SetFoldoutStatic(key, value);

        private static void SetFoldoutStatic(string key, bool value)
        {
            var fullKey = FoldoutKeyPrefix + key;
            if (FoldoutMemory.TryGetValue(fullKey, out var previous) && previous == value)
            {
                return;
            }

            FoldoutMemory[fullKey] = value;
            SessionState.SetBool(fullKey, value);
        }

        private static SdfxMaterialLookPreset LoadCustomPreset()
        {
            var path = SessionState.GetString(CustomPresetKey, string.Empty);
            return string.IsNullOrWhiteSpace(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<SdfxMaterialLookPreset>(path);
        }

        private static void SaveCustomPreset(SdfxMaterialLookPreset preset)
        {
            if (preset == null)
            {
                SessionState.EraseString(CustomPresetKey);
                return;
            }

            var path = AssetDatabase.GetAssetPath(preset);
            if (!string.IsNullOrWhiteSpace(path))
            {
                SessionState.SetString(CustomPresetKey, path);
            }
        }

    }
}

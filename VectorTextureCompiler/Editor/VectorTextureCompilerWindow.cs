using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SDFX.VectorTextureCompiler.Core;
using SDFX.VectorTextureCompiler.Core.CodeGen;
using SDFX.VectorTextureCompiler.Core.Compiler;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Modules;
using SDFX.VectorTextureCompiler.Core.Optimize;
using SDFX.VectorTextureCompiler.Core.Parsing;
using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Editor
{
    public sealed class VectorTextureCompilerWindow : EditorWindow
    {
        private const string LastCompiledAssetPathKey = "SDFX.VectorTextureCompiler.LastCompiledAssetPath";
        private const string SettingsPrefix = "SDFX.CompilerWindow.";

        private CompileSourceType sourceType = CompileSourceType.Auto;
        private UnityEngine.Object sourceAsset;
        private string outputDirectory = string.Empty;
        private bool buildQuestVariant = true;
        private ParserStrictness parserStrictness = ParserStrictness.Strict;
        private CoordinateModel coordinateModel = CoordinateModel.Hybrid;
        private OptimizationProfile optimizationProfile = OptimizationProfile.Pc;
        private Color backgroundColor = Color.white;
        private TransparencyMode transparencyMode = TransparencyMode.Auto;
        private Vector2 reportScroll;
        private Vector2 mainScroll;
        private Vector2 moduleListScroll;
        private Vector2 conflictScroll;
        private string moduleSearch = string.Empty;
        private CompileReport latestReport;
        private string latestCompiledAssetPath = string.Empty;
        private readonly Dictionary<string, bool> moduleSelection = new Dictionary<string, bool>();
        private readonly Dictionary<ModuleCategory, bool> moduleCategoryFoldouts = new Dictionary<ModuleCategory, bool>();
        private bool modulesFoldout = true;
        private bool conflictsFoldout;
        private bool reportFoldout;
        private string modulePresetId = "avatar";
        private int moduleLodTier;
        private int compileBlendMode;
        private bool sourceFoldout = true;
        private bool compileOptionsFoldout;
        private bool enableForwardAddPass;
        private bool decalLayersFoldout;
        private readonly List<DecalCompositor.DecalLayer> decalLayers = new List<DecalCompositor.DecalLayer>();

        [MenuItem(SdfxLanguage.Menu.OpenCompilerWindow)]
        public static void Open()
        {
            var window = GetWindow<VectorTextureCompilerWindow>();
            window.titleContent = new GUIContent(SdfxLanguage.EditorWindow.WindowTitle);
            window.minSize = new Vector2(420f, 480f);
            window.ApplyOpenDefaults();
            window.LoadPersistedReport();
        }

        private void OnEnable()
        {
            LoadSettings();
            ApplyOpenDefaults();
            LoadPersistedReport();
        }

        private void ApplyOpenDefaults()
        {
            reportFoldout = false;
            compileOptionsFoldout = false;
        }

        private void OnDisable()
        {
            SaveSettings();
            SdfxBannerRenderer.Cleanup();
        }

        private void OnGUI()
        {
            mainScroll = SdfxEditorScroll.Begin(
                mainScroll,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(position.height));

            try
            {
                DrawWindowContents();
            }
            finally
            {
                SdfxEditorScroll.End();
            }
        }

        private void DrawWindowContents()
        {
            SdfxBannerRenderer.Draw(this);
            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.Header, EditorStyles.centeredGreyMiniLabel);
            EditorGUI.BeginChangeCheck();

            DrawSourceSection();
            DrawCompileOptionsSection();
            DrawDecalLayersSection();
            DrawModuleSelection();

            var settingsChanged = EditorGUI.EndChangeCheck();
            if (settingsChanged)
            {
                SaveSettings();
            }

            if (latestReport == null)
            {
                LoadPersistedReport();
            }

            DrawActionBar();
            DrawLatestReport();
        }

        private void DrawSourceSection()
        {
            sourceFoldout = EditorGUILayout.Foldout(sourceFoldout, SdfxLanguage.EditorWindow.SourceSectionHeader, true, EditorStyles.foldoutHeader);
            if (!sourceFoldout)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                sourceType = DrawSourceTypePopup(SdfxLanguage.EditorWindow.SourceTypeField, sourceType);
                sourceAsset = EditorGUILayout.ObjectField(SdfxLanguage.EditorWindow.SourceField, sourceAsset, typeof(UnityEngine.Object), false);
                if (sourceAsset != null && !IsValidVectorSourceAsset(sourceAsset))
                {
                    EditorGUILayout.HelpBox(SdfxLanguage.EditorWindow.InvalidSourceAssetHelp, MessageType.Warning);
                }
            }
        }

        private static CompileSourceType DrawSourceTypePopup(string label, CompileSourceType current)
        {
            var values = new[] { CompileSourceType.Auto, CompileSourceType.Custom };
            var labels = values.Select(v => v.ToString()).ToArray();
            var index = Array.IndexOf(values, current);
            if (index < 0)
            {
                index = 0;
            }

            return values[EditorGUILayout.Popup(label, index, labels)];
        }

        private void DrawCompileOptionsSection()
        {
            compileOptionsFoldout = EditorGUILayout.Foldout(compileOptionsFoldout, SdfxLanguage.EditorWindow.CompileOptionsHeader, true, EditorStyles.foldoutHeader);
            if (!compileOptionsFoldout)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                outputDirectory = EditorGUILayout.TextField(SdfxLanguage.EditorWindow.OutputDirectoryField, outputDirectory);
                DrawOutputDirectoryHelp();
                backgroundColor = EditorGUILayout.ColorField(SdfxLanguage.EditorWindow.BackgroundColorField, backgroundColor);
                transparencyMode = (TransparencyMode)EditorGUILayout.EnumPopup(SdfxLanguage.EditorWindow.TransparencyModeField, transparencyMode);
                DrawCompileBlendModeField();
                optimizationProfile = (OptimizationProfile)EditorGUILayout.EnumPopup(SdfxLanguage.EditorWindow.OptimizationProfileField, optimizationProfile);
                if (optimizationProfile == OptimizationProfile.Quest)
                {
                    buildQuestVariant = EditorGUILayout.Toggle(SdfxLanguage.EditorWindow.BuildQuestVariantField, buildQuestVariant);
                }

                parserStrictness = (ParserStrictness)EditorGUILayout.EnumPopup(SdfxLanguage.EditorWindow.ParserStrictnessField, parserStrictness);
                coordinateModel = (CoordinateModel)EditorGUILayout.EnumPopup(SdfxLanguage.EditorWindow.CoordinateModelField, coordinateModel);
                enableForwardAddPass = EditorGUILayout.Toggle(
                    SdfxLanguage.EditorWindow.EnableForwardAddPassField,
                    enableForwardAddPass);
                if (enableForwardAddPass)
                {
                    EditorGUILayout.HelpBox(SdfxLanguage.EditorWindow.EnableForwardAddPassHelp, MessageType.Info);
                }
            }
        }

        private void DrawDecalLayersSection()
        {
            decalLayersFoldout = EditorGUILayout.Foldout(
                decalLayersFoldout,
                SdfxLanguage.EditorWindow.DecalLayersHeader,
                true,
                EditorStyles.foldoutHeader);
            if (!decalLayersFoldout)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                for (var i = 0; i < decalLayers.Count; i++)
                {
                    var layer = decalLayers[i];
                    EditorGUILayout.LabelField($"Layer {i}", EditorStyles.boldLabel);
                    layer.Albedo = (Texture2D)EditorGUILayout.ObjectField(
                        SdfxLanguage.EditorWindow.DecalAlbedoField,
                        layer.Albedo,
                        typeof(Texture2D),
                        false);
                    layer.UvOffset = EditorGUILayout.Vector2Field(
                        SdfxLanguage.EditorWindow.DecalUvOffsetField,
                        layer.UvOffset);
                    layer.UvScale = EditorGUILayout.Vector2Field(
                        SdfxLanguage.EditorWindow.DecalUvScaleField,
                        layer.UvScale);
                    layer.BlendStrength = EditorGUILayout.Slider(
                        SdfxLanguage.EditorWindow.DecalBlendField,
                        layer.BlendStrength,
                        0f,
                        1f);
                    layer.BlendMode = (DecalCompositor.DecalBlendMode)EditorGUILayout.EnumPopup(
                        SdfxLanguage.EditorWindow.DecalBlendModeField,
                        layer.BlendMode);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(SdfxLanguage.EditorWindow.DecalRemoveButton, GUILayout.Width(80f)))
                        {
                            decalLayers.RemoveAt(i);
                            i--;
                        }
                    }

                    EditorGUILayout.Space(4f);
                }

                using (new EditorGUI.DisabledScope(decalLayers.Count >= DecalCompositor.MaxDecalLayers))
                {
                    if (GUILayout.Button(SdfxLanguage.EditorWindow.DecalAddButton))
                    {
                        decalLayers.Add(new DecalCompositor.DecalLayer());
                    }
                }
            }
        }

        private void DrawCompileBlendModeField()
        {
            compileBlendMode = EditorGUILayout.Popup(
                SdfxLanguage.EditorWindow.CompileBlendModeField,
                Mathf.Max(0, compileBlendMode),
                SdfxLanguage.EditorWindow.CompileBlendModeOptionLabels);
        }

        private void DrawActionBar()
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!CanCompile()))
                {
                    if (GUILayout.Button(SdfxLanguage.EditorWindow.CompileButton, GUILayout.Height(28f), GUILayout.Width(120f)))
                    {
                        CompileSelected();
                    }
                }

                if (GUILayout.Button(SdfxLanguage.EditorWindow.ClearReportButton, GUILayout.Height(28f), GUILayout.Width(120f)))
                {
                    ClearLatestReport();
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawOutputDirectoryHelp()
        {
            if (!string.IsNullOrWhiteSpace(outputDirectory)
                && !string.Equals(outputDirectory, CompileOutputPaths.LegacyDefaultRoot, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.OutputDirectoryAutoHelp, EditorStyles.wordWrappedMiniLabel);

            if (!TryBuildPreviewCompileOptions(out var preview))
            {
                return;
            }

            var resolved = CompileOutputPaths.Resolve(preview);
            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.OutputDirectoryResolved(resolved), EditorStyles.miniLabel);
        }

        private bool TryBuildPreviewCompileOptions(out CompileOptions options)
        {
            options = null;
            if (sourceAsset == null)
            {
                return false;
            }

            var sourcePath = AssetDatabase.GetAssetPath(sourceAsset);
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return false;
            }

            options = new CompileOptions
            {
                SourceType = sourceType,
                SourcePath = sourcePath,
                OutputDirectory = outputDirectory
            };
            return true;
        }

        private void DrawModuleSelection()
        {
            EditorGUILayout.Space(4f);
            var newModulesFoldout = EditorGUILayout.Foldout(modulesFoldout, SdfxLanguage.EditorWindow.ModulesHeader, true, EditorStyles.foldoutHeader);
            if (newModulesFoldout != modulesFoldout)
            {
                modulesFoldout = newModulesFoldout;
                if (!modulesFoldout)
                {
                    moduleListScroll = Vector2.zero;
                }
            }

            if (!modulesFoldout)
            {
                return;
            }

            EditorGUILayout.HelpBox(SdfxLanguage.EditorWindow.ModulesHelp, MessageType.None);
            EditorGUILayout.HelpBox(SdfxLanguage.EditorWindow.ModulesCustomHelp, MessageType.Info);

            var assetModules = ShaderModuleRegistrar.FindAssetDefinitions();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.ModulesAssetCount(assetModules.Count), EditorStyles.miniLabel);
                if (GUILayout.Button(SdfxLanguage.EditorWindow.ModulesCreateAssetButton, GUILayout.Width(170f)))
                {
                    ShaderModuleRegistrar.CreateAssetInProject();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var presetLabels = ShaderModuleRegistry.PresetsCatalog.Select(p => p.DisplayName).ToArray();
                var presetIds = ShaderModuleRegistry.PresetsCatalog.Select(p => p.Id).ToArray();
                var currentIndex = Array.IndexOf(presetIds, modulePresetId);
                if (currentIndex < 0)
                {
                    currentIndex = 0;
                }

                var newIndex = EditorGUILayout.Popup(SdfxLanguage.EditorWindow.ModulePresetField, currentIndex, presetLabels);
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < presetIds.Length)
                {
                    modulePresetId = presetIds[newIndex];
                    ApplyModulePreset(modulePresetId);
                }
            }

            moduleLodTier = EditorGUILayout.IntSlider(SdfxLanguage.EditorWindow.ModuleLodTierField, moduleLodTier, 0, 4);

            var selectedIds = ShaderModuleRegistry.All.Where(m => IsModuleSelected(m.Id)).Select(m => m.Id).ToList();
            var samplerCount = ShaderModuleRegistry.TotalExtraSamplerCount(
                ShaderModuleRegistry.Resolve(selectedIds, moduleLodTier));
            EditorGUILayout.LabelField(
                SdfxLanguage.EditorWindow.SamplerBudgetLabel,
                $"{samplerCount} / {CorePipeline.QuestMaxSamplerBudget}");

            var conflicts = ShaderModuleRegistry.FindConflicts(selectedIds);
            if (conflicts.Count > 0)
            {
                var conflictTitle = SdfxLanguage.EditorWindow.ModuleConflictsFoldout(conflicts.Count);
                conflictsFoldout = EditorGUILayout.Foldout(conflictsFoldout, conflictTitle, true, EditorStyles.foldoutHeader);
                if (conflictsFoldout)
                {
                    var conflictScrollHeight = Mathf.Clamp(conflicts.Count * 48f, 48f, 160f);
                    conflictScroll = SdfxEditorScroll.Begin(
                        conflictScroll,
                        GUILayout.ExpandWidth(true),
                        GUILayout.MaxHeight(conflictScrollHeight));
                    foreach (var conflict in conflicts)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.HelpBox(conflict.Message, MessageType.Warning);
                            var left = ShaderModuleRegistry.Find(conflict.LeftId);
                            var right = ShaderModuleRegistry.Find(conflict.RightId);
                            var leftName = left?.DisplayName ?? conflict.LeftId;
                            var rightName = right?.DisplayName ?? conflict.RightId;
                            if (GUILayout.Button(
                                    new GUIContent(
                                        leftName,
                                        SdfxLanguage.EditorWindow.DisableModuleTooltip(leftName)),
                                    GUILayout.Width(88f),
                                    GUILayout.Height(38f)))
                            {
                                moduleSelection[conflict.LeftId] = false;
                                SaveSettings();
                                GUIUtility.ExitGUI();
                            }

                            if (GUILayout.Button(
                                    new GUIContent(
                                        rightName,
                                        SdfxLanguage.EditorWindow.DisableModuleTooltip(rightName)),
                                    GUILayout.Width(88f),
                                    GUILayout.Height(38f)))
                            {
                                moduleSelection[conflict.RightId] = false;
                                SaveSettings();
                                GUIUtility.ExitGUI();
                            }
                        }
                    }

                    SdfxEditorScroll.End();
                }
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                moduleSearch = SdfxEditorSearch.DrawInlineField(moduleSearch, SdfxLanguage.EditorWindow.SearchPlaceholder);
                if (GUILayout.Button(SdfxLanguage.EditorWindow.ModulesSelectAll, EditorStyles.miniButtonLeft, GUILayout.Width(44f)))
                {
                    SetAllModules(true, moduleSearch);
                }

                if (GUILayout.Button(SdfxLanguage.EditorWindow.ModulesSelectNone, EditorStyles.miniButtonRight, GUILayout.Width(44f)))
                {
                    SetAllModules(false, moduleSearch);
                }
            }

            var visibleCount = 0;
            var searching = SdfxEditorSearch.HasQuery(moduleSearch);
            var moduleScrollHeight = Mathf.Clamp(position.height * 0.38f, 200f, 520f);
            moduleListScroll = SdfxEditorScroll.Begin(
                moduleListScroll,
                GUILayout.ExpandWidth(true),
                GUILayout.MaxHeight(moduleScrollHeight));
            foreach (var group in ShaderModuleRegistry.All.GroupBy(m => m.Category).OrderBy(g => (int)g.Key))
            {
                var categoryLabel = SdfxEditorLabels.Category(group.Key);
                var visibleModules = group
                    .Where(m => SdfxEditorSearch.ModuleMatches(m, moduleSearch, categoryLabel))
                    .ToList();
                if (visibleModules.Count == 0)
                {
                    continue;
                }

                visibleCount += visibleModules.Count;
                var selectedInCategory = visibleModules.Count(m => IsModuleSelected(m.Id));
                var foldoutLabel = SdfxLanguage.ShaderGui.CategoryWithCount(categoryLabel, selectedInCategory, visibleModules.Count);
                var open = searching || IsCategoryFoldoutOpen(group.Key);
                var nextOpen = EditorGUILayout.Foldout(open, foldoutLabel, true, EditorStyles.foldoutHeader);
                if (!searching)
                {
                    moduleCategoryFoldouts[group.Key] = nextOpen;
                    open = nextOpen;
                }
                else
                {
                    open = true;
                }

                if (!open)
                {
                    continue;
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var module in visibleModules)
                    {
                        var selected = IsModuleSelected(module.Id);
                        var isAsset = module is AssetBackedShaderModule;
                        var label = isAsset
                            ? $"{module.DisplayName} [{SdfxLanguage.EditorWindow.ModulesAssetBadge}]"
                            : module.DisplayName;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            moduleSelection[module.Id] = EditorGUILayout.ToggleLeft(
                                new GUIContent(label, module.Description),
                                selected);
                            if (isAsset && module is AssetBackedShaderModule assetModule && assetModule.Definition != null)
                            {
                                if (GUILayout.Button(SdfxLanguage.EditorWindow.ModulesPingAssetButton, EditorStyles.miniButton, GUILayout.Width(44f)))
                                {
                                    EditorGUIUtility.PingObject(assetModule.Definition);
                                    Selection.activeObject = assetModule.Definition;
                                }
                            }
                        }
                    }
                }
            }

            if (SdfxEditorSearch.HasQuery(moduleSearch) && visibleCount == 0)
            {
                EditorGUILayout.HelpBox(SdfxLanguage.EditorWindow.SearchNoResults, MessageType.Info);
            }

            SdfxEditorScroll.End();
        }

        private bool IsCategoryFoldoutOpen(ModuleCategory category)
            => moduleCategoryFoldouts.TryGetValue(category, out var open) && open;

        private void ApplyModulePreset(string presetId)
        {
            var preset = ShaderModuleRegistry.FindPreset(presetId);
            SetAllModules(false);
            if (preset?.EnabledModuleIds == null)
            {
                SetAllModules(true);
            }
            else
            {
                foreach (var id in preset.EnabledModuleIds)
                {
                    moduleSelection[id] = true;
                }
            }

            if (preset != null)
            {
                if (preset.OptimizationProfile.HasValue)
                {
                    optimizationProfile = preset.OptimizationProfile.Value;
                }

                if (preset.ModuleLodTier.HasValue)
                {
                    moduleLodTier = preset.ModuleLodTier.Value;
                }

                if (preset.BuildQuestVariant.HasValue)
                {
                    buildQuestVariant = preset.BuildQuestVariant.Value;
                }

                if (preset.TransparencyMode.HasValue)
                {
                    transparencyMode = preset.TransparencyMode.Value;
                }

                if (preset.BlendMode.HasValue)
                {
                    compileBlendMode = (int)preset.BlendMode.Value + 1;
                }
            }

            SaveSettings();
        }

        private bool IsModuleSelected(string moduleId)
            => !moduleSelection.TryGetValue(moduleId, out var selected) || selected;

        private void SetAllModules(bool selected, string searchFilter = null)
        {
            foreach (var module in ShaderModuleRegistry.All)
            {
                if (SdfxEditorSearch.HasQuery(searchFilter))
                {
                    var categoryLabel = SdfxEditorLabels.Category(module.Category);
                    if (!SdfxEditorSearch.ModuleMatches(module, searchFilter, categoryLabel))
                    {
                        continue;
                    }
                }

                moduleSelection[module.Id] = selected;
            }

            SaveSettings();
        }

        private void SaveSettings()
        {
            SetSetting("sourceType", ((int)sourceType).ToString());
            SetSetting("sourceAsset", AssetDatabase.GetAssetPath(sourceAsset));
            SetSetting("outputDirectory", outputDirectory);
            SetSetting("buildQuestVariant", buildQuestVariant.ToString());
            SetSetting("parserStrictness", ((int)parserStrictness).ToString());
            SetSetting("coordinateModel", ((int)coordinateModel).ToString());
            SetSetting("optimizationProfile", ((int)optimizationProfile).ToString());
            SetSetting("backgroundColor", "#" + ColorUtility.ToHtmlStringRGBA(backgroundColor));
            SetSetting("transparencyMode", ((int)transparencyMode).ToString());
            SetSetting("modulePresetId", modulePresetId ?? string.Empty);
            SetSetting("moduleLodTier", moduleLodTier.ToString());
            SetSetting("compileBlendMode", compileBlendMode.ToString());
            SetSetting("enableForwardAddPass", enableForwardAddPass.ToString());

            var disabledModules = ShaderModuleRegistry.All
                .Where(m => !IsModuleSelected(m.Id))
                .Select(m => m.Id);
            SetSetting("disabledModules", string.Join(",", disabledModules));
        }

        private void LoadSettings()
        {
            sourceType = (CompileSourceType)GetSettingInt("sourceType", (int)sourceType);
            if ((int)sourceType == 3 || sourceType == CompileSourceType.Svg)
            {
                sourceType = CompileSourceType.Auto;
            }

            sourceAsset = LoadAssetSetting<UnityEngine.Object>("sourceAsset");
            outputDirectory = GetSettingString("outputDirectory", outputDirectory);
            if (string.Equals(outputDirectory, CompileOutputPaths.LegacyDefaultRoot, StringComparison.OrdinalIgnoreCase))
            {
                outputDirectory = string.Empty;
            }
            buildQuestVariant = GetSettingBool("buildQuestVariant", buildQuestVariant);
            parserStrictness = (ParserStrictness)GetSettingInt("parserStrictness", (int)parserStrictness);
            coordinateModel = (CoordinateModel)GetSettingInt("coordinateModel", (int)coordinateModel);
            optimizationProfile = (OptimizationProfile)GetSettingInt("optimizationProfile", (int)optimizationProfile);
            var bgHex = GetSettingString("backgroundColor", string.Empty);
            if (!string.IsNullOrWhiteSpace(bgHex) && ColorUtility.TryParseHtmlString(bgHex, out var parsedBg))
            {
                backgroundColor = parsedBg;
            }

            transparencyMode = (TransparencyMode)GetSettingInt("transparencyMode", (int)transparencyMode);
            modulePresetId = GetSettingString("modulePresetId", modulePresetId);
            moduleLodTier = GetSettingInt("moduleLodTier", moduleLodTier);
            compileBlendMode = Mathf.Max(0, GetSettingInt("compileBlendMode", compileBlendMode));
            enableForwardAddPass = GetSettingBool("enableForwardAddPass", enableForwardAddPass);

            moduleSelection.Clear();
            var disabledCsv = GetSettingString("disabledModules", string.Empty);
            if (!string.IsNullOrWhiteSpace(disabledCsv))
            {
                foreach (var id in disabledCsv.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        moduleSelection[id.Trim()] = false;
                    }
                }
            }
        }

        private static void SetSetting(string key, string value)
            => EditorUserSettings.SetConfigValue(SettingsPrefix + key, value ?? string.Empty);

        private static string GetSettingString(string key, string fallback)
        {
            var value = EditorUserSettings.GetConfigValue(SettingsPrefix + key);
            return value ?? fallback;
        }

        private static int GetSettingInt(string key, int fallback)
            => int.TryParse(EditorUserSettings.GetConfigValue(SettingsPrefix + key), out var parsed) ? parsed : fallback;

        private static bool GetSettingBool(string key, bool fallback)
            => bool.TryParse(EditorUserSettings.GetConfigValue(SettingsPrefix + key), out var parsed) ? parsed : fallback;

        private static T LoadAssetSetting<T>(string key) where T : UnityEngine.Object
        {
            var path = EditorUserSettings.GetConfigValue(SettingsPrefix + key);
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private List<string> SelectedModuleIds()
            => ShaderModuleRegistry.All.Where(m => IsModuleSelected(m.Id)).Select(m => m.Id).ToList();

        private void CompileSelected()
        {
            var sourcePath = AssetDatabase.GetAssetPath(sourceAsset);
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                Debug.LogError(SdfxLanguage.EditorWindow.ResolveSourcePathFailed);
                return;
            }

            var options = BuildCompileOptions(sourcePath);
            var result = VectorTextureCompilerFacade.Compile(options);
            if (!result.Success)
            {
                Debug.LogError(SdfxLanguage.EditorWindow.CompileFailed(result.Message));
                return;
            }

            var sourceName = CompileOutputPaths.ResolveSourceName(options);
            latestCompiledAssetPath = Path.Combine(CompileOutputPaths.Resolve(options, sourceName), sourceName + "_Compiled.asset").Replace("\\", "/");
            var compiled = AssetDatabase.LoadAssetAtPath<CompiledVectorTextureAsset>(latestCompiledAssetPath);
            latestReport = compiled != null ? compiled.compileReport : null;
            if (!string.IsNullOrWhiteSpace(latestCompiledAssetPath))
            {
                EditorPrefs.SetString(LastCompiledAssetPathKey, latestCompiledAssetPath);
            }

            Debug.Log(SdfxLanguage.EditorWindow.CompileComplete(result.MaterialAssetPath));
            AssetDatabase.Refresh();
            Repaint();
        }

        private CompileOptions BuildCompileOptions(string sourcePath)
        {
            return new CompileOptions
            {
                SourceType = sourceType,
                SourcePath = sourcePath,
                OutputDirectory = outputDirectory,
                BuildQuestVariant = optimizationProfile == OptimizationProfile.Quest && buildQuestVariant,
                ParserStrictness = parserStrictness,
                CoordinateModel = coordinateModel,
                OptimizationProfile = optimizationProfile,
                EnabledModules = SelectedModuleIds(),
                ModulePresetId = modulePresetId,
                ModuleLodTier = moduleLodTier,
                BackgroundColor = backgroundColor,
                TransparencyMode = transparencyMode,
                BlendMode = compileBlendMode <= 0
                    ? null
                    : (BlendModePreset?)(compileBlendMode - 1),
                EnableForwardAddPass = enableForwardAddPass,
                DecalLayers = decalLayers.Count > 0 ? new List<DecalCompositor.DecalLayer>(decalLayers) : null
            };
        }

        private bool CanCompile()
        {
            return IsValidVectorSourceAsset(sourceAsset);
        }

        private static bool IsValidVectorSourceAsset(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return false;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (asset is TextAsset)
            {
                return true;
            }

            return path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
        }

        private void LoadPersistedReport()
        {
            if (!string.IsNullOrWhiteSpace(latestCompiledAssetPath) && latestReport != null)
            {
                return;
            }

            var persistedPath = EditorPrefs.GetString(LastCompiledAssetPathKey, string.Empty);
            if (string.IsNullOrWhiteSpace(persistedPath))
            {
                return;
            }

            var compiled = AssetDatabase.LoadAssetAtPath<CompiledVectorTextureAsset>(persistedPath);
            if (compiled == null)
            {
                return;
            }

            latestCompiledAssetPath = persistedPath;
            latestReport = compiled.compileReport;
        }

        private void DrawLatestReport()
        {
            EditorGUILayout.Space(4f);
            reportFoldout = EditorGUILayout.Foldout(reportFoldout, SdfxLanguage.EditorWindow.LatestCompileReportHeader, true, EditorStyles.foldoutHeader);

            if (!reportFoldout)
            {
                return;
            }

            if (latestReport == null)
            {
                EditorGUILayout.HelpBox(SdfxLanguage.EditorWindow.NoReportHelp, MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.CompiledAssetLabel, string.IsNullOrWhiteSpace(latestCompiledAssetPath) ? SdfxLanguage.EditorWindow.UnknownValue : latestCompiledAssetPath);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(SdfxLanguage.EditorWindow.ReportPingButton, GUILayout.Width(90f)))
                {
                    SdfxCompilerActions.PingCompiledAsset(latestCompiledAssetPath);
                }

                if (GUILayout.Button(SdfxLanguage.EditorWindow.ReportOpenFolderButton, GUILayout.Width(100f)))
                {
                    SdfxCompilerActions.OpenOutputFolder(latestCompiledAssetPath);
                }

                var compiled = AssetDatabase.LoadAssetAtPath<CompiledVectorTextureAsset>(latestCompiledAssetPath);
                using (new EditorGUI.DisabledScope(compiled == null))
                {
                    if (GUILayout.Button(SdfxLanguage.EditorWindow.ReportRecompileButton, GUILayout.Width(90f)))
                    {
                        if (SdfxCompilerActions.TryRecompile(compiled, out var message) && !string.IsNullOrWhiteSpace(latestCompiledAssetPath))
                        {
                            var refreshed = AssetDatabase.LoadAssetAtPath<CompiledVectorTextureAsset>(latestCompiledAssetPath);
                            latestReport = refreshed != null ? refreshed.compileReport : latestReport;
                        }
                        else if (!string.IsNullOrWhiteSpace(message))
                        {
                            Debug.LogWarning(message);
                        }
                    }
                }
            }
            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.GeneratedUtcLabel, latestReport.generatedAtUtc);
            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.ProfileLabel, latestReport.optimizationProfile);
            DrawStatusPill(
                SdfxLanguage.EditorWindow.StatusLabel,
                latestReport.warnings.parseErrors > 0
                    ? SdfxLanguage.EditorWindow.StatusErrors
                    : (latestReport.warnings.totalWarnings > 0 ? SdfxLanguage.EditorWindow.StatusWarnings : SdfxLanguage.EditorWindow.StatusHealthy),
                latestReport.warnings.parseErrors > 0
                    ? new Color(0.85f, 0.30f, 0.30f)
                    : (latestReport.warnings.totalWarnings > 0 ? new Color(0.95f, 0.70f, 0.25f) : new Color(0.25f, 0.75f, 0.30f)));

            var reportScrollHeight = Mathf.Clamp(position.height * 0.28f, 140f, 360f);
            reportScroll = SdfxEditorScroll.Begin(
                reportScroll,
                GUILayout.ExpandWidth(true),
                GUILayout.MaxHeight(reportScrollHeight));
            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.PrimitiveCountsHeader, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.CountParsed, latestReport.counts.parsed.ToString());
            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.CountSimplified, latestReport.counts.simplified.ToString());
            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.CountResolved, latestReport.counts.resolved.ToString());
            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.CountQuantized, latestReport.counts.quantized.ToString());
            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.CountFinal, latestReport.counts.final.ToString());
            DrawStatusRow(
                SdfxLanguage.EditorWindow.CountPathEdges,
                latestReport.counts.pathEdges.ToString(),
                latestReport.warnings.highPathEdgeCount ? new Color(0.95f, 0.70f, 0.25f) : Color.white);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.WarningsHeader, EditorStyles.boldLabel);
            DrawStatusRow(SdfxLanguage.EditorWindow.ParseWarnings, latestReport.warnings.parseWarnings.ToString(), latestReport.warnings.parseWarnings > 0 ? new Color(0.95f, 0.70f, 0.25f) : Color.white);
            DrawStatusRow(SdfxLanguage.EditorWindow.ParseErrors, latestReport.warnings.parseErrors.ToString(), latestReport.warnings.parseErrors > 0 ? new Color(0.85f, 0.30f, 0.30f) : Color.white);
            DrawStatusRow(SdfxLanguage.EditorWindow.DroppedGridRefs, latestReport.warnings.droppedGridReferences.ToString(), latestReport.warnings.droppedGridReferences > 0 ? new Color(0.95f, 0.70f, 0.25f) : Color.white);
            DrawStatusRow(
                SdfxLanguage.EditorWindow.HighPathEdgeCount,
                latestReport.warnings.highPathEdgeCount ? "Yes" : "No",
                latestReport.warnings.highPathEdgeCount ? new Color(0.95f, 0.70f, 0.25f) : Color.white);
            DrawStatusRow(SdfxLanguage.EditorWindow.TotalWarnings, latestReport.warnings.totalWarnings.ToString(), latestReport.warnings.totalWarnings > 0 ? new Color(0.95f, 0.70f, 0.25f) : new Color(0.25f, 0.75f, 0.30f));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.TimingsHeader, EditorStyles.boldLabel);
            var timingWarnMs = latestReport.optimizationProfile == "Quest" ? 6L : 8L;
            var timingErrorMs = latestReport.optimizationProfile == "Quest" ? 12L : 16L;
            DrawTimingRow(SdfxLanguage.EditorWindow.TimingParse, latestReport.timings.parseMs, timingWarnMs, timingErrorMs);
            DrawTimingRow(SdfxLanguage.EditorWindow.TimingSimplify, latestReport.timings.simplifyMs, timingWarnMs, timingErrorMs);
            DrawTimingRow(SdfxLanguage.EditorWindow.TimingBoolean, latestReport.timings.booleanMs, timingWarnMs, timingErrorMs);
            DrawTimingRow(SdfxLanguage.EditorWindow.TimingQuantize, latestReport.timings.quantizeMs, timingWarnMs, timingErrorMs);
            DrawTimingRow(SdfxLanguage.EditorWindow.TimingQuest, latestReport.timings.questMs, timingWarnMs, timingErrorMs);
            DrawTimingRow(SdfxLanguage.EditorWindow.TimingGrid, latestReport.timings.gridMs, timingWarnMs, timingErrorMs);
            DrawTimingRow(SdfxLanguage.EditorWindow.TimingBake, latestReport.timings.bakeMs, timingWarnMs, timingErrorMs);
            DrawTimingRow(SdfxLanguage.EditorWindow.TimingCodegen, latestReport.timings.codegenMs, timingWarnMs, timingErrorMs);
            DrawTimingRow(SdfxLanguage.EditorWindow.TimingAsset, latestReport.timings.assetMs, timingWarnMs, timingErrorMs);
            DrawTimingRow(SdfxLanguage.EditorWindow.TimingTotal, latestReport.timings.totalMs, timingWarnMs * 2, timingErrorMs * 2);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.ParseIssuesHeader, EditorStyles.boldLabel);
            var issueCount = latestReport.parseIssues == null ? 0 : latestReport.parseIssues.Count;
            if (issueCount == 0)
            {
                EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.None);
            }
            else
            {
                var maxIssues = Mathf.Min(issueCount, 8);
                for (var i = 0; i < maxIssues; i++)
                {
                    var issue = latestReport.parseIssues[i];
                    EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.ParseIssueSummary(issue.severity, issue.code, issue.lineNumber, issue.elementName));
                    EditorGUILayout.LabelField(issue.message, EditorStyles.wordWrappedMiniLabel);
                }

                if (issueCount > maxIssues)
                {
                    EditorGUILayout.LabelField(SdfxLanguage.EditorWindow.MoreIssues(issueCount - maxIssues), EditorStyles.miniLabel);
                }
            }

            SdfxEditorScroll.End();
        }

        private void ClearLatestReport()
        {
            latestReport = null;
            latestCompiledAssetPath = string.Empty;
            reportScroll = Vector2.zero;
            EditorPrefs.DeleteKey(LastCompiledAssetPathKey);
            Repaint();
        }

        private static void DrawStatusPill(string label, string value, Color color)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(120f));
                var prev = GUI.color;
                GUI.color = color;
                GUILayout.Label(value, EditorStyles.helpBox, GUILayout.MaxWidth(160f));
                GUI.color = prev;
            }
        }

        private static void DrawStatusRow(string label, string value, Color valueColor)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(180f));
                var previous = GUI.contentColor;
                GUI.contentColor = valueColor;
                EditorGUILayout.LabelField(value);
                GUI.contentColor = previous;
            }
        }

        private static void DrawTimingRow(string label, long milliseconds, long warningThreshold, long errorThreshold)
        {
            var color = Color.white;
            if (milliseconds >= errorThreshold)
            {
                color = new Color(0.85f, 0.30f, 0.30f);
            }
            else if (milliseconds >= warningThreshold)
            {
                color = new Color(0.95f, 0.70f, 0.25f);
            }

            DrawStatusRow(label, milliseconds.ToString(), color);
        }
    }
}
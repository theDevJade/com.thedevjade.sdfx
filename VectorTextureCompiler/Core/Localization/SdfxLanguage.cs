using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SDFX.VectorTextureCompiler.Core.CodeGen;

namespace SDFX.VectorTextureCompiler.Core.Localization
{
    public static class SdfxLanguage
    {
        private const string LanguageFilePath = "Packages/com.thedevjade.sdfx/VectorTextureCompiler/Core/Localization/sdfx-language.txt";
        private static readonly Dictionary<string, string> Entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static bool loaded;
        private static DateTime lastLoadedWriteUtc = DateTime.MinValue;

        private static string Get(string key, string fallback)
        {
            EnsureLoaded();
            return Entries.TryGetValue(key, out var value) ? value : fallback;
        }

        private static void EnsureLoaded()
        {
            if (!File.Exists(LanguageFilePath))
            {
                Entries.Clear();
                loaded = true;
                lastLoadedWriteUtc = DateTime.MinValue;
                return;
            }

            var currentWriteUtc = File.GetLastWriteTimeUtc(LanguageFilePath);
            if (loaded && currentWriteUtc == lastLoadedWriteUtc)
            {
                return;
            }

            Entries.Clear();
            var lines = File.ReadAllLines(LanguageFilePath);
            for (var i = 0; i < lines.Length; i++)
            {
                var raw = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var splitIndex = raw.IndexOf('=');
                if (splitIndex <= 0)
                {
                    continue;
                }

                var key = raw.Substring(0, splitIndex).Trim();
                var value = raw.Substring(splitIndex + 1).Trim();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    Entries[key] = value;
                }
            }

            loaded = true;
            lastLoadedWriteUtc = currentWriteUtc;
        }

        public static class Menu
        {
            public const string OpenCompilerWindow = "SDFX/Vector Texture Compiler";
            public const string CompileAllVectorTextures = "SDFX/Compile All Vector Textures";
            public const string OpenRasterizer = "SDFX/Rasterizer";
            public const string AutoConvertToSvg = "Assets/SDFX/Auto Convert To SVG";
            public const string OpenRasterizerFromAssets = "Assets/SDFX/Open Rasterizer";
        }

        public static class EditorWindow
        {
            public static string WindowTitle => Get("editor.windowTitle", "SDFX Compiler");
            public static string Header => Get("editor.header", "GPU-Native Vector Texture Compiler");
            public static string SourceField => Get("editor.sourceField", "Source (SVG asset or TextAsset)");
            public static string SourceTypeField => Get("editor.sourceTypeField", "Source Type");
            public static string BrowseButton => Get("editor.browseButton", "Browse");
            public static string OutputDirectoryField => Get("editor.outputDirectoryField", "Output Root Override");
            public static string OutputDirectoryAutoHelp => Get("editor.outputDirectoryAutoHelp", "Leave empty to write each compile to Generated/{name} beside the source.");
            public static string OutputDirectoryResolved(string path) => Get("editor.outputDirectoryResolved", "Next compile: {0}").Replace("{0}", path);
            public static string CompileOptionsHeader => Get("editor.compileOptionsHeader", "Compile Options");
            public static string CompileBlendModeField => Get("editor.compileBlendModeField", "Compile Blend Mode");
            public static string CompileBlendModeAuto => Get("editor.compileBlendModeAuto", "Auto");

            public static string[] CompileBlendModeOptionLabels
            {
                get
                {
                    var labels = new string[CompileBlendModeLabels.Length + 1];
                    labels[0] = CompileBlendModeAuto;
                    Array.Copy(CompileBlendModeLabels, 0, labels, 1, CompileBlendModeLabels.Length);
                    return labels;
                }
            }

            public static string SourceSectionHeader => Get("editor.sourceSectionHeader", "Source");
            public static string DecalLayersHeader => Get("editor.decalLayersHeader", "Decal Layers");
            public static string DecalAlbedoField => Get("editor.decalAlbedoField", "Albedo");
            public static string DecalUvOffsetField => Get("editor.decalUvOffsetField", "UV Offset");
            public static string DecalUvScaleField => Get("editor.decalUvScaleField", "UV Scale");
            public static string DecalBlendField => Get("editor.decalBlendField", "Blend");
            public static string DecalBlendModeField => Get("editor.decalBlendModeField", "Blend Mode");
            public static string DecalRemoveButton => Get("editor.decalRemoveButton", "Remove");
            public static string DecalAddButton => Get("editor.decalAddButton", "Add Decal Layer");
            public static string EnableForwardAddPassField => Get(
                "editor.enableForwardAddPassField",
                "ForwardAdd Pass (extra lights)");
            public static string EnableForwardAddPassHelp => Get(
                "editor.enableForwardAddPassHelp",
                "Adds realtime point/spot lighting.");
            public static string ReportPingButton => Get("editor.reportPingButton", "Ping Asset");
            public static string ReportOpenFolderButton => Get("editor.reportOpenFolderButton", "Open Folder");
            public static string ReportRecompileButton => Get("editor.reportRecompileButton", "Recompile");
            public static string BuildQuestVariantField => Get("editor.buildQuestVariantField", "Build Quest Variant");
            public static string ParserStrictnessField => Get("editor.parserStrictnessField", "Parser Strictness");
            public static string CoordinateModelField => Get("editor.coordinateModelField", "Coordinate Model");
            public static string OptimizationProfileField => Get("editor.optimizationProfileField", "Optimization Profile");
            public static string BackgroundColorField => Get("editor.backgroundColorField", "Background Color");
            public static string TransparencyModeField => Get("editor.transparencyModeField", "Transparency");
            public static string ModulesHeader => Get("editor.modulesHeader", "Shader Modules");
            public static string ModulesHelp => Get("editor.modulesHelp", "Selected modules are compiled into the generated shader behind keywords. They cost nothing until enabled on the material.");
            public static string ModulesCustomHelp => Get(
                "editor.modulesCustomHelp",
                "Add custom modules with Create → SDFX → Shader Module Definition, or from a C# class marked [SdfxModule]. Asset modules appear below after reload.");
            public static string ModulesAssetCount(int count)
                => Get("editor.modulesAssetCount", "Asset modules registered: {0}").Replace("{0}", count.ToString());
            public static string ModulesCreateAssetButton => Get("editor.modulesCreateAssetButton", "Create Module Definition");
            public static string ModulesPingAssetButton => Get("editor.modulesPingAssetButton", "Ping");
            public static string ModulesAssetBadge => Get("editor.modulesAssetBadge", "Asset");
            public static string ModulesSelectAll => Get("editor.modulesSelectAll", "All");
            public static string ModulesSelectNone => Get("editor.modulesSelectNone", "None");
            public static string SearchPlaceholder => Get("editor.searchPlaceholder", "Search modules...");
            public static string SearchNoResults => Get("editor.searchNoResults", "No modules match the current search.");
            public static string ModulePresetField => Get("editor.modulePresetField", "Module Preset");
            public static string ModuleLodTierField => Get("editor.moduleLodTierField", "Module LOD Tier");
            public static string ModuleConflictsHeader => Get("editor.moduleConflictsHeader", "Module Conflicts");
            public static string ModuleConflictsFoldout(int count)
                => Get("editor.moduleConflictsFoldout", "Module Conflicts ({0})").Replace("{0}", count.ToString());
            public static string DisableModuleButton => Get("editor.disableModuleButton", "Disable");
            public static string DisableModuleTooltip(string displayName)
                => Get("editor.disableModuleTooltip", "Disable {0}").Replace("{0}", displayName);
            public static string SamplerBudgetLabel => Get("editor.samplerBudgetLabel", "Extra Samplers (Quest)");
            public static string MoreIssues(int count)
                => Get("editor.moreIssues", "+ {0} more").Replace("{0}", count.ToString());

            public static string[] CompileBlendModeLabels => new[]
            {
                Get("editor.blendModeOpaque", "Opaque"),
                Get("editor.blendModeCutout", "Cutout"),
                Get("editor.blendModeFade", "Fade"),
                Get("editor.blendModeTransparent", "Transparent"),
                Get("editor.blendModeAdditive", "Additive"),
                Get("editor.blendModeMultiply", "Multiply"),
                Get("editor.blendModeScreen", "Screen"),
                Get("editor.blendModeOverlay", "Overlay"),
                Get("editor.blendModeSoftLight", "Soft Light"),
                Get("editor.blendModePremultipliedAlpha", "Premultiplied Alpha"),
                Get("editor.blendModeSoftAdditive", "Soft Additive")
            };

            public static string[] CompileBlendModeDescriptions => new[]
            {
                Get("editor.blendModeDescOpaque", "Solid surface, no transparency."),
                Get("editor.blendModeDescCutout", "Hard alpha cutout. Pair with Alpha Clip threshold."),
                Get("editor.blendModeDescFade", "Alpha fade without premultiplication."),
                Get("editor.blendModeDescTransparent", "Standard alpha blending."),
                Get("editor.blendModeDescAdditive", "Additive glow. Brightens the framebuffer."),
                Get("editor.blendModeDescMultiply", "Multiplies destination color by source."),
                Get("editor.blendModeDescScreen", "Screen blend. Soft brightening."),
                Get("editor.blendModeDescOverlay", "Overlay compositing in the fragment shader."),
                Get("editor.blendModeDescSoftLight", "Soft light compositing in the fragment shader."),
                Get("editor.blendModeDescPremultipliedAlpha", "Premultiplied alpha for cleaner edges."),
                Get("editor.blendModeDescSoftAdditive", "Soft additive. Gentler than full additive.")
            };

            public static string CoreBlendModeDisplayName => Get("editor.coreBlendModeDisplayName", "Blend Mode");
            public static string CoreRenderQueueDisplayName => Get("editor.coreRenderQueueDisplayName", "Render Queue");

            public static string[] RenderQueueLabels => new[]
            {
                Get("editor.renderQueueBackground", "Background"),
                Get("editor.renderQueueGeometry", "Geometry"),
                Get("editor.renderQueueAlphaTest", "Alpha Test"),
                Get("editor.renderQueueTransparent", "Transparent"),
                Get("editor.renderQueueOverlay", "Overlay")
            };

            public static string[] RenderQueueDescriptions => new[]
            {
                Get("editor.renderQueueDescBackground", "Renders behind everything (queue 1000)."),
                Get("editor.renderQueueDescGeometry", "Default opaque geometry (queue 2000)."),
                Get("editor.renderQueueDescAlphaTest", "Alpha-tested cutout geometry (queue 2450)."),
                Get("editor.renderQueueDescTransparent", "Transparent sorting bucket (queue 3000)."),
                Get("editor.renderQueueDescOverlay", "Overlay / on-top effects (queue 4000).")
            };
            public static string CompileButton => Get("editor.compileButton", "Compile");
            public static string LatestCompileReportHeader => Get("editor.latestCompileReportHeader", "Latest Compile Report");
            public static string ClearReportButton => Get("editor.clearReportButton", "Clear Report");
            public static string NoReportHelp => Get("editor.noReportHelp", "Run a compile to view stage metrics and warnings here.");
            public static string CompiledAssetLabel => Get("editor.compiledAssetLabel", "Compiled Asset");
            public static string UnknownValue => Get("editor.unknownValue", "(unknown)");
            public static string GeneratedUtcLabel => Get("editor.generatedUtcLabel", "Generated (UTC)");
            public static string ProfileLabel => Get("editor.profileLabel", "Profile");
            public static string StatusLabel => Get("editor.statusLabel", "Status");
            public static string StatusErrors => Get("editor.statusErrors", "Errors");
            public static string StatusWarnings => Get("editor.statusWarnings", "Warnings");
            public static string StatusHealthy => Get("editor.statusHealthy", "Healthy");
            public static string PrimitiveCountsHeader => Get("editor.primitiveCountsHeader", "Primitive Counts");
            public static string CountParsed => Get("editor.countParsed", "Parsed");
            public static string CountSimplified => Get("editor.countSimplified", "Simplified");
            public static string CountResolved => Get("editor.countResolved", "Resolved");
            public static string CountQuantized => Get("editor.countQuantized", "Quantized");
            public static string CountFinal => Get("editor.countFinal", "Final");
            public static string CountPathEdges => Get("editor.countPathEdges", "Path Edges");
            public static string WarningsHeader => Get("editor.warningsHeader", "Warnings");
            public static string ParseWarnings => Get("editor.parseWarnings", "Parse Warnings");
            public static string ParseErrors => Get("editor.parseErrors", "Parse Errors");
            public static string DroppedGridRefs => Get("editor.droppedGridRefs", "Dropped Grid References");
            public static string HighPathEdgeCount => Get("editor.highPathEdgeCount", "High Path Edge Count");
            public static string TotalWarnings => Get("editor.totalWarnings", "Total Warnings");
            public static string TimingsHeader => Get("editor.timingsHeader", "Timings (ms)");
            public static string TimingParse => Get("editor.timingParse", "Parse");
            public static string TimingSimplify => Get("editor.timingSimplify", "Simplify");
            public static string TimingBoolean => Get("editor.timingBoolean", "Boolean");
            public static string TimingQuantize => Get("editor.timingQuantize", "Quantize");
            public static string TimingQuest => Get("editor.timingQuest", "Quest");
            public static string TimingGrid => Get("editor.timingGrid", "Grid");
            public static string TimingBake => Get("editor.timingBake", "Bake");
            public static string TimingCodegen => Get("editor.timingCodegen", "Codegen");
            public static string TimingAsset => Get("editor.timingAsset", "Asset");
            public static string TimingTotal => Get("editor.timingTotal", "Total");
            public static string ParseIssuesHeader => Get("editor.parseIssuesHeader", "Parse Issues");
            public static string None => Get("editor.none", "None");
            public static string ResolveSourcePathFailed => Get("editor.resolveSourcePathFailed", "SDFX: Unable to resolve source asset path.");
            public static string ResolveRasterSourceFailed => Get("editor.resolveRasterSourceFailed", "SDFX: Provide a raster source asset or raster file path.");
            public static string InvalidSourceAssetHelp => Get("editor.invalidSourceAssetHelp", "Assign an SVG asset or a TextAsset for custom format sources.");

            public static string ParseIssueSummary(string severity, string code, int lineNumber, string elementName)
            {
                var format = Get("editor.parseIssueSummary", "{0} {1} line {2} [{3}]");
                return string.Format(format, severity, code, lineNumber, elementName);
            }

            public static string CompileFailed(string message)
            {
                var format = Get("editor.compileFailed", "SDFX compile failed: {0}");
                return string.Format(format, message);
            }

            public static string CompileComplete(string materialPath)
            {
                var format = Get("editor.compileComplete", "SDFX compile complete. Material: {0}");
                return string.Format(format, materialPath);
            }

            public static string OkButton => Get("editor.okButton", "OK");
        }

        public static class Compiler
        {
            public static string OptionsNull => Get("compiler.optionsNull", "Options are null.");
            public static string SourcePathEmpty => Get("compiler.sourcePathEmpty", "Source path is empty.");
            public static string SourceFileNotFound => Get("compiler.sourceFileNotFound", "Source file not found.");
            public static string ParseErrorsPreventedCompilation => Get("compiler.parseErrorsPreventedCompilation", "Parse errors prevented compilation.");
            public static string CompileSucceeded => Get("compiler.compileSucceeded", "Compile succeeded.");
            public static string ProjectRootResolveFailed => Get("compiler.projectRootResolveFailed", "Unable to resolve Unity project root.");
            public static string OutputMustBeInsideAssets => Get("compiler.outputMustBeInsideAssets", "Output must be located under Assets for AssetDatabase import.");
            public static string OutputDirectoryMustBeInsideAssets => Get("compiler.outputDirectoryMustBeInsideAssets", "Output directory must be inside Assets.");

            public static string CompileSkipped(string message)
            {
                var format = Get("compiler.compileSkipped", "SDFX compile skipped: {0}");
                return string.Format(format, message);
            }

            public static string ParseIssue(string level, string code, string elementName, int lineNumber, string message)
            {
                var format = Get("compiler.parseIssue", "SDFX Parse {0}: [{1}] [{2}] line {3} - {4}");
                return string.Format(format, level, code, elementName, lineNumber, message);
            }

            public static string QuestVariantStage(long elapsedMs, int primitiveCount)
            {
                var format = Get("compiler.questVariantStage", "SDFX Stage QuestVariant: {0} ms, primitives={1}");
                return string.Format(format, elapsedMs, primitiveCount);
            }

            public static string GridClippingWarning(int droppedReferences, int maxPerCell)
            {
                var format = Get("compiler.gridClippingWarning", "SDFX grid clipping dropped {0} primitive references at max-per-cell={1}.");
                return string.Format(format, droppedReferences, maxPerCell);
            }

            public static string GridCapacityRaised(int previousMax, int newMax, int droppedReferences)
            {
                var format = Get(
                    "compiler.gridCapacityRaised",
                    "SDFX raised max-primitives-per-cell from {0} to {1} after {2} grid references would have been clipped.");
                return string.Format(format, previousMax, newMax, droppedReferences);
            }

            public static string HighPathEdgeCountWarning(int totalPathEdges)
            {
                var format = Get(
                    "compiler.highPathEdgeCountWarning",
                    "SDFX: compiled source contains {0} raw path edges across all primitives.");
                return string.Format(format, totalPathEdges);
            }

            public static string SamplerBudgetExceeded(int samplerCount, int budget)
            {
                var format = Get("compiler.samplerBudgetExceeded", "SDFX Quest sampler budget exceeded: {0}/{1} extra samplers from enabled modules.");
                return string.Format(format, samplerCount, budget);
            }

            public static string ModuleConflict(string message)
            {
                var format = Get("compiler.moduleConflict", "SDFX module conflict: {0}");
                return string.Format(format, message);
            }

            public static string ModuleConflictsSummary(int count, string examples)
            {
                var format = Get(
                    "compiler.moduleConflictsSummary",
                    "SDFX: {0} module conflicts (lighting models overlap). Examples: {1}. Enable one lighting model, or use Avatar/Toon/PBR instead of All Modules.");
                return string.Format(format, count, examples);
            }

            public static string LargeModuleShaderWarning(int moduleCount)
            {
                var format = Get(
                    "compiler.largeModuleShaderWarning",
                    "SDFX is compiling {0} shader modules into one generated shader. Use the Avatar or Toon preset for faster imports; All Modules creates a very large shader.");
                return string.Format(format, moduleCount);
            }

            public static string GrabPassCompileWarning => Get(
                "compiler.grabPassCompileWarning",
                "SDFX compiled GrabPass into this shader. ShaderLab passes ignore material keywords. Every material on this shader pays for a full-screen grab. Recompile without grabpass unless you need it.");

            public static string AssetModuleDuplicate(string id)
                => Get("compiler.assetModuleDuplicate", "SDFX skipped asset module '{0}' because that id is already registered.")
                    .Replace("{0}", id);

            public static string AssetModuleRegisterFailed(string id, string message)
                => Get("compiler.assetModuleRegisterFailed", "SDFX failed to register asset module '{0}': {1}")
                    .Replace("{0}", id)
                    .Replace("{1}", message);

            public static string AssetModulePropertySkipped(string moduleId, string propertyName, string message)
                => Get("compiler.assetModulePropertySkipped", "SDFX module '{0}' skipped property '{1}': {2}")
                    .Replace("{0}", moduleId)
                    .Replace("{1}", propertyName)
                    .Replace("{2}", message);

            public static string RasterCompileRemoved => Get(
                "compiler.rasterCompileRemoved",
                "Raster compile was removed. Use SDFX/Rasterizer to convert to SVG, then compile the SVG.");
            public static string CompiledAssetMissing => Get("compiler.compiledAssetMissing", "Compiled asset is missing.");
            public static string CompiledAssetNoSourcePath => Get("compiler.compiledAssetNoSourcePath", "Compiled asset has no source path.");
            public static string MaterialMissing => Get("compiler.materialMissing", "Material is missing.");
            public static string MaterialNoShader => Get("compiler.materialNoShader", "Material has no shader.");
            public static string ProjectRootResolveFailedShort => Get("compiler.projectRootResolveFailedShort", "Could not resolve project root.");

            public static string ModuleConflictsWith(string leftName, string rightName)
                => string.Format(Get("compiler.moduleConflictsWith", "{0} conflicts with {1}."), leftName, rightName);

            public static string ModuleAlreadyRegistered(string id)
                => string.Format(Get("compiler.moduleAlreadyRegistered", "A shader module with id '{0}' is already registered."), id);

            public static string PrimitiveTextureDropped(int width, int height, int maxPrimitives, int dropped)
                => string.Format(
                    Get(
                        "compiler.primitiveTextureDropped",
                        "SDFX: Primitive texture {0}x{1} fits {2} primitives; {3} were dropped. Use the auto-sized bake overload."),
                    width,
                    height,
                    maxPrimitives,
                    dropped);

            public static string SnippetNotFound(string fullPath)
                => string.Format(Get("compiler.snippetNotFound", "SDFX snippet not found: {0}"), fullPath);

            public static string ModuleSnippetNotFound(string fullPath)
                => string.Format(Get("compiler.moduleSnippetNotFound", "SDFX module snippet not found: {0}"), fullPath);

            public static string GeneratedShaderNotFound => Get("compiler.generatedShaderNotFound", "Generated shader not found in project");
            public static string ModulePropertyNameRequired => Get("compiler.modulePropertyNameRequired", "Module property Name is required.");
            public static string EnumPropertyRequiresLabels => Get("compiler.enumPropertyRequiresLabels", "Enum property requires at least one label.");

            public static string ShaderImportFailed(string shaderPath)
            {
                var format = Get("compiler.shaderImportFailed", "SDFX shader import failed: {0}");
                return string.Format(format, shaderPath);
            }

            public static string ShaderCompileFailed(string shaderPath, int line, string message)
            {
                var format = Get(
                    "compiler.shaderCompileFailed",
                    "SDFX shader compile failed in {0} at line {1}: {2}");
                return string.Format(format, shaderPath, line, message);
            }

            public static string CompileSummary(
                string sourcePath,
                string profile,
                int parsed,
                int simplified,
                int resolved,
                int quantized,
                int finalCount,
                int warnings,
                int droppedGridRefs,
                long parseMs,
                long simplifyMs,
                long booleanMs,
                long quantizeMs,
                long gridMs,
                long bakeMs,
                long codegenMs,
                long assetMs,
                long totalMs)
            {
                var format = Get(
                    "compiler.compileSummary",
                    "SDFX Compile Summary: source={0}, profile={1}, parsed={2}, simplified={3}, resolved={4}, quantized={5}, final={6}, warnings={7}, droppedGridRefs={8}, parseMs={9}, simplifyMs={10}, booleanMs={11}, quantizeMs={12}, gridMs={13}, bakeMs={14}, codegenMs={15}, assetMs={16}, totalMs={17}");
                return string.Format(
                    format,
                    sourcePath,
                    profile,
                    parsed,
                    simplified,
                    resolved,
                    quantized,
                    finalCount,
                    warnings,
                    droppedGridRefs,
                    parseMs,
                    simplifyMs,
                    booleanMs,
                    quantizeMs,
                    gridMs,
                    bakeMs,
                    codegenMs,
                    assetMs,
                    totalMs);
            }
        }

        public static class ShaderGui
        {
            public static string BannerTitle => Get("shadergui.bannerTitle", "SDFX Vector Texture");
            public static string BaseHeader => Get("shadergui.baseHeader", "Base");
            public static string ModulesHeader => Get("shadergui.modulesHeader", "Effect Modules");
            public static string BakedDataHeader => Get("shadergui.bakedDataHeader", "Baked Vector Data");
            public static string BakedDataHelp => Get("shadergui.bakedDataHelp", "These textures are generated by the SDFX compiler. Replacing them by hand will break rendering; recompile the source asset instead.");
            public static string MetricsHeader => Get("shadergui.metricsHeader", "Primitive Metrics");
            public static string MetricsHelp => Get(
                "shadergui.metricsHelp",
                "Profiles each primitive with Unity Profiler.");
            public static string MetricsProfileButton => Get("shadergui.metricsProfileButton", "Profile Per-Primitive");
            public static string MetricsProfileWithGpuButton => Get("shadergui.metricsProfileWithGpuButton", "Benchmark + Material GPU");
            public static string MetricsCopyButton => Get("shadergui.metricsCopyButton", "Copy to Clipboard");
            public static string MetricsCopied => Get("shadergui.metricsCopied", "Copied primitive metrics to clipboard.");
            public static string MetricsNoCompiled => Get("shadergui.metricsNoCompiled", "Assign/find a CompiledVectorTextureAsset to profile primitives.");
            public static string MetricsEmpty => Get("shadergui.metricsEmpty", "Run a profile to fill this table.");
            public static string MetricsColIndex => Get("shadergui.metricsColIndex", "#");
            public static string MetricsColType => Get("shadergui.metricsColType", "Type");
            public static string MetricsColEdges => Get("shadergui.metricsColEdges", "Edges");
            public static string MetricsColCpu => Get("shadergui.metricsColCpu", "CPU µs");
            public static string MetricsColShare => Get("shadergui.metricsColShare", "Share");
            public static string MetricsMaterialGpu(double ms)
                => string.Format(Get("shadergui.metricsMaterialGpu", "Material GPU fence avg: {0:0.000} ms / frame (128x128)"), ms);
            public static string MetricsMaterialGpuRange(double minMs, double maxMs)
                => string.Format(Get("shadergui.metricsMaterialGpuRange", "Material GPU range: {0:0.000} - {1:0.000} ms"), minMs, maxMs);
            public static string MetricsFrameTiming(double cpuMs, double gpuMs)
                => string.Format(Get("shadergui.metricsFrameTiming", "FrameTimingManager avg CPU {0:0.000} ms | GPU {1:0.000} ms"), cpuMs, gpuMs);
            public static string MetricsTotalCpu(double ms, int count)
                => string.Format(Get("shadergui.metricsTotalCpu", "CPU profile total: {0:0.000} ms across {1} primitives (sorted by cost)"), ms, count);
            public static string MetricsBenchmarkPasses(int passes)
                => string.Format(Get("shadergui.metricsBenchmarkPasses", "Benchmark average over {0} timed pass(es)"), passes);
            public static string MetricsCapturedAt(string utc)
                => string.Format(Get("shadergui.metricsCapturedAt", "Captured {0} UTC"), utc);
            public static string MetricsShowingTop(int shown, int total)
                => string.Format(Get("shadergui.metricsShowingTop", "Showing top {0} of {1}"), shown, total);
            public static string DebugHeader => Get("shadergui.debugHeader", "Debug");
            public static string AdvancedHeader => Get("shadergui.advancedHeader", "Advanced");
            public static string CategoryLighting => Get("shadergui.categoryLighting", "Lighting");
            public static string CategorySurface => Get("shadergui.categorySurface", "Surface");
            public static string CategorySdfEffects => Get("shadergui.categorySdfEffects", "SDF Effects");
            public static string CategoryColorGrading => Get("shadergui.categoryColorGrading", "Color Grading");
            public static string CategoryAnimation => Get("shadergui.categoryAnimation", "Animation");
            public static string CategoryUv => Get("shadergui.categoryUv", "UV");
            public static string CategoryMaterials => Get("shadergui.categoryMaterials", "Materials");
            public static string CategoryStylized => Get("shadergui.categoryStylized", "Stylized");
            public static string CategoryWorld => Get("shadergui.categoryWorld", "World");
            public static string CategoryParticles => Get("shadergui.categoryParticles", "Particles");
            public static string CategoryGeometry => Get("shadergui.categoryGeometry", "Geometry");
            public static string CategoryVrChat => Get("shadergui.categoryVrChat", "VRChat");
            public static string CategoryAdvanced => Get("shadergui.categoryAdvanced", "Advanced");
            public static string CorePipelineHeader => Get("shadergui.corePipelineHeader", "Core Pipeline");
            public static string LookPresetField => Get("shadergui.lookPresetField", "Material Look Preset");
            public static string LookPresetNone => Get("shadergui.lookPresetNone", "(None)");
            public static string LookPresetApply => Get("shadergui.lookPresetApply", "Apply Look");
            public static string LookPresetSaveAs => Get("shadergui.lookPresetSaveAs", "Save Look As...");
            public static string LookPresetSaveTitle => Get("shadergui.lookPresetSaveTitle", "Save Material Look Preset");
            public static string LookPresetHeader => Get("shadergui.lookPresetHeader", "Look Presets");
            public static string LookPresetAssetField => Get("shadergui.lookPresetAssetField", "Preset Asset");
            public static string LookPresetCapture => Get("shadergui.lookPresetCapture", "Capture to Field");
            public static string LookPresetHint => Get("shadergui.lookPresetHint", "Choose a built-in preset, assign a preset asset, or capture the current material.");
            public static string SearchPlaceholder => Get("shadergui.searchPlaceholder", "Search properties...");
            public static string SearchNoResults => Get("shadergui.searchNoResults", "No properties match the current search.");
            public static string SearchClear => Get("shadergui.searchClear", "Clear");
            public static string SubRendering => Get("shadergui.subRendering", "Rendering & Transparency");
            public static string SubColorGrading => Get("shadergui.subColorGrading", "Color Grading");
            public static string SubVertexColor => Get("shadergui.subVertexColor", "Vertex Colors");
            public static string SubStencil => Get("shadergui.subStencil", "Stencil");
            public static string StencilHelp => Get("shadergui.stencilHelp", "Stencil is optional. Leave defaults unless you need masking, portals, or see-through effects.");
            public static string ModulesEnableAll => Get("shadergui.modulesEnableAll", "Enable All");
            public static string ModulesDisableAll => Get("shadergui.modulesDisableAll", "Disable All");
            public static string ModulesExpandAll => Get("shadergui.modulesExpandAll", "Expand All");
            public static string ModulesCollapseAll => Get("shadergui.modulesCollapseAll", "Collapse All");
            public static string ModuleSolo => Get("shadergui.moduleSolo", "Solo");
            public static string ModuleReset => Get("shadergui.moduleReset", "Reset");
            public static string ModuleModeLabel => Get("shadergui.moduleModeLabel", "Mode");
            public static string OpenCompilerButton => Get("shadergui.openCompilerButton", "Open Compiler");
            public static string RecompileButton => Get("shadergui.recompileButton", "Recompile");
            public static string OptimizeButton => Get("shadergui.optimizeButton", "Optimize");
            public static string RecompileShaderButton => Get("shadergui.recompileShaderButton", "Re-compile");
            public static string OptimizeConfirmTitle => Get("shadergui.optimizeConfirmTitle", "Optimize SDFX Shader");
            public static string OptimizeConfirmMessage(int enabledCount, int compiledCount)
            {
                var format = Get(
                    "shadergui.optimizeConfirmMessage",
                    "Regenerate this shader with only the {0} currently enabled modules ({1} compiled-in modules will shrink). Baked vector data is kept.");
                return string.Format(format, enabledCount, compiledCount);
            }

            public static string RecompileShaderTitle => Get("shadergui.recompileShaderTitle", "Re-compile SDFX Shader");
            public static string RecompileShaderHelp => Get(
                "shadergui.recompileShaderHelp",
                "Choose which modules to bake into the shader. Data textures are not rebuilt. Add modules here, then enable them on the material.");
            public static string RecompileShaderConfirm => Get("shadergui.recompileShaderConfirm", "Re-compile Shader");
            public static string CancelButton => Get("shadergui.cancelButton", "Cancel");
            public static string ModulesSelectEnabled => Get("shadergui.modulesSelectEnabled", "Enabled");
            public static string ModuleNotInShaderSuffix => Get("shadergui.moduleNotInShaderSuffix", "(new)");
            public static string NoCompiledAsset => Get(
                "shadergui.noCompiledAsset",
                "No CompiledVectorTextureAsset found for this material. Compile from the SDFX window first.");
            public static string OptimizeNeedsModules => Get(
                "shadergui.optimizeNeedsModules",
                "Enable at least one module on the material before optimizing.");
            public static string RecompileNeedsModules => Get(
                "shadergui.recompileNeedsModules",
                "Select at least one module to include in the shader.");
            public static string ShaderNotGenerated => Get(
                "shadergui.shaderNotGenerated",
                "This material is not using a generated SDFX shader asset on disk.");
            public static string MissingBakedTextures => Get(
                "shadergui.missingBakedTextures",
                "Baked SDFX data textures are missing. Full recompile from source is required.");
            public static string ShaderActionsHeader => Get("shadergui.shaderActionsHeader", "Shader Actions");
            public static string ShaderActionsHelp => Get(
                "shadergui.shaderActionsHelp",
                "Optimize strips unused modules. Re-compile regenerates the shader only so you can add or change modules without rebaking data.");

            public static string ModulesSelectedCount(int selected, int total)
            {
                var format = Get("shadergui.modulesSelectedCount", "Selected: {0}/{1}");
                return string.Format(format, selected, total);
            }

            public static string ModulePickerConflicts(int count)
            {
                var format = Get("shadergui.modulePickerConflicts", "{0} module conflicts in the selection (usually overlapping lighting models).");
                return string.Format(format, count);
            }

            public static string ShaderRegenerated(int moduleCount)
            {
                var format = Get("shadergui.shaderRegenerated", "SDFX regenerated shader with {0} modules (data textures unchanged).");
                return string.Format(format, moduleCount);
            }

            public static string CompiledAssetField => Get("shadergui.compiledAssetField", "Compiled Asset");
            public static string SelectSourceButton => Get("shadergui.selectSourceButton", "Ping");
            public static string SourcePathLabel => Get("shadergui.sourcePathLabel", "Source");
            public static string StatusUnknown => Get("shadergui.statusUnknown", "Unknown");

            public static string StatusModules(int enabled, int total)
            {
                var format = Get("shadergui.statusModules", "Modules: {0}/{1}");
                return string.Format(format, enabled, total);
            }

            public static string StatusBlend(string blend)
            {
                var format = Get("shadergui.statusBlend", "Blend: {0}");
                return string.Format(format, blend);
            }

            public static string StatusQueue(int queue)
            {
                var format = Get("shadergui.statusQueue", "Queue: {0}");
                return string.Format(format, queue);
            }

            public static string CategoryWithCount(string label, int enabled, int total)
            {
                var format = Get("shadergui.categoryWithCount", "{0} ({1}/{2})");
                return string.Format(format, label, enabled, total);
            }

            public static string BlendModeInfo(BlendModePreset preset, int src, int dst, int zWrite, int queue)
            {
                var format = Get(
                    "shadergui.blendModeInfo",
                    "{0}: Src={1} Dst={2} ZWrite={3} · Render queue {4}");
                return string.Format(format, preset, src, dst, zWrite, queue);
            }

            public static string BlendMismatch(string presetName, BlendModePreset hint, BlendModePreset current)
            {
                var format = Get(
                    "shadergui.blendMismatch",
                    "Preset \"{0}\" expects {1} blend; material is currently {2}. Apply will update blend state.");
                return string.Format(format, presetName, hint, current);
            }

            public static string ResetModuleUndo(string moduleName)
            {
                var format = Get("shadergui.resetModuleUndo", "Reset {0}");
                return string.Format(format, moduleName);
            }

            public static string SoloModuleUndo(string moduleName)
            {
                var format = Get("shadergui.soloModuleUndo", "Solo {0}");
                return string.Format(format, moduleName);
            }

            public static string ModePopupTooltip => Get("shadergui.modePopupTooltip", "Choose how this module behaves.");
            public static string RangeTooltip(float min, float max)
            {
                var format = Get("shadergui.rangeTooltip", "Range {0} - {1}");
                return string.Format(format, min, max);
            }

            public static string ModeFallbackDescription(string modeLabel)
            {
                var format = Get("shadergui.modeFallbackDescription", "{0} mode.");
                return string.Format(format, modeLabel);
            }

            public static string OkButton => Get("shadergui.okButton", "OK");
        }

        public static class ModuleDefinition
        {
            public const string CreateMenuPath = "SDFX/Shader Module Definition";

            public static string InspectorHelp => Get(
                "moduledef.inspectorHelp",
                "Assign HLSL hook assets (.hlsl / .txt / TextAsset). Fragment locals: uv, col, art, sdfDist, worldNormal, viewDir, i. This asset registers on import — enable it under Shader Modules, then recompile.");
            public static string ReloadButton => Get("moduledef.reloadButton", "Reload Shader Modules");
            public static string SectionIdentity => Get("moduledef.sectionIdentity", "Identity");
            public static string SectionHooks => Get("moduledef.sectionHooks", "HLSL Hooks");
            public static string SectionModes => Get("moduledef.sectionModes", "Composite Modes");
            public static string SectionProperties => Get("moduledef.sectionProperties", "Properties");
            public static string Id => Get("moduledef.id", "Id");
            public static string IdTooltip => Get("moduledef.idTooltip", "Stable id used in compile options and shader keywords (letters, digits, underscore).");
            public static string DisplayName => Get("moduledef.displayName", "Display Name");
            public static string Description => Get("moduledef.description", "Description");
            public static string Category => Get("moduledef.category", "Category");
            public static string Order => Get("moduledef.order", "Order");
            public static string OrderTooltip => Get("moduledef.orderTooltip", "Hook execution order. Lower runs first. Prefer 800+ for third-party modules.");
            public static string LodTier => Get("moduledef.lodTier", "LOD Tier");
            public static string LodTierTooltip => Get("moduledef.lodTierTooltip", "Strip this module when Module LOD Tier is below this value (0 = always).");
            public static string ConflictIds => Get("moduledef.conflictIds", "Conflict Ids");
            public static string ExtraSamplerOverride => Get("moduledef.extraSamplerOverride", "Extra Sampler Override");
            public static string ExtraSamplerTooltip => Get(
                "moduledef.extraSamplerTooltip",
                "Override sampler budget. When less than 0, texture properties are counted automatically.");
            public static string FunctionsSnippet => Get("moduledef.functionsSnippet", "Functions");
            public static string VertexSnippet => Get("moduledef.vertexSnippet", "Vertex");
            public static string UvSnippet => Get("moduledef.uvSnippet", "UV");
            public static string FragmentSnippet => Get("moduledef.fragmentSnippet", "Fragment");
            public static string ExtraPassesSnippet => Get("moduledef.extraPassesSnippet", "Extra Passes");
            public static string ModePropertyName => Get("moduledef.modePropertyName", "Mode Property Name");
            public static string ModePropertyTooltip => Get(
                "moduledef.modePropertyTooltip",
                "When set, Fragment is emitted as mode branches using Fragment Mode Snippets.");
            public static string ModeLabels => Get("moduledef.modeLabels", "Mode Labels");
            public static string FragmentModeSnippets => Get("moduledef.fragmentModeSnippets", "Fragment Mode Snippets");
            public static string PropName => Get("moduledef.propName", "Name");
            public static string PropDisplayName => Get("moduledef.propDisplayName", "Display Name");
            public static string PropKind => Get("moduledef.propKind", "Kind");
            public static string PropDefaultFloat => Get("moduledef.propDefaultFloat", "Default Float");
            public static string PropDefaultColor => Get("moduledef.propDefaultColor", "Default Color");
            public static string PropDefaultVector => Get("moduledef.propDefaultVector", "Default Vector");
            public static string PropDefaultTexture => Get("moduledef.propDefaultTexture", "Default Texture");
            public static string PropRangeMin => Get("moduledef.propRangeMin", "Range Min");
            public static string PropRangeMax => Get("moduledef.propRangeMax", "Range Max");
            public static string PropEnumLabels => Get("moduledef.propEnumLabels", "Enum Labels");
            public static string PropEnumDescriptions => Get("moduledef.propEnumDescriptions", "Enum Descriptions");
            public static string PropAttributes => Get("moduledef.propAttributes", "Attributes");
            public static string PropSignalInput => Get("moduledef.propSignalInput", "Signal Input");
            public static string DefaultDisplayName => Get("moduledef.defaultDisplayName", "Custom Module");
            public static string DefaultDescription => Get("moduledef.defaultDescription", "Custom SDFX shader module.");
            public static string DefaultId => Get("moduledef.defaultId", "custommodule");
        }

        public static class Postprocessor
        {
            public static string AutoCompilePrefix => Get("post.autoCompilePrefix", "SDFX_AUTO_");

            public static string AutoCompileFailed(string assetPath, string message)
            {
                var format = Get("post.autoCompileFailed", "SDFX auto-compile failed for {0}: {1}");
                return string.Format(format, assetPath, message);
            }
        }

        public static class Modules
        {
            public static string ModePropertyDisplayName => Get("module.modePropertyDisplayName", "Mode");

            public static string EnableToggle(string displayName)
                => string.Format(Get("module.enableToggle", "Enable {0}"), displayName);

            public static string DisplayName(string id, string fallback)
                => Get($"module.{id}.displayName", fallback);

            public static string Description(string id, string fallback)
                => Get($"module.{id}.description", fallback);

            public static string Mode(string id, int index, string fallback)
                => Get($"module.{id}.mode.{index}", fallback);

            public static string ModeDescription(string id, int index, string fallback)
                => Get($"module.{id}.mode.{index}.description", fallback);

            public static string Prop(string id, string propName, string fallback)
                => Get($"module.{id}.prop.{propName}", fallback);

            public static string PropEnum(string id, string propName, int index, string fallback)
                => Get($"module.{id}.prop.{propName}.{index}", fallback);

            public static string PropEnumDescription(string id, string propName, int index, string fallback)
                => Get($"module.{id}.prop.{propName}.{index}.description", fallback);

            public static string Preset(string id, string fallback)
                => Get($"preset.{id}", fallback);

            public static string LookPreset(string id, string fallback)
                => Get($"lookPreset.{id}", fallback);
        }

        public static class ShaderProps
        {
            public static string Prop(string propName, string fallback)
                => Get($"shader.prop.{propName}", fallback);

            public static string EnumLabel(string propName, int index, string fallback)
                => Get($"shader.prop.{propName}.{index}", fallback);

            public static string EnumDescription(string propName, int index, string fallback)
                => Get($"shader.prop.{propName}.{index}.description", fallback);
        }

        public static class Rasterizer
        {
            public static string WindowTitle => Get("raster.windowTitle", "SDFX Rasterizer");
            public static string HelpBox => Get("raster.helpBox", "Convert a raster texture to SVG, then compile it in Tools → SDFX → Vector Texture Compiler.");
            public static string SourceTextureField => Get("raster.sourceTextureField", "Source Texture");
            public static string OutputFolderField => Get("raster.outputFolderField", "Output Folder");
            public static string BrowseButton => Get("raster.browseButton", "Browse");
            public static string DefaultOutputFolder(string folder) => string.Format(Get("raster.defaultOutputFolder", "Default: same folder as source ({0})"), folder);
            public static string WriteFlatPreviewField => Get("raster.writeFlatPreviewField", "Write Flat Preview PNG");
            public static string FlatPreviewBackgroundField => Get("raster.flatPreviewBackgroundField", "Flat Preview Background");
            public static string VectorizationSettingsHeader => Get("raster.vectorizationSettingsHeader", "Vectorization Settings");
            public static string AlgorithmField => Get("raster.algorithmField", "Algorithm");
            public static string CommonSettingsLabel => Get("raster.commonSettingsLabel", "Common");
            public static string UseComputeAccelerationField => Get("raster.useComputeAccelerationField", "Compute Acceleration");
            public static string MinAlphaField => Get("raster.minAlphaField", "Min Alpha");
            public static string MaxPathsField => Get("raster.maxPathsField", "Max Paths");
            public static string EdgeThresholdField => Get("raster.edgeThresholdField", "Edge Threshold");
            public static string SampleStrideField => Get("raster.sampleStrideField", "Sample Stride");
            public static string UseTilingField => Get("raster.useTilingField", "Use Tiling");
            public static string TileSizeField => Get("raster.tileSizeField", "Tile Size");
            public static string TileOverlapField => Get("raster.tileOverlapField", "Tile Overlap");
            public static string AutoTileMinDimensionField => Get("raster.autoTileMinDimensionField", "Auto Tile Min Dimension");
            public static string ConvertToSvgButton => Get("raster.convertToSvgButton", "Convert To SVG");
            public static string PingSvgButton => Get("raster.pingSvgButton", "Ping SVG");
            public static string PreviewLabel => Get("raster.previewLabel", "Preview");
            public static string StatusSelectSource => Get("raster.statusSelectSource", "Select a source texture.");
            public static string DefaultSourceName => Get("raster.defaultSourceName", "Raster");
            public static string StatusOutputFolderFailed => Get("raster.statusOutputFolderFailed", "Could not resolve output folder.");
            public static string StatusConversionFailed => Get("raster.statusConversionFailed", "Conversion failed. Check the Console for details.");
            public static string StatusConversionSuccess(string svgPath, int pathCount)
                => string.Format(Get("raster.statusConversionSuccess", "Wrote {0} ({1} paths). Compile this SVG in Vector Texture Compiler."), svgPath, pathCount);
            public static string DefaultGeneratedOutputPath => Get("raster.defaultGeneratedOutputPath", "Assets/Generated/SDFX");
            public static string OutputFolderDialogTitle => Get("raster.outputFolderDialogTitle", "SDFX Rasterizer Output");
            public static string OutputOutsideProject => Get("raster.outputOutsideProject", "Output folder must be inside the Unity project Assets folder.");
            public static string OkButton => Get("raster.okButton", "OK");
            public static string SelectTexturesFirst => Get("raster.selectTexturesFirst", "Select one or more Texture2D assets first.");
            public static string ProgressAnalyzing(string textureName, int index, int total)
                => string.Format(Get("raster.progressAnalyzing", "Analyzing {0} ({1}/{2})…"), textureName, index, total);
            public static string ProgressConverting(string textureName, string algorithmName, int index, int total)
                => string.Format(Get("raster.progressConverting", "Converting {0} with {1} ({2}/{3})…"), textureName, algorithmName, index, total);
            public static string AutoConvertSuccessLog(string assetPath, string svgPath, int pathCount, string algorithmName, string reason)
                => string.Format(
                    Get(
                        "raster.autoConvertSuccessLog",
                        "SDFX Rasterizer auto-converted '{0}' → '{1}' ({2} paths) using {3}.\nWhy: {4}"),
                    assetPath,
                    svgPath,
                    pathCount,
                    algorithmName,
                    reason);
            public static string AutoConvertFailedLog(string assetPath, string algorithmName, string reason)
                => string.Format(
                    Get(
                        "raster.autoConvertFailedLog",
                        "SDFX Rasterizer failed to auto-convert '{0}' using {1}. Why chosen: {2}"),
                    assetPath,
                    algorithmName,
                    reason);
            public static string BatchResultSummary(int converted, int failed)
                => string.Format(Get("raster.batchResultSummary", "Converted {0} texture(s), {1} failed. Check the Console for details."), converted, failed);
            public static string BestForLabel(string bestFor) => string.Format(Get("raster.bestForLabel", "Best for: {0}"), bestFor);
            public static string SentisUnavailableNeural => Get("raster.sentisUnavailableNeural", "Unity Sentis is not available. Neural vectorization requires the Sentis package.");
            public static string SentisUnavailableHybrid => Get("raster.sentisUnavailableHybrid", "Unity Sentis is not available. Hybrid neural vectorization requires the Sentis package.");
            public static string AssignNeuralModelHelp => Get("raster.assignNeuralModelHelp", "Assign a Sentis ModelAsset for neural vectorization.");
            public static string AssignSegmentationModelHelp => Get("raster.assignSegmentationModelHelp", "Assign a Sentis segmentation ModelAsset.");
            public static string ModelLoadFailedWarning => Get("raster.modelLoadFailedWarning", "Model asset path is set but the asset could not be loaded. Reassign a Sentis ModelAsset.");
            public static string ColorCountField => Get("raster.colorCountField", "Color Count");
            public static string QuantMethodField => Get("raster.quantMethodField", "Quant Method");
            public static string SimplifyToleranceField => Get("raster.simplifyToleranceField", "Simplify Tolerance");
            public static string MinRegionAreaField => Get("raster.minRegionAreaField", "Min Region Area");
            public static string ThresholdModeField => Get("raster.thresholdModeField", "Threshold Mode");
            public static string TraceHolesField => Get("raster.traceHolesField", "Trace Holes");
            public static string TurdSizeField => Get("raster.turdSizeField", "Turd Size");
            public static string AlphaMaxField => Get("raster.alphaMaxField", "Alpha Max");
            public static string OptToleranceField => Get("raster.optToleranceField", "Opt Tolerance");
            public static string BezierMaxErrorField => Get("raster.bezierMaxErrorField", "Bezier Max Error");
            public static string CornerAngleField => Get("raster.cornerAngleField", "Corner Angle");
            public static string MinSegmentLengthField => Get("raster.minSegmentLengthField", "Min Segment Length");
            public static string NeuralModelField => Get("raster.neuralModelField", "Neural Model");
            public static string ConfidenceThresholdField => Get("raster.confidenceThresholdField", "Confidence Threshold");
            public static string MaxCurvesField => Get("raster.maxCurvesField", "Max Curves");
            public static string SuperpixelCountField => Get("raster.superpixelCountField", "Superpixel Count");
            public static string CompactnessField => Get("raster.compactnessField", "Compactness");
            public static string MergeThresholdField => Get("raster.mergeThresholdField", "Merge Threshold");
            public static string SampleDensityField => Get("raster.sampleDensityField", "Sample Density");
            public static string MaxCellsField => Get("raster.maxCellsField", "Max Cells");
            public static string OutputModeField => Get("raster.outputModeField", "Output Mode");
            public static string SegmentationModelField => Get("raster.segmentationModelField", "Segmentation Model");
            public static string PerRegionAlgorithmField => Get("raster.perRegionAlgorithmField", "Per-Region Algorithm");
            public static string BaseAlgorithmField => Get("raster.baseAlgorithmField", "Base Algorithm");
            public static string ProgressWarmupShaders => Get("raster.progressWarmupShaders", "Warming up raster compute shaders");
            public static string ComputeWarmupFailed(string message)
                => string.Format(Get("raster.computeWarmupFailed", "SDFX raster compute warmup failed: {0}"), message);
            public static string InferenceUsingFallback => Get("raster.inferenceUsingFallback", "Using fallback segmentation.");
            public static string InferenceModelLoadFailed => Get("raster.inferenceModelLoadFailed", "Model load failed.");
            public static string InferenceModelLoadFailedDetail(string reason)
                => string.Format(Get("raster.inferenceModelLoadFailedDetail", "Model load failed: {0}"), reason);
            public static string InferenceLogWarning(string message)
                => string.Format(Get("raster.inferenceLogWarning", "SDFX raster inference: {0}"), message);
            public static string InferenceModelActive => Get("raster.inferenceModelActive", "Model active.");
            public static string AutoReasonNoSourceTexture => Get("raster.autoReason.noSourceTexture", "No source texture; using the general-purpose default.");
            public static string AutoReasonPixelsUnreadable => Get("raster.autoReason.pixelsUnreadable", "Texture pixels could not be read; using the general-purpose default.");
            public static string AutoReasonAlphaSilhouette(float transparentFraction, float opaqueCoverage)
                => string.Format(
                    Get(
                        "raster.autoReason.alphaSilhouette",
                        "Alpha silhouette: {0} transparent and one color covers {1} of opaque pixels → Suzuki–Abe alpha contours."),
                    transparentFraction.ToString("P0", CultureInfo.InvariantCulture),
                    opaqueCoverage.ToString("P0", CultureInfo.InvariantCulture));
            public static string AutoReasonBlackWhiteArt(float binaryLumaFraction, float meanSaturation)
                => string.Format(
                    Get(
                        "raster.autoReason.blackWhiteArt",
                        "Black & white art: {0} of pixels near luma extremes, mean saturation {1} → Potrace binary tracing."),
                    binaryLumaFraction.ToString("P0", CultureInfo.InvariantCulture),
                    meanSaturation.ToString("F2", CultureInfo.InvariantCulture));
            public static string AutoReasonFlatPixelArt(int colorCount, float colorCoverage, int paletteSize)
                => string.Format(
                    Get(
                        "raster.autoReason.flatPixelArt",
                        "Flat pixel art: {0} colors cover {1} with hard edges → Color Quant + Marching Squares ({2} colors)."),
                    colorCount,
                    colorCoverage.ToString("P0", CultureInfo.InvariantCulture),
                    paletteSize);
            public static string AutoReasonGeneralImage(int dominantColorCount, float softEdgeFraction, int paletteSize, bool useBezierFit)
                => string.Format(
                    Get(
                        "raster.autoReason.generalImage",
                        "General {0} image: ~{1} dominant colors, {2} soft edges → Hybrid Contour ({3} colors, bezier {4})."),
                    dominantColorCount > 48 ? AutoKindContinuousTone : AutoKindFlatColor,
                    dominantColorCount,
                    softEdgeFraction.ToString("P0", CultureInfo.InvariantCulture),
                    paletteSize,
                    useBezierFit ? AutoBezierOn : AutoBezierOff);
            public static string AutoKindContinuousTone => Get("raster.autoKind.continuousTone", "continuous-tone");
            public static string AutoKindFlatColor => Get("raster.autoKind.flatColor", "flat-color");
            public static string AutoBezierOn => Get("raster.autoBezier.on", "on");
            public static string AutoBezierOff => Get("raster.autoBezier.off", "off");

            public static string AlgorithmName(string algorithmKey, string fallback)
                => Get($"raster.algorithm.{algorithmKey}.name", fallback);

            public static string AlgorithmDescription(string algorithmKey, string fallback)
                => Get($"raster.algorithm.{algorithmKey}.description", fallback);

            public static string AlgorithmBestFor(string algorithmKey, string fallback)
                => Get($"raster.algorithm.{algorithmKey}.bestFor", fallback);
        }

        public static class Parsing
        {
            public static string SvgElementName => Get("parse.svgElementName", "svg");
            public static string CustomElementName => Get("parse.customElementName", "custom");
            public static string CanvasElementName => Get("parse.canvasElementName", "canvas");
            public static string RasterElementName => Get("parse.rasterElementName", "raster");
            public static string SvgTextEmpty => Get("parse.svgTextEmpty", "SVG text is empty.");
            public static string RootIsNotSvg => Get("parse.rootIsNotSvg", "Root element is not <svg>.");
            public static string UnsupportedGradientReference => Get("parse.unsupportedGradientReference", "Gradient reference is currently unsupported.");
            public static string PathDetailReduced(int maxEdges) => string.Format(Get("parse.pathDetailReduced", "Path exceeded {0} edges after flattening and was decimated; fine detail may be lost."), maxEdges);
            public static string LimitedInlineStyle => Get("parse.limitedInlineStyle", "Inline style attribute support is limited; only direct fill/stroke is handled.");
            public static string UnsupportedDefinitionElement => Get("parse.unsupportedDefinitionElement", "Definition/masking/gradient element is unsupported.");
            public static string UnsupportedSvgElementIgnored => Get("parse.unsupportedSvgElementIgnored", "Unsupported SVG element was ignored.");
            public static string InvalidPolygonPoints => Get("parse.invalidPolygonPoints", "Polygon points attribute is invalid.");
            public static string CustomSourceTextEmpty => Get("parse.customSourceTextEmpty", "Custom source text is empty.");
            public static string InvalidCanvasDirective => Get("parse.invalidCanvasDirective", "Invalid @canvas directive. Expected '@canvas,width,height'.");
            public static string InvalidCustomRow => Get("parse.invalidCustomRow", "Invalid custom primitive row. Expected at least 6 comma-separated values.");
            public static string InvalidCustomBounds => Get("parse.invalidCustomBounds", "Failed to parse x,y,width,height fields.");
            public static string RasterInputMissing => Get("parse.rasterInputMissing", "Raster source is missing.");
            public static string RasterReadbackFailed => Get("parse.rasterReadbackFailed", "Failed to read raster pixel data for tracing.");
            public static string RasterModeFallbackToEdges => Get("parse.rasterModeFallbackToEdges", "Selected raster tracing mode is not yet implemented; using edge tracing.");
            public static string RasterComputeUnavailable => Get("parse.rasterComputeUnavailable", "Compute acceleration unavailable; falling back to CPU edge analysis.");
            public static string RasterPrimitiveCapReached => Get("parse.rasterPrimitiveCapReached", "Raster primitive cap reached; output was clipped.");
            public static string RasterNoEdgesDetected => Get("parse.rasterNoEdgesDetected", "No edges were detected in raster source with current settings.");
            public static string RasterInferenceUnavailable => Get("parse.rasterInferenceUnavailable", "Neural raster modes require Unity Sentis.");
            public static string RasterModelLoadFailed => Get("parse.rasterModelLoadFailed", "Failed to load Sentis model.");
            public static string RasterModelLoadFailedDetail(string modelPath, string reason)
                => string.Format(Get("parse.rasterModelLoadFailedDetail", "Sentis failed for '{0}': {1}. Using color-quant segmentation."), modelPath, reason);
            public static string RasterModelActive(string modelName)
                => string.Format(Get("parse.rasterModelActive", "Sentis model: {0}"), modelName);
            public static string RasterUsingFallbackSegmentation => Get("parse.rasterUsingFallbackSegmentation", "No model assigned; using color-quant segmentation.");
            public static string RasterProgressLoadingImage => Get("parse.rasterProgressLoadingImage", "Loading raster image...");
            public static string RasterProgressGpuUpload => Get("parse.rasterProgressGpuUpload", "Uploading image to GPU...");
            public static string RasterProgressGpuWarmup => Get("parse.rasterProgressGpuWarmup", "Warming up GPU...");
            public static string RasterProgressGpuEdgeDetect => Get("parse.rasterProgressGpuEdgeDetect", "GPU edge detection...");
            public static string RasterProgressGpuEdgeMask => Get("parse.rasterProgressGpuEdgeMask", "GPU edge mask...");
            public static string RasterProgressGpuColorQuant => Get("parse.rasterProgressGpuColorQuant", "GPU color quantization...");
            public static string RasterProgressGpuThresholdMask => Get("parse.rasterProgressGpuThresholdMask", "GPU threshold mask...");
            public static string RasterProgressGpuSuperpixel => Get("parse.rasterProgressGpuSuperpixel", "GPU superpixel segmentation...");
            public static string RasterProgressGpuVoronoi => Get("parse.rasterProgressGpuVoronoi", "GPU Voronoi field...");
            public static string RasterProgressGpuPreviewTint => Get("parse.rasterProgressGpuPreviewTint", "GPU preview tint...");
            public static string RasterProgressGpuDone => Get("parse.rasterProgressGpuDone", "GPU pass done.");
            public static string RasterProgressCpuVectorize => Get("parse.rasterProgressCpuVectorize", "CPU contour vectorization...");
            public static string RasterProgressDone => Get("parse.rasterProgressDone", "Raster preview complete.");

            public static string UnsupportedCustomPrimitiveType(string shape)
            {
                var format = Get("parse.unsupportedCustomPrimitiveType", "Unsupported custom primitive type: {0}");
                return string.Format(format, shape);
            }
        }
    }
}
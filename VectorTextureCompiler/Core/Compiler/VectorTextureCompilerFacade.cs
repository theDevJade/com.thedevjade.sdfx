using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stopwatch = System.Diagnostics.Stopwatch;
using SDFX.VectorTextureCompiler.Core.Baking;
using SDFX.VectorTextureCompiler.Core.CodeGen;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Modules;
using SDFX.VectorTextureCompiler.Core.Optimize;
using SDFX.VectorTextureCompiler.Core.Parsing;
using SDFX.VectorTextureCompiler.Core.Primitives;
using SDFX.VectorTextureCompiler.Editor;
using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Compiler
{
    public enum CompileSourceType
    {
        Auto = 0,
        Svg = 1,
        Custom = 2,
        [Obsolete("Raster compile was removed. Use SDFX/Rasterizer to convert to SVG, then compile the SVG.")]
        Raster = 3
    }

    public enum TransparencyMode
    {
        Auto = 0,
        ForceOpaque = 1,
        ForceTransparent = 2
    }

    public sealed class CompileOptions
    {
        public string SourcePath { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
        public bool BuildQuestVariant { get; set; } = true;
        public int GridWidth { get; set; } = 32;
        public int GridHeight { get; set; } = 32;
        public int MaxPrimitivesPerCell { get; set; } = 8;
        public ParserStrictness ParserStrictness { get; set; } = ParserStrictness.Strict;
        public CoordinateModel CoordinateModel { get; set; } = CoordinateModel.Hybrid;
        public OptimizationProfile OptimizationProfile { get; set; } = OptimizationProfile.Pc;
        public CompileSourceType SourceType { get; set; } = CompileSourceType.Auto;

        public List<string> EnabledModules { get; set; }

        public string ModulePresetId { get; set; }

        public int ModuleLodTier { get; set; }

        public Color BackgroundColor { get; set; } = Color.white;

        /// <summary>
        /// How the generated shader's render queue/blending is chosen. Auto uses the
        /// transparent path only when <see cref="BackgroundColor"/> is translucent
        /// translucent primitives alone do not require it, because layer compositing
        /// happens inside the shader.
        /// </summary>
        public TransparencyMode TransparencyMode { get; set; } = TransparencyMode.Auto;

        public BlendModePreset? BlendMode { get; set; }

        /// <summary>
        /// When true, the generated shader includes a ForwardAdd pass for extra
        /// realtime lights. Off by default — the pass re-runs full SDF evaluation
        /// once per additional light.
        /// </summary>
        public bool EnableForwardAddPass { get; set; }

        /// <summary>
        /// When true, the generated ForwardBase pass receives the main directional
        /// light's realtime shadows.
        /// </summary>
        public bool EnableShadowReceiving { get; set; }

        public List<DecalCompositor.DecalLayer> DecalLayers { get; set; }

        public bool AggressiveOcclusionClipping { get; set; }
    }

    public readonly struct CompileResult
    {
        public CompileResult(bool success, string message, string materialAssetPath)
        {
            Success = success;
            Message = message;
            MaterialAssetPath = materialAssetPath;
        }

        public bool Success { get; }
        public string Message { get; }
        public string MaterialAssetPath { get; }
    }

    public static class VectorTextureCompilerFacade
    {
        [MenuItem(SdfxLanguage.Menu.CompileAllVectorTextures)]
        public static void CompileAll()
        {
            foreach (var assetPath in FindSvgAssetPaths())
            {
                var result = Compile(new CompileOptions { SourcePath = assetPath });
                if (!result.Success)
                {
                    Debug.LogWarning(SdfxLanguage.Compiler.CompileSkipped(result.Message));
                }
            }
        }

        public static CompileResult Compile(CompileOptions options)
        {
            var totalWatch = Stopwatch.StartNew();

            if (options == null)
            {
                return new CompileResult(false, SdfxLanguage.Compiler.OptionsNull, string.Empty);
            }

            if (LooksLikeRaster(options))
            {
                return new CompileResult(false, SdfxLanguage.Compiler.RasterCompileRemoved, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(options.SourcePath))
            {
                return new CompileResult(false, SdfxLanguage.Compiler.SourcePathEmpty, string.Empty);
            }

            if (!File.Exists(options.SourcePath))
            {
                return new CompileResult(false, SdfxLanguage.Compiler.SourceFileNotFound, string.Empty);
            }

            var parserOptions = new ParserOptions
            {
                Strictness = options.ParserStrictness,
                CoordinateModel = options.CoordinateModel
            };

            var parseWarningCount = 0;
            var parseErrorCount = 0;

            var parseWatch = Stopwatch.StartNew();
            var parseResult = ParseBySourceType(options, parserOptions);
            parseWatch.Stop();

            for (var i = 0; i < parseResult.Issues.Count; i++)
            {
                var issue = parseResult.Issues[i];
                var label = issue.Severity == ParseIssueSeverity.Error ? "ERROR" : "WARN";
                if (issue.Severity == ParseIssueSeverity.Error)
                {
                    parseErrorCount++;
                }
                else
                {
                    parseWarningCount++;
                }

                Debug.Log(SdfxLanguage.Compiler.ParseIssue(label, issue.Code.ToString(), issue.ElementName, issue.LineNumber, issue.Message));
            }

            if (parseResult.HasErrors)
            {
                return new CompileResult(false, SdfxLanguage.Compiler.ParseErrorsPreventedCompilation, string.Empty);
            }

            var optimizationSettings = OptimizationSettings.FromProfile(options.OptimizationProfile);

            var simplifyWatch = Stopwatch.StartNew();
            var simplified = Simplifier.Simplify(parseResult.Primitives, optimizationSettings);
            simplifyWatch.Stop();

            var booleanWatch = Stopwatch.StartNew();
            var resolved = BooleanResolver.Resolve(simplified, parseResult.PathEdges);
            var pathEdges = new List<Vector4>(parseResult.PathEdges);
            if (options.AggressiveOcclusionClipping)
            {
                var clipResult = OcclusionPathClipper.Apply(resolved, pathEdges);
                resolved = clipResult.Primitives;
                pathEdges = clipResult.PathEdges;
            }

            booleanWatch.Stop();

            var quantizeWatch = Stopwatch.StartNew();
            var quantized = Quantizer.Quantize(resolved, optimizationSettings);
            quantizeWatch.Stop();

            var primitiveArray = quantized.ToArray();
            long questMs = 0;
            if (options.BuildQuestVariant && options.OptimizationProfile == OptimizationProfile.Quest)
            {
                var questWatch = Stopwatch.StartNew();
                primitiveArray = QuestVariantBaker.BuildSimplifiedPrimitives(primitiveArray, QuestVariantBaker.DefaultMaxPrimitives);
                questWatch.Stop();
                questMs = questWatch.ElapsedMilliseconds;
                Debug.Log(SdfxLanguage.Compiler.QuestVariantStage(questWatch.ElapsedMilliseconds, primitiveArray.Length));
            }

            var isQuest = options.OptimizationProfile == OptimizationProfile.Quest;
            var enableForwardAdd = !isQuest && options.EnableForwardAddPass;
            var hardEdgeCoverage = false;
            var enableVertexPointLights = isQuest;

            var pathSdfBake = PathSdfBaker.Bake(primitiveArray, pathEdges);
            var hasBakedSdf = pathSdfBake.BakedCount > 0;

            primitiveArray = CanvasDomainCuller.Cull(primitiveArray);

            var gridWatch = Stopwatch.StartNew();
            var maxPerCellCap = isQuest ? 8 : 64;
            var maxPerCell = Mathf.Clamp(options.MaxPrimitivesPerCell, 1, maxPerCellCap);
            var gridResCap = isQuest ? 64 : 128;
            var gridWidth = Mathf.Clamp(options.GridWidth, 32, gridResCap);
            var gridHeight = Mathf.Clamp(options.GridHeight, 32, gridResCap);
            var targetGrid = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(primitiveArray.Length, 1)) * 2f),
                32,
                gridResCap);
            targetGrid = DataTextureBaker.RoundUpToPowerOfTwo(targetGrid);
            gridWidth = Mathf.Max(gridWidth, targetGrid);
            gridHeight = Mathf.Max(gridHeight, targetGrid);

            SpatialGrid spatialGrid;
            do
            {
                spatialGrid = SpatialGridBuilder.Build(
                    primitiveArray,
                    gridWidth,
                    gridHeight,
                    maxPerCell);
                if (spatialGrid.DroppedPrimitiveReferences == 0)
                {
                    break;
                }

                if (gridWidth < gridResCap || gridHeight < gridResCap)
                {
                    var nextW = Mathf.Min(gridResCap, Mathf.Max(gridWidth + 8, gridWidth * 2));
                    var nextH = Mathf.Min(gridResCap, Mathf.Max(gridHeight + 8, gridHeight * 2));
                    nextW = DataTextureBaker.RoundUpToPowerOfTwo(nextW);
                    nextH = DataTextureBaker.RoundUpToPowerOfTwo(nextH);
                    if (nextW > gridWidth || nextH > gridHeight)
                    {
                        Debug.Log(SdfxLanguage.Compiler.GridResolutionRaised(
                            gridWidth, gridHeight, nextW, nextH, spatialGrid.DroppedPrimitiveReferences));
                        gridWidth = nextW;
                        gridHeight = nextH;
                        continue;
                    }
                }

                if (maxPerCell >= maxPerCellCap)
                {
                    break;
                }

                var next = Mathf.Min(maxPerCellCap, Mathf.Max(maxPerCell + 4, maxPerCell * 2));
                if (next <= maxPerCell)
                {
                    break;
                }

                Debug.Log(SdfxLanguage.Compiler.GridCapacityRaised(maxPerCell, next, spatialGrid.DroppedPrimitiveReferences));
                maxPerCell = next;
            }
            while (true);

            gridWatch.Stop();
            if (spatialGrid.DroppedPrimitiveReferences > 0)
            {
                Debug.LogWarning(SdfxLanguage.Compiler.GridClippingWarning(spatialGrid.DroppedPrimitiveReferences, maxPerCell));
            }

            options.GridWidth = gridWidth;
            options.GridHeight = gridHeight;

            var useHalfIndices = DataTextureBaker.CanUseHalfIndices(spatialGrid);
            var formatReport = DataTextureBaker.DescribeFormats(spatialGrid);
            Debug.Log(SdfxLanguage.Compiler.DataTextureFormats(
                formatReport.PrimitiveFormat,
                formatReport.GridLookupFormat,
                formatReport.GridIndexFormat,
                formatReport.PathFormat));

            var bakeWatch = Stopwatch.StartNew();
            var primitiveTex = DataTextureBaker.BakePrimitiveTexture(primitiveArray);
            var gridTex = DataTextureBaker.BakeGridLookupTexture(spatialGrid, useHalfIndices);
            var gridIndexTex = DataTextureBaker.BakeGridIndexTexture(spatialGrid, 256, useHalfIndices);
            var pathTex = DataTextureBaker.BakePathDataTexture(pathEdges);
            bakeWatch.Stop();

            var hasTransparency = ResolveTransparency(options);
            var sourceName = CompileOutputPaths.ResolveSourceName(options);
            var outputDirectory = CompileOutputPaths.Resolve(options, sourceName);
            var shaderName = "Custom/VectorTexture/Generated_" + sourceName;
            var absoluteOutputPath = ToAbsolutePath(outputDirectory);

            var codegenWatch = Stopwatch.StartNew();
            var enabledModuleIds = options.EnabledModules;
            if ((enabledModuleIds == null || enabledModuleIds.Count == 0) && !string.IsNullOrWhiteSpace(options.ModulePresetId))
            {
                enabledModuleIds = ShaderModuleRegistry.ResolvePreset(options.ModulePresetId)?.ToList();
            }

            var decalModuleIds = DecalCompositor.RequiredModuleIds(options.DecalLayers);
            if (decalModuleIds.Count > 0)
            {
                enabledModuleIds = enabledModuleIds != null
                    ? new List<string>(enabledModuleIds)
                    : new List<string>();
                for (var d = 0; d < decalModuleIds.Count; d++)
                {
                    var id = decalModuleIds[d];
                    var alreadyPresent = false;
                    for (var i = 0; i < enabledModuleIds.Count; i++)
                    {
                        if (string.Equals(enabledModuleIds[i], id, StringComparison.OrdinalIgnoreCase))
                        {
                            alreadyPresent = true;
                            break;
                        }
                    }

                    if (!alreadyPresent)
                    {
                        enabledModuleIds.Add(id);
                    }
                }
            }

            enabledModuleIds = StripGrabPassForQuest(enabledModuleIds, isQuest);

            var resolvedModules = ShaderModuleRegistry.Resolve(enabledModuleIds, options.ModuleLodTier);
            var samplerCount = ShaderModuleRegistry.TotalExtraSamplerCount(resolvedModules);
            if (isQuest && samplerCount > CorePipeline.QuestMaxSamplerBudget)
            {
                Debug.LogWarning(SdfxLanguage.Compiler.SamplerBudgetExceeded(samplerCount, CorePipeline.QuestMaxSamplerBudget));
            }

            var conflictWarnings = ShaderModuleRegistry.ValidateSelection(enabledModuleIds);
            LogModuleConflictWarnings(conflictWarnings);

            var generationRequest = new ShaderGenerationRequest
            {
                ShaderName = shaderName,
                MaxPrimitivesPerCell = maxPerCell,
                HasTransparency = hasTransparency,
                BackgroundColor = options.BackgroundColor,
                BlendMode = ResolveCompileBlendMode(options),
                Modules = resolvedModules,
                OptimizationProfile = options.OptimizationProfile,
                FlatTextures = FlatTextureLayout.FromTextures(primitiveTex, gridIndexTex, pathTex),
                EnableForwardAddPass = enableForwardAdd,
                EnableShadowReceiving = options.EnableShadowReceiving,
                HasBakedSdfAtlas = hasBakedSdf,
                BakedSdfPxRange = pathSdfBake.PxRange,
                HardEdgeCoverage = hardEdgeCoverage,
                EnableVertexPointLights = enableVertexPointLights
            };

            if (resolvedModules.Count > 20)
            {
                Debug.LogWarning(SdfxLanguage.Compiler.LargeModuleShaderWarning(resolvedModules.Count));
            }

            if (resolvedModules.Any(m => string.Equals(m.Id, "grabpass", StringComparison.OrdinalIgnoreCase)))
            {
                Debug.LogWarning(SdfxLanguage.Compiler.GrabPassCompileWarning);
            }
            var generatedShaderPath = HlslGenerator.WriteShaderToDisk(absoluteOutputPath, generationRequest);
            var shaderAssetPath = ToAssetPath(generatedShaderPath);
            SdfxShaderVariantCache.RecordCompiledShader(shaderName, resolvedModules);
            AssetDatabase.ImportAsset(shaderAssetPath, ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderAssetPath);
            if (shader == null)
            {
                return new CompileResult(false, SdfxLanguage.Compiler.ShaderImportFailed(shaderAssetPath), string.Empty);
            }

            if (ShaderUtil.ShaderHasError(shader))
            {
                var messages = ShaderUtil.GetShaderMessages(shader);
                for (var i = 0; i < messages.Length; i++)
                {
                    var msg = messages[i];
                    if ((int)msg.severity != 0)
                    {
                        continue;
                    }

                    return new CompileResult(
                        false,
                        SdfxLanguage.Compiler.ShaderCompileFailed(shaderAssetPath, msg.line, msg.message),
                        string.Empty);
                }

                return new CompileResult(false, SdfxLanguage.Compiler.ShaderImportFailed(shaderAssetPath), string.Empty);
            }

            codegenWatch.Stop();

            var primitiveAssetPath = Path.Combine(outputDirectory, sourceName + "_Primitive.asset").Replace("\\", "/");
            var gridAssetPath = Path.Combine(outputDirectory, sourceName + "_Grid.asset").Replace("\\", "/");
            var gridIndexAssetPath = Path.Combine(outputDirectory, sourceName + "_GridIndex.asset").Replace("\\", "/");
            var pathAssetPath = Path.Combine(outputDirectory, sourceName + "_PathData.asset").Replace("\\", "/");

            var assetWriteWatch = Stopwatch.StartNew();
            EnsureFolderExists(outputDirectory);
            AssetDatabase.CreateAsset(primitiveTex, primitiveAssetPath);
            AssetDatabase.CreateAsset(gridTex, gridAssetPath);
            AssetDatabase.CreateAsset(gridIndexTex, gridIndexAssetPath);
            AssetDatabase.CreateAsset(pathTex, pathAssetPath);

            Texture2D bakedSdfAtlas = null;
            Texture2D bakedSdfMeta = null;
            if (hasBakedSdf)
            {
                bakedSdfAtlas = pathSdfBake.Atlas;
                bakedSdfMeta = pathSdfBake.Meta;
                var atlasPath = Path.Combine(outputDirectory, sourceName + "_BakedSdfAtlas.asset").Replace("\\", "/");
                var metaPath = Path.Combine(outputDirectory, sourceName + "_BakedSdfMeta.asset").Replace("\\", "/");
                AssetDatabase.CreateAsset(bakedSdfAtlas, atlasPath);
                AssetDatabase.CreateAsset(bakedSdfMeta, metaPath);
                bakedSdfAtlas.filterMode = FilterMode.Bilinear;
                bakedSdfAtlas.wrapMode = TextureWrapMode.Clamp;
                bakedSdfMeta.filterMode = FilterMode.Point;
                EditorUtility.SetDirty(bakedSdfAtlas);
                EditorUtility.SetDirty(bakedSdfMeta);
            }

            var materialPath = MaterialGenerator.CreateMaterialAsset(
                shaderName,
                outputDirectory,
                primitiveTex,
                gridTex,
                gridIndexTex,
                pathTex,
                sourceName,
                hasTransparency,
                options.BackgroundColor,
                generationRequest.ResolvedBlendMode,
                bakedSdfAtlas,
                bakedSdfMeta,
                pathSdfBake.PxRange,
                hardEdgeCoverage);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            DecalCompositor.ApplyToMaterial(material, options.DecalLayers);
            if (options.DecalLayers != null && options.DecalLayers.Count > 0)
            {
                EditorUtility.SetDirty(material);
            }

            var lodFlatPath = LodFlatExporter.WriteFlatTexture(
                outputDirectory,
                sourceName,
                primitiveArray,
                pathEdges,
                options.BackgroundColor,
                bakedSdfAtlas: bakedSdfAtlas,
                bakedSdfMeta: bakedSdfMeta);
            var lodFlatMatPath = LodFlatExporter.CreateLodFlatMaterial(
                outputDirectory,
                sourceName,
                lodFlatPath,
                hasTransparency);
            var lodFlatMat = string.IsNullOrEmpty(lodFlatMatPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<Material>(lodFlatMatPath);
            if (material != null && lodFlatMat != null)
            {
                LodFlatExporter.CreateLodGroupPrefab(outputDirectory, sourceName, material, lodFlatMat);
            }

            var compiledAsset = ScriptableObject.CreateInstance<CompiledVectorTextureAsset>();
            compiledAsset.sourcePath = options.SourcePath;
            compiledAsset.primitives = primitiveArray;
            compiledAsset.primitiveDataTexture = primitiveTex;
            compiledAsset.gridLookupTexture = gridTex;
            compiledAsset.gridIndexTexture = gridIndexTex;
            compiledAsset.pathDataTexture = pathTex;
            compiledAsset.bakedSdfAtlas = bakedSdfAtlas;
            compiledAsset.bakedSdfMeta = bakedSdfMeta;
            compiledAsset.material = material;
            compiledAsset.compileReport = BuildCompileReport(
                options,
                parseResult,
                parseWarningCount,
                parseErrorCount,
                simplified.Count,
                resolved.Count,
                quantized.Count,
                primitiveArray.Length,
                spatialGrid.DroppedPrimitiveReferences,
                parseWatch.ElapsedMilliseconds,
                simplifyWatch.ElapsedMilliseconds,
                booleanWatch.ElapsedMilliseconds,
                quantizeWatch.ElapsedMilliseconds,
                questMs,
                gridWatch.ElapsedMilliseconds,
                bakeWatch.ElapsedMilliseconds,
                codegenWatch.ElapsedMilliseconds,
                assetWriteWatch.ElapsedMilliseconds,
                pathSdfBake.BakedCount,
                gridWidth,
                gridHeight,
                formatReport,
                pathEdges.Count);

            var compiledAssetPath = Path.Combine(outputDirectory, sourceName + "_Compiled.asset").Replace("\\", "/");
            AssetDatabase.CreateAsset(compiledAsset, compiledAssetPath);
            AssetDatabase.SaveAssets();
            assetWriteWatch.Stop();
            SdfxMaterialInspectorUI.InvalidateCompiledAssetCache();

            totalWatch.Stop();
            Debug.Log(SdfxLanguage.Compiler.CompileSummary(
                options.SourcePath,
                options.OptimizationProfile.ToString(),
                parseResult.Primitives.Count,
                simplified.Count,
                resolved.Count,
                quantized.Count,
                primitiveArray.Length,
                parseWarningCount,
                spatialGrid.DroppedPrimitiveReferences,
                parseWatch.ElapsedMilliseconds,
                simplifyWatch.ElapsedMilliseconds,
                booleanWatch.ElapsedMilliseconds,
                quantizeWatch.ElapsedMilliseconds,
                gridWatch.ElapsedMilliseconds,
                bakeWatch.ElapsedMilliseconds,
                codegenWatch.ElapsedMilliseconds,
                assetWriteWatch.ElapsedMilliseconds,
                totalWatch.ElapsedMilliseconds));

            return new CompileResult(true, SdfxLanguage.Compiler.CompileSucceeded, materialPath);
        }

        private static CompileReport BuildCompileReport(
            CompileOptions options,
            ParseResult parseResult,
            int parseWarnings,
            int parseErrors,
            int simplifiedCount,
            int resolvedCount,
            int quantizedCount,
            int finalCount,
            int droppedGridReferences,
            long parseMs,
            long simplifyMs,
            long booleanMs,
            long quantizeMs,
            long questMs,
            long gridMs,
            long bakeMs,
            long codegenMs,
            long assetMs,
            int bakedPathCount = 0,
            int gridWidth = 32,
            int gridHeight = 32,
            DataTextureBaker.FormatReport formatReport = null,
            int pathEdgeCount = -1)
        {
            const int highPathEdgeThreshold = 500;
            var totalPathEdges = pathEdgeCount >= 0 ? pathEdgeCount : (parseResult.PathEdges?.Count ?? 0);
            var highPathEdgeCount = totalPathEdges > highPathEdgeThreshold;
            if (highPathEdgeCount)
            {
                Debug.LogWarning(SdfxLanguage.Compiler.HighPathEdgeCountWarning(totalPathEdges));
            }

            var report = new CompileReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                sourcePath = options.SourcePath,
                sourceType = options.SourceType.ToString(),
                optimizationProfile = options.OptimizationProfile.ToString(),
                parserStrictness = options.ParserStrictness.ToString(),
                coordinateModel = options.CoordinateModel.ToString(),
                rasterAlgorithm = string.Empty,
                buildQuestVariant = options.BuildQuestVariant,
                aggressiveOcclusionClipping = options.AggressiveOcclusionClipping,
                counts = new PrimitiveCountReport
                {
                    parsed = parseResult.Primitives.Count,
                    simplified = simplifiedCount,
                    resolved = resolvedCount,
                    quantized = quantizedCount,
                    final = finalCount,
                    pathEdges = totalPathEdges,
                    bakedPaths = bakedPathCount,
                    gridWidth = gridWidth,
                    gridHeight = gridHeight
                },
                timings = new StageTimingReport
                {
                    parseMs = parseMs,
                    simplifyMs = simplifyMs,
                    booleanMs = booleanMs,
                    quantizeMs = quantizeMs,
                    questMs = questMs,
                    gridMs = gridMs,
                    bakeMs = bakeMs,
                    codegenMs = codegenMs,
                    assetMs = assetMs,
                    totalMs = parseMs + simplifyMs + booleanMs + quantizeMs + questMs + gridMs + bakeMs + codegenMs + assetMs
                },
                warnings = new WarningReport
                {
                    parseWarnings = parseWarnings,
                    parseErrors = parseErrors,
                    droppedGridReferences = droppedGridReferences,
                    highPathEdgeCount = highPathEdgeCount,
                    totalWarnings = parseWarnings
                        + (droppedGridReferences > 0 ? 1 : 0)
                        + (highPathEdgeCount ? 1 : 0)
                },
                dataTextureFormats = formatReport == null
                    ? null
                    : new DataTextureFormatReport
                    {
                        primitiveFormat = formatReport.PrimitiveFormat,
                        gridLookupFormat = formatReport.GridLookupFormat,
                        gridIndexFormat = formatReport.GridIndexFormat,
                        pathFormat = formatReport.PathFormat,
                        usedHalfIndices = formatReport.UsedHalfIndices
                    }
            };

            for (var i = 0; i < parseResult.Issues.Count; i++)
            {
                var issue = parseResult.Issues[i];
                report.parseIssues.Add(new ParseIssueReport
                {
                    severity = issue.Severity.ToString(),
                    code = issue.Code.ToString(),
                    elementName = issue.ElementName,
                    lineNumber = issue.LineNumber,
                    message = issue.Message
                });
            }

            return report;
        }

        private static List<string> StripGrabPassForQuest(List<string> enabledModuleIds, bool isQuest)
        {
            if (!isQuest || enabledModuleIds == null || enabledModuleIds.Count == 0)
            {
                return enabledModuleIds;
            }

            var filtered = new List<string>(enabledModuleIds.Count);
            var removed = false;
            for (var i = 0; i < enabledModuleIds.Count; i++)
            {
                if (string.Equals(enabledModuleIds[i], "grabpass", StringComparison.OrdinalIgnoreCase))
                {
                    removed = true;
                    continue;
                }

                filtered.Add(enabledModuleIds[i]);
            }

            if (removed)
            {
                Debug.LogWarning(SdfxLanguage.Compiler.GrabPassQuestBlocked);
            }

            return filtered;
        }

        private static string ToAbsolutePath(string assetPath)
        {
            if (Path.IsPathRooted(assetPath))
            {
                return assetPath;
            }

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new DirectoryNotFoundException(SdfxLanguage.Compiler.ProjectRootResolveFailed);
            }

            return Path.Combine(projectRoot.FullName, assetPath);
        }

        private static IEnumerable<string> FindSvgAssetPaths()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var guids = AssetDatabase.FindAssets("glob:\"**/*.svg\"", new[] { "Assets" });
            for (var i = 0; i < guids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!assetPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (seen.Add(assetPath))
                {
                    yield return assetPath;
                }
            }
        }

        private static ParseResult ParseBySourceType(
            CompileOptions options,
            ParserOptions parserOptions)
        {
            var sourceType = options.SourceType;
            if (sourceType == CompileSourceType.Auto)
            {
                sourceType = InferSourceType(options);
            }

            switch (sourceType)
            {
                case (CompileSourceType)3:
                {
                    return RemovedRasterParseResult();
                }
                case CompileSourceType.Custom:
                {
                    var sourceText = File.ReadAllText(options.SourcePath);
                    return CustomFormatParser.Parse(sourceText, parserOptions);
                }
                case CompileSourceType.Svg:
                default:
                {
                    var sourceText = File.ReadAllText(options.SourcePath);
                    return SvgParser.Parse(sourceText, parserOptions);
                }
            }
        }

        private static CompileSourceType InferSourceType(CompileOptions options)
        {
            var path = options.SourcePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return CompileSourceType.Svg;
            }

            if (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                return CompileSourceType.Svg;
            }

            return CompileSourceType.Custom;
        }

        private static bool LooksLikeRaster(CompileOptions options)
        {
            if ((int)options.SourceType == 3)
            {
                return true;
            }

            var path = options.SourcePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".tga", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".psd", StringComparison.OrdinalIgnoreCase);
        }

        private static ParseResult RemovedRasterParseResult()
        {
            return new ParseResult(
                new List<Primitive>(),
                new List<PrimitiveSourceData>(),
                new List<ParseIssue>
                {
                    new ParseIssue(
                        ParseIssueSeverity.Error,
                        SdfxLanguage.Compiler.RasterCompileRemoved,
                        "raster",
                        0,
                        ParseIssueCode.InvalidInput)
                });
        }

        private static string ToAssetPath(string absolutePath)
        {
            var normalizedAbsolute = absolutePath.Replace("\\", "/");
            var normalizedDataPath = Application.dataPath.Replace("\\", "/");

            if (!normalizedAbsolute.StartsWith(normalizedDataPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(SdfxLanguage.Compiler.OutputMustBeInsideAssets);
            }

            return "Assets" + normalizedAbsolute.Substring(normalizedDataPath.Length);
        }

        private static BlendModePreset ResolveCompileBlendMode(CompileOptions options)
        {
            if (options.BlendMode.HasValue)
            {
                return options.BlendMode.Value;
            }

            if (!string.IsNullOrWhiteSpace(options.ModulePresetId))
            {
                var preset = ShaderModuleRegistry.FindPreset(options.ModulePresetId);
                if (preset?.BlendMode != null)
                {
                    return preset.BlendMode.Value;
                }
            }

            return BlendModePreset.Opaque;
        }

        private static bool ResolveTransparency(CompileOptions options)
        {
            switch (options.TransparencyMode)
            {
                case TransparencyMode.ForceOpaque:
                    return false;
                case TransparencyMode.ForceTransparent:
                    return true;
                default:
                    return options.BackgroundColor.a < 0.999f;
            }
        }

        private static void LogModuleConflictWarnings(IReadOnlyList<string> conflictWarnings)
        {
            if (conflictWarnings == null || conflictWarnings.Count == 0)
            {
                return;
            }

            const int maxListed = 4;
            if (conflictWarnings.Count <= maxListed)
            {
                for (var i = 0; i < conflictWarnings.Count; i++)
                {
                    Debug.LogWarning(SdfxLanguage.Compiler.ModuleConflict(conflictWarnings[i]));
                }

                return;
            }

            var examples = string.Join("; ", conflictWarnings.Take(maxListed));
            Debug.LogWarning(SdfxLanguage.Compiler.ModuleConflictsSummary(conflictWarnings.Count, examples));
        }

        private static void EnsureFolderExists(string assetFolder)
        {
            var normalized = assetFolder.Replace("\\", "/").Trim('/');
            var segments = normalized.Split('/');
            if (segments.Length == 0 || segments[0] != "Assets")
            {
                throw new InvalidOperationException(SdfxLanguage.Compiler.OutputDirectoryMustBeInsideAssets);
            }

            var current = "Assets";
            for (var i = 1; i < segments.Length; i++)
            {
                var next = segments[i];
                var candidate = current + "/" + next;
                if (!AssetDatabase.IsValidFolder(candidate))
                {
                    AssetDatabase.CreateFolder(current, next);
                }

                current = candidate;
            }
        }
    }
}
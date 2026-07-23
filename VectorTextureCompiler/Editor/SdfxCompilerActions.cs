using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    internal static class SdfxCompilerActions
    {
        public static bool TryRecompile(CompiledVectorTextureAsset compiledAsset, out string message)
        {
            message = string.Empty;
            if (compiledAsset == null)
            {
                message = SdfxLanguage.Compiler.CompiledAssetMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(compiledAsset.sourcePath))
            {
                message = SdfxLanguage.Compiler.CompiledAssetNoSourcePath;
                return false;
            }

            var report = compiledAsset.compileReport;
            if (report != null && string.Equals(report.sourceType, "Raster", StringComparison.Ordinal))
            {
                if (!compiledAsset.sourcePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    message = SdfxLanguage.Compiler.RasterCompileRemoved;
                    return false;
                }
            }

            var options = new CompileOptions
            {
                SourcePath = compiledAsset.sourcePath,
                BuildQuestVariant = report?.buildQuestVariant ?? true,
                ParserStrictness = ParseEnum(report?.parserStrictness, ParserStrictness.Strict),
                CoordinateModel = ParseEnum(report?.coordinateModel, CoordinateModel.Hybrid),
                OptimizationProfile = ParseEnum(report?.optimizationProfile, OptimizationProfile.Pc)
            };

            var result = VectorTextureCompilerFacade.Compile(options);
            message = result.Message;
            return result.Success;
        }

        public static bool TryOptimizeShader(Material material, out string message)
        {
            var compiled = SdfxMaterialInspectorUI.FindCompiledAsset(material);
            if (compiled == null)
            {
                message = SdfxLanguage.ShaderGui.NoCompiledAsset;
                return false;
            }

            var enabled = SdfxMaterialInspectorUI.GetEnabledModuleIds(material, ShaderModuleRegistry.All);
            if (enabled.Count == 0)
            {
                message = SdfxLanguage.ShaderGui.OptimizeNeedsModules;
                return false;
            }

            var shaderPath = AssetDatabase.GetAssetPath(material.shader);
            return TryRegenerateShaderOnly(
                material,
                compiled,
                enabled,
                ReadReceivesShadows(shaderPath),
                ReadForwardAddPass(shaderPath),
                out message);
        }

        public static bool TryRegenerateShaderOnly(
            Material material,
            CompiledVectorTextureAsset compiledAsset,
            IReadOnlyList<string> moduleIds,
            bool enableShadowReceiving,
            bool enableForwardAddPass,
            out string message)
        {
            message = string.Empty;
            if (material == null)
            {
                message = SdfxLanguage.Compiler.MaterialMissing;
                return false;
            }

            if (compiledAsset == null)
            {
                message = SdfxLanguage.ShaderGui.NoCompiledAsset;
                return false;
            }

            if (moduleIds == null || moduleIds.Count == 0)
            {
                message = SdfxLanguage.ShaderGui.RecompileNeedsModules;
                return false;
            }

            var shader = material.shader;
            if (shader == null)
            {
                message = SdfxLanguage.Compiler.MaterialNoShader;
                return false;
            }

            var shaderAssetPath = AssetDatabase.GetAssetPath(shader);
            if (string.IsNullOrWhiteSpace(shaderAssetPath) || !shaderAssetPath.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
            {
                message = SdfxLanguage.ShaderGui.ShaderNotGenerated;
                return false;
            }

            var prim = compiledAsset.primitiveDataTexture
                       ?? material.GetTexture("_PrimitiveDataTex") as Texture2D;
            var grid = compiledAsset.gridLookupTexture
                       ?? material.GetTexture("_GridLookupTex") as Texture2D;
            var gridIndex = compiledAsset.gridIndexTexture
                            ?? material.GetTexture("_GridIndexTex") as Texture2D;
            var path = compiledAsset.pathDataTexture
                       ?? material.GetTexture("_PathDataTex") as Texture2D;

            if (prim == null || grid == null || gridIndex == null)
            {
                message = SdfxLanguage.ShaderGui.MissingBakedTextures;
                return false;
            }

            var resolvedModules = ShaderModuleRegistry.Resolve(moduleIds, maxLodTier: 0);
            if (resolvedModules.Count == 0)
            {
                message = SdfxLanguage.ShaderGui.RecompileNeedsModules;
                return false;
            }

            var conflictWarnings = ShaderModuleRegistry.ValidateSelection(moduleIds);
            if (conflictWarnings.Count > 0 && conflictWarnings.Count <= 4)
            {
                for (var i = 0; i < conflictWarnings.Count; i++)
                {
                    Debug.LogWarning(SdfxLanguage.Compiler.ModuleConflict(conflictWarnings[i]));
                }
            }
            else if (conflictWarnings.Count > 4)
            {
                Debug.LogWarning(SdfxLanguage.Compiler.ModuleConflictsSummary(
                    conflictWarnings.Count,
                    string.Join("; ", conflictWarnings.Take(4))));
            }

            if (resolvedModules.Any(m => string.Equals(m.Id, "grabpass", StringComparison.OrdinalIgnoreCase)))
            {
                Debug.LogWarning(SdfxLanguage.Compiler.GrabPassCompileWarning);
            }

            var blend = BlendModePreset.Opaque;
            if (material.HasProperty("_BlendMode"))
            {
                blend = (BlendModePreset)Mathf.RoundToInt(material.GetFloat("_BlendMode"));
            }

            var bg = material.HasProperty("_BackgroundColor")
                ? material.GetColor("_BackgroundColor")
                : Color.white;

            var hasTransparency = blend != BlendModePreset.Opaque && blend != BlendModePreset.Cutout;
            var profile = ParseEnum(compiledAsset.compileReport?.optimizationProfile, OptimizationProfile.Pc);
            var maxPerCell = ReadMaxPrimitivesPerCell(shaderAssetPath, profile == OptimizationProfile.Quest ? 8 : 32);
            var hasBaked = compiledAsset.bakedSdfAtlas != null;

            if (profile == OptimizationProfile.Quest)
            {
                var withoutGrab = resolvedModules
                    .Where(m => !string.Equals(m.Id, "grabpass", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (withoutGrab.Count != resolvedModules.Count)
                {
                    Debug.LogWarning(SdfxLanguage.Compiler.GrabPassQuestBlocked);
                    resolvedModules = withoutGrab;
                }
            }

            var request = new ShaderGenerationRequest
            {
                ShaderName = shader.name,
                MaxPrimitivesPerCell = maxPerCell,
                HasTransparency = hasTransparency,
                BackgroundColor = bg,
                BlendMode = blend,
                Modules = resolvedModules,
                OptimizationProfile = profile,
                FlatTextures = FlatTextureLayout.FromTextures(prim, gridIndex, path),
                EnableShadowReceiving = enableShadowReceiving,
                EnableForwardAddPass = profile != OptimizationProfile.Quest && enableForwardAddPass,
                HasBakedSdfAtlas = hasBaked,
                HardEdgeCoverage = false,
                EnableVertexPointLights = profile == OptimizationProfile.Quest
            };

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                message = SdfxLanguage.Compiler.ProjectRootResolveFailedShort;
                return false;
            }

            var absoluteShaderPath = Path.GetFullPath(Path.Combine(projectRoot.FullName, shaderAssetPath));
            var outputFolder = Path.GetDirectoryName(absoluteShaderPath);
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                message = SdfxLanguage.ShaderGui.ShaderNotGenerated;
                return false;
            }

            try
            {
                HlslGenerator.WriteShaderToDisk(outputFolder, request);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }

            SdfxShaderVariantCache.RecordCompiledShader(shader.name, resolvedModules);
            AssetDatabase.ImportAsset(shaderAssetPath, ImportAssetOptions.ForceUpdate);

            var imported = AssetDatabase.LoadAssetAtPath<Shader>(shaderAssetPath);
            if (imported == null)
            {
                message = SdfxLanguage.Compiler.ShaderImportFailed(shaderAssetPath);
                return false;
            }

            if (ShaderUtil.ShaderHasError(imported))
            {
                var messages = ShaderUtil.GetShaderMessages(imported);
                for (var i = 0; i < messages.Length; i++)
                {
                    if ((int)messages[i].severity != 0)
                    {
                        continue;
                    }

                    message = SdfxLanguage.Compiler.ShaderCompileFailed(
                        shaderAssetPath,
                        messages[i].line,
                        messages[i].message);
                    return false;
                }

                message = SdfxLanguage.Compiler.ShaderImportFailed(shaderAssetPath);
                return false;
            }

            material.shader = imported;
            SyncModuleKeywords(material, resolvedModules);
            if (hasBaked)
            {
                if (material.HasProperty("_BakedSdfAtlas") && compiledAsset.bakedSdfAtlas != null)
                {
                    material.SetTexture("_BakedSdfAtlas", compiledAsset.bakedSdfAtlas);
                }

                if (material.HasProperty("_BakedSdfMeta") && compiledAsset.bakedSdfMeta != null)
                {
                    material.SetTexture("_BakedSdfMeta", compiledAsset.bakedSdfMeta);
                }
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            message = SdfxLanguage.ShaderGui.ShaderRegenerated(resolvedModules.Count);
            return true;
        }

        private static void SyncModuleKeywords(Material material, IReadOnlyList<ShaderModule> compiledModules)
        {
            var compiledIds = new HashSet<string>(
                compiledModules.Select(m => m.Id),
                StringComparer.OrdinalIgnoreCase);

            foreach (var module in ShaderModuleRegistry.All)
            {
                if (!compiledIds.Contains(module.Id) || !material.HasProperty(module.ToggleProperty))
                {
                    material.DisableKeyword(module.Keyword);
                    continue;
                }

                if (material.GetFloat(module.ToggleProperty) > 0.5f)
                {
                    material.EnableKeyword(module.Keyword);
                }
                else
                {
                    material.DisableKeyword(module.Keyword);
                }
            }
        }

        public static bool ReadReceivesShadows(string shaderAssetPath)
        {
            if (string.IsNullOrWhiteSpace(shaderAssetPath)
                || !shaderAssetPath.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                var text = File.ReadAllText(shaderAssetPath);
                return Regex.IsMatch(text, @"#pragma\s+multi_compile_fwdbase");
            }
            catch
            {
                return false;
            }
        }

        public static bool ReadForwardAddPass(string shaderAssetPath)
        {
            if (string.IsNullOrWhiteSpace(shaderAssetPath)
                || !shaderAssetPath.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                var text = File.ReadAllText(shaderAssetPath);
                return text.IndexOf("SDFX_ForwardAdd", StringComparison.Ordinal) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private static int ReadMaxPrimitivesPerCell(string shaderAssetPath, int fallback)
        {
            try
            {
                var text = File.ReadAllText(shaderAssetPath);
                var match = Regex.Match(text, @"#define\s+MAX_PRIMITIVES_PER_CELL\s+(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var value) && value > 0)
                {
                    return value;
                }
            }
            catch
            {
                // Fall through to default.
            }

            return fallback;
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            return Enum.TryParse(value, out TEnum parsed) ? parsed : fallback;
        }

        public static void PingCompiledAsset(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
        }

        public static void OpenOutputFolder(string compiledAssetPath)
        {
            if (string.IsNullOrWhiteSpace(compiledAssetPath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(compiledAssetPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            var absolute = Path.GetFullPath(directory);
            if (Directory.Exists(absolute))
            {
                EditorUtility.RevealInFinder(absolute);
            }
        }
    }
}

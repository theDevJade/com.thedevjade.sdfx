using System;
using System.IO;
using SDFX.VectorTextureCompiler.Core;
using SDFX.VectorTextureCompiler.Core.Compiler;
using SDFX.VectorTextureCompiler.Core.Optimize;
using SDFX.VectorTextureCompiler.Core.Parsing;
using UnityEditor;

namespace SDFX.VectorTextureCompiler.Editor
{
    internal static class SdfxCompilerActions
    {
        public static bool TryRecompile(CompiledVectorTextureAsset compiledAsset, out string message)
        {
            const string rasterRemovedMessage = "Raster compile was removed. Use Tools/SDFX/Rasterizer to convert to SVG, then compile the SVG.";

            message = string.Empty;
            if (compiledAsset == null)
            {
                message = "Compiled asset is missing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(compiledAsset.sourcePath))
            {
                message = "Compiled asset has no source path.";
                return false;
            }

            var report = compiledAsset.compileReport;
            if (report != null && string.Equals(report.sourceType, "Raster", StringComparison.Ordinal))
            {
                if (!compiledAsset.sourcePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    message = rasterRemovedMessage;
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

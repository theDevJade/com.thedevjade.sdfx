using System.IO;
using SDFX.VectorTextureCompiler.Core.Compiler;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Optimize;
using SDFX.VectorTextureCompiler.Core.Parsing;
using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Editor
{
    public sealed class SVGImportPostprocessor : AssetPostprocessor
    {
        private static string AutoCompilePrefix => SdfxLanguage.Postprocessor.AutoCompilePrefix;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var assetPath in importedAssets)
            {
                if (!assetPath.EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileName = Path.GetFileNameWithoutExtension(assetPath);
                if (fileName == null || !fileName.StartsWith(AutoCompilePrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var options = new CompileOptions
                {
                    SourcePath = assetPath,
                    BuildQuestVariant = true,
                    ParserStrictness = ParserStrictness.Permissive,
                    CoordinateModel = CoordinateModel.Hybrid,
                    OptimizationProfile = OptimizationProfile.Quest
                };

                var result = VectorTextureCompilerFacade.Compile(options);
                if (!result.Success)
                {
                    Debug.LogWarning(SdfxLanguage.Postprocessor.AutoCompileFailed(assetPath, result.Message));
                }
            }
        }
    }
}
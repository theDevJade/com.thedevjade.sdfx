using System.IO;
using UnityEditor;
using UnityEngine;

namespace SDFX.Rasterizer
{
    public static class RasterToSvg
    {
        public static RasterToSvgResult Export(Texture2D source, RasterParsingOptions options = null)
        {
            options ??= new RasterParsingOptions();
            options.Algorithm = ResolveAlgorithm(options);
            var work = RasterVectorizerRegistry.Resolve(options.Algorithm).Build(source, options);
            var success = !HasErrors(work.Issues) && !string.IsNullOrEmpty(work.SvgText) && work.PathCount > 0;
            return new RasterToSvgResult(success, work.SvgText, string.Empty, work.OverlayPreview, work.Issues, work.PathCount);
        }

        public static RasterToSvgResult ExportToFile(
            Texture2D source,
            string svgOutputPath,
            RasterParsingOptions options = null)
        {
            var result = Export(source, options);
            if (!result.Success || string.IsNullOrWhiteSpace(svgOutputPath))
            {
                return result;
            }

            var directory = Path.GetDirectoryName(svgOutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(svgOutputPath, result.SvgText);
            if (svgOutputPath.Replace('\\', '/').StartsWith("Assets/"))
            {
                AssetDatabase.ImportAsset(svgOutputPath.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
            }

            return new RasterToSvgResult(
                result.Success,
                result.SvgText,
                svgOutputPath.Replace('\\', '/'),
                result.OverlayPreview,
                result.Issues,
                result.PathCount);
        }

        public static void DestroyPreview(RasterToSvgResult result)
        {
            if (result.OverlayPreview != null)
            {
                Object.DestroyImmediate(result.OverlayPreview);
            }
        }

        private static bool HasErrors(System.Collections.Generic.List<RasterIssue> issues)
        {
            if (issues == null)
            {
                return false;
            }

            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == RasterIssueSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        private static RasterVectorizationAlgorithm ResolveAlgorithm(RasterParsingOptions rasterOptions)
        {
#pragma warning disable CS0618
            if (rasterOptions.TracingMode != RasterTracingMode.Edges &&
                rasterOptions.Algorithm == RasterVectorizationAlgorithm.GradientEdgeVectorization)
            {
                return RasterParsingOptions.MigrateTracingMode(rasterOptions.TracingMode);
            }
#pragma warning restore CS0618

            return rasterOptions.Algorithm;
        }
    }
}

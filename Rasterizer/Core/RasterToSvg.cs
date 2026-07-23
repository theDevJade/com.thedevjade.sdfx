using System.Collections.Generic;
using System.IO;
using SDFX.Rasterizer.Editor;
using UnityEditor;
using UnityEngine;

namespace SDFX.Rasterizer
{
    public static class RasterToSvg
    {
        public static RasterToSvgResult Export(Texture2D source, RasterParsingOptions options = null)
        {
            options ??= new RasterParsingOptions();
            var issues = new List<RasterIssue>();

            if (!NativeDllConsent.EnsureAccepted())
            {
                issues.Add(new RasterIssue(
                    RasterIssueSeverity.Error,
                    "Native DLL was not accepted or failed to load.",
                    code: RasterIssueCode.NativeUnavailable));
                return new RasterToSvgResult(false, string.Empty, string.Empty, null, issues, 0);
            }

            if (source == null)
            {
                issues.Add(new RasterIssue(
                    RasterIssueSeverity.Error,
                    "Raster source is missing.",
                    code: RasterIssueCode.InvalidInput));
                return new RasterToSvgResult(false, string.Empty, string.Empty, null, issues, 0);
            }

            var readable = NativeVtracer.EnsureReadable(source);
            if (readable == null)
            {
                issues.Add(new RasterIssue(
                    RasterIssueSeverity.Error,
                    "Failed to read raster pixel data.",
                    code: RasterIssueCode.InvalidInput));
                return new RasterToSvgResult(false, string.Empty, string.Empty, null, issues, 0);
            }

            try
            {
                var pixels = readable.GetPixels32();
                var native = options.ToNative();
                if (!NativeVtracer.TryVectorize(
                        pixels,
                        readable.width,
                        readable.height,
                        native,
                        out var svg,
                        out var pathCount,
                        out var error))
                {
                    var code = !string.IsNullOrEmpty(error)
                               && error.IndexOf("dll not found", System.StringComparison.OrdinalIgnoreCase) >= 0
                        ? RasterIssueCode.NativeUnavailable
                        : RasterIssueCode.NativeFailed;
                    issues.Add(new RasterIssue(RasterIssueSeverity.Error, error, code: code));
                    return new RasterToSvgResult(false, string.Empty, string.Empty, null, issues, 0);
                }

                var success = !string.IsNullOrEmpty(svg) && pathCount > 0;
                if (!success)
                {
                    issues.Add(new RasterIssue(
                        RasterIssueSeverity.Error,
                        "Vectorize produced no paths.",
                        code: RasterIssueCode.NativeFailed));
                }

                return new RasterToSvgResult(success, svg, string.Empty, null, issues, pathCount);
            }
            finally
            {
                NativeVtracer.CleanupReadableCopy(readable, source);
            }
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
    }
}

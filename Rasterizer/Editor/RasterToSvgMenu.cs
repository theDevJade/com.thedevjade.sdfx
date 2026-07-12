using System.IO;
using UnityEditor;
using UnityEngine;

namespace SDFX.Rasterizer.Editor
{
    public static class RasterToSvgMenu
    {
        [MenuItem("Assets/SDFX/Auto Convert To SVG", false, 1999)]
        private static void AutoConvertSelected()
        {
            var textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
            if (textures == null || textures.Length == 0)
            {
                EditorUtility.DisplayDialog("SDFX Rasterizer", "Select one or more Texture2D assets first.", "OK");
                return;
            }

            var converted = 0;
            var failed = 0;
            Object lastSvg = null;
            try
            {
                for (var i = 0; i < textures.Length; i++)
                {
                    var texture = textures[i];
                    var assetPath = AssetDatabase.GetAssetPath(texture);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        "SDFX Rasterizer",
                        $"Analyzing {texture.name} ({i + 1}/{textures.Length})…",
                        i / (float)textures.Length);

                    var recommendation = RasterAutoAlgorithmSelector.Analyze(texture);

                    EditorUtility.DisplayProgressBar(
                        "SDFX Rasterizer",
                        $"Converting {texture.name} with {RasterAlgorithmMetadata.Get(recommendation.Algorithm).Name} ({i + 1}/{textures.Length})…",
                        (i + 0.5f) / textures.Length);

                    var svgPath = Path.ChangeExtension(assetPath, ".svg");
                    var result = RasterToSvg.ExportToFile(texture, svgPath, recommendation.Options);
                    RasterToSvg.DestroyPreview(result);

                    if (result.Success)
                    {
                        converted++;
                        lastSvg = AssetDatabase.LoadAssetAtPath<Object>(svgPath);
                        Debug.Log(
                            $"SDFX Rasterizer auto-converted '{assetPath}' → '{svgPath}' " +
                            $"({result.PathCount} paths) using {RasterAlgorithmMetadata.Get(recommendation.Algorithm).Name}.\n" +
                            $"Why: {recommendation.Reason}",
                            lastSvg);
                    }
                    else
                    {
                        failed++;
                        Debug.LogError(
                            $"SDFX Rasterizer failed to auto-convert '{assetPath}' " +
                            $"using {RasterAlgorithmMetadata.Get(recommendation.Algorithm).Name}. " +
                            $"Why chosen: {recommendation.Reason}",
                            texture);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (lastSvg != null)
            {
                Selection.activeObject = lastSvg;
                EditorGUIUtility.PingObject(lastSvg);
            }

            if (failed > 0)
            {
                EditorUtility.DisplayDialog(
                    "SDFX Rasterizer",
                    $"Converted {converted} texture(s), {failed} failed. Check the Console for details.",
                    "OK");
            }
        }

        [MenuItem("Assets/SDFX/Auto Convert To SVG", true)]
        private static bool AutoConvertSelectedValidate() =>
            Selection.GetFiltered<Texture2D>(SelectionMode.Assets).Length > 0;

        [MenuItem("Assets/SDFX/Open Rasterizer", false, 2000)]
        private static void OpenRasterizer() => RasterizerWindow.Open();
    }
}

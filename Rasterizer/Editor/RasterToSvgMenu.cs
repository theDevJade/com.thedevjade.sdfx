using System.IO;
using SDFX.VectorTextureCompiler.Core.Localization;
using UnityEditor;
using UnityEngine;

namespace SDFX.Rasterizer.Editor
{
    public static class RasterToSvgMenu
    {
        [MenuItem(SdfxLanguage.Menu.AutoConvertToSvg, false, 1999)]
        private static void AutoConvertSelected()
        {
            var textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
            if (textures == null || textures.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    SdfxLanguage.Rasterizer.WindowTitle,
                    SdfxLanguage.Rasterizer.SelectTexturesFirst,
                    SdfxLanguage.Rasterizer.OkButton);
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
                        SdfxLanguage.Rasterizer.WindowTitle,
                        SdfxLanguage.Rasterizer.ProgressAnalyzing(texture.name, i + 1, textures.Length),
                        i / (float)textures.Length);

                    var recommendation = RasterAutoAlgorithmSelector.Analyze(texture);

                    EditorUtility.DisplayProgressBar(
                        SdfxLanguage.Rasterizer.WindowTitle,
                        SdfxLanguage.Rasterizer.ProgressConverting(
                            texture.name,
                            RasterAlgorithmMetadata.Get(recommendation.Algorithm).Name,
                            i + 1,
                            textures.Length),
                        (i + 0.5f) / textures.Length);

                    var svgPath = Path.ChangeExtension(assetPath, ".svg");
                    var result = RasterToSvg.ExportToFile(texture, svgPath, recommendation.Options);
                    RasterToSvg.DestroyPreview(result);

                    if (result.Success)
                    {
                        converted++;
                        lastSvg = AssetDatabase.LoadAssetAtPath<Object>(svgPath);
                        Debug.Log(
                            SdfxLanguage.Rasterizer.AutoConvertSuccessLog(
                                assetPath,
                                svgPath,
                                result.PathCount,
                                RasterAlgorithmMetadata.Get(recommendation.Algorithm).Name,
                                recommendation.Reason),
                            lastSvg);
                    }
                    else
                    {
                        failed++;
                        Debug.LogError(
                            SdfxLanguage.Rasterizer.AutoConvertFailedLog(
                                assetPath,
                                RasterAlgorithmMetadata.Get(recommendation.Algorithm).Name,
                                recommendation.Reason),
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
                    SdfxLanguage.Rasterizer.WindowTitle,
                    SdfxLanguage.Rasterizer.BatchResultSummary(converted, failed),
                    SdfxLanguage.Rasterizer.OkButton);
            }
        }

        [MenuItem(SdfxLanguage.Menu.AutoConvertToSvg, true)]
        private static bool AutoConvertSelectedValidate() =>
            Selection.GetFiltered<Texture2D>(SelectionMode.Assets).Length > 0;

        [MenuItem(SdfxLanguage.Menu.OpenRasterizerFromAssets, false, 2000)]
        private static void OpenRasterizer() => RasterizerWindow.Open();
    }
}

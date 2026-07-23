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
            if (!NativeDllConsent.EnsureAccepted())
            {
                return;
            }

            var textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
            if (textures == null || textures.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    SdfxLanguage.Rasterizer.WindowTitle,
                    SdfxLanguage.Rasterizer.SelectTexturesFirst,
                    SdfxLanguage.Rasterizer.OkButton);
                return;
            }

            var options = new RasterParsingOptions();
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
                        SdfxLanguage.Rasterizer.ProgressConverting(
                            texture.name,
                            SdfxLanguage.Rasterizer.EngineName,
                            i + 1,
                            textures.Length),
                        i / (float)textures.Length);

                    var svgPath = Path.ChangeExtension(assetPath, ".svg");
                    var result = RasterToSvg.ExportToFile(texture, svgPath, options);
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
                                SdfxLanguage.Rasterizer.EngineName,
                                SdfxLanguage.Rasterizer.AutoConvertDefaultReason),
                            lastSvg);
                    }
                    else
                    {
                        failed++;
                        var detail = result.Issues.Count > 0
                            ? result.Issues[0].Message
                            : SdfxLanguage.Rasterizer.AutoConvertDefaultReason;
                        Debug.LogError(
                            SdfxLanguage.Rasterizer.AutoConvertFailedLog(
                                assetPath,
                                SdfxLanguage.Rasterizer.EngineName,
                                detail),
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

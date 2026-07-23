using System;
using System.Globalization;
using System.IO;
using SDFX.VectorTextureCompiler.Core.Localization;
using UnityEditor;
using UnityEngine;

namespace SDFX.Rasterizer.Editor
{
    public sealed class RasterizerWindow : EditorWindow
    {
        private const string SettingsPrefix = "SDFX.RasterizerWindow.";

        private Texture2D sourceTexture;
        private string outputDirectory = string.Empty;
        private bool settingsFoldout = true;
        private Vector2 scroll;
        private string lastStatus = string.Empty;
        private string lastSvgPath = string.Empty;
        private readonly RasterParsingOptions options = new RasterParsingOptions();

        [MenuItem(SdfxLanguage.Menu.OpenRasterizer, false, 100)]
        public static void Open()
        {
            if (!NativeDllConsent.EnsureAccepted())
            {
                return;
            }

            var window = GetWindow<RasterizerWindow>();
            window.titleContent = new GUIContent(SdfxLanguage.Rasterizer.WindowTitle);
            window.minSize = new Vector2(420f, 480f);
        }

        private void OnEnable()
        {
            if (!NativeDllConsent.EnsureAccepted())
            {
                EditorApplication.delayCall += Close;
                return;
            }

            LoadSettings();
        }

        private void OnDisable() => SaveSettings();

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            try
            {
                DrawContents();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawContents()
        {
            EditorGUILayout.HelpBox(SdfxLanguage.Rasterizer.HelpBox, MessageType.Info);

            sourceTexture = (Texture2D)EditorGUILayout.ObjectField(
                SdfxLanguage.Rasterizer.SourceTextureField,
                sourceTexture,
                typeof(Texture2D),
                false);

            using (new EditorGUILayout.HorizontalScope())
            {
                outputDirectory = EditorGUILayout.TextField(SdfxLanguage.Rasterizer.OutputFolderField, outputDirectory);
                if (GUILayout.Button(SdfxLanguage.Rasterizer.BrowseButton, GUILayout.Width(80f)))
                {
                    BrowseOutputFolder();
                }
            }

            if (string.IsNullOrWhiteSpace(outputDirectory) && sourceTexture != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(sourceTexture);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    EditorGUILayout.LabelField(
                        SdfxLanguage.Rasterizer.DefaultOutputFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/')),
                        EditorStyles.miniLabel);
                }
            }

            settingsFoldout = EditorGUILayout.Foldout(
                settingsFoldout,
                SdfxLanguage.Rasterizer.VectorizationSettingsHeader,
                true,
                EditorStyles.foldoutHeader);
            if (settingsFoldout)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    options.ColorMode = (RasterColorMode)EditorGUILayout.EnumPopup(
                        SdfxLanguage.Rasterizer.ColorModeField,
                        options.ColorMode);
                    options.CurveMode = (RasterCurveMode)EditorGUILayout.EnumPopup(
                        SdfxLanguage.Rasterizer.CurveModeField,
                        options.CurveMode);
                    options.FilterSpeckle = EditorGUILayout.IntField(
                        SdfxLanguage.Rasterizer.FilterSpeckleField,
                        options.FilterSpeckle);
                    options.CornerThreshold = EditorGUILayout.IntField(
                        SdfxLanguage.Rasterizer.CornerThresholdField,
                        options.CornerThreshold);
                    options.SpliceThreshold = EditorGUILayout.IntField(
                        SdfxLanguage.Rasterizer.SpliceThresholdField,
                        options.SpliceThreshold);
                    options.Precision = EditorGUILayout.IntSlider(
                        SdfxLanguage.Rasterizer.PrecisionField,
                        options.Precision,
                        0,
                        8);
                    options.SimplifyTolerance = EditorGUILayout.DoubleField(
                        SdfxLanguage.Rasterizer.SimplifyToleranceField,
                        options.SimplifyTolerance);
                    options.MinSimilarity = EditorGUILayout.DoubleField(
                        SdfxLanguage.Rasterizer.MinSimilarityField,
                        options.MinSimilarity);
                    EditorGUILayout.HelpBox(SdfxLanguage.Rasterizer.MinSimilarityHelp, MessageType.None);
                }
            }

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(sourceTexture == null))
            {
                if (GUILayout.Button(SdfxLanguage.Rasterizer.ConvertToSvgButton, GUILayout.Height(28f)))
                {
                    Convert();
                }
            }

            if (!string.IsNullOrEmpty(lastStatus))
            {
                EditorGUILayout.HelpBox(lastStatus, MessageType.None);
            }

            if (!string.IsNullOrEmpty(lastSvgPath))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(SdfxLanguage.Rasterizer.PingSvgButton, GUILayout.Width(90f)))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(lastSvgPath);
                        if (asset != null)
                        {
                            EditorGUIUtility.PingObject(asset);
                            Selection.activeObject = asset;
                        }
                    }
                }
            }
        }

        private void Convert()
        {
            if (!NativeDllConsent.EnsureAccepted())
            {
                return;
            }

            if (sourceTexture == null)
            {
                lastStatus = SdfxLanguage.Rasterizer.StatusSelectSource;
                return;
            }

            var sourceAssetPath = AssetDatabase.GetAssetPath(sourceTexture);
            var sourceName = string.IsNullOrWhiteSpace(sourceTexture.name)
                ? SdfxLanguage.Rasterizer.DefaultSourceName
                : sourceTexture.name;
            var folder = ResolveOutputFolder(sourceAssetPath);
            if (string.IsNullOrEmpty(folder))
            {
                lastStatus = SdfxLanguage.Rasterizer.StatusOutputFolderFailed;
                return;
            }

            EnsureAssetFolder(folder);
            var svgPath = Path.Combine(folder, sourceName + ".svg").Replace("\\", "/");

            var result = RasterToSvg.ExportToFile(sourceTexture, svgPath, CloneOptions(options));
            if (!result.Success)
            {
                RasterToSvg.DestroyPreview(result);
                lastStatus = result.Issues.Count > 0
                    ? result.Issues[0].Message
                    : SdfxLanguage.Rasterizer.StatusConversionFailed;
                lastSvgPath = string.Empty;
                return;
            }

            lastSvgPath = svgPath;
            lastStatus = SdfxLanguage.Rasterizer.StatusConversionSuccess(svgPath, result.PathCount);
            SaveSettings();
            Repaint();
        }

        private string ResolveOutputFolder(string sourceAssetPath)
        {
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                return outputDirectory.Replace("\\", "/").TrimEnd('/');
            }

            if (!string.IsNullOrEmpty(sourceAssetPath))
            {
                return Path.GetDirectoryName(sourceAssetPath)?.Replace("\\", "/") ?? string.Empty;
            }

            return SdfxLanguage.Rasterizer.DefaultGeneratedOutputPath;
        }

        private void BrowseOutputFolder()
        {
            var start = string.IsNullOrWhiteSpace(outputDirectory) ? Application.dataPath : outputDirectory;
            var selected = EditorUtility.OpenFolderPanel(SdfxLanguage.Rasterizer.OutputFolderDialogTitle, start, string.Empty);
            if (string.IsNullOrWhiteSpace(selected))
            {
                return;
            }

            selected = selected.Replace("\\", "/");
            var dataPath = Application.dataPath.Replace("\\", "/");
            if (selected.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                outputDirectory = "Assets" + selected.Substring(dataPath.Length);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    SdfxLanguage.Rasterizer.WindowTitle,
                    SdfxLanguage.Rasterizer.OutputOutsideProject,
                    SdfxLanguage.Rasterizer.OkButton);
            }
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            var parts = assetFolder.Replace("\\", "/").Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static RasterParsingOptions CloneOptions(RasterParsingOptions source)
        {
            return new RasterParsingOptions
            {
                ColorMode = source.ColorMode,
                CurveMode = source.CurveMode,
                FilterSpeckle = Mathf.Max(0, source.FilterSpeckle),
                CornerThreshold = source.CornerThreshold,
                SpliceThreshold = source.SpliceThreshold,
                Precision = Mathf.Clamp(source.Precision, 0, 8),
                SimplifyTolerance = Math.Max(0.0, source.SimplifyTolerance),
                MinSimilarity = Math.Max(0.0, source.MinSimilarity)
            };
        }

        private void SaveSettings()
        {
            Set("sourceTexture", AssetDatabase.GetAssetPath(sourceTexture));
            Set("outputDirectory", outputDirectory);
            Set("colorMode", ((int)options.ColorMode).ToString());
            Set("curveMode", ((int)options.CurveMode).ToString());
            Set("filterSpeckle", options.FilterSpeckle.ToString());
            Set("cornerThreshold", options.CornerThreshold.ToString());
            Set("spliceThreshold", options.SpliceThreshold.ToString());
            Set("precision", options.Precision.ToString());
            Set("simplifyTolerance", options.SimplifyTolerance.ToString(CultureInfo.InvariantCulture));
            Set("minSimilarity", options.MinSimilarity.ToString(CultureInfo.InvariantCulture));
        }

        private void LoadSettings()
        {
            sourceTexture = LoadAsset<Texture2D>("sourceTexture");
            outputDirectory = Get("outputDirectory", outputDirectory);

            options.ColorMode = (RasterColorMode)GetInt("colorMode", (int)options.ColorMode);
            options.CurveMode = (RasterCurveMode)GetInt("curveMode", (int)options.CurveMode);
            options.FilterSpeckle = GetInt("filterSpeckle", options.FilterSpeckle);
            options.CornerThreshold = GetInt("cornerThreshold", options.CornerThreshold);
            options.SpliceThreshold = GetInt("spliceThreshold", options.SpliceThreshold);
            options.Precision = GetInt("precision", options.Precision);
            options.SimplifyTolerance = GetDouble("simplifyTolerance", options.SimplifyTolerance);
            options.MinSimilarity = GetDouble("minSimilarity", options.MinSimilarity);
        }

        private static void Set(string key, string value)
            => EditorUserSettings.SetConfigValue(SettingsPrefix + key, value ?? string.Empty);

        private static string Get(string key, string fallback)
            => EditorUserSettings.GetConfigValue(SettingsPrefix + key) ?? fallback;

        private static int GetInt(string key, int fallback)
            => int.TryParse(EditorUserSettings.GetConfigValue(SettingsPrefix + key), out var parsed) ? parsed : fallback;

        private static double GetDouble(string key, double fallback)
            => double.TryParse(
                EditorUserSettings.GetConfigValue(SettingsPrefix + key),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed)
                ? parsed
                : fallback;

        private static T LoadAsset<T>(string key) where T : UnityEngine.Object
        {
            var path = EditorUserSettings.GetConfigValue(SettingsPrefix + key);
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}

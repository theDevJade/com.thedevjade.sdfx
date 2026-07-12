using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SDFX.Rasterizer.Editor
{
    public sealed class RasterizerWindow : EditorWindow
    {
        private const string SettingsPrefix = "SDFX.RasterizerWindow.";

        private Texture2D sourceTexture;
        private string outputDirectory = string.Empty;
        private Color backgroundColor = Color.white;
        private bool writeFlatPreview = true;
        private bool settingsFoldout = true;
        private Vector2 scroll;
        private string lastStatus = string.Empty;
        private string lastSvgPath = string.Empty;
        private Texture2D lastPreview;
        private readonly RasterParsingOptions options = new RasterParsingOptions
        {
            Algorithm = RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf,
            UseComputeAcceleration = true,
            ColorQuant = { ColorCount = 32 },
            Hybrid = { SimplifyTolerance = 0.5f, MinRegionArea = 12 }
        };

        [MenuItem("Tools/SDFX/Rasterizer", false, 100)]
        public static void Open()
        {
            var window = GetWindow<RasterizerWindow>();
            window.titleContent = new GUIContent("SDFX Rasterizer");
            window.minSize = new Vector2(420f, 480f);
        }

        private void OnEnable() => LoadSettings();

        private void OnDisable()
        {
            SaveSettings();
            ClearPreview();
        }

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
            EditorGUILayout.HelpBox(
                "Convert a raster texture to SVG, then compile it in Tools → SDFX → Vector Texture Compiler.",
                MessageType.Info);

            sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", sourceTexture, typeof(Texture2D), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                outputDirectory = EditorGUILayout.TextField("Output Folder", outputDirectory);
                if (GUILayout.Button("Browse", GUILayout.Width(80f)))
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
                        $"Default: same folder as source ({Path.GetDirectoryName(assetPath)?.Replace('\\', '/')})",
                        EditorStyles.miniLabel);
                }
            }

            writeFlatPreview = EditorGUILayout.Toggle("Write Flat Preview PNG", writeFlatPreview);
            if (writeFlatPreview)
            {
                backgroundColor = EditorGUILayout.ColorField("Flat Preview Background", backgroundColor);
            }

            settingsFoldout = EditorGUILayout.Foldout(settingsFoldout, "Vectorization Settings", true, EditorStyles.foldoutHeader);
            if (settingsFoldout)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    var newAlgorithm = (RasterVectorizationAlgorithm)EditorGUILayout.EnumPopup("Algorithm", options.Algorithm);
                    if (newAlgorithm != options.Algorithm)
                    {
                        options.Algorithm = newAlgorithm;
                    }

                    RasterAlgorithmUI.DrawAlgorithmInfo(options.Algorithm);
                    EditorGUILayout.Space(4f);
                    RasterAlgorithmUI.DrawAlgorithmSettings(options);
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Common", EditorStyles.miniBoldLabel);
                    options.UseComputeAcceleration = EditorGUILayout.Toggle("Compute Acceleration", options.UseComputeAcceleration);
                    options.MinAlpha = EditorGUILayout.Slider("Min Alpha", options.MinAlpha, 0f, 1f);
                    options.MaxPrimitives = EditorGUILayout.IntField("Max Paths", options.MaxPrimitives);
                    if (UsesEdgeSampling(options.Algorithm))
                    {
                        options.EdgeThreshold = EditorGUILayout.Slider("Edge Threshold", options.EdgeThreshold, 0f, 2f);
                        options.SampleStride = EditorGUILayout.IntField("Sample Stride", options.SampleStride);
                        options.UseTiling = EditorGUILayout.Toggle("Use Tiling", options.UseTiling);
                        if (options.UseTiling)
                        {
                            options.TileSize = EditorGUILayout.IntField("Tile Size", options.TileSize);
                            options.TileOverlap = EditorGUILayout.IntField("Tile Overlap", options.TileOverlap);
                            options.AutoTileMinDimension = EditorGUILayout.IntField("Auto Tile Min Dimension", options.AutoTileMinDimension);
                        }
                    }
                }
            }

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(sourceTexture == null))
            {
                if (GUILayout.Button("Convert To SVG", GUILayout.Height(28f)))
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
                    if (GUILayout.Button("Ping SVG", GUILayout.Width(90f)))
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

            if (lastPreview != null)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                var rect = GUILayoutUtility.GetRect(256f, 256f, GUILayout.ExpandWidth(true));
                EditorGUI.DrawPreviewTexture(rect, lastPreview, null, ScaleMode.ScaleToFit);
            }
        }

        private void Convert()
        {
            if (sourceTexture == null)
            {
                lastStatus = "Select a source texture.";
                return;
            }

            var sourceAssetPath = AssetDatabase.GetAssetPath(sourceTexture);
            var sourceName = string.IsNullOrWhiteSpace(sourceTexture.name) ? "Raster" : sourceTexture.name;
            var folder = ResolveOutputFolder(sourceAssetPath);
            if (string.IsNullOrEmpty(folder))
            {
                lastStatus = "Could not resolve output folder.";
                return;
            }

            EnsureAssetFolder(folder);
            var svgPath = Path.Combine(folder, sourceName + ".svg").Replace("\\", "/");

            ClearPreview();
            var result = RasterToSvg.ExportToFile(sourceTexture, svgPath, CloneOptions(options));
            if (!result.Success)
            {
                RasterToSvg.DestroyPreview(result);
                lastStatus = "Conversion failed. Check the Console for details.";
                lastSvgPath = string.Empty;
                return;
            }

            if (writeFlatPreview && result.OverlayPreview != null)
            {
                RasterFlatExport.Write(folder, sourceName, result.OverlayPreview, backgroundColor);
            }

            lastPreview = result.OverlayPreview;
            lastSvgPath = svgPath;
            lastStatus = $"Wrote {svgPath} ({result.PathCount} paths). Compile this SVG in Vector Texture Compiler.";
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

            return "Assets/Generated/SDFX";
        }

        private void BrowseOutputFolder()
        {
            var start = string.IsNullOrWhiteSpace(outputDirectory) ? Application.dataPath : outputDirectory;
            var selected = EditorUtility.OpenFolderPanel("SDFX Rasterizer Output", start, string.Empty);
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
                EditorUtility.DisplayDialog("SDFX Rasterizer", "Output folder must be inside the Unity project Assets folder.", "OK");
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

        private void ClearPreview()
        {
            if (lastPreview != null)
            {
                DestroyImmediate(lastPreview);
                lastPreview = null;
            }
        }

        private static bool UsesEdgeSampling(RasterVectorizationAlgorithm algorithm)
        {
            return algorithm == RasterVectorizationAlgorithm.GradientEdgeVectorization
                || algorithm == RasterVectorizationAlgorithm.AdaptiveBezierFitting
                || algorithm == RasterVectorizationAlgorithm.VoronoiDelaunay;
        }

        private static RasterParsingOptions CloneOptions(RasterParsingOptions source)
        {
            return new RasterParsingOptions
            {
                Algorithm = source.Algorithm,
                EdgeThreshold = Mathf.Clamp(source.EdgeThreshold, 0f, 2f),
                MinAlpha = Mathf.Clamp01(source.MinAlpha),
                SampleStride = Mathf.Max(1, source.SampleStride),
                MaxPrimitives = Mathf.Max(1, source.MaxPrimitives),
                UseComputeAcceleration = source.UseComputeAcceleration,
                UseTiling = source.UseTiling,
                TileSize = Mathf.Max(64, source.TileSize),
                TileOverlap = Mathf.Clamp(source.TileOverlap, 0, 8),
                AutoTileMinDimension = Mathf.Max(64, source.AutoTileMinDimension),
                ColorQuant = source.ColorQuant,
                Contour = source.Contour,
                Potrace = source.Potrace,
                Bezier = source.Bezier,
                Neural = source.Neural,
                Superpixel = source.Superpixel,
                Voronoi = source.Voronoi,
                Gradient = source.Gradient,
                Hybrid = source.Hybrid,
                NeuralHybrid = source.NeuralHybrid,
                Lod = source.Lod
            };
        }

        private void SaveSettings()
        {
            Set("sourceTexture", AssetDatabase.GetAssetPath(sourceTexture));
            Set("outputDirectory", outputDirectory);
            Set("writeFlatPreview", writeFlatPreview.ToString());
            Set("backgroundColor", "#" + ColorUtility.ToHtmlStringRGBA(backgroundColor));
            Set("algorithm", ((int)options.Algorithm).ToString());
            Set("useCompute", options.UseComputeAcceleration.ToString());
            Set("useTiling", options.UseTiling.ToString());
            Set("edgeThreshold", options.EdgeThreshold.ToString(CultureInfo.InvariantCulture));
            Set("minAlpha", options.MinAlpha.ToString(CultureInfo.InvariantCulture));
            Set("sampleStride", options.SampleStride.ToString());
            Set("maxPrimitives", options.MaxPrimitives.ToString());
            Set("tileSize", options.TileSize.ToString());
            Set("tileOverlap", options.TileOverlap.ToString());
            Set("autoTileMin", options.AutoTileMinDimension.ToString());
            Set("colorCount", options.ColorQuant.ColorCount.ToString());
            Set("quantMethod", ((int)options.ColorQuant.Method).ToString());
            Set("simplifyTolerance", options.ColorQuant.SimplifyTolerance.ToString(CultureInfo.InvariantCulture));
        }

        private void LoadSettings()
        {
            sourceTexture = LoadAsset<Texture2D>("sourceTexture");
            outputDirectory = Get("outputDirectory", outputDirectory);
            writeFlatPreview = GetBool("writeFlatPreview", writeFlatPreview);
            var bgHex = Get("backgroundColor", string.Empty);
            if (!string.IsNullOrWhiteSpace(bgHex) && ColorUtility.TryParseHtmlString(bgHex, out var parsedBg))
            {
                backgroundColor = parsedBg;
            }

            options.Algorithm = (RasterVectorizationAlgorithm)GetInt("algorithm", (int)options.Algorithm);
            options.UseComputeAcceleration = GetBool("useCompute", options.UseComputeAcceleration);
            options.UseTiling = GetBool("useTiling", options.UseTiling);
            options.EdgeThreshold = GetFloat("edgeThreshold", options.EdgeThreshold);
            options.MinAlpha = GetFloat("minAlpha", options.MinAlpha);
            options.SampleStride = GetInt("sampleStride", options.SampleStride);
            options.MaxPrimitives = GetInt("maxPrimitives", options.MaxPrimitives);
            options.TileSize = GetInt("tileSize", options.TileSize);
            options.TileOverlap = GetInt("tileOverlap", options.TileOverlap);
            options.AutoTileMinDimension = GetInt("autoTileMin", options.AutoTileMinDimension);
            options.ColorQuant.ColorCount = GetInt("colorCount", options.ColorQuant.ColorCount);
            options.ColorQuant.Method = (RasterColorQuantMethod)GetInt("quantMethod", (int)options.ColorQuant.Method);
            options.ColorQuant.SimplifyTolerance = GetFloat("simplifyTolerance", options.ColorQuant.SimplifyTolerance);
        }

        private static void Set(string key, string value)
            => EditorUserSettings.SetConfigValue(SettingsPrefix + key, value ?? string.Empty);

        private static string Get(string key, string fallback)
            => EditorUserSettings.GetConfigValue(SettingsPrefix + key) ?? fallback;

        private static int GetInt(string key, int fallback)
            => int.TryParse(EditorUserSettings.GetConfigValue(SettingsPrefix + key), out var parsed) ? parsed : fallback;

        private static float GetFloat(string key, float fallback)
            => float.TryParse(
                EditorUserSettings.GetConfigValue(SettingsPrefix + key),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed)
                ? parsed
                : fallback;

        private static bool GetBool(string key, bool fallback)
            => bool.TryParse(EditorUserSettings.GetConfigValue(SettingsPrefix + key), out var parsed) ? parsed : fallback;

        private static T LoadAsset<T>(string key) where T : UnityEngine.Object
        {
            var path = EditorUserSettings.GetConfigValue(SettingsPrefix + key);
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}

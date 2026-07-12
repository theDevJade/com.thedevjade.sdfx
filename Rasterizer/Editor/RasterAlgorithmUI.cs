using System;
using System.IO;
using SDFX.Rasterizer.Inference;
using UnityEditor;
using UnityEngine;

namespace SDFX.Rasterizer.Editor
{
    internal static class RasterAlgorithmUI
    {
        public static void DrawAlgorithmInfo(RasterVectorizationAlgorithm algorithm)
        {
            var info = RasterAlgorithmMetadata.Get(algorithm);
            EditorGUILayout.LabelField(info.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(info.Description, EditorStyles.wordWrappedMiniLabel);
            if (!string.IsNullOrWhiteSpace(info.BestFor))
            {
                EditorGUILayout.LabelField($"Best for: {info.BestFor}", EditorStyles.miniLabel);
            }
        }

        public static void DrawAlgorithmSettings(RasterParsingOptions options)
        {
            switch (options.Algorithm)
            {
                case RasterVectorizationAlgorithm.ColorQuantMarchingSquares:
                case RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf:
                    DrawColorQuantSettings(options.ColorQuant);
                    if (options.Algorithm == RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf)
                    {
                        DrawHybridSettings(options.Hybrid);
                    }
                    break;
                case RasterVectorizationAlgorithm.SuzukiAbeContours:
                    DrawContourSettings(options.Contour);
                    break;
                case RasterVectorizationAlgorithm.PotraceTracing:
                    DrawPotraceSettings(options.Potrace);
                    break;
                case RasterVectorizationAlgorithm.AdaptiveBezierFitting:
                    DrawBezierSettings(options.Bezier);
                    break;
                case RasterVectorizationAlgorithm.NeuralVectorization:
                    DrawNeuralSettings(options.Neural);
                    DrawBezierSettings(options.Bezier);
                    if (!SentisInferenceService.IsAvailable)
                    {
                        EditorGUILayout.HelpBox("Unity Sentis is not available. Neural vectorization requires the Sentis package.", MessageType.Warning);
                    }
                    break;
                case RasterVectorizationAlgorithm.SuperpixelSegmentation:
                    DrawSuperpixelSettings(options.Superpixel);
                    break;
                case RasterVectorizationAlgorithm.VoronoiDelaunay:
                    DrawVoronoiSettings(options.Voronoi);
                    break;
                case RasterVectorizationAlgorithm.GradientEdgeVectorization:
                    DrawGradientSettings(options.Gradient);
                    break;
                case RasterVectorizationAlgorithm.HybridNeuralClassical:
                    DrawNeuralHybridSettings(options.NeuralHybrid);
                    if (!SentisInferenceService.IsAvailable)
                    {
                        EditorGUILayout.HelpBox("Unity Sentis is not available. Hybrid neural vectorization requires the Sentis package.", MessageType.Warning);
                    }
                    break;
                case RasterVectorizationAlgorithm.HybridMultiResolutionLod:
                    DrawLodSettings(options.Lod);
                    break;
            }
        }

        private static void DrawColorQuantSettings(RasterColorQuantOptions options)
        {
            options.ColorCount = EditorGUILayout.IntSlider("Color Count", options.ColorCount, 2, 64);
            options.Method = (RasterColorQuantMethod)EditorGUILayout.EnumPopup("Quant Method", options.Method);
            options.SimplifyTolerance = EditorGUILayout.Slider("Simplify Tolerance", options.SimplifyTolerance, 0.5f, 8f);
            options.MinRegionArea = EditorGUILayout.IntField("Min Region Area", options.MinRegionArea);
        }

        private static void DrawContourSettings(RasterContourOptions options)
        {
            options.ThresholdMode = (RasterThresholdMode)EditorGUILayout.EnumPopup("Threshold Mode", options.ThresholdMode);
            options.TraceHoles = EditorGUILayout.Toggle("Trace Holes", options.TraceHoles);
            options.SimplifyTolerance = EditorGUILayout.Slider("Simplify Tolerance", options.SimplifyTolerance, 0.5f, 8f);
        }

        private static void DrawPotraceSettings(RasterPotraceOptions options)
        {
            options.TurdSize = EditorGUILayout.IntField("Turd Size", options.TurdSize);
            options.AlphaMax = EditorGUILayout.Slider("Alpha Max", options.AlphaMax, 0f, 2f);
            options.OptTolerance = EditorGUILayout.Slider("Opt Tolerance", options.OptTolerance, 0.05f, 2f);
        }

        private static void DrawBezierSettings(RasterBezierOptions options)
        {
            options.MaxError = EditorGUILayout.Slider("Bezier Max Error", options.MaxError, 0.5f, 8f);
            options.CornerAngle = EditorGUILayout.Slider("Corner Angle", options.CornerAngle, 10f, 120f);
            options.MinSegmentLength = EditorGUILayout.Slider("Min Segment Length", options.MinSegmentLength, 1f, 16f);
        }

        private static void DrawNeuralSettings(RasterNeuralOptions options)
        {
            if (SentisInferenceService.IsAvailable)
            {
                EditorGUILayout.HelpBox("Assign a Sentis ModelAsset for neural vectorization.", MessageType.Info);
            }

            options.ModelAssetPath = DrawModelAssetField("Neural Model", options.ModelAssetPath);
            options.ConfidenceThreshold = EditorGUILayout.Slider("Confidence Threshold", options.ConfidenceThreshold, 0f, 1f);
            options.MaxCurves = EditorGUILayout.IntField("Max Curves", options.MaxCurves);
        }

        private static void DrawSuperpixelSettings(RasterSuperpixelOptions options)
        {
            options.SuperpixelCount = EditorGUILayout.IntSlider("Superpixel Count", options.SuperpixelCount, 16, 2048);
            options.Compactness = EditorGUILayout.Slider("Compactness", options.Compactness, 1f, 40f);
            options.MergeThreshold = EditorGUILayout.Slider("Merge Threshold", options.MergeThreshold, 0.01f, 0.5f);
        }

        private static void DrawVoronoiSettings(RasterVoronoiOptions options)
        {
            options.SampleDensity = EditorGUILayout.IntSlider("Sample Density", options.SampleDensity, 1, 16);
            options.MaxCells = EditorGUILayout.IntField("Max Cells", options.MaxCells);
        }

        private static void DrawGradientSettings(RasterGradientOptions options)
        {
            options.OutputMode = (RasterGradientOutputMode)EditorGUILayout.EnumPopup("Output Mode", options.OutputMode);
        }

        private static void DrawHybridSettings(RasterHybridOptions options)
        {
            options.SimplifyTolerance = EditorGUILayout.Slider("Simplify Tolerance", options.SimplifyTolerance, 0.5f, 8f);
            options.MinRegionArea = EditorGUILayout.IntField("Min Region Area", options.MinRegionArea);
        }

        private static void DrawNeuralHybridSettings(RasterNeuralHybridOptions options)
        {
            if (SentisInferenceService.IsAvailable)
            {
                EditorGUILayout.HelpBox("Assign a Sentis segmentation ModelAsset.", MessageType.Info);
            }

            options.SegmentationModelPath = DrawModelAssetField("Segmentation Model", options.SegmentationModelPath);
            options.PerRegionAlgorithm = (RasterPerRegionAlgorithm)EditorGUILayout.EnumPopup("Per-Region Algorithm", options.PerRegionAlgorithm);
            options.MinRegionArea = EditorGUILayout.IntField("Min Region Area", options.MinRegionArea);
        }

        private static void DrawLodSettings(RasterLodOptions options)
        {
            options.BaseAlgorithm = (RasterVectorizationAlgorithm)EditorGUILayout.EnumPopup("Base Algorithm", options.BaseAlgorithm);
        }

        private static string DrawModelAssetField(string label, string path)
        {
            if (!SentisInferenceService.IsAvailable)
            {
                return EditorGUILayout.TextField(label, path);
            }

            var modelAssetType = Type.GetType("Unity.Sentis.ModelAsset, Unity.Sentis");
            if (modelAssetType == null)
            {
                return EditorGUILayout.TextField(label, path);
            }

            UnityEngine.Object current = null;
            if (!string.IsNullOrWhiteSpace(path))
            {
                current = AssetDatabase.LoadAssetAtPath(path, modelAssetType);
                if (current == null)
                {
                    foreach (var candidate in new[]
                    {
                        path,
                        ToProjectAssetPath(path),
                        "Packages/com.thedevjade.sdfx/Rasterizer/Models/Raster/" + Path.GetFileName(path)
                    })
                    {
                        if (string.IsNullOrWhiteSpace(candidate))
                        {
                            continue;
                        }

                        current = AssetDatabase.LoadAssetAtPath(candidate, modelAssetType);
                        if (current != null)
                        {
                            path = candidate;
                            break;
                        }
                    }
                }
            }

            var selected = EditorGUILayout.ObjectField(label, current, modelAssetType, false);
            if (selected == null)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    EditorGUILayout.HelpBox("Model asset path is set but the asset could not be loaded. Reassign a Sentis ModelAsset.", MessageType.Warning);
                }

                return string.Empty;
            }

            return AssetDatabase.GetAssetPath(selected);
        }

        private static string ToProjectAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            path = path.Replace("\\", "/");
            var dataPath = Application.dataPath.Replace("\\", "/");
            if (!path.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                return path.StartsWith("Assets/", StringComparison.Ordinal) ? path : string.Empty;
            }

            return "Assets" + path.Substring(dataPath.Length);
        }
    }
}

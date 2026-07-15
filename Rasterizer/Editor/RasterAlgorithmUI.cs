using System;
using System.IO;
using SDFX.Rasterizer.Inference;
using SDFX.VectorTextureCompiler.Core.Localization;
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
                EditorGUILayout.LabelField(SdfxLanguage.Rasterizer.BestForLabel(info.BestFor), EditorStyles.miniLabel);
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
                        EditorGUILayout.HelpBox(SdfxLanguage.Rasterizer.SentisUnavailableNeural, MessageType.Warning);
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
                        EditorGUILayout.HelpBox(SdfxLanguage.Rasterizer.SentisUnavailableHybrid, MessageType.Warning);
                    }
                    break;
                case RasterVectorizationAlgorithm.HybridMultiResolutionLod:
                    DrawLodSettings(options.Lod);
                    break;
            }
        }

        private static void DrawColorQuantSettings(RasterColorQuantOptions options)
        {
            options.ColorCount = EditorGUILayout.IntSlider(SdfxLanguage.Rasterizer.ColorCountField, options.ColorCount, 2, 64);
            options.Method = (RasterColorQuantMethod)EditorGUILayout.EnumPopup(SdfxLanguage.Rasterizer.QuantMethodField, options.Method);
            options.SimplifyTolerance = EditorGUILayout.Slider(
                SdfxLanguage.Rasterizer.SimplifyToleranceField,
                options.SimplifyTolerance,
                0.5f,
                8f);
            options.MinRegionArea = EditorGUILayout.IntField(SdfxLanguage.Rasterizer.MinRegionAreaField, options.MinRegionArea);
        }

        private static void DrawContourSettings(RasterContourOptions options)
        {
            options.ThresholdMode = (RasterThresholdMode)EditorGUILayout.EnumPopup(
                SdfxLanguage.Rasterizer.ThresholdModeField,
                options.ThresholdMode);
            options.TraceHoles = EditorGUILayout.Toggle(SdfxLanguage.Rasterizer.TraceHolesField, options.TraceHoles);
            options.SimplifyTolerance = EditorGUILayout.Slider(
                SdfxLanguage.Rasterizer.SimplifyToleranceField,
                options.SimplifyTolerance,
                0.5f,
                8f);
        }

        private static void DrawPotraceSettings(RasterPotraceOptions options)
        {
            options.TurdSize = EditorGUILayout.IntField(SdfxLanguage.Rasterizer.TurdSizeField, options.TurdSize);
            options.AlphaMax = EditorGUILayout.Slider(SdfxLanguage.Rasterizer.AlphaMaxField, options.AlphaMax, 0f, 2f);
            options.OptTolerance = EditorGUILayout.Slider(SdfxLanguage.Rasterizer.OptToleranceField, options.OptTolerance, 0.05f, 2f);
        }

        private static void DrawBezierSettings(RasterBezierOptions options)
        {
            options.MaxError = EditorGUILayout.Slider(SdfxLanguage.Rasterizer.BezierMaxErrorField, options.MaxError, 0.5f, 8f);
            options.CornerAngle = EditorGUILayout.Slider(SdfxLanguage.Rasterizer.CornerAngleField, options.CornerAngle, 10f, 120f);
            options.MinSegmentLength = EditorGUILayout.Slider(
                SdfxLanguage.Rasterizer.MinSegmentLengthField,
                options.MinSegmentLength,
                1f,
                16f);
        }

        private static void DrawNeuralSettings(RasterNeuralOptions options)
        {
            if (SentisInferenceService.IsAvailable)
            {
                EditorGUILayout.HelpBox(SdfxLanguage.Rasterizer.AssignNeuralModelHelp, MessageType.Info);
            }

            options.ModelAssetPath = DrawModelAssetField(SdfxLanguage.Rasterizer.NeuralModelField, options.ModelAssetPath);
            options.ConfidenceThreshold = EditorGUILayout.Slider(
                SdfxLanguage.Rasterizer.ConfidenceThresholdField,
                options.ConfidenceThreshold,
                0f,
                1f);
            options.MaxCurves = EditorGUILayout.IntField(SdfxLanguage.Rasterizer.MaxCurvesField, options.MaxCurves);
        }

        private static void DrawSuperpixelSettings(RasterSuperpixelOptions options)
        {
            options.SuperpixelCount = EditorGUILayout.IntSlider(
                SdfxLanguage.Rasterizer.SuperpixelCountField,
                options.SuperpixelCount,
                16,
                2048);
            options.Compactness = EditorGUILayout.Slider(SdfxLanguage.Rasterizer.CompactnessField, options.Compactness, 1f, 40f);
            options.MergeThreshold = EditorGUILayout.Slider(
                SdfxLanguage.Rasterizer.MergeThresholdField,
                options.MergeThreshold,
                0.01f,
                0.5f);
        }

        private static void DrawVoronoiSettings(RasterVoronoiOptions options)
        {
            options.SampleDensity = EditorGUILayout.IntSlider(
                SdfxLanguage.Rasterizer.SampleDensityField,
                options.SampleDensity,
                1,
                16);
            options.MaxCells = EditorGUILayout.IntField(SdfxLanguage.Rasterizer.MaxCellsField, options.MaxCells);
        }

        private static void DrawGradientSettings(RasterGradientOptions options)
        {
            options.OutputMode = (RasterGradientOutputMode)EditorGUILayout.EnumPopup(
                SdfxLanguage.Rasterizer.OutputModeField,
                options.OutputMode);
        }

        private static void DrawHybridSettings(RasterHybridOptions options)
        {
            options.SimplifyTolerance = EditorGUILayout.Slider(
                SdfxLanguage.Rasterizer.SimplifyToleranceField,
                options.SimplifyTolerance,
                0.5f,
                8f);
            options.MinRegionArea = EditorGUILayout.IntField(SdfxLanguage.Rasterizer.MinRegionAreaField, options.MinRegionArea);
        }

        private static void DrawNeuralHybridSettings(RasterNeuralHybridOptions options)
        {
            if (SentisInferenceService.IsAvailable)
            {
                EditorGUILayout.HelpBox(SdfxLanguage.Rasterizer.AssignSegmentationModelHelp, MessageType.Info);
            }

            options.SegmentationModelPath = DrawModelAssetField(
                SdfxLanguage.Rasterizer.SegmentationModelField,
                options.SegmentationModelPath);
            options.PerRegionAlgorithm = (RasterPerRegionAlgorithm)EditorGUILayout.EnumPopup(
                SdfxLanguage.Rasterizer.PerRegionAlgorithmField,
                options.PerRegionAlgorithm);
            options.MinRegionArea = EditorGUILayout.IntField(SdfxLanguage.Rasterizer.MinRegionAreaField, options.MinRegionArea);
        }

        private static void DrawLodSettings(RasterLodOptions options)
        {
            options.BaseAlgorithm = (RasterVectorizationAlgorithm)EditorGUILayout.EnumPopup(
                SdfxLanguage.Rasterizer.BaseAlgorithmField,
                options.BaseAlgorithm);
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
                    EditorGUILayout.HelpBox(SdfxLanguage.Rasterizer.ModelLoadFailedWarning, MessageType.Warning);
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

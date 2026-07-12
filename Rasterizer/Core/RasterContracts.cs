using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    public enum RasterTracingMode
    {
        Edges = 0,
        Strokes = 1,
        Contours = 2
    }

    public enum RasterVectorizationAlgorithm
    {
        ColorQuantMarchingSquares = 0,
        SuzukiAbeContours = 1,
        PotraceTracing = 2,
        AdaptiveBezierFitting = 3,
        NeuralVectorization = 4,
        SuperpixelSegmentation = 5,
        VoronoiDelaunay = 6,
        GradientEdgeVectorization = 7,
        HybridSegmentContourBezierSdf = 8,
        HybridNeuralClassical = 9,
        HybridMultiResolutionLod = 10
    }

    public enum RasterColorQuantMethod
    {
        MedianCut = 0,
        KMeans = 1,
        Octree = 2
    }

    public enum RasterThresholdMode
    {
        Alpha = 0,
        Luma = 1,
        Quantized = 2
    }

    public enum RasterGradientOutputMode
    {
        Stamp = 0,
        Chain = 1,
        Bezier = 2
    }

    public enum RasterPerRegionAlgorithm
    {
        SuzukiAbeContours = 0,
        PotraceTracing = 1
    }

    public enum RasterIssueSeverity
    {
        Warning = 0,
        Error = 1
    }

    public enum RasterIssueCode
    {
        Unknown = 0,
        InvalidGeometry = 4,
        InvalidInput = 5,
        UnsupportedRasterMode = 20,
        RasterComputeUnavailable = 21,
        RasterInferenceUnavailable = 22,
        RasterModelLoadFailed = 23,
        RasterModelActive = 24,
        RasterUsingFallbackSegmentation = 25,
        PathDetailReduced = 15
    }

    public readonly struct RasterIssue
    {
        public RasterIssue(RasterIssueSeverity severity, string message, string elementName = "raster", int lineNumber = 0, RasterIssueCode code = RasterIssueCode.Unknown)
        {
            Severity = severity;
            Message = message ?? string.Empty;
            ElementName = elementName ?? "raster";
            LineNumber = lineNumber;
            Code = code;
        }

        public RasterIssueSeverity Severity { get; }
        public string Message { get; }
        public string ElementName { get; }
        public int LineNumber { get; }
        public RasterIssueCode Code { get; }
    }

    public enum RasterStrictness
    {
        Strict = 0,
        Permissive = 1
    }

    [Serializable]
    public sealed class RasterColorQuantOptions
    {
        public int ColorCount = 32;
        public RasterColorQuantMethod Method = RasterColorQuantMethod.MedianCut;
        public float SimplifyTolerance = 0.5f;
        public int MinRegionArea = 12;
        public float SoftOverlayAlpha = 0f;
    }

    [Serializable]
    public sealed class RasterContourOptions
    {
        public RasterThresholdMode ThresholdMode = RasterThresholdMode.Alpha;
        public bool TraceHoles = true;
        public float SimplifyTolerance = 1.5f;
    }

    [Serializable]
    public sealed class RasterPotraceOptions
    {
        public int TurdSize = 2;
        public float AlphaMax = 1f;
        public float OptTolerance = 0.2f;
    }

    [Serializable]
    public sealed class RasterBezierOptions
    {
        public float MaxError = 2f;
        public float CornerAngle = 45f;
        public float MinSegmentLength = 2f;
    }

    [Serializable]
    public sealed class RasterNeuralOptions
    {
        public string ModelAssetPath = string.Empty;
        public float ConfidenceThreshold = 0.5f;
        public int MaxCurves = 256;
    }

    [Serializable]
    public sealed class RasterSuperpixelOptions
    {
        public int SuperpixelCount = 256;
        public float Compactness = 10f;
        public float MergeThreshold = 0.08f;
    }

    [Serializable]
    public sealed class RasterVoronoiOptions
    {
        public int SampleDensity = 4;
        public int MaxCells = 8192;
    }

    [Serializable]
    public sealed class RasterGradientOptions
    {
        public RasterGradientOutputMode OutputMode = RasterGradientOutputMode.Stamp;
    }

    [Serializable]
    public sealed class RasterHybridOptions
    {
        public bool UseBezierFit = false;
        public float SimplifyTolerance = 0.5f;
        public float BezierMaxError = 2f;
        public int MinRegionArea = 12;
        public float SoftOverlayAlpha = 0f;
    }

    [Serializable]
    public sealed class RasterNeuralHybridOptions
    {
        public string SegmentationModelPath = string.Empty;
        public RasterPerRegionAlgorithm PerRegionAlgorithm = RasterPerRegionAlgorithm.SuzukiAbeContours;
        public int MinRegionArea = 16;
    }

    [Serializable]
    public sealed class RasterLodOptions
    {
        public int LodLevels = 3;
        public float ErrorThreshold = 2f;
        public RasterVectorizationAlgorithm BaseAlgorithm = RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf;
    }

    [Serializable]
    public sealed class RasterParsingOptions
    {
        public RasterVectorizationAlgorithm Algorithm = RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf;

        [Obsolete("Use Algorithm instead.")]
        public RasterTracingMode TracingMode = RasterTracingMode.Edges;

        public float EdgeThreshold = 0.15f;
        public float MinAlpha = 0.05f;
        public int SampleStride = 1;
        public int MaxPrimitives = 120000;
        public bool UseComputeAcceleration = true;
        public bool UseTiling = true;
        public int TileSize = 1024;
        public int TileOverlap = 1;
        public int AutoTileMinDimension = 2048;
        public int MaxPathEdgesPerPrimitive = 512;
        public RasterStrictness Strictness = RasterStrictness.Permissive;

        public RasterColorQuantOptions ColorQuant = new RasterColorQuantOptions();
        public RasterContourOptions Contour = new RasterContourOptions();
        public RasterPotraceOptions Potrace = new RasterPotraceOptions();
        public RasterBezierOptions Bezier = new RasterBezierOptions();
        public RasterNeuralOptions Neural = new RasterNeuralOptions();
        public RasterSuperpixelOptions Superpixel = new RasterSuperpixelOptions();
        public RasterVoronoiOptions Voronoi = new RasterVoronoiOptions();
        public RasterGradientOptions Gradient = new RasterGradientOptions();
        public RasterHybridOptions Hybrid = new RasterHybridOptions();
        public RasterNeuralHybridOptions NeuralHybrid = new RasterNeuralHybridOptions();
        public RasterLodOptions Lod = new RasterLodOptions();

        public static RasterVectorizationAlgorithm MigrateTracingMode(RasterTracingMode mode)
        {
            return mode switch
            {
                RasterTracingMode.Strokes => RasterVectorizationAlgorithm.SuzukiAbeContours,
                RasterTracingMode.Contours => RasterVectorizationAlgorithm.SuzukiAbeContours,
                _ => RasterVectorizationAlgorithm.GradientEdgeVectorization
            };
        }
    }

    public readonly struct RasterToSvgResult
    {
        public RasterToSvgResult(bool success, string svgText, string svgFilePath, Texture2D overlayPreview, List<RasterIssue> issues, int pathCount)
        {
            Success = success;
            SvgText = svgText ?? string.Empty;
            SvgFilePath = svgFilePath ?? string.Empty;
            OverlayPreview = overlayPreview;
            Issues = issues ?? new List<RasterIssue>();
            PathCount = pathCount;
        }

        public bool Success { get; }
        public string SvgText { get; }
        public string SvgFilePath { get; }
        public Texture2D OverlayPreview { get; }
        public List<RasterIssue> Issues { get; }
        public int PathCount { get; }

        public bool HasErrors
        {
            get
            {
                for (var i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i].Severity == RasterIssueSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

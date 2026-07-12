using System;
using System.Collections.Generic;
using SDFX.Rasterizer;
using UnityEditor;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class RasterGpuPreprocessor
    {
        public static RasterGpuBuffers PreprocessSync(
            RasterVectorizationAlgorithm algorithm,
            RasterImageBuffer image,
            RasterParsingOptions options,
            List<RasterIssue> issues,
            Action<float, string> reportProgress = null,
            int paletteSampleLimit = 0)
        {
            if (!options.UseComputeAcceleration || !RasterComputeService.IsSupported)
            {
                return null;
            }

            RenderTexture sourceRt = null;
            RenderTexture edgeRt = null;
            RenderTexture maskRt = null;
            RenderTexture labelRt = null;
            RenderTexture nearestRt = null;
            RenderTexture previewRt = null;
            try
            {
                reportProgress?.Invoke(0.05f, "Working...");
                sourceRt = RasterComputeService.CreateSourceRt(image);
                var width = image.Width;
                var height = image.Height;
                var buffers = new RasterGpuBuffers();

                if (NeedsEdgeMap(algorithm))
                {
                    reportProgress?.Invoke(0.15f, "Working...");
                    edgeRt = RasterComputeService.CreateFloatRt(width, height);
                    RasterComputeService.RunEdgeDetect(sourceRt, edgeRt, width, height);
                    buffers.EdgeMap = RasterComputeService.ReadFloatPixelsSync(edgeRt, width, height);
                }

                if (NeedsEdgeMask(algorithm) && edgeRt != null)
                {
                    reportProgress?.Invoke(0.25f, "Working...");
                    maskRt = RasterComputeService.CreateMaskRt(width, height);
                    RasterComputeService.RunEdgeMask(sourceRt, edgeRt, maskRt, width, height, options.EdgeThreshold, options.MinAlpha);
                    buffers.EdgeMask = RasterComputeService.ReadMaskPixelsSync(maskRt, width, height);
                }

                if (NeedsQuantLabels(algorithm))
                {
                    reportProgress?.Invoke(0.2f, "Working...");
                    buffers.QuantPalette = ColorQuantizer.BuildPalette(image, options.ColorQuant, options.MinAlpha, paletteSampleLimit);
                    labelRt = RasterComputeService.CreateFloatRt(width, height);
                    RasterComputeService.RunColorQuantAssign(sourceRt, labelRt, width, height, buffers.QuantPalette, options.MinAlpha);
                    buffers.QuantLabels = RasterComputeService.ReadLabelPixelsSync(labelRt, width, height);
                }

                if (NeedsBinaryMask(algorithm))
                {
                    reportProgress?.Invoke(0.3f, "Working...");
                    if (maskRt == null)
                    {
                        maskRt = RasterComputeService.CreateMaskRt(width, height);
                    }

                    var thresholdMode = algorithm == RasterVectorizationAlgorithm.SuzukiAbeContours
                        ? options.Contour.ThresholdMode
                        : RasterThresholdMode.Alpha;
                    RasterComputeService.RunThresholdMask(
                        sourceRt,
                        maskRt,
                        width,
                        height,
                        thresholdMode,
                        options.MinAlpha,
                        labelRt);
                    buffers.BinaryMask = RasterComputeService.ReadMaskPixelsSync(maskRt, width, height);
                }

                if (algorithm == RasterVectorizationAlgorithm.SuperpixelSegmentation)
                {
                    reportProgress?.Invoke(0.35f, "Working...");
                    buffers.SuperpixelLabels = RunSuperpixelGpu(sourceRt, image, options, labelRt);
                }

                if (algorithm == RasterVectorizationAlgorithm.VoronoiDelaunay && buffers.EdgeMap != null)
                {
                    reportProgress?.Invoke(0.35f, "Working...");
                    var sites = VoronoiMeshBuilder.SampleSites(image, buffers.EdgeMap, options.Voronoi.SampleDensity, options.MinAlpha, options.EdgeThreshold);
                    if (sites.Count > 0)
                    {
                        nearestRt = RasterComputeService.CreateFloatRt(width, height);
                        RasterComputeService.RunVoronoiNearest(sourceRt, nearestRt, width, height, sites, options.MinAlpha);
                        buffers.VoronoiNearest = RasterComputeService.ReadLabelPixelsSync(nearestRt, width, height);
                    }
                }

                if (NeedsEdgePreview(algorithm) && edgeRt != null)
                {
                    reportProgress?.Invoke(0.4f, "Working...");
                    previewRt = RasterComputeService.CreatePreviewRt(width, height);
                    RasterComputeService.RunEdgePreviewTint(sourceRt, edgeRt, previewRt, width, height, options.EdgeThreshold, options.MinAlpha);
                    buffers.EdgePreviewPixels = RasterComputeService.ReadColorPixelsSync(previewRt, width, height);
                }

                reportProgress?.Invoke(0.45f, "Working...");
                return buffers;
            }
            catch
            {
                issues?.Add(new RasterIssue(
                    RasterIssueSeverity.Warning,
                    "Raster compute unavailable; using CPU fallback.",
                    "raster",
                    0,
                    RasterIssueCode.RasterComputeUnavailable));
                return null;
            }
            finally
            {
                RasterComputeService.ReleaseRt(ref previewRt);
                RasterComputeService.ReleaseRt(ref nearestRt);
                RasterComputeService.ReleaseRt(ref labelRt);
                RasterComputeService.ReleaseRt(ref maskRt);
                RasterComputeService.ReleaseRt(ref edgeRt);
                RasterComputeService.ReleaseRt(ref sourceRt);
            }
        }

        public static void PreprocessAsync(
            RasterVectorizationAlgorithm algorithm,
            RasterImageBuffer image,
            RasterParsingOptions options,
            List<RasterIssue> issues,
            Action<RasterGpuBuffers> onComplete,
            Action<float, string> reportProgress = null,
            int paletteSampleLimit = 0,
            Func<bool> shouldCancel = null)
        {
            if (!options.UseComputeAcceleration || !RasterComputeService.IsSupported)
            {
                onComplete?.Invoke(null);
                return;
            }

            if (shouldCancel?.Invoke() == true)
            {
                onComplete?.Invoke(null);
                return;
            }

            reportProgress?.Invoke(0.05f, "Working...");
            RenderTexture sourceRt = null;
            RenderTexture edgeRt = null;
            try
            {
                sourceRt = RasterComputeService.CreateSourceRt(image);
                var width = image.Width;
                var height = image.Height;
                var buffers = new RasterGpuBuffers();

                if (!NeedsEdgeMap(algorithm))
                {
                    EditorApplication.delayCall += () => FinishNonEdgeAsync(
                        algorithm,
                        image,
                        options,
                        issues,
                        sourceRt,
                        buffers,
                        onComplete,
                        reportProgress,
                        paletteSampleLimit,
                        shouldCancel);
                    return;
                }

                reportProgress?.Invoke(0.12f, "Working...");
                edgeRt = RasterComputeService.CreateFloatRt(width, height);
                RasterComputeService.RunEdgeDetect(sourceRt, edgeRt, width, height);
                RasterComputeService.RequestFloatReadback(edgeRt, width, height, edgeValues =>
                {
                    if (shouldCancel?.Invoke() == true)
                    {
                        RasterComputeService.ReleaseRt(ref edgeRt);
                        RasterComputeService.ReleaseRt(ref sourceRt);
                        onComplete?.Invoke(null);
                        return;
                    }

                    buffers.EdgeMap = edgeValues;
                    FinishAfterEdgeAsync(algorithm, image, options, issues, sourceRt, edgeRt, buffers, onComplete, reportProgress, paletteSampleLimit, shouldCancel);
                }, () =>
                {
                    issues?.Add(WarningIssue());
                    RasterComputeService.ReleaseRt(ref edgeRt);
                    RasterComputeService.ReleaseRt(ref sourceRt);
                    onComplete?.Invoke(null);
                });
            }
            catch
            {
                issues?.Add(WarningIssue());
                RasterComputeService.ReleaseRt(ref edgeRt);
                RasterComputeService.ReleaseRt(ref sourceRt);
                onComplete?.Invoke(null);
            }
        }

        private static bool AbortIfCancelled(
            Func<bool> shouldCancel,
            RenderTexture sourceRt,
            RenderTexture edgeRt,
            RenderTexture maskRt,
            RenderTexture labelRt,
            RenderTexture nearestRt,
            RenderTexture previewRt,
            Action<RasterGpuBuffers> onComplete)
        {
            if (shouldCancel?.Invoke() != true)
            {
                return false;
            }

            CleanupFinish(sourceRt, edgeRt, maskRt, labelRt, nearestRt, previewRt);
            onComplete?.Invoke(null);
            return true;
        }

        private static void FinishAfterEdgeAsync(
            RasterVectorizationAlgorithm algorithm,
            RasterImageBuffer image,
            RasterParsingOptions options,
            List<RasterIssue> issues,
            RenderTexture sourceRt,
            RenderTexture edgeRt,
            RasterGpuBuffers buffers,
            Action<RasterGpuBuffers> onComplete,
            Action<float, string> reportProgress,
            int paletteSampleLimit,
            Func<bool> shouldCancel)
        {
            if (shouldCancel?.Invoke() == true)
            {
                CleanupFinish(sourceRt, edgeRt, null, null, null, null);
                onComplete?.Invoke(null);
                return;
            }

            var width = image.Width;
            var height = image.Height;
            RenderTexture maskRt = null;
            RenderTexture labelRt = null;
            RenderTexture nearestRt = null;
            RenderTexture previewRt = null;
            try
            {
                if (NeedsEdgeMask(algorithm))
                {
                    reportProgress?.Invoke(0.22f, "Working...");
                    maskRt = RasterComputeService.CreateMaskRt(width, height);
                    RasterComputeService.RunEdgeMask(sourceRt, edgeRt, maskRt, width, height, options.EdgeThreshold, options.MinAlpha);
                    buffers.EdgeMask = RasterComputeService.ReadMaskPixelsSync(maskRt, width, height);
                }

                if (AbortIfCancelled(shouldCancel, sourceRt, edgeRt, maskRt, labelRt, nearestRt, previewRt, onComplete))
                {
                    return;
                }

                if (NeedsQuantLabels(algorithm))
                {
                    reportProgress?.Invoke(0.28f, "Working...");
                    buffers.QuantPalette = ColorQuantizer.BuildPalette(image, options.ColorQuant, options.MinAlpha, paletteSampleLimit);
                    labelRt = RasterComputeService.CreateFloatRt(width, height);
                    RasterComputeService.RunColorQuantAssign(sourceRt, labelRt, width, height, buffers.QuantPalette, options.MinAlpha);
                    buffers.QuantLabels = RasterComputeService.ReadLabelPixelsSync(labelRt, width, height);
                }

                if (AbortIfCancelled(shouldCancel, sourceRt, edgeRt, maskRt, labelRt, nearestRt, previewRt, onComplete))
                {
                    return;
                }

                if (NeedsBinaryMask(algorithm))
                {
                    reportProgress?.Invoke(0.32f, "Working...");
                    if (maskRt == null)
                    {
                        maskRt = RasterComputeService.CreateMaskRt(width, height);
                    }

                    var thresholdMode = algorithm == RasterVectorizationAlgorithm.SuzukiAbeContours
                        ? options.Contour.ThresholdMode
                        : RasterThresholdMode.Alpha;
                    RasterComputeService.RunThresholdMask(sourceRt, maskRt, width, height, thresholdMode, options.MinAlpha, labelRt);
                    buffers.BinaryMask = RasterComputeService.ReadMaskPixelsSync(maskRt, width, height);
                }

                if (algorithm == RasterVectorizationAlgorithm.SuperpixelSegmentation)
                {
                    reportProgress?.Invoke(0.36f, "Working...");
                    buffers.SuperpixelLabels = RunSuperpixelGpu(sourceRt, image, options, labelRt);
                }

                if (algorithm == RasterVectorizationAlgorithm.VoronoiDelaunay)
                {
                    reportProgress?.Invoke(0.36f, "Working...");
                    var sites = VoronoiMeshBuilder.SampleSites(image, buffers.EdgeMap, options.Voronoi.SampleDensity, options.MinAlpha, options.EdgeThreshold);
                    if (sites.Count > 0)
                    {
                        nearestRt = RasterComputeService.CreateFloatRt(width, height);
                        RasterComputeService.RunVoronoiNearest(sourceRt, nearestRt, width, height, sites, options.MinAlpha);
                        buffers.VoronoiNearest = RasterComputeService.ReadLabelPixelsSync(nearestRt, width, height);
                    }
                }

                if (NeedsEdgePreview(algorithm))
                {
                    reportProgress?.Invoke(0.4f, "Working...");
                    previewRt = RasterComputeService.CreatePreviewRt(width, height);
                    RasterComputeService.RunEdgePreviewTint(sourceRt, edgeRt, previewRt, width, height, options.EdgeThreshold, options.MinAlpha);
                    RasterComputeService.RequestColorReadback(previewRt, width, height, pixels =>
                    {
                        if (shouldCancel?.Invoke() == true)
                        {
                            CleanupFinish(sourceRt, edgeRt, maskRt, labelRt, nearestRt, previewRt);
                            onComplete?.Invoke(null);
                            return;
                        }

                        buffers.EdgePreviewPixels = pixels;
                        reportProgress?.Invoke(0.45f, "Working...");
                        CleanupFinish(sourceRt, edgeRt, maskRt, labelRt, nearestRt, previewRt);
                        onComplete?.Invoke(buffers);
                    }, () =>
                    {
                        issues?.Add(WarningIssue());
                        CleanupFinish(sourceRt, edgeRt, maskRt, labelRt, nearestRt, previewRt);
                        onComplete?.Invoke(buffers.EdgeMap != null ? buffers : null);
                    });
                    return;
                }

                reportProgress?.Invoke(0.45f, "Working...");
                CleanupFinish(sourceRt, edgeRt, maskRt, labelRt, nearestRt, previewRt);
                onComplete?.Invoke(buffers);
            }
            catch
            {
                issues?.Add(WarningIssue());
                CleanupFinish(sourceRt, edgeRt, maskRt, labelRt, nearestRt, previewRt);
                onComplete?.Invoke(null);
            }
        }

        private static void FinishNonEdgeAsync(
            RasterVectorizationAlgorithm algorithm,
            RasterImageBuffer image,
            RasterParsingOptions options,
            List<RasterIssue> issues,
            RenderTexture sourceRt,
            RasterGpuBuffers buffers,
            Action<RasterGpuBuffers> onComplete,
            Action<float, string> reportProgress,
            int paletteSampleLimit,
            Func<bool> shouldCancel)
        {
            RenderTexture maskRt = null;
            RenderTexture labelRt = null;
            try
            {
                if (NeedsQuantLabels(algorithm))
                {
                    reportProgress?.Invoke(0.2f, "Working...");
                    buffers.QuantPalette = ColorQuantizer.BuildPalette(image, options.ColorQuant, options.MinAlpha, paletteSampleLimit);
                    labelRt = RasterComputeService.CreateFloatRt(image.Width, image.Height);
                    RasterComputeService.RunColorQuantAssign(sourceRt, labelRt, image.Width, image.Height, buffers.QuantPalette, options.MinAlpha);
                    buffers.QuantLabels = RasterComputeService.ReadLabelPixelsSync(labelRt, image.Width, image.Height);
                }

                if (AbortIfCancelled(shouldCancel, sourceRt, null, maskRt, labelRt, null, null, onComplete))
                {
                    return;
                }

                if (NeedsBinaryMask(algorithm))
                {
                    reportProgress?.Invoke(0.3f, "Working...");
                    maskRt = RasterComputeService.CreateMaskRt(image.Width, image.Height);
                    var thresholdMode = algorithm == RasterVectorizationAlgorithm.SuzukiAbeContours
                        ? options.Contour.ThresholdMode
                        : RasterThresholdMode.Alpha;
                    RasterComputeService.RunThresholdMask(sourceRt, maskRt, image.Width, image.Height, thresholdMode, options.MinAlpha, labelRt);
                    buffers.BinaryMask = RasterComputeService.ReadMaskPixelsSync(maskRt, image.Width, image.Height);
                }

                if (algorithm == RasterVectorizationAlgorithm.SuperpixelSegmentation)
                {
                    reportProgress?.Invoke(0.35f, "Working...");
                    buffers.SuperpixelLabels = RunSuperpixelGpu(sourceRt, image, options, labelRt);
                }

                reportProgress?.Invoke(0.45f, "Working...");
                CleanupFinish(sourceRt, null, maskRt, labelRt, null, null);
                onComplete?.Invoke(buffers);
            }
            catch
            {
                issues?.Add(WarningIssue());
                CleanupFinish(sourceRt, null, maskRt, labelRt, null, null);
                onComplete?.Invoke(null);
            }
        }

        private static int[] RunSuperpixelGpu(RenderTexture sourceRt, RasterImageBuffer image, RasterParsingOptions options, RenderTexture labelRt)
        {
            var width = image.Width;
            var height = image.Height;
            if (labelRt == null)
            {
                labelRt = RasterComputeService.CreateFloatRt(width, height);
            }

            SuperpixelSegmenter.BuildInitialCenters(image, options.Superpixel, options.MinAlpha, out var positions, out var colors);
            if (positions.Count == 0)
            {
                return new int[image.Pixels.Length];
            }

            int[] labels = null;
            for (var iter = 0; iter < 5; iter++)
            {
                RasterComputeService.RunSuperpixelAssign(sourceRt, labelRt, width, height, positions, colors, options.Superpixel.Compactness, options.MinAlpha);
                labels = RasterComputeService.ReadLabelPixelsSync(labelRt, width, height);
                SuperpixelSegmenter.UpdateCenters(image, labels, positions, colors, options.MinAlpha);
            }

            return SuperpixelSegmenter.RelabelConnected(labels);
        }

        private static void CleanupFinish(
            RenderTexture sourceRt,
            RenderTexture edgeRt,
            RenderTexture maskRt,
            RenderTexture labelRt,
            RenderTexture nearestRt,
            RenderTexture previewRt)
        {
            RasterComputeService.ReleaseRt(ref previewRt);
            RasterComputeService.ReleaseRt(ref nearestRt);
            RasterComputeService.ReleaseRt(ref labelRt);
            RasterComputeService.ReleaseRt(ref maskRt);
            RasterComputeService.ReleaseRt(ref edgeRt);
            RasterComputeService.ReleaseRt(ref sourceRt);
        }

        private static RasterIssue WarningIssue()
        {
            return new RasterIssue(
                RasterIssueSeverity.Warning,
                "Raster compute unavailable; using CPU fallback.",
                "raster",
                0,
                RasterIssueCode.RasterComputeUnavailable);
        }

        private static bool NeedsEdgeMap(RasterVectorizationAlgorithm algorithm)
        {
            return algorithm is RasterVectorizationAlgorithm.GradientEdgeVectorization
                or RasterVectorizationAlgorithm.AdaptiveBezierFitting
                or RasterVectorizationAlgorithm.VoronoiDelaunay;
        }

        private static bool NeedsEdgeMask(RasterVectorizationAlgorithm algorithm)
        {
            return algorithm is RasterVectorizationAlgorithm.GradientEdgeVectorization
                or RasterVectorizationAlgorithm.AdaptiveBezierFitting;
        }

        private static bool NeedsEdgePreview(RasterVectorizationAlgorithm algorithm)
        {
            return algorithm is RasterVectorizationAlgorithm.GradientEdgeVectorization
                or RasterVectorizationAlgorithm.AdaptiveBezierFitting
                or RasterVectorizationAlgorithm.VoronoiDelaunay;
        }

        private static bool NeedsQuantLabels(RasterVectorizationAlgorithm algorithm)
        {
            return algorithm is RasterVectorizationAlgorithm.ColorQuantMarchingSquares
                or RasterVectorizationAlgorithm.SuzukiAbeContours
                or RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf
                or RasterVectorizationAlgorithm.HybridNeuralClassical
                or RasterVectorizationAlgorithm.NeuralVectorization;
        }

        private static bool NeedsBinaryMask(RasterVectorizationAlgorithm algorithm)
        {
            return algorithm is RasterVectorizationAlgorithm.SuzukiAbeContours
                or RasterVectorizationAlgorithm.PotraceTracing;
        }
    }
}

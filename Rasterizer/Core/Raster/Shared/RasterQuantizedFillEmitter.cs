using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class RasterQuantizedFillEmitter
    {
        public static Color32[] Emit(
            RasterVectorizerContext ctx,
            int[] paletteLabels,
            Color32[] palette,
            float simplifyTolerance,
            bool useBezierFit,
            float bezierMaxError,
            int minRegionArea = 24,
            float overlayAlpha = 0f)
        {
            _ = overlayAlpha;
            var image = ctx.Image;
            var width = image.Width;
            var height = image.Height;
            var imageArea = Mathf.Max(1, width * height);
            var preview = RasterContourPainter.CreateTransparent(width, height);

            ConnectedComponents.MajorityFilter(paletteLabels, width, height, iterations: 1);
            var absorbArea = Mathf.Max(4, minRegionArea);
            ConnectedComponents.AbsorbSmallRegions(paletteLabels, width, height, absorbArea);
            ConnectedComponents.AbsorbFringesAndSpeckles(
                paletteLabels,
                palette,
                width,
                height,
                maxArea: Mathf.Max(absorbArea * 2, imageArea / 2000),
                absorbDarkSpeckles: false);

            var backgroundLabel = ConnectedComponents.FindBorderBackgroundLabel(paletteLabels, width, height);

            // Background regions are intentionally not emitted to SVG (Unity composites
            // them as transparent). Prefill the preview with that color when it is
            // opaque so the editor preview reads like the Unity result instead of
            // showing black holes where background-labeled regions were skipped.
            if (backgroundLabel >= 0 && backgroundLabel < palette.Length)
            {
                var bg = palette[backgroundLabel];
                if (bg.a > 8)
                {
                    bg.a = 255;
                    RasterContourPainter.Fill(preview, bg);
                }
            }

            var components = ConnectedComponents.LabelSameColorRegions(paletteLabels, width, height);
            var boundaries = MarchingSquares.ExtractAllBoundaries(components, width, height);
            if (boundaries.Count == 0)
            {
                return preview;
            }

            var componentColor = new Dictionary<int, int>(boundaries.Count);
            var componentArea = new Dictionary<int, int>(boundaries.Count);
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component <= 0)
                {
                    continue;
                }

                if (!componentColor.ContainsKey(component))
                {
                    componentColor[component] = paletteLabels[i];
                }

                componentArea.TryGetValue(component, out var area);
                componentArea[component] = area + 1;
            }

            var ordered = new List<int>(boundaries.Keys);
            ordered.Sort((a, b) =>
            {
                componentArea.TryGetValue(a, out var areaA);
                componentArea.TryGetValue(b, out var areaB);
                var cmp = areaB.CompareTo(areaA);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            for (var i = 0; i < ordered.Count; i++)
            {
                var component = ordered[i];
                if (!componentColor.TryGetValue(component, out var colorLabel))
                {
                    continue;
                }

                if (colorLabel == backgroundLabel)
                {
                    continue;
                }

                componentArea.TryGetValue(component, out var area);
                if (area < minRegionArea)
                {
                    continue;
                }

                if (!boundaries.TryGetValue(component, out var contours) || contours.Count == 0)
                {
                    continue;
                }

                var maxEdges = Mathf.Max(8, ctx.RasterOptions.MaxPathEdgesPerPrimitive);
                var simplifiedContours = new List<List<Vector2>>(contours.Count);
                for (var c = 0; c < contours.Count; c++)
                {
                    var contour = contours[c];
                    if (contour == null || contour.Count < 3)
                    {
                        continue;
                    }

                    List<Vector2> simplified;
                    if (useBezierFit)
                    {
                        simplified = BezierFitter.FitPolyline(contour, bezierMaxError, 45f, 2f);
                    }
                    else
                    {
                        simplified = PathSimplifier.DouglasPeucker(contour, simplifyTolerance);
                    }

                    if (simplified == null || simplified.Count < 3)
                    {
                        continue;
                    }

                    if (simplified.Count - 1 > maxEdges)
                    {
                        simplified = PathSimplifier.SimplifyToEdgeBudget(simplified, maxEdges, ctx.Issues);
                    }

                    if (simplified.Count < 3)
                    {
                        continue;
                    }

                    simplifiedContours.Add(simplified);
                }

                if (simplifiedContours.Count == 0)
                {
                    continue;
                }

                var color = palette[Mathf.Clamp(colorLabel, 0, palette.Length - 1)];
                color.a = 255;
                RasterContourPainter.FillPolygonsEvenOdd(preview, width, height, simplifiedContours, color);
                RasterPathBuilder.AddPolygonContours(
                    ctx,
                    simplifiedContours,
                    (Color)color,
                    "raster");
            }

            return preview;
        }
    }
}

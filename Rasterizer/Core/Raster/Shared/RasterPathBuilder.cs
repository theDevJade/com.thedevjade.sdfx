using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class RasterPathBuilder
    {
        public static void AddRectangle(
            RasterVectorizerContext ctx,
            float pixelX,
            float pixelY,
            float pixelWidth,
            float pixelHeight,
            Color color,
            string sourceElement,
            byte layer = 0)
        {
            _ = sourceElement;
            _ = layer;
            if (!ctx.TryAddPath())
            {
                return;
            }

            ctx.Svg.AddRectangle(pixelX, pixelY, pixelWidth, pixelHeight, color);
        }

        public static void AddPolygon(
            RasterVectorizerContext ctx,
            IReadOnlyList<Vector2> pixelPoints,
            Color color,
            string sourceElement,
            byte layer = 0)
        {
            _ = sourceElement;
            _ = layer;
            if (pixelPoints == null || pixelPoints.Count < 3)
            {
                return;
            }

            AddPolygonContours(ctx, new[] { pixelPoints as List<Vector2> ?? CopyPoints(pixelPoints) }, color, sourceElement, layer);
        }

        public static void AddPolygonContours(
            RasterVectorizerContext ctx,
            IReadOnlyList<List<Vector2>> pixelContours,
            Color color,
            string sourceElement,
            byte layer = 0)
        {
            _ = sourceElement;
            _ = layer;
            if (pixelContours == null || pixelContours.Count == 0)
            {
                return;
            }

            var maxEdges = Mathf.Max(8, ctx.RasterOptions.MaxPathEdgesPerPrimitive);
            var simplifiedContours = new List<IReadOnlyList<Vector2>>(pixelContours.Count);
            for (var c = 0; c < pixelContours.Count; c++)
            {
                var points = pixelContours[c];
                if (points == null || points.Count < 3)
                {
                    continue;
                }

                var simplified = PathSimplifier.SimplifyToEdgeBudget(points, maxEdges, ctx.Issues);
                if (simplified.Count < 3)
                {
                    continue;
                }

                simplifiedContours.Add(simplified);
            }

            if (simplifiedContours.Count == 0)
            {
                return;
            }

            if (!ctx.TryAddPath())
            {
                return;
            }

            ctx.Svg.AddPolygonContours(simplifiedContours, color);
        }

        public static void AddPolyline(
            RasterVectorizerContext ctx,
            IReadOnlyList<Vector2> pixelPoints,
            Color color,
            float strokeRadiusUv,
            string sourceElement,
            byte layer = 0)
        {
            _ = sourceElement;
            _ = layer;
            if (pixelPoints == null || pixelPoints.Count < 2)
            {
                return;
            }

            var maxEdges = Mathf.Max(8, ctx.RasterOptions.MaxPathEdgesPerPrimitive);
            var simplified = PathSimplifier.Decimate(pixelPoints, maxEdges, ctx.Issues);
            if (simplified.Count < 2)
            {
                return;
            }

            if (!ctx.TryAddPath())
            {
                return;
            }

            var strokePx = Mathf.Max(1f, strokeRadiusUv * Mathf.Max(ctx.Image.Width, ctx.Image.Height));
            ctx.Svg.AddPolyline(simplified, color, strokePx);
        }

        public static List<Vector2> PixelPointsToUv(IReadOnlyList<Vector2> pixelPoints, int width, int height, object model)
        {
            _ = width;
            _ = height;
            _ = model;
            return pixelPoints as List<Vector2> ?? CopyPoints(pixelPoints);
        }

        public static List<int> OrderLabelsByAreaDescending(int[] labels, ICollection<int> labelIds)
        {
            var areas = new Dictionary<int, int>(labelIds.Count);
            foreach (var id in labelIds)
            {
                areas[id] = 0;
            }

            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (areas.TryGetValue(label, out var count))
                {
                    areas[label] = count + 1;
                }
            }

            var ordered = new List<int>(labelIds);
            ordered.Sort((a, b) =>
            {
                areas.TryGetValue(a, out var areaA);
                areas.TryGetValue(b, out var areaB);
                var cmp = areaB.CompareTo(areaA);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });
            return ordered;
        }

        private static List<Vector2> CopyPoints(IReadOnlyList<Vector2> points)
        {
            var copy = new List<Vector2>(points.Count);
            for (var i = 0; i < points.Count; i++)
            {
                copy.Add(points[i]);
            }

            return copy;
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using SDFX.VectorTextureCompiler.Core.Baking;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Primitives;
using Unity.VectorGraphics;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Core.Parsing
{
    internal static class VectorGraphicsSvgAdapter
    {
        private const float DefaultDpi = 96f;

        public static ParseResult Parse(string svgText, ParserOptions options)
        {
            options ??= new ParserOptions();

            var primitives = new List<Primitive>();
            var sourceData = new List<PrimitiveSourceData>();
            var issues = new List<ParseIssue>();
            var pathEdges = new List<Vector4>();

            if (string.IsNullOrWhiteSpace(svgText))
            {
                issues.Add(new ParseIssue(ParseIssueSeverity.Error, SdfxLanguage.Parsing.SvgTextEmpty, SdfxLanguage.Parsing.SvgElementName, 0));
                return new ParseResult(primitives, sourceData, issues, pathEdges);
            }

            SVGParser.SceneInfo sceneInfo;
            try
            {
                using (var reader = new StringReader(svgText))
                {
                    sceneInfo = SVGParser.ImportSVG(
                        reader,
                        ViewportOptions.DontPreserve,
                        DefaultDpi,
                        pixelsPerUnit: 1f);
                }
            }
            catch (Exception ex)
            {
                issues.Add(new ParseIssue(
                    ParseIssueSeverity.Error,
                    ex.Message,
                    SdfxLanguage.Parsing.SvgElementName,
                    0,
                    ParseIssueCode.ParseFailure));
                return new ParseResult(primitives, sourceData, issues, pathEdges);
            }

            if (sceneInfo.Scene?.Root == null)
            {
                issues.Add(new ParseIssue(
                    ParseIssueSeverity.Error,
                    SdfxLanguage.Parsing.RootIsNotSvg,
                    SdfxLanguage.Parsing.SvgElementName,
                    0,
                    ParseIssueCode.InvalidDocument));
                return new ParseResult(primitives, sourceData, issues, pathEdges);
            }

            var canvas = sceneInfo.SceneViewport;
            if (canvas.width <= 0.0001f || canvas.height <= 0.0001f)
            {
                canvas = VectorUtils.SceneNodeBounds(sceneInfo.Scene.Root);
            }

            canvas.width = Mathf.Max(0.0001f, canvas.width);
            canvas.height = Mathf.Max(0.0001f, canvas.height);

            var diagonal = Mathf.Sqrt(canvas.width * canvas.width + canvas.height * canvas.height);
            var stepDistance = Mathf.Max(diagonal / 400f, 0.01f);

            foreach (var transformedNode in VectorUtils.WorldTransformedSceneNodes(sceneInfo.Scene.Root, sceneInfo.NodeOpacity))
            {
                if (transformedNode.Node.Shapes == null)
                {
                    continue;
                }

                for (var i = 0; i < transformedNode.Node.Shapes.Count; i++)
                {
                    ConvertShape(
                        transformedNode.Node.Shapes[i],
                        transformedNode.WorldTransform,
                        transformedNode.WorldOpacity,
                        canvas,
                        stepDistance,
                        options,
                        primitives,
                        sourceData,
                        pathEdges,
                        issues);
                }
            }

            return new ParseResult(primitives, sourceData, issues, pathEdges);
        }

        private static void ConvertShape(
            Shape shape,
            Matrix2D worldTransform,
            float worldOpacity,
            Rect canvas,
            float stepDistance,
            ParserOptions options,
            List<Primitive> primitives,
            List<PrimitiveSourceData> sourceData,
            List<Vector4> pathEdges,
            List<ParseIssue> issues)
        {
            if (shape.Contours == null || shape.Contours.Length == 0)
            {
                return;
            }

            var hasFill = shape.Fill != null;
            var hasStroke = shape.PathProps.Stroke != null && shape.PathProps.Stroke.HalfThickness > 0f;
            if (!hasFill && !hasStroke)
            {
                return;
            }

            Color fillColor = Color.white;
            var fillGradientIndex = 0;
            if (hasFill)
            {
                if (shape.Fill is GradientFill gradientFill && gradientFill.Stops != null && gradientFill.Stops.Length > 0)
                {
                    fillGradientIndex = BakeGradientRun(gradientFill, shape.FillTransform, pathEdges);
                    fillColor = new Color(1f, 1f, 1f, gradientFill.Opacity * worldOpacity);
                }
                else if (!TryReadFillColor(shape.Fill, worldOpacity, out fillColor))
                {
                    fillColor = Color.white;
                    issues.Add(new ParseIssue(
                        ParseIssueSeverity.Warning,
                        SdfxLanguage.Parsing.UnsupportedGradientReference,
                        "shape",
                        0,
                        ParseIssueCode.UnsupportedGradient));
                }
            }

            Color strokeColor = Color.white;
            var strokeRadiusUv = 0f;
            if (hasStroke)
            {
                strokeColor = ReadStrokeColor(shape.PathProps.Stroke, worldOpacity);
                strokeRadiusUv = shape.PathProps.Stroke.HalfThickness / ((canvas.width + canvas.height) * 0.5f);
            }

            var contourPoints = new List<List<Vector2>>(shape.Contours.Length);
            for (var contourIndex = 0; contourIndex < shape.Contours.Length; contourIndex++)
            {
                var contour = shape.Contours[contourIndex];
                if (contour.Segments == null || contour.Segments.Length == 0)
                {
                    contourPoints.Add(null);
                    continue;
                }

                contourPoints.Add(FlattenContour(contour, worldTransform, stepDistance, canvas, options, issues));
            }

            if (hasFill)
            {
                EmitFill(contourPoints, fillColor, fillGradientIndex, primitives, sourceData, pathEdges);
            }

            if (hasStroke)
            {
                EmitStrokes(shape, contourPoints, strokeColor, strokeRadiusUv, primitives, sourceData, pathEdges);
            }
        }

        private static List<Vector2> FlattenContour(
            BezierContour contour,
            Matrix2D worldTransform,
            float stepDistance,
            Rect canvas,
            ParserOptions options,
            List<ParseIssue> issues)
        {
            var segments = VectorUtils.TransformBezierPath(contour.Segments, worldTransform);
            if (segments == null || segments.Length < 2)
            {
                return null;
            }

            var flattened = new List<Vector2>(64) { segments[0].P0 };
            foreach (var segment in VectorUtils.SegmentsInPath(segments, contour.Closed))
            {
                var length = VectorUtils.SegmentLength(segment, 0.01f);
                var steps = Mathf.Clamp(Mathf.CeilToInt(length / stepDistance), 1, 64);
                for (var s = 1; s <= steps; s++)
                {
                    flattened.Add(VectorUtils.Eval(segment, s / (float)steps));
                }
            }

            var points = new List<Vector2>(flattened.Count);
            for (var i = 0; i < flattened.Count; i++)
            {
                // SVG Y grows downward, Unity UV Y grows upward: flip so art is upright.
                var uv = new Vector2(
                    (flattened[i].x - canvas.x) / canvas.width,
                    1f - (flattened[i].y - canvas.y) / canvas.height);
                if (points.Count == 0 || (points[points.Count - 1] - uv).sqrMagnitude > 1e-12f)
                {
                    points.Add(uv);
                }
            }

            if (points.Count < 2)
            {
                return null;
            }

            var maxEdges = Mathf.Max(8, options.MaxPathEdgesPerPrimitive);
            if (points.Count - 1 > maxEdges)
            {
                points = Decimate(points, maxEdges);
                issues.Add(new ParseIssue(
                    ParseIssueSeverity.Warning,
                    SdfxLanguage.Parsing.PathDetailReduced(maxEdges),
                    "path",
                    0,
                    ParseIssueCode.PathDetailReduced));
            }

            return points;
        }

        private static List<Vector2> Decimate(List<Vector2> points, int maxEdges)
        {
            var result = new List<Vector2>(maxEdges + 1);
            var step = (points.Count - 1) / (float)maxEdges;
            for (var i = 0; i < maxEdges; i++)
            {
                result.Add(points[Mathf.Min((int)(i * step), points.Count - 1)]);
            }

            result.Add(points[points.Count - 1]);
            return result;
        }

        private static void EmitFill(
            List<List<Vector2>> contourPoints,
            Color fillColor,
            int fillGradientIndex,
            List<Primitive> primitives,
            List<PrimitiveSourceData> sourceData,
            List<Vector4> pathEdges)
        {
            if (contourPoints.Count == 1 && TryGetAxisAlignedRect(contourPoints[0], out var rectMin, out var rectMax))
            {
                AddPrimitive(primitives, sourceData, PrimitiveKind.Rectangle, rectMin, rectMax - rectMin, fillColor, -1, 0, 0f, fillGradientIndex, "rect");
                return;
            }

            // One polygon primitive per shape: all contour loops share one edge run so
            // the shader's even-odd rule carves holes correctly.
            var start = pathEdges.Count;
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            var edgeCount = 0;

            for (var c = 0; c < contourPoints.Count; c++)
            {
                var points = contourPoints[c];
                if (points == null || points.Count < 3)
                {
                    continue;
                }

                edgeCount += AppendLoopEdges(pathEdges, points, closeLoop: true, ref min, ref max);
            }

            if (edgeCount == 0)
            {
                return;
            }

            AddPrimitive(primitives, sourceData, PrimitiveKind.Polygon, min, max - min, fillColor, start, edgeCount, 0f, fillGradientIndex, "path");
        }

        private static void EmitStrokes(
            Shape shape,
            List<List<Vector2>> contourPoints,
            Color strokeColor,
            float strokeRadiusUv,
            List<Primitive> primitives,
            List<PrimitiveSourceData> sourceData,
            List<Vector4> pathEdges)
        {
            for (var c = 0; c < contourPoints.Count; c++)
            {
                var points = contourPoints[c];
                if (points == null || points.Count < 2)
                {
                    continue;
                }

                var start = pathEdges.Count;
                var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                var closed = shape.Contours[c].Closed && points.Count >= 3;
                var edgeCount = AppendLoopEdges(pathEdges, points, closed, ref min, ref max);
                if (edgeCount == 0)
                {
                    continue;
                }

                var inflate = new Vector2(strokeRadiusUv, strokeRadiusUv);
                min -= inflate;
                max += inflate;
                AddPrimitive(primitives, sourceData, PrimitiveKind.Polyline, min, max - min, strokeColor, start, edgeCount, strokeRadiusUv, 0, "stroke");
            }
        }

        private static int AppendLoopEdges(
            List<Vector4> pathEdges,
            List<Vector2> points,
            bool closeLoop,
            ref Vector2 min,
            ref Vector2 max)
        {
            var count = 0;
            for (var i = 0; i < points.Count - 1; i++)
            {
                pathEdges.Add(new Vector4(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y));
                count++;
            }

            var first = points[0];
            var last = points[points.Count - 1];
            if (closeLoop && (last - first).sqrMagnitude > 1e-12f)
            {
                pathEdges.Add(new Vector4(last.x, last.y, first.x, first.y));
                count++;
            }

            for (var i = 0; i < points.Count; i++)
            {
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            return count;
        }

        private static bool TryGetAxisAlignedRect(List<Vector2> points, out Vector2 min, out Vector2 max)
        {
            min = Vector2.zero;
            max = Vector2.zero;
            if (points == null)
            {
                return false;
            }

            var count = points.Count;
            if (count == 5 && (points[4] - points[0]).sqrMagnitude < 1e-10f)
            {
                count = 4;
            }

            if (count != 4)
            {
                return false;
            }

            const float epsilon = 1e-5f;
            for (var i = 0; i < 4; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % 4];
                var axisAligned = Mathf.Abs(a.x - b.x) < epsilon || Mathf.Abs(a.y - b.y) < epsilon;
                if (!axisAligned)
                {
                    return false;
                }
            }

            min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (var i = 0; i < 4; i++)
            {
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            return (max - min).sqrMagnitude > 1e-10f;
        }

        private static void AddPrimitive(
            List<Primitive> primitives,
            List<PrimitiveSourceData> sourceData,
            PrimitiveKind type,
            Vector2 uvPosition,
            Vector2 uvSize,
            Color color,
            int parameterIndex,
            int parameterCount,
            float strokeRadius,
            int gradientIndex,
            string sourceElement)
        {
            primitives.Add(new Primitive
            {
                Type = type,
                Position = uvPosition,
                Size = uvSize,
                RotationDegrees = 0f,
                Color = color,
                ParameterIndex = parameterIndex,
                ParameterCount = parameterCount,
                StrokeRadius = strokeRadius,
                GradientIndex = gradientIndex,
                Softness = 0f,
                Layer = 0
            });

            sourceData.Add(new PrimitiveSourceData
            {
                SourcePosition = uvPosition,
                SourceSize = uvSize,
                SourceElement = sourceElement
            });
        }

        private const int GradientRampSize = 8;

        private static int BakeGradientRun(GradientFill gradientFill, Matrix2D fillTransform, List<Vector4> pathEdges)
        {
            var start = pathEdges.Count;

            pathEdges.Add(new Vector4(
                gradientFill.Type == GradientFillType.Radial ? 1f : 0f,
                (float)gradientFill.Addressing,
                gradientFill.RadialFocus.x,
                gradientFill.RadialFocus.y));
            pathEdges.Add(new Vector4(fillTransform.m00, fillTransform.m01, fillTransform.m02, 0f));
            pathEdges.Add(new Vector4(fillTransform.m10, fillTransform.m11, fillTransform.m12, 0f));

            var stops = gradientFill.Stops;
            for (var i = 0; i < GradientRampSize; i++)
            {
                var t = i / (float)(GradientRampSize - 1);
                var color = DataTextureBaker.EncodeColorForDataTexture(EvaluateStops(stops, t));
                pathEdges.Add(new Vector4(color.r, color.g, color.b, color.a));
            }

            return start + 1;
        }

        private static Color EvaluateStops(GradientStop[] stops, float t)
        {
            if (stops.Length == 1)
            {
                return stops[0].Color;
            }

            var below = stops[0];
            var above = stops[stops.Length - 1];
            var belowPct = float.NegativeInfinity;
            var abovePct = float.PositiveInfinity;
            for (var i = 0; i < stops.Length; i++)
            {
                var pct = Mathf.Clamp01(stops[i].StopPercentage);
                if (pct <= t && pct > belowPct)
                {
                    below = stops[i];
                    belowPct = pct;
                }

                if (pct >= t && pct < abovePct)
                {
                    above = stops[i];
                    abovePct = pct;
                }
            }

            if (float.IsNegativeInfinity(belowPct))
            {
                return above.Color;
            }

            if (float.IsPositiveInfinity(abovePct) || abovePct - belowPct < 1e-5f)
            {
                return below.Color;
            }

            return Color.Lerp(below.Color, above.Color, (t - belowPct) / (abovePct - belowPct));
        }

        private static bool TryReadFillColor(IFill fill, float opacity, out Color color)
        {
            color = Color.white;

            if (fill is SolidFill solidFill)
            {
                color = solidFill.Color;
                color.a *= solidFill.Opacity * opacity;
                return true;
            }

            return false;
        }

        private static Color ReadStrokeColor(Stroke stroke, float opacity)
        {
            if (stroke == null)
            {
                return Color.white;
            }

            if (stroke.Fill is SolidFill solidFill)
            {
                var color = solidFill.Color;
                color.a *= solidFill.Opacity * opacity;
                return color;
            }

            var fallback = stroke.Color;
            fallback.a *= opacity;
            return fallback;
        }
    }
}

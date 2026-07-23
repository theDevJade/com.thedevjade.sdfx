using System;
using System.Collections.Generic;
using Clipper2Lib;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Core.Optimize
{
    public static class OcclusionPathClipper
    {
        private const double Scale = 1_000_000.0;
        private const float RotationEpsilon = 0.01f;
        private const int CircleSegments = 32;
        private const int RoundRectCornerSegments = 4;

        public readonly struct Result
        {
            public Result(List<Primitive> primitives, List<Vector4> pathEdges, int clippedCount, int emittedPolygons)
            {
                Primitives = primitives;
                PathEdges = pathEdges;
                ClippedCount = clippedCount;
                EmittedPolygons = emittedPolygons;
            }

            public List<Primitive> Primitives { get; }
            public List<Vector4> PathEdges { get; }
            public int ClippedCount { get; }
            public int EmittedPolygons { get; }
        }

        public static Result Apply(IReadOnlyList<Primitive> input, IReadOnlyList<Vector4> pathEdges)
        {
            if (input == null || input.Count == 0)
            {
                return new Result(new List<Primitive>(), new List<Vector4>(), 0, 0);
            }

            var edges = pathEdges != null ? new List<Vector4>(pathEdges) : new List<Vector4>();
            var output = new List<Primitive>(input.Count);
            var clippedCount = 0;
            var emittedPolygons = 0;

            for (var i = 0; i < input.Count; i++)
            {
                var current = input[i];
                if (current.Type != PrimitiveKind.Polygon
                    || current.ParameterCount <= 0
                    || current.ParameterIndex < 0
                    || Mathf.Abs(current.RotationDegrees) > RotationEpsilon)
                {
                    output.Add(current);
                    continue;
                }

                if (!TryBuildSubjectPath(current, edges, out var subject))
                {
                    output.Add(current);
                    continue;
                }

                var clips = new Paths64();
                for (var j = i + 1; j < input.Count; j++)
                {
                    var cover = input[j];
                    if (cover.Layer < current.Layer || !BooleanResolver.IsValidOpaqueCover(cover, edges))
                    {
                        continue;
                    }

                    if (!TryTessellateCover(cover, out var coverPath) || coverPath.Count < 3)
                    {
                        continue;
                    }

                    var subjectBounds = PrimitiveBounds.GetAabb(current);
                    var coverBounds = PrimitiveBounds.GetAabb(cover);
                    if (coverBounds.max.x < subjectBounds.min.x
                        || coverBounds.min.x > subjectBounds.max.x
                        || coverBounds.max.y < subjectBounds.min.y
                        || coverBounds.min.y > subjectBounds.max.y)
                    {
                        continue;
                    }

                    clips.Add(coverPath);
                }

                if (clips.Count == 0)
                {
                    output.Add(current);
                    continue;
                }

                Paths64 difference;
                try
                {
                    var subjects = new Paths64 { subject };
                    difference = Clipper.Difference(subjects, clips, FillRule.EvenOdd);
                }
                catch (Exception)
                {
                    output.Add(current);
                    continue;
                }

                if (difference == null || difference.Count == 0)
                {
                    clippedCount++;
                    continue;
                }

                if (!TryEmitDifferencePolygons(current, difference, edges, output, out var emitted))
                {
                    output.Add(current);
                    continue;
                }

                clippedCount++;
                emittedPolygons += emitted;
            }

            return new Result(output, edges, clippedCount, emittedPolygons);
        }

        private static bool TryBuildSubjectPath(Primitive prim, IReadOnlyList<Vector4> edges, out Path64 path)
        {
            path = new Path64();
            var start = prim.ParameterIndex;
            var count = prim.ParameterCount;
            if (start < 0 || count <= 0 || start + count > edges.Count)
            {
                return false;
            }

            path.Add(ToIntPoint(edges[start].x, edges[start].y));
            for (var e = 0; e < count; e++)
            {
                var edge = edges[start + e];
                path.Add(ToIntPoint(edge.z, edge.w));
            }

            if (path.Count >= 2 && path[0] == path[path.Count - 1])
            {
                path.RemoveAt(path.Count - 1);
            }

            return path.Count >= 3;
        }

        private static bool TryTessellateCover(Primitive cover, out Path64 path)
        {
            path = new Path64();
            var softness = Mathf.Max(cover.Softness, 0f) + 0.0015f;
            var halfExt = cover.Size * 0.5f;
            var center = cover.Position + halfExt;

            switch (cover.Type)
            {
                case PrimitiveKind.Rectangle:
                {
                    var min = Vector2.Min(cover.Position, cover.Position + cover.Size);
                    var max = Vector2.Max(cover.Position, cover.Position + cover.Size);
                    min.x += softness;
                    min.y += softness;
                    max.x -= softness;
                    max.y -= softness;
                    if (max.x <= min.x || max.y <= min.y)
                    {
                        return false;
                    }

                    path.Add(ToIntPoint(min.x, min.y));
                    path.Add(ToIntPoint(max.x, min.y));
                    path.Add(ToIntPoint(max.x, max.y));
                    path.Add(ToIntPoint(min.x, max.y));
                    return true;
                }
                case PrimitiveKind.Circle:
                {
                    var r = Mathf.Min(halfExt.x, halfExt.y) - softness;
                    if (r <= 0f)
                    {
                        return false;
                    }

                    for (var i = 0; i < CircleSegments; i++)
                    {
                        var a = (i / (float)CircleSegments) * Mathf.PI * 2f;
                        path.Add(ToIntPoint(center.x + Mathf.Cos(a) * r, center.y + Mathf.Sin(a) * r));
                    }

                    return true;
                }
                case PrimitiveKind.Ellipse:
                {
                    var rx = halfExt.x - softness;
                    var ry = halfExt.y - softness;
                    if (rx <= 0f || ry <= 0f)
                    {
                        return false;
                    }

                    for (var i = 0; i < CircleSegments; i++)
                    {
                        var a = (i / (float)CircleSegments) * Mathf.PI * 2f;
                        path.Add(ToIntPoint(center.x + Mathf.Cos(a) * rx, center.y + Mathf.Sin(a) * ry));
                    }

                    return true;
                }
                case PrimitiveKind.RoundedRectangle:
                {
                    var corner = Mathf.Min(halfExt.x, halfExt.y) * 0.25f;
                    var insetHalf = halfExt - new Vector2(softness, softness);
                    if (insetHalf.x <= 0f || insetHalf.y <= 0f)
                    {
                        return false;
                    }

                    corner = Mathf.Min(corner, Mathf.Min(insetHalf.x, insetHalf.y));
                    AppendRoundRect(path, center, insetHalf, corner);
                    return path.Count >= 3;
                }
                default:
                    return false;
            }
        }

        private static void AppendRoundRect(Path64 path, Vector2 center, Vector2 halfExt, float corner)
        {
            var inner = halfExt - new Vector2(corner, corner);
            AppendArc(path, center + new Vector2(inner.x, -inner.y), corner, -90f, 0f);
            AppendArc(path, center + new Vector2(inner.x, inner.y), corner, 0f, 90f);
            AppendArc(path, center + new Vector2(-inner.x, inner.y), corner, 90f, 180f);
            AppendArc(path, center + new Vector2(-inner.x, -inner.y), corner, 180f, 270f);
        }

        private static void AppendArc(Path64 path, Vector2 c, float r, float deg0, float deg1)
        {
            for (var i = 0; i <= RoundRectCornerSegments; i++)
            {
                var t = i / (float)RoundRectCornerSegments;
                var deg = Mathf.Lerp(deg0, deg1, t) * Mathf.Deg2Rad;
                path.Add(ToIntPoint(c.x + Mathf.Cos(deg) * r, c.y + Mathf.Sin(deg) * r));
            }
        }

        private static bool TryEmitDifferencePolygons(
            Primitive source,
            Paths64 difference,
            List<Vector4> edges,
            List<Primitive> output,
            out int emitted)
        {
            emitted = 0;
            var pending = new List<(Primitive prim, List<Vector4> newEdges)>();
            for (var p = 0; p < difference.Count; p++)
            {
                var path = difference[p];
                if (path == null || path.Count < 3)
                {
                    continue;
                }

                var area = Clipper.Area(path);
                if (Math.Abs(area) < 1e-6 * Scale * Scale)
                {
                    continue;
                }

                var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                var points = new List<Vector2>(path.Count);
                for (var i = 0; i < path.Count; i++)
                {
                    var pt = FromIntPoint(path[i]);
                    points.Add(pt);
                    min = Vector2.Min(min, pt);
                    max = Vector2.Max(max, pt);
                }

                if (!IsFinite(min) || !IsFinite(max) || max.x <= min.x || max.y <= min.y)
                {
                    return false;
                }

                var newEdges = new List<Vector4>(points.Count);
                for (var i = 0; i < points.Count; i++)
                {
                    var a = points[i];
                    var b = points[(i + 1) % points.Count];
                    newEdges.Add(new Vector4(a.x, a.y, b.x, b.y));
                }

                if (newEdges.Count < 3)
                {
                    continue;
                }

                var prim = source;
                prim.Position = min;
                prim.Size = max - min;
                prim.ParameterIndex = 0; // rewritten when committing
                prim.ParameterCount = newEdges.Count;
                pending.Add((prim, newEdges));
            }

            if (pending.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < pending.Count; i++)
            {
                var (prim, newEdges) = pending[i];
                var start = edges.Count;
                edges.AddRange(newEdges);
                prim.ParameterIndex = start;
                prim.ParameterCount = newEdges.Count;
                output.Add(prim);
                emitted++;
            }

            return true;
        }

        private static Point64 ToIntPoint(float x, float y)
        {
            return new Point64(Math.Round(x * Scale), Math.Round(y * Scale));
        }

        private static Vector2 FromIntPoint(Point64 p)
        {
            return new Vector2((float)(p.X / Scale), (float)(p.Y / Scale));
        }

        private static bool IsFinite(Vector2 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.x) || float.IsInfinity(v.y));
        }
    }
}

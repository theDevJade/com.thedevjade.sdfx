using System;
using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Core.Optimize
{
    public static class BooleanResolver
    {
        public const int GradientRampSize = 8;
        public const int GradientHeaderTexels = 3;
        private const float OpaqueAlpha = 0.999f;
        private const float RotationEpsilon = 0.01f;
        // Extra inset beyond Softness so AA fringes are not culled as fully covered.
        private const float ContainmentAaEpsilon = 0.0015f;
        private const int BroadPhaseGridSize = 16;

        public static List<Primitive> Resolve(IReadOnlyList<Primitive> input)
        {
            return Resolve(input, pathEdges: null);
        }

        public static List<Primitive> Resolve(IReadOnlyList<Primitive> input, IReadOnlyList<Vector4> pathEdges)
        {
            if (input == null || input.Count == 0)
            {
                return new List<Primitive>();
            }

            var indices = new int[input.Count];
            for (var i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            Array.Sort(indices, (a, b) =>
            {
                var byLayer = input[a].Layer.CompareTo(input[b].Layer);
                return byLayer != 0 ? byLayer : a.CompareTo(b);
            });

            var sorted = new List<Primitive>(input.Count);
            for (var i = 0; i < indices.Length; i++)
            {
                sorted.Add(input[indices[i]]);
            }

            var keep = new bool[sorted.Count];
            for (var i = 0; i < keep.Length; i++)
            {
                keep[i] = true;
            }

            var bounds = new (Vector2 min, Vector2 max)[sorted.Count];
            for (var i = 0; i < sorted.Count; i++)
            {
                bounds[i] = PrimitiveBounds.GetAabb(sorted[i]);
            }

            var cellBuckets = BuildBroadPhase(bounds);

            for (var i = 0; i < sorted.Count; i++)
            {
                if (!keep[i] || sorted[i].Color.a <= 0f)
                {
                    keep[i] = false;
                    continue;
                }

                var current = sorted[i];
                if (Mathf.Abs(current.RotationDegrees) > RotationEpsilon)
                {
                    // Position/Size is not a true rotated AABB — fail-open.
                    continue;
                }

                var currentBounds = bounds[i];
                var candidates = GatherCandidates(cellBuckets, currentBounds, i, sorted.Count);
                for (var c = 0; c < candidates.Count; c++)
                {
                    var j = candidates[c];
                    if (!keep[j] || j <= i)
                    {
                        continue;
                    }

                    var cover = sorted[j];
                    if (cover.Layer < current.Layer)
                    {
                        continue;
                    }

                    if (!IsValidOpaqueCover(cover, pathEdges))
                    {
                        continue;
                    }

                    if (CoverContainsAabb(cover, currentBounds))
                    {
                        keep[i] = false;
                        break;
                    }
                }
            }

            ApplyRectUnionCoverage(sorted, keep, bounds, cellBuckets, pathEdges);

            var output = new List<Primitive>(sorted.Count);
            for (var i = 0; i < sorted.Count; i++)
            {
                if (keep[i])
                {
                    output.Add(sorted[i]);
                }
            }

            return output;
        }

        private static List<int>[] BuildBroadPhase((Vector2 min, Vector2 max)[] bounds)
        {
            var total = BroadPhaseGridSize * BroadPhaseGridSize;
            var buckets = new List<int>[total];
            for (var i = 0; i < bounds.Length; i++)
            {
                if (!TryGetCellRange(bounds[i], out var xMin, out var yMin, out var xMax, out var yMax))
                {
                    continue;
                }

                for (var y = yMin; y <= yMax; y++)
                {
                    for (var x = xMin; x <= xMax; x++)
                    {
                        var cell = y * BroadPhaseGridSize + x;
                        var bucket = buckets[cell];
                        if (bucket == null)
                        {
                            bucket = new List<int>(8);
                            buckets[cell] = bucket;
                        }

                        bucket.Add(i);
                    }
                }
            }

            return buckets;
        }

        private static List<int> GatherCandidates(
            List<int>[] buckets,
            (Vector2 min, Vector2 max) currentBounds,
            int currentIndex,
            int count)
        {
            var seen = new bool[count];
            var result = new List<int>(16);
            if (!TryGetCellRange(currentBounds, out var xMin, out var yMin, out var xMax, out var yMax))
            {
                return result;
            }

            for (var y = yMin; y <= yMax; y++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    var bucket = buckets[y * BroadPhaseGridSize + x];
                    if (bucket == null)
                    {
                        continue;
                    }

                    for (var b = 0; b < bucket.Count; b++)
                    {
                        var j = bucket[b];
                        if (j <= currentIndex || seen[j])
                        {
                            continue;
                        }

                        seen[j] = true;
                        result.Add(j);
                    }
                }
            }

            return result;
        }

        private static bool TryGetCellRange(
            (Vector2 min, Vector2 max) bounds,
            out int xMin,
            out int yMin,
            out int xMax,
            out int yMax)
        {
            xMin = yMin = xMax = yMax = 0;
            xMin = Mathf.Clamp((int)Mathf.Floor(bounds.min.x * BroadPhaseGridSize), 0, BroadPhaseGridSize - 1);
            yMin = Mathf.Clamp((int)Mathf.Floor(bounds.min.y * BroadPhaseGridSize), 0, BroadPhaseGridSize - 1);
            xMax = Mathf.Clamp((int)Mathf.Floor(bounds.max.x * BroadPhaseGridSize), 0, BroadPhaseGridSize - 1);
            yMax = Mathf.Clamp((int)Mathf.Floor(bounds.max.y * BroadPhaseGridSize), 0, BroadPhaseGridSize - 1);
            if (xMax < xMin)
            {
                xMax = xMin;
            }

            if (yMax < yMin)
            {
                yMax = yMin;
            }

            return true;
        }

        internal static bool IsValidOpaqueCover(Primitive cover, IReadOnlyList<Vector4> pathEdges)
        {
            if (Mathf.Abs(cover.RotationDegrees) > RotationEpsilon)
            {
                return false;
            }

            if (cover.ParameterCount != 0)
            {
                return false;
            }

            switch (cover.Type)
            {
                case PrimitiveKind.Rectangle:
                case PrimitiveKind.RoundedRectangle:
                case PrimitiveKind.Circle:
                case PrimitiveKind.Ellipse:
                    break;
                default:
                    return false;
            }

            return IsFullyOpaque(cover, pathEdges);
        }

        internal static bool IsFullyOpaque(Primitive cover, IReadOnlyList<Vector4> pathEdges)
        {
            if (cover.Color.a < OpaqueAlpha)
            {
                return false;
            }

            if (cover.GradientIndex <= 0)
            {
                return true;
            }

            if (pathEdges == null)
            {
                return false;
            }

            var start = cover.GradientIndex - 1;
            var rampStart = start + GradientHeaderTexels;
            var rampEnd = rampStart + GradientRampSize;
            if (start < 0 || rampEnd > pathEdges.Count)
            {
                return false;
            }

            for (var i = rampStart; i < rampEnd; i++)
            {
                if (pathEdges[i].w < OpaqueAlpha)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool CoverContainsAabb(Primitive cover, (Vector2 min, Vector2 max) aabb)
        {
            var softness = Mathf.Max(cover.Softness, 0f) + ContainmentAaEpsilon;
            var halfExt = cover.Size * 0.5f;
            var center = cover.Position + halfExt;

            switch (cover.Type)
            {
                case PrimitiveKind.Rectangle:
                    return RectContainsAabb(cover.Position, cover.Size, softness, aabb);
                case PrimitiveKind.Circle:
                    return CircleContainsAabb(center, Mathf.Min(halfExt.x, halfExt.y), softness, aabb);
                case PrimitiveKind.Ellipse:
                    return EllipseContainsAabb(center, halfExt, softness, aabb);
                case PrimitiveKind.RoundedRectangle:
                    return RoundRectContainsAabb(center, halfExt, softness, aabb);
                default:
                    return false;
            }
        }

        private static bool RectContainsAabb(Vector2 pos, Vector2 size, float softness, (Vector2 min, Vector2 max) aabb)
        {
            var min = Vector2.Min(pos, pos + size);
            var max = Vector2.Max(pos, pos + size);
            min.x += softness;
            min.y += softness;
            max.x -= softness;
            max.y -= softness;
            if (max.x < min.x || max.y < min.y)
            {
                return false;
            }

            return min.x <= aabb.min.x
                && min.y <= aabb.min.y
                && max.x >= aabb.max.x
                && max.y >= aabb.max.y;
        }

        private static bool CircleContainsAabb(Vector2 center, float radius, float softness, (Vector2 min, Vector2 max) aabb)
        {
            var r = radius - softness;
            if (r <= 0f)
            {
                return false;
            }

            return CornerMaxDistanceSq(center, aabb) <= r * r;
        }

        private static bool EllipseContainsAabb(Vector2 center, Vector2 halfExt, float softness, (Vector2 min, Vector2 max) aabb)
        {
            var rx = halfExt.x - softness;
            var ry = halfExt.y - softness;
            if (rx <= 0f || ry <= 0f)
            {
                return false;
            }

            return PointInEllipse(aabb.min, center, rx, ry)
                && PointInEllipse(new Vector2(aabb.max.x, aabb.min.y), center, rx, ry)
                && PointInEllipse(new Vector2(aabb.min.x, aabb.max.y), center, rx, ry)
                && PointInEllipse(aabb.max, center, rx, ry);
        }

        private static bool PointInEllipse(Vector2 p, Vector2 center, float rx, float ry)
        {
            var lx = (p.x - center.x) / rx;
            var ly = (p.y - center.y) / ry;
            return lx * lx + ly * ly <= 1f + 1e-6f;
        }

        private static bool RoundRectContainsAabb(Vector2 center, Vector2 halfExt, float softness, (Vector2 min, Vector2 max) aabb)
        {
            // Match HLSL: corner = min(halfExt) * 0.25
            var corner = Mathf.Min(halfExt.x, halfExt.y) * 0.25f;
            return RoundRectSdf(aabb.min - center, halfExt, corner) <= -softness
                && RoundRectSdf(new Vector2(aabb.max.x, aabb.min.y) - center, halfExt, corner) <= -softness
                && RoundRectSdf(new Vector2(aabb.min.x, aabb.max.y) - center, halfExt, corner) <= -softness
                && RoundRectSdf(aabb.max - center, halfExt, corner) <= -softness;
        }

        private static float RoundRectSdf(Vector2 p, Vector2 halfExt, float corner)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - halfExt + new Vector2(corner, corner);
            var maxQ = Mathf.Max(q.x, q.y);
            var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return outside.magnitude + Mathf.Min(maxQ, 0f) - corner;
        }

        private static float CornerMaxDistanceSq(Vector2 center, (Vector2 min, Vector2 max) aabb)
        {
            var d0 = (aabb.min - center).sqrMagnitude;
            var d1 = (new Vector2(aabb.max.x, aabb.min.y) - center).sqrMagnitude;
            var d2 = (new Vector2(aabb.min.x, aabb.max.y) - center).sqrMagnitude;
            var d3 = (aabb.max - center).sqrMagnitude;
            return Mathf.Max(Mathf.Max(d0, d1), Mathf.Max(d2, d3));
        }

        private static void ApplyRectUnionCoverage(
            List<Primitive> sorted,
            bool[] keep,
            (Vector2 min, Vector2 max)[] bounds,
            List<int>[] cellBuckets,
            IReadOnlyList<Vector4> pathEdges)
        {
            for (var i = 0; i < sorted.Count; i++)
            {
                if (!keep[i])
                {
                    continue;
                }

                var current = sorted[i];
                if (Mathf.Abs(current.RotationDegrees) > RotationEpsilon)
                {
                    continue;
                }

                var currentBounds = bounds[i];
                var area = (currentBounds.max.x - currentBounds.min.x) * (currentBounds.max.y - currentBounds.min.y);
                if (area <= 1e-12f)
                {
                    continue;
                }

                var candidates = GatherCandidates(cellBuckets, currentBounds, i, sorted.Count);
                var coverRects = new List<(Vector2 min, Vector2 max)>(candidates.Count);
                for (var c = 0; c < candidates.Count; c++)
                {
                    var j = candidates[c];
                    if (!keep[j] || j <= i)
                    {
                        continue;
                    }

                    var cover = sorted[j];
                    if (cover.Layer < current.Layer
                        || cover.Type != PrimitiveKind.Rectangle
                        || !IsValidOpaqueCover(cover, pathEdges))
                    {
                        continue;
                    }

                    var softness = Mathf.Max(cover.Softness, 0f) + ContainmentAaEpsilon;
                    var cMin = Vector2.Min(cover.Position, cover.Position + cover.Size);
                    var cMax = Vector2.Max(cover.Position, cover.Position + cover.Size);
                    cMin.x += softness;
                    cMin.y += softness;
                    cMax.x -= softness;
                    cMax.y -= softness;
                    if (cMax.x <= cMin.x || cMax.y <= cMin.y)
                    {
                        continue;
                    }

                    var clipMin = Vector2.Max(cMin, currentBounds.min);
                    var clipMax = Vector2.Min(cMax, currentBounds.max);
                    if (clipMax.x <= clipMin.x || clipMax.y <= clipMin.y)
                    {
                        continue;
                    }

                    coverRects.Add((clipMin, clipMax));
                }

                if (coverRects.Count >= 2 && RectUnionCovers(currentBounds, coverRects))
                {
                    keep[i] = false;
                }
            }
        }

        internal static bool RectUnionCovers((Vector2 min, Vector2 max) target, List<(Vector2 min, Vector2 max)> rects)
        {
            if (rects == null || rects.Count == 0)
            {
                return false;
            }

            var xs = new List<float>(rects.Count * 2 + 2) { target.min.x, target.max.x };
            for (var i = 0; i < rects.Count; i++)
            {
                xs.Add(Mathf.Clamp(rects[i].min.x, target.min.x, target.max.x));
                xs.Add(Mathf.Clamp(rects[i].max.x, target.min.x, target.max.x));
            }

            xs.Sort();
            for (var s = 0; s < xs.Count - 1; s++)
            {
                var x0 = xs[s];
                var x1 = xs[s + 1];
                if (x1 - x0 <= 1e-8f)
                {
                    continue;
                }

                var midX = (x0 + x1) * 0.5f;
                var intervals = new List<(float y0, float y1)>();
                for (var r = 0; r < rects.Count; r++)
                {
                    var rect = rects[r];
                    if (rect.min.x <= midX && rect.max.x >= midX)
                    {
                        intervals.Add((rect.min.y, rect.max.y));
                    }
                }

                if (!YIntervalsCover(target.min.y, target.max.y, intervals))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool YIntervalsCover(float yMin, float yMax, List<(float y0, float y1)> intervals)
        {
            if (intervals.Count == 0)
            {
                return false;
            }

            intervals.Sort((a, b) => a.y0.CompareTo(b.y0));
            var coverTo = yMin;
            for (var i = 0; i < intervals.Count; i++)
            {
                var (y0, y1) = intervals[i];
                if (y0 > coverTo + 1e-6f)
                {
                    return false;
                }

                if (y1 > coverTo)
                {
                    coverTo = y1;
                }

                if (coverTo >= yMax - 1e-6f)
                {
                    return true;
                }
            }

            return coverTo >= yMax - 1e-6f;
        }

        public static float SmoothUnion(float a, float b, float k)
        {
            var h = Mathf.Clamp(0.5f + 0.5f * (b - a) / Mathf.Max(k, 1e-6f), 0f, 1f);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }
    }
}

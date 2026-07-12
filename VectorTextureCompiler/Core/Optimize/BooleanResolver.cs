using System;
using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Core.Optimize
{
    public static class BooleanResolver
    {
        public static List<Primitive> Resolve(IReadOnlyList<Primitive> input)
        {
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

            for (var i = 0; i < sorted.Count; i++)
            {
                if (!keep[i] || sorted[i].Color.a <= 0f)
                {
                    keep[i] = false;
                    continue;
                }

                var current = sorted[i];
                var currentBounds = GetBounds(current);
                for (var j = i + 1; j < sorted.Count; j++)
                {
                    if (!keep[j])
                    {
                        continue;
                    }

                    var cover = sorted[j];
                    if (cover.Layer < current.Layer || cover.Color.a < 0.999f)
                    {
                        continue;
                    }

                    if (cover.Type != PrimitiveKind.Rectangle || cover.ParameterCount > 0 || cover.GradientIndex != 0 || Mathf.Abs(cover.RotationDegrees) > 0.01f)
                    {
                        continue;
                    }

                    if (!SamePaint(current, cover))
                    {
                        continue;
                    }

                    var coverBounds = GetBounds(cover);
                    if (Contains(coverBounds, currentBounds))
                    {
                        keep[i] = false;
                        break;
                    }
                }
            }

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

        private static bool SamePaint(Primitive a, Primitive b)
        {
            return a.GradientIndex == b.GradientIndex
                && Mathf.Abs(a.Color.r - b.Color.r) <= 0.001f
                && Mathf.Abs(a.Color.g - b.Color.g) <= 0.001f
                && Mathf.Abs(a.Color.b - b.Color.b) <= 0.001f
                && Mathf.Abs(a.Color.a - b.Color.a) <= 0.001f
                && Mathf.Abs(a.Softness - b.Softness) <= 0.001f;
        }

        private static (Vector2 min, Vector2 max) GetBounds(Primitive primitive)
        {
            var p0 = primitive.Position;
            var p1 = primitive.Position + primitive.Size;
            return (Vector2.Min(p0, p1), Vector2.Max(p0, p1));
        }

        private static bool Contains((Vector2 min, Vector2 max) outer, (Vector2 min, Vector2 max) inner)
        {
            return outer.min.x <= inner.min.x
                && outer.min.y <= inner.min.y
                && outer.max.x >= inner.max.x
                && outer.max.y >= inner.max.y;
        }

        public static float SmoothUnion(float a, float b, float k)
        {
            var h = Mathf.Clamp(0.5f + 0.5f * (b - a) / Mathf.Max(k, 1e-6f), 0f, 1f);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }
    }
}
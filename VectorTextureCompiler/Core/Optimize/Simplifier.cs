using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Core.Optimize
{
    public static class Simplifier
    {
        public static List<Primitive> Simplify(IReadOnlyList<Primitive> input)
        {
            return Simplify(input, OptimizationSettings.FromProfile(OptimizationProfile.Pc));
        }

        public static List<Primitive> Simplify(IReadOnlyList<Primitive> input, OptimizationSettings settings)
        {
            var output = new List<Primitive>(input.Count);
            for (var i = 0; i < input.Count; i++)
            {
                var primitive = input[i];
                if (IsDegenerate(primitive))
                {
                    continue;
                }

                var merged = false;
                for (var j = 0; j < output.Count; j++)
                {
                    if (!CanMerge(output[j], primitive, settings))
                    {
                        continue;
                    }

                    output[j] = Merge(output[j], primitive);
                    merged = true;
                    break;
                }

                if (!merged)
                {
                    output.Add(primitive);
                }
            }

            return output;
        }

        private static bool IsDegenerate(Primitive primitive)
        {
            if (primitive.Color.a <= 0f)
            {
                return true;
            }

            if (primitive.Type == PrimitiveKind.Line)
            {
                return primitive.Size.sqrMagnitude <= 0.0000001f;
            }

            return primitive.Size.x <= 0f || primitive.Size.y <= 0f;
        }

        private static bool CanMerge(Primitive a, Primitive b, OptimizationSettings settings)
        {
            return a.Type == b.Type
                && a.Layer == b.Layer
                && a.GradientIndex == b.GradientIndex
                && Mathf.Abs(a.RotationDegrees - b.RotationDegrees) <= settings.MergeDistanceEpsilon
                && Mathf.Abs(a.Softness - b.Softness) <= settings.MergeDistanceEpsilon
                && ColorsEqual(a.Color, b.Color)
                && Vector2.Distance(a.Position, b.Position) <= settings.MergeDistanceEpsilon
                && Vector2.Distance(a.Size, b.Size) <= settings.MergeSizeEpsilon;
        }

        private static Primitive Merge(Primitive a, Primitive b)
        {
            var merged = a;
            merged.Position = (a.Position + b.Position) * 0.5f;
            merged.Size = Vector2.Max(a.Size, b.Size);
            merged.Softness = Mathf.Max(a.Softness, b.Softness);
            merged.StrokeRadius = Mathf.Max(a.StrokeRadius, b.StrokeRadius);
            return merged;
        }

        private static bool ColorsEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) <= 0.001f
                && Mathf.Abs(a.g - b.g) <= 0.001f
                && Mathf.Abs(a.b - b.b) <= 0.001f
                && Mathf.Abs(a.a - b.a) <= 0.001f;
        }
    }
}
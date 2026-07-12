using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Optimize
{
    public static class Quantizer
    {
        public static List<Primitive> Quantize(IReadOnlyList<Primitive> input)
        {
            return Quantize(input, OptimizationSettings.FromProfile(OptimizationProfile.Pc));
        }

        public static List<Primitive> Quantize(IReadOnlyList<Primitive> input, OptimizationSettings settings)
        {
            var output = new List<Primitive>(input.Count);
            for (var i = 0; i < input.Count; i++)
            {
                var primitive = input[i];
                // Path-backed primitives keep exact bounds: their geometry lives in the
                // edge texture, and rounding the bounds down can clip grid binning.
                if (primitive.ParameterCount == 0)
                {
                    primitive.Position = QuantizeVector2(primitive.Position, settings.PositionStep);
                    primitive.Size = QuantizeVector2(primitive.Size, settings.SizeStep);
                }
                primitive.RotationDegrees = QuantizeFloat(primitive.RotationDegrees, settings.RotationStep);
                primitive.Softness = Mathf.Clamp01(QuantizeFloat(primitive.Softness, settings.SoftnessStep));
                primitive.Color = QuantizeColor(primitive.Color);
                output.Add(primitive);
            }

            return output;
        }

        private static Vector2 QuantizeVector2(Vector2 value, float step)
        {
            return new Vector2(QuantizeFloat(value.x, step), QuantizeFloat(value.y, step));
        }

        private static float QuantizeFloat(float value, float step)
        {
            if (step <= 0f)
            {
                return value;
            }

            return Mathf.Round(value / step) * step;
        }

        private static Color QuantizeColor(Color color)
        {
            return new Color(
                QuantizeFloat(Mathf.Clamp01(color.r), 1f / 255f),
                QuantizeFloat(Mathf.Clamp01(color.g), 1f / 255f),
                QuantizeFloat(Mathf.Clamp01(color.b), 1f / 255f),
                QuantizeFloat(Mathf.Clamp01(color.a), 1f / 255f));
        }
    }
}
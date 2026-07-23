using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Optimize
{
    public static class PrimitiveBounds
    {
        public const float DegenerateAxisEpsilon = 0.0001f;

        public static (Vector2 min, Vector2 max) GetAabb(Primitive primitive, bool expandDegenerateAxes = false)
        {
            var p0 = primitive.Position;
            var p1 = primitive.Position + primitive.Size;
            var min = Vector2.Min(p0, p1);
            var max = Vector2.Max(p0, p1);

            if (expandDegenerateAxes)
            {
                if (Mathf.Approximately(min.x, max.x))
                {
                    max.x += DegenerateAxisEpsilon;
                }

                if (Mathf.Approximately(min.y, max.y))
                {
                    max.y += DegenerateAxisEpsilon;
                }
            }

            return (min, max);
        }

        public static bool IntersectsUnitCanvas((Vector2 min, Vector2 max) bounds)
        {
            return bounds.max.x >= 0f
                && bounds.min.x <= 1f
                && bounds.max.y >= 0f
                && bounds.min.y <= 1f;
        }

        public static bool IntersectsUnitCanvas(Primitive primitive)
        {
            return IntersectsUnitCanvas(GetAabb(primitive));
        }
    }
}

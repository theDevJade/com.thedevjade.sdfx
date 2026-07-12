using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.CodeGen
{
    public sealed class FlatTextureLayout
    {
        public int? PrimitiveWidthShift { get; set; }
        public int GridIndexWidthShift { get; set; } = 8;
        public int? PathWidthShift { get; set; }

        public static FlatTextureLayout FromTextures(Texture2D primitive, Texture2D gridIndex, Texture2D path)
        {
            return new FlatTextureLayout
            {
                PrimitiveWidthShift = GetPotShift(primitive != null ? primitive.width : 0),
                GridIndexWidthShift = GetPotShift(gridIndex != null ? gridIndex.width : 0) ?? 8,
                PathWidthShift = GetPotShift(path != null ? path.width : 0)
            };
        }

        public static int? GetPotShift(int width)
        {
            if (width <= 0 || (width & (width - 1)) != 0)
            {
                return null;
            }

            var shift = 0;
            while ((1 << shift) < width)
            {
                shift++;
            }

            return shift;
        }
    }
}

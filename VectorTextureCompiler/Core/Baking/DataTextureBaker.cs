using System;
using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Optimize;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Baking
{
    public static class DataTextureBaker
    {
        public const int TexelsPerPrimitive = 4;

        // IEEE half represents integers exactly only up to 2048.
        public const int HalfIndexSafeLimit = 2000;

        public sealed class FormatReport
        {
            public string PrimitiveFormat = string.Empty;
            public string GridLookupFormat = string.Empty;
            public string GridIndexFormat = string.Empty;
            public string PathFormat = string.Empty;
            public bool UsedHalfIndices;
        }

        public static int ComputePrimitiveTextureSize(int primitiveCount)
        {
            var texels = Math.Max(primitiveCount, 1) * TexelsPerPrimitive;
            var size = (int)Math.Ceiling(Math.Sqrt(texels));
            return RoundUpToPowerOfTwo(Math.Max(16, size));
        }

        public static int RoundUpToPowerOfTwo(int value)
        {
            if (value <= 1)
            {
                return 1;
            }

            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }

        public static bool CanUseHalfIndices(SpatialGrid grid)
        {
            if (grid == null)
            {
                return true;
            }

            var indexCount = grid.PrimitiveIndices?.Length ?? 0;
            if (indexCount >= HalfIndexSafeLimit)
            {
                return false;
            }

            var maxStartOrCount = 0;
            if (grid.Cells != null)
            {
                for (var i = 0; i < grid.Cells.Length; i++)
                {
                    var cell = grid.Cells[i];
                    maxStartOrCount = Math.Max(maxStartOrCount, cell.StartIndex);
                    maxStartOrCount = Math.Max(maxStartOrCount, cell.Count);
                    maxStartOrCount = Math.Max(maxStartOrCount, cell.StartIndex + cell.Count);
                }
            }

            if (maxStartOrCount >= HalfIndexSafeLimit)
            {
                return false;
            }

            if (grid.PrimitiveIndices != null)
            {
                for (var i = 0; i < grid.PrimitiveIndices.Length; i++)
                {
                    if (grid.PrimitiveIndices[i] >= HalfIndexSafeLimit)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static Texture2D BakePrimitiveTexture(Primitive[] primitives)
        {
            var size = ComputePrimitiveTextureSize(primitives.Length);
            return BakePrimitiveTexture(primitives, size, size);
        }

        public static Texture2D BakePrimitiveTexture(Primitive[] primitives, int width, int height)
        {
            var texture = NewDataTexture(width, height, TextureFormat.RGBAHalf);
            var pixels = new Color[width * height];

            var maxPrimitives = pixels.Length / TexelsPerPrimitive;
            var count = Math.Min(primitives.Length, maxPrimitives);
            if (primitives.Length > maxPrimitives)
            {
                Debug.LogWarning(SdfxLanguage.Compiler.PrimitiveTextureDropped(
                    width, height, maxPrimitives, primitives.Length - maxPrimitives));
            }

            for (var i = 0; i < count; i++)
            {
                var baseIdx = i * TexelsPerPrimitive;
                var p = primitives[i];
                pixels[baseIdx + 0] = new Color(p.Position.x, p.Position.y, p.Size.x, p.Size.y);
                pixels[baseIdx + 1] = EncodeColorForDataTexture(p.Color);
                var rotNorm = ((p.RotationDegrees % 360f) + 360f) % 360f / 360f;
                // t2.a reserved (Layer is resolved at bake time into grid index order).
                pixels[baseIdx + 2] = new Color((float)p.Type, p.Softness, rotNorm, 0f);
                // ParameterCount may be negative to mark a baked path SDF (pathCount < 0 in shader).
                pixels[baseIdx + 3] = new Color(
                    Math.Max(p.ParameterIndex, 0),
                    p.ParameterCount,
                    p.StrokeRadius,
                    Math.Max(p.GradientIndex, 0));
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return texture;
        }

        public static Texture2D BakeGridLookupTexture(SpatialGrid grid, bool useHalf = true)
        {
            var format = useHalf ? TextureFormat.RGHalf : TextureFormat.RGFloat;
            var texture = NewDataTexture(grid.Width, grid.Height, format);
            var pixels = new Color[grid.Width * grid.Height];

            for (var i = 0; i < grid.Cells.Length; i++)
            {
                var cell = grid.Cells[i];
                pixels[i] = new Color(cell.StartIndex, cell.Count, 0f, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return texture;
        }

        public static Texture2D BakeGridLookupTexture(SpatialGrid grid)
        {
            return BakeGridLookupTexture(grid, CanUseHalfIndices(grid));
        }

        public static Texture2D BakeGridIndexTexture(SpatialGrid grid, int width, bool useHalf = true)
        {
            var indexCount = grid.PrimitiveIndices.Length;
            var height = Math.Max(1, (int)Math.Ceiling((double)Math.Max(indexCount, 1) / width));
            var format = useHalf ? TextureFormat.RHalf : TextureFormat.RFloat;
            var texture = NewDataTexture(width, height, format);
            var pixels = new Color[width * height];
            for (var i = 0; i < indexCount; i++)
            {
                pixels[i] = new Color(grid.PrimitiveIndices[i], 0f, 0f, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return texture;
        }

        public static Texture2D BakeGridIndexTexture(SpatialGrid grid, int width)
        {
            return BakeGridIndexTexture(grid, width, CanUseHalfIndices(grid));
        }

        public static Texture2D BakePathDataTexture(IReadOnlyList<Vector4> pathEdges)
        {
            var count = pathEdges?.Count ?? 0;
            var size = RoundUpToPowerOfTwo(Math.Max(4, (int)Math.Ceiling(Math.Sqrt(Math.Max(count, 1)))));

            var texture = NewDataTexture(size, size, TextureFormat.RGBAHalf);
            var pixels = new Color[size * size];
            for (var i = 0; i < count; i++)
            {
                var edge = pathEdges[i];
                pixels[i] = new Color(edge.x, edge.y, edge.z, edge.w);
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return texture;
        }

        public static FormatReport DescribeFormats(SpatialGrid grid)
        {
            var halfIndices = CanUseHalfIndices(grid);
            return new FormatReport
            {
                PrimitiveFormat = TextureFormat.RGBAHalf.ToString(),
                PathFormat = TextureFormat.RGBAHalf.ToString(),
                GridLookupFormat = (halfIndices ? TextureFormat.RGHalf : TextureFormat.RGFloat).ToString(),
                GridIndexFormat = (halfIndices ? TextureFormat.RHalf : TextureFormat.RFloat).ToString(),
                UsedHalfIndices = halfIndices
            };
        }

        private static Texture2D NewDataTexture(int width, int height, TextureFormat format)
        {
            return new Texture2D(width, height, format, mipChain: false, linear: true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
                name = "SDFX_DataTexture"
            };
        }

        /// <summary>
        /// Data textures are created with <c>linear: true</c> (no sRGB sampler decode).
        /// SVG / VectorGraphics colors are authored in sRGB, so convert RGB to linear when
        /// the project uses Linear color space.
        /// </summary>
        public static Color EncodeColorForDataTexture(Color srgb)
        {
            if (QualitySettings.activeColorSpace != ColorSpace.Linear)
            {
                return srgb;
            }

            var linear = srgb.linear;
            linear.a = srgb.a;
            return linear;
        }
    }
}

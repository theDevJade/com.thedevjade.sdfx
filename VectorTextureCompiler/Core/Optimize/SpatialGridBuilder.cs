using System;
using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Optimize
{
    public readonly struct SpatialGridCell
    {
        public SpatialGridCell(int startIndex, int count)
        {
            StartIndex = startIndex;
            Count = count;
        }

        public int StartIndex { get; }
        public int Count { get; }
    }

    public sealed class SpatialGrid
    {
        public SpatialGrid(int width, int height, SpatialGridCell[] cells, int[] primitiveIndices, int droppedPrimitiveReferences)
        {
            Width = width;
            Height = height;
            Cells = cells;
            PrimitiveIndices = primitiveIndices;
            DroppedPrimitiveReferences = droppedPrimitiveReferences;
        }

        public int Width { get; }
        public int Height { get; }
        public SpatialGridCell[] Cells { get; }
        public int[] PrimitiveIndices { get; }
        public int DroppedPrimitiveReferences { get; }
    }

    public static class SpatialGridBuilder
    {
        public static SpatialGrid Build(IReadOnlyList<Primitive> primitives, int width, int height, int maxPerCell)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (maxPerCell <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPerCell));
            }

            var totalCells = width * height;
            var cells = new SpatialGridCell[totalCells];
            var indices = new List<int>(Math.Max(primitives.Count * 2, totalCells));
            var buckets = new List<int>[totalCells];

            // Grid domain is fixed [0,1] UV; shader samples lookup with raw fragment UV.
            var domainMin = Vector2.zero;
            const float domainWidth = 1f;
            const float domainHeight = 1f;

            for (var primitiveIndex = 0; primitiveIndex < primitives.Count; primitiveIndex++)
            {
                var bounds = GetBounds(primitives[primitiveIndex]);

                var xMin = Mathf.Clamp((int)Mathf.Floor((bounds.min.x - domainMin.x) / domainWidth * width), 0, width - 1);
                var yMin = Mathf.Clamp((int)Mathf.Floor((bounds.min.y - domainMin.y) / domainHeight * height), 0, height - 1);
                var xMax = Mathf.Clamp((int)Mathf.Floor((bounds.max.x - domainMin.x) / domainWidth * width), 0, width - 1);
                var yMax = Mathf.Clamp((int)Mathf.Floor((bounds.max.y - domainMin.y) / domainHeight * height), 0, height - 1);

                xMin = Mathf.Max(0, xMin - 1);
                yMin = Mathf.Max(0, yMin - 1);
                xMax = Mathf.Min(width - 1, xMax + 1);
                yMax = Mathf.Min(height - 1, yMax + 1);

                if (xMax < xMin)
                {
                    xMax = xMin;
                }

                if (yMax < yMin)
                {
                    yMax = yMin;
                }

                for (var y = yMin; y <= yMax; y++)
                {
                    for (var x = xMin; x <= xMax; x++)
                    {
                        var cellIndex = y * width + x;
                        var bucket = buckets[cellIndex];
                        if (bucket == null)
                        {
                            bucket = new List<int>(Math.Min(maxPerCell, 8));
                            buckets[cellIndex] = bucket;
                        }

                        bucket.Add(primitiveIndex);
                    }
                }
            }

            var droppedReferences = 0;
            for (var cellIndex = 0; cellIndex < totalCells; cellIndex++)
            {
                var bucket = buckets[cellIndex];
                var startIndex = indices.Count;
                if (bucket == null || bucket.Count == 0)
                {
                    cells[cellIndex] = new SpatialGridCell(startIndex, 0);
                    continue;
                }

                var keptCount = Mathf.Min(maxPerCell, bucket.Count);
                var dropCount = bucket.Count - keptCount;
                var keepFrom = dropCount;
                for (var i = keepFrom; i < bucket.Count; i++)
                {
                    indices.Add(bucket[i]);
                }

                if (dropCount > 0)
                {
                    droppedReferences += dropCount;
                }

                cells[cellIndex] = new SpatialGridCell(startIndex, keptCount);
            }

            return new SpatialGrid(width, height, cells, indices.ToArray(), droppedReferences);
        }

        private static (Vector2 min, Vector2 max) GetBounds(Primitive primitive)
        {
            var p0 = primitive.Position;
            var p1 = primitive.Position + primitive.Size;
            var min = Vector2.Min(p0, p1);
            var max = Vector2.Max(p0, p1);

            if (Mathf.Approximately(min.x, max.x))
            {
                max.x += 0.0001f;
            }

            if (Mathf.Approximately(min.y, max.y))
            {
                max.y += 0.0001f;
            }

            return (min, max);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Core.Baking
{
    public static class PathSdfBaker
    {
        public const int EdgeCountThreshold = 48;
        public const int DefaultBakeResolution = 160;
        public const int MinBakeResolution = 96;
        public const int MaxBakeResolution = 256;
        public const float DefaultPxRange = 8f;
        public const int AtlasPadding = 2;

        private const int SpatialCells = 48;

        public readonly struct BakedSlot
        {
            public BakedSlot(int primitiveIndex, Rect atlasUvRect, int resolution)
            {
                PrimitiveIndex = primitiveIndex;
                AtlasUvRect = atlasUvRect;
                Resolution = resolution;
            }

            public int PrimitiveIndex { get; }
            public Rect AtlasUvRect { get; }
            public int Resolution { get; }
        }

        public sealed class BakeResult
        {
            public Texture2D Atlas { get; set; }
            public Texture2D Meta { get; set; }
            public IReadOnlyList<BakedSlot> Slots { get; set; } = Array.Empty<BakedSlot>();
            public float PxRange { get; set; } = DefaultPxRange;
            public int BakedCount => Slots?.Count ?? 0;
        }

        public static BakeResult Bake(
            Primitive[] primitives,
            IReadOnlyList<Vector4> pathEdges,
            float pxRange = DefaultPxRange)
        {
            if (primitives == null || primitives.Length == 0 || pathEdges == null)
            {
                return new BakeResult();
            }

            var candidates = new List<(int index, int resolution)>();
            for (var i = 0; i < primitives.Length; i++)
            {
                var p = primitives[i];
                if ((p.Type != PrimitiveKind.Polygon && p.Type != PrimitiveKind.Polyline)
                    || p.ParameterCount < EdgeCountThreshold)
                {
                    continue;
                }

                candidates.Add((i, ResolveResolution(p)));
            }

            if (candidates.Count == 0)
            {
                return new BakeResult();
            }

            var patches = new (int primIndex, int res, float[] sdf, Vector2 origin, Vector2 extent)[candidates.Count];
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Parallel.For(0, candidates.Count, c =>
            {
                var (primIndex, res) = candidates[c];
                var sdf = RasterizeSignedDistance(
                    primitives[primIndex],
                    pathEdges,
                    res,
                    out var bakeOrigin,
                    out var bakeExtent);
                patches[c] = (primIndex, res, sdf, bakeOrigin, bakeExtent);
            });

            var patchList = new List<(int primIndex, int res, float[] sdf, Vector2 origin, Vector2 extent)>(candidates.Count);
            for (var i = 0; i < patches.Length; i++)
            {
                if (patches[i].sdf != null)
                {
                    patchList.Add(patches[i]);
                }
            }

            if (patchList.Count == 0)
            {
                return new BakeResult();
            }

            PackAtlas(patchList, out var atlasSize, out var placements, out var atlasPixels);
            var atlas = new Texture2D(atlasSize, atlasSize, TextureFormat.RHalf, mipChain: false, linear: true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
                name = "SDFX_BakedSdfAtlas"
            };
            atlas.SetPixelData(atlasPixels, 0);
            atlas.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            var metaSize = DataTextureBaker.RoundUpToPowerOfTwo(Math.Max(4, patchList.Count));
            var meta = new Texture2D(metaSize, 1, TextureFormat.RGBAHalf, mipChain: false, linear: true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
                name = "SDFX_BakedSdfMeta"
            };
            var metaPixels = new Color[metaSize];
            var slots = new List<BakedSlot>(patchList.Count);

            for (var i = 0; i < patchList.Count; i++)
            {
                var (primIndex, res, _, bakeOrigin, bakeExtent) = patchList[i];
                var place = placements[i];
                var uMin = (place.x + 0.5f) / atlasSize;
                var vMin = (place.y + 0.5f) / atlasSize;
                var uMax = (place.x + res - 0.5f) / atlasSize;
                var vMax = (place.y + res - 0.5f) / atlasSize;
                metaPixels[i] = new Color(uMin, vMin, uMax, vMax);

                var prim = primitives[primIndex];
                prim.Position = bakeOrigin;
                prim.Size = bakeExtent;
                prim.ParameterIndex = i;
                prim.ParameterCount = -1;
                primitives[primIndex] = prim;

                slots.Add(new BakedSlot(primIndex, new Rect(uMin, vMin, uMax - uMin, vMax - vMin), res));
            }

            meta.SetPixels(metaPixels);
            meta.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            sw.Stop();
            Debug.Log(SdfxLanguage.Compiler.PathSdfBakeStage(slots.Count, atlasSize, pxRange)
                      + $" ({sw.ElapsedMilliseconds} ms)");

            return new BakeResult
            {
                Atlas = atlas,
                Meta = meta,
                Slots = slots,
                PxRange = pxRange
            };
        }

        private static int ResolveResolution(Primitive p)
        {
            var area = Mathf.Max(p.Size.x * p.Size.y, 1e-6f);
            var res = DefaultBakeResolution;
            if (p.ParameterCount >= 160 || area < 0.04f)
            {
                res = MaxBakeResolution;
            }
            else if (area > 0.35f && p.ParameterCount < 80)
            {
                res = MinBakeResolution;
            }

            return Mathf.Clamp(res, MinBakeResolution, MaxBakeResolution);
        }

        private static float[] RasterizeSignedDistance(
            Primitive prim,
            IReadOnlyList<Vector4> pathEdges,
            int resolution,
            out Vector2 bakeOrigin,
            out Vector2 bakeExtent)
        {
            var padUv = Mathf.Max(prim.Size.x, prim.Size.y) * (4f / resolution);
            if (prim.Type == PrimitiveKind.Polyline)
            {
                padUv = Mathf.Max(padUv, prim.StrokeRadius * 2f);
            }

            bakeOrigin = prim.Position - new Vector2(padUv, padUv);
            bakeExtent = prim.Size + new Vector2(padUv * 2f, padUv * 2f);
            var origin = bakeOrigin;
            var extent = bakeExtent;
            var pixelSize = new Vector2(extent.x / resolution, extent.y / resolution);

            var start = prim.ParameterIndex;
            var count = Mathf.Max(prim.ParameterCount, 0);
            if (count <= 0 || start < 0 || start + count > pathEdges.Count)
            {
                return null;
            }

            // Flat edge buffer: ax, ay, bx, by per edge (UV space).
            var edges = new float[count * 4];
            for (var e = 0; e < count; e++)
            {
                var seg = pathEdges[start + e];
                var i = e * 4;
                edges[i] = seg.x;
                edges[i + 1] = seg.y;
                edges[i + 2] = seg.z;
                edges[i + 3] = seg.w;
            }

            var spatial = BuildEdgeSpatialGrid(edges, count, origin, extent);
            var sdf = new float[resolution * resolution];
            var isStroke = prim.Type == PrimitiveKind.Polyline;
            var stroke = prim.StrokeRadius;

            if (isStroke)
            {
                Parallel.For(0, resolution, y =>
                {
                    var row = y * resolution;
                    var uy = origin.y + (y + 0.5f) * pixelSize.y;
                    for (var x = 0; x < resolution; x++)
                    {
                        var ux = origin.x + (x + 0.5f) * pixelSize.x;
                        var d = NearestEdgeDistance(spatial, edges, ux, uy);
                        sdf[row + x] = d - stroke;
                    }
                });
                return sdf;
            }

            var mask = ScanlineFillMask(edges, count, origin, pixelSize, resolution);

            Parallel.For(0, resolution, y =>
            {
                var row = y * resolution;
                var uy = origin.y + (y + 0.5f) * pixelSize.y;
                for (var x = 0; x < resolution; x++)
                {
                    var i = row + x;
                    var ux = origin.x + (x + 0.5f) * pixelSize.x;
                    var d = NearestEdgeDistance(spatial, edges, ux, uy);
                    sdf[i] = mask[i] ? -d : d;
                }
            });

            return sdf;
        }

        private sealed class EdgeSpatialGrid
        {
            public int CellsX;
            public int CellsY;
            public Vector2 Origin;
            public Vector2 CellSize;
            public List<int>[] Buckets;
        }

        private static EdgeSpatialGrid BuildEdgeSpatialGrid(
            float[] edges,
            int count,
            Vector2 origin,
            Vector2 extent)
        {
            var cells = SpatialCells;
            var grid = new EdgeSpatialGrid
            {
                CellsX = cells,
                CellsY = cells,
                Origin = origin,
                CellSize = new Vector2(
                    Mathf.Max(extent.x / cells, 1e-8f),
                    Mathf.Max(extent.y / cells, 1e-8f)),
                Buckets = new List<int>[cells * cells]
            };

            for (var e = 0; e < count; e++)
            {
                var i = e * 4;
                var minX = Mathf.Min(edges[i], edges[i + 2]);
                var maxX = Mathf.Max(edges[i], edges[i + 2]);
                var minY = Mathf.Min(edges[i + 1], edges[i + 3]);
                var maxY = Mathf.Max(edges[i + 1], edges[i + 3]);

                var x0 = Mathf.Clamp((int)((minX - origin.x) / grid.CellSize.x), 0, cells - 1);
                var x1 = Mathf.Clamp((int)((maxX - origin.x) / grid.CellSize.x), 0, cells - 1);
                var y0 = Mathf.Clamp((int)((minY - origin.y) / grid.CellSize.y), 0, cells - 1);
                var y1 = Mathf.Clamp((int)((maxY - origin.y) / grid.CellSize.y), 0, cells - 1);

                for (var cy = y0; cy <= y1; cy++)
                {
                    for (var cx = x0; cx <= x1; cx++)
                    {
                        var bi = cy * cells + cx;
                        grid.Buckets[bi] ??= new List<int>(4);
                        grid.Buckets[bi].Add(e);
                    }
                }
            }

            return grid;
        }

        private static float NearestEdgeDistance(
            EdgeSpatialGrid grid,
            float[] edges,
            float ux,
            float uy)
        {
            var cx = Mathf.Clamp((int)((ux - grid.Origin.x) / grid.CellSize.x), 0, grid.CellsX - 1);
            var cy = Mathf.Clamp((int)((uy - grid.Origin.y) / grid.CellSize.y), 0, grid.CellsY - 1);
            var best = float.PositiveInfinity;
            var maxRing = Mathf.Max(grid.CellsX, grid.CellsY);

            for (var ring = 0; ring <= maxRing; ring++)
            {
                var found = false;
                var y0 = Math.Max(0, cy - ring);
                var y1 = Math.Min(grid.CellsY - 1, cy + ring);
                var x0 = Math.Max(0, cx - ring);
                var x1 = Math.Min(grid.CellsX - 1, cx + ring);

                for (var gy = y0; gy <= y1; gy++)
                {
                    for (var gx = x0; gx <= x1; gx++)
                    {
                        if (ring > 0
                            && gx != x0 && gx != x1
                            && gy != y0 && gy != y1)
                        {
                            continue;
                        }

                        var bucket = grid.Buckets[gy * grid.CellsX + gx];
                        if (bucket == null)
                        {
                            continue;
                        }

                        for (var b = 0; b < bucket.Count; b++)
                        {
                            var e = bucket[b] * 4;
                            var dSq = SegmentDistSq(
                                ux, uy,
                                edges[e], edges[e + 1],
                                edges[e + 2], edges[e + 3]);
                            if (dSq < best)
                            {
                                best = dSq;
                                found = true;
                            }
                        }
                    }
                }

                if (found)
                {
                    var minCell = Mathf.Min(grid.CellSize.x, grid.CellSize.y);
                    var ringReach = (ring + 1) * minCell;
                    if (ringReach * ringReach >= best)
                    {
                        break;
                    }
                }
            }

            if (float.IsPositiveInfinity(best))
            {
                return 0f;
            }

            return Mathf.Sqrt(best);
        }

        private static float SegmentDistSq(float px, float py, float ax, float ay, float bx, float by)
        {
            var abx = bx - ax;
            var aby = by - ay;
            var apx = px - ax;
            var apy = py - ay;
            var abLenSq = abx * abx + aby * aby;
            var t = abLenSq > 1e-24f ? Mathf.Clamp01((apx * abx + apy * aby) / abLenSq) : 0f;
            var qx = apx - abx * t;
            var qy = apy - aby * t;
            return qx * qx + qy * qy;
        }

        private static bool[] ScanlineFillMask(
            float[] edges,
            int count,
            Vector2 origin,
            Vector2 pixelSize,
            int resolution)
        {
            var mask = new bool[resolution * resolution];
            var crossings = new List<float>[resolution];
            for (var y = 0; y < resolution; y++)
            {
                crossings[y] = new List<float>(8);
            }

            var invPy = 1f / Mathf.Max(pixelSize.y, 1e-12f);
            var invPx = 1f / Mathf.Max(pixelSize.x, 1e-12f);

            for (var e = 0; e < count; e++)
            {
                var i = e * 4;
                var ax = edges[i];
                var ay = edges[i + 1];
                var bx = edges[i + 2];
                var by = edges[i + 3];
                if (Mathf.Abs(by - ay) < 1e-12f)
                {
                    continue;
                }

                if (ay > by)
                {
                    var tx = ax;
                    var ty = ay;
                    ax = bx;
                    ay = by;
                    bx = tx;
                    by = ty;
                }

                var y0 = Mathf.Clamp(Mathf.CeilToInt((ay - origin.y) * invPy - 0.5f), 0, resolution - 1);
                var y1 = Mathf.Clamp(Mathf.FloorToInt((by - origin.y) * invPy - 0.5f), 0, resolution - 1);
                if (y1 < y0)
                {
                    continue;
                }

                var dy = by - ay;
                for (var y = y0; y <= y1; y++)
                {
                    var uy = origin.y + (y + 0.5f) * pixelSize.y;
                    if (uy < ay || uy >= by)
                    {
                        continue;
                    }

                    var t = (uy - ay) / dy;
                    var ux = ax + (bx - ax) * t;
                    crossings[y].Add((ux - origin.x) * invPx);
                }
            }

            for (var y = 0; y < resolution; y++)
            {
                var row = crossings[y];
                if (row.Count == 0)
                {
                    continue;
                }

                row.Sort();
                var baseIdx = y * resolution;
                for (var c = 0; c + 1 < row.Count; c += 2)
                {
                    var x0 = Mathf.Clamp(Mathf.CeilToInt(row[c] - 0.5f), 0, resolution);
                    var x1 = Mathf.Clamp(Mathf.FloorToInt(row[c + 1] - 0.5f), -1, resolution - 1);
                    for (var x = x0; x <= x1; x++)
                    {
                        mask[baseIdx + x] = true;
                    }
                }
            }

            return mask;
        }

        public static float[] JumpFloodSignedDistanceCpu(bool[] mask, int resolution, Vector2 pixelSizeUv)
        {
            var n = resolution * resolution;
            var seedX = new float[n];
            var seedY = new float[n];

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var i = y * resolution + x;
                    var inside = mask[i];
                    var boundary = false;
                    if (x > 0 && mask[i - 1] != inside) boundary = true;
                    if (x + 1 < resolution && mask[i + 1] != inside) boundary = true;
                    if (y > 0 && mask[i - resolution] != inside) boundary = true;
                    if (y + 1 < resolution && mask[i + resolution] != inside) boundary = true;

                    if (boundary)
                    {
                        seedX[i] = x;
                        seedY[i] = y;
                    }
                    else
                    {
                        seedX[i] = -1f;
                        seedY[i] = -1f;
                    }
                }
            }

            var outX = new float[n];
            var outY = new float[n];
            for (var step = Mathf.NextPowerOfTwo(resolution) / 2; step >= 1; step /= 2)
            {
                JumpFloodPass(seedX, seedY, outX, outY, resolution, step);
                var tmpX = seedX;
                var tmpY = seedY;
                seedX = outX;
                seedY = outY;
                outX = tmpX;
                outY = tmpY;
            }

            var sdf = new float[n];
            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var i = y * resolution + x;
                    var sx = seedX[i];
                    var sy = seedY[i];
                    float distUv;
                    if (sx < 0f)
                    {
                        distUv = 0f;
                    }
                    else
                    {
                        var dx = (sx - x) * pixelSizeUv.x;
                        var dy = (sy - y) * pixelSizeUv.y;
                        distUv = Mathf.Sqrt(dx * dx + dy * dy);
                    }

                    sdf[i] = mask[i] ? -distUv : distUv;
                }
            }

            return sdf;
        }

        private static void JumpFloodPass(
            float[] inX,
            float[] inY,
            float[] outX,
            float[] outY,
            int resolution,
            int step)
        {
            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var i = y * resolution + x;
                    var bestX = inX[i];
                    var bestY = inY[i];
                    var bestDist = bestX < 0f
                        ? float.PositiveInfinity
                        : (bestX - x) * (bestX - x) + (bestY - y) * (bestY - y);

                    for (var oy = -1; oy <= 1; oy++)
                    {
                        for (var ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oy == 0)
                            {
                                continue;
                            }

                            var nx = x + ox * step;
                            var ny = y + oy * step;
                            if (nx < 0 || ny < 0 || nx >= resolution || ny >= resolution)
                            {
                                continue;
                            }

                            var ni = ny * resolution + nx;
                            var cx = inX[ni];
                            var cy = inY[ni];
                            if (cx < 0f)
                            {
                                continue;
                            }

                            var dist = (cx - x) * (cx - x) + (cy - y) * (cy - y);
                            if (dist < bestDist)
                            {
                                bestDist = dist;
                                bestX = cx;
                                bestY = cy;
                            }
                        }
                    }

                    outX[i] = bestX;
                    outY[i] = bestY;
                }
            }
        }

        private static void PackAtlas(
            List<(int primIndex, int res, float[] sdf, Vector2 origin, Vector2 extent)> patches,
            out int atlasSize,
            out List<Vector2Int> placements,
            out ushort[] atlasPixels)
        {
            var totalArea = 0;
            var maxRes = 0;
            for (var i = 0; i < patches.Count; i++)
            {
                var res = patches[i].res;
                totalArea += (res + AtlasPadding) * (res + AtlasPadding);
                maxRes = Mathf.Max(maxRes, res);
            }

            atlasSize = Mathf.Max(
                DataTextureBaker.RoundUpToPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(totalArea))),
                DataTextureBaker.RoundUpToPowerOfTwo(maxRes + AtlasPadding));

            while (true)
            {
                placements = new List<Vector2Int>(patches.Count);
                atlasPixels = new ushort[atlasSize * atlasSize];
                var cursorX = AtlasPadding;
                var cursorY = AtlasPadding;
                var rowHeight = 0;
                var ok = true;

                for (var i = 0; i < patches.Count; i++)
                {
                    var res = patches[i].res;
                    if (cursorX + res + AtlasPadding > atlasSize)
                    {
                        cursorX = AtlasPadding;
                        cursorY += rowHeight + AtlasPadding;
                        rowHeight = 0;
                    }

                    if (cursorY + res + AtlasPadding > atlasSize)
                    {
                        ok = false;
                        break;
                    }

                    placements.Add(new Vector2Int(cursorX, cursorY));
                    var sdf = patches[i].sdf;
                    for (var y = 0; y < res; y++)
                    {
                        for (var x = 0; x < res; x++)
                        {
                            var dst = (cursorY + y) * atlasSize + (cursorX + x);
                            atlasPixels[dst] = Mathf.FloatToHalf(sdf[y * res + x]);
                        }
                    }

                    cursorX += res + AtlasPadding;
                    rowHeight = Mathf.Max(rowHeight, res);
                }

                if (ok)
                {
                    return;
                }

                atlasSize *= 2;
            }
        }
    }
}

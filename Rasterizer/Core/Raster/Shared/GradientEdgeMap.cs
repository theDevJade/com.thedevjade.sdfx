using System.Collections.Generic;
using SDFX.Rasterizer;
using UnityEditor;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class GradientEdgeMap
    {
        private const string EdgeComputeShaderPath = "Packages/com.thedevjade.sdfx/Rasterizer/Compute/RasterEdgeDetect.compute";

        public static float[] Compute(RasterImageBuffer image, RasterParsingOptions options, List<RasterIssue> issues, RasterGpuBuffers gpuBuffers = null)
        {
            if (gpuBuffers?.EdgeMap != null)
            {
                return gpuBuffers.EdgeMap;
            }

            if (!options.UseComputeAcceleration)
            {
                return ComputeCpu(image);
            }

            var gpu = TryComputeGpu(image, options);
            if (gpu == null)
            {
                issues?.Add(new RasterIssue(
                    RasterIssueSeverity.Warning,
                    "Raster compute unavailable; using CPU fallback.",
                    "raster",
                    0,
                    RasterIssueCode.RasterComputeUnavailable));
                return ComputeCpu(image);
            }

            return gpu;
        }

        public static float Sample(Color32[] pixels, float[] edgeMap, int width, int height, int x, int y, float edgeThreshold)
        {
            var idx = y * width + x;
            if (edgeMap != null && idx >= 0 && idx < edgeMap.Length)
            {
                return edgeMap[idx];
            }

            if (x <= 0 || y <= 0 || x >= width - 1 || y >= height - 1)
            {
                return 0f;
            }

            var c = pixels[idx];
            var right = pixels[idx + 1];
            var down = pixels[idx + width];
            return Mathf.Abs(Luma(c) - Luma(right)) + Mathf.Abs(Luma(c) - Luma(down));
        }

        public static Color32[] BuildEdgePreview(RasterImageBuffer image, float[] edgeMap, RasterParsingOptions options, RasterGpuBuffers gpuBuffers = null)
        {
            if (gpuBuffers?.EdgePreviewPixels != null)
            {
                return gpuBuffers.EdgePreviewPixels;
            }

            var output = new Color32[image.Pixels.Length];
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var idx = image.Index(x, y);
                    var c = image.Pixels[idx];
                    var edge = Sample(image.Pixels, edgeMap, image.Width, image.Height, x, y, options.EdgeThreshold);
                    var hit = edge >= options.EdgeThreshold && c.a / 255f >= options.MinAlpha;
                    output[idx] = hit
                        ? new Color32(255, 70, 70, 255)
                        : new Color32((byte)(c.r / 2), (byte)(c.g / 2), (byte)(c.b / 2), 255);
                }
            }

            return output;
        }

        public static List<List<Vector2>> ChainEdgePixels(bool[] edgeMask, int width, int height)
        {
            var visited = new bool[edgeMask.Length];
            var chains = new List<List<Vector2>>();
            var neighbors = new[]
            {
                new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(-1, 1),
                new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1)
            };

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var idx = y * width + x;
                    if (!edgeMask[idx] || visited[idx])
                    {
                        continue;
                    }

                    var chain = new List<Vector2> { new Vector2(x + 0.5f, y + 0.5f) };
                    visited[idx] = true;
                    var cx = x;
                    var cy = y;
                    for (var guard = 0; guard < edgeMask.Length; guard++)
                    {
                        var found = false;
                        for (var n = 0; n < neighbors.Length; n++)
                        {
                            var nx = cx + neighbors[n].x;
                            var ny = cy + neighbors[n].y;
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                            {
                                continue;
                            }

                            var nidx = ny * width + nx;
                            if (!edgeMask[nidx] || visited[nidx])
                            {
                                continue;
                            }

                            visited[nidx] = true;
                            cx = nx;
                            cy = ny;
                            chain.Add(new Vector2(cx + 0.5f, cy + 0.5f));
                            found = true;
                            break;
                        }

                        if (!found)
                        {
                            break;
                        }
                    }

                    if (chain.Count >= 2)
                    {
                        chains.Add(chain);
                    }
                }
            }

            return chains;
        }

        private static float[] ComputeCpu(RasterImageBuffer image)
        {
            var map = new float[image.Pixels.Length];
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    map[image.Index(x, y)] = Sample(image.Pixels, null, image.Width, image.Height, x, y, 0f);
                }
            }

            return map;
        }

        private static float[] TryComputeGpu(RasterImageBuffer image, RasterParsingOptions options)
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                return null;
            }

            var compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(EdgeComputeShaderPath);
            if (compute == null)
            {
                return null;
            }

            var width = image.Width;
            var height = image.Height;
            var shouldTile = options.UseTiling &&
                             (width >= Mathf.Max(1, options.AutoTileMinDimension) ||
                              height >= Mathf.Max(1, options.AutoTileMinDimension));

            var kernel = compute.FindKernel("CSMain");
            var sourceRt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var edgeRt = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            try
            {
                sourceRt.Create();
                edgeRt.Create();
                Graphics.Blit(image.ReadableCopy, sourceRt);
                compute.SetInt("_Width", width);
                compute.SetInt("_Height", height);
                compute.SetTexture(kernel, "_SourceTex", sourceRt);
                compute.SetTexture(kernel, "_EdgeTex", edgeRt);
                compute.Dispatch(kernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);

                var previous = RenderTexture.active;
                RenderTexture.active = edgeRt;
                try
                {
                    return shouldTile ? ReadTiled(width, height, options) : ReadFull(width, height);
                }
                finally
                {
                    RenderTexture.active = previous;
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                if (RenderTexture.active == sourceRt || RenderTexture.active == edgeRt)
                {
                    RenderTexture.active = null;
                }

                if (sourceRt.IsCreated()) sourceRt.Release();
                if (edgeRt.IsCreated()) edgeRt.Release();
                Object.DestroyImmediate(sourceRt);
                Object.DestroyImmediate(edgeRt);
            }
        }

        private static float[] ReadFull(int width, int height)
        {
            var readback = new Texture2D(width, height, TextureFormat.RFloat, false, true);
            try
            {
                readback.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                readback.Apply(false, false);
                var colors = readback.GetPixels();
                var values = new float[colors.Length];
                for (var i = 0; i < colors.Length; i++)
                {
                    values[i] = colors[i].r;
                }

                return values;
            }
            finally
            {
                Object.DestroyImmediate(readback);
            }
        }

        private static float[] ReadTiled(int width, int height, RasterParsingOptions options)
        {
            var output = new float[width * height];
            var tileSize = Mathf.Max(64, options.TileSize);
            var overlap = Mathf.Clamp(options.TileOverlap, 0, 8);
            for (var y = 0; y < height; y += tileSize)
            {
                for (var x = 0; x < width; x += tileSize)
                {
                    var coreWidth = Mathf.Min(tileSize, width - x);
                    var coreHeight = Mathf.Min(tileSize, height - y);
                    var expandedX = Mathf.Max(0, x - overlap);
                    var expandedY = Mathf.Max(0, y - overlap);
                    var expandedMaxX = Mathf.Min(width, x + coreWidth + overlap);
                    var expandedMaxY = Mathf.Min(height, y + coreHeight + overlap);
                    var expandedWidth = Mathf.Max(1, expandedMaxX - expandedX);
                    var expandedHeight = Mathf.Max(1, expandedMaxY - expandedY);
                    var readback = new Texture2D(expandedWidth, expandedHeight, TextureFormat.RFloat, false, true);
                    try
                    {
                        readback.ReadPixels(new Rect(expandedX, expandedY, expandedWidth, expandedHeight), 0, 0);
                        readback.Apply(false, false);
                        var colors = readback.GetPixels();
                        for (var py = y; py < y + coreHeight; py++)
                        {
                            var tileY = py - expandedY;
                            var srcRow = tileY * expandedWidth;
                            var dstRow = py * width;
                            for (var px = x; px < x + coreWidth; px++)
                            {
                                output[dstRow + px] = colors[srcRow + (px - expandedX)].r;
                            }
                        }
                    }
                    finally
                    {
                        Object.DestroyImmediate(readback);
                    }
                }
            }

            return output;
        }

        private static float Luma(Color32 c) => (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
    }
}

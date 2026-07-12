using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class MarchingSquares
    {
        public static List<List<Vector2>> ExtractBoundaries(int[] labels, int width, int height, int targetLabel)
        {
            var edges = new List<EdgeSegment>(256);
            CollectEdgesForLabel(labels, width, height, targetLabel, edges);
            return ChainEdges(edges);
        }

        public static Dictionary<int, List<List<Vector2>>> ExtractAllBoundaries(int[] labels, int width, int height)
        {
            var edgesByLabel = new Dictionary<int, List<EdgeSegment>>();
            var scratchLabels = new int[4];

            // Cells extend one step past the image so border-touching regions get
            // closing edges along the image edge (out-of-bounds samples read -1).
            // Without this, their contours stay open and 'Z' slams them shut with
            // a diagonal cut across the shape.
            for (var y = -1; y < height; y++)
            {
                for (var x = -1; x < width; x++)
                {
                    scratchLabels[0] = LabelValue(labels, width, height, x, y);
                    scratchLabels[1] = LabelValue(labels, width, height, x + 1, y);
                    scratchLabels[2] = LabelValue(labels, width, height, x + 1, y + 1);
                    scratchLabels[3] = LabelValue(labels, width, height, x, y + 1);

                    for (var i = 0; i < 4; i++)
                    {
                        var label = scratchLabels[i];
                        if (label < 0)
                        {
                            continue;
                        }

                        var alreadyHandled = false;
                        for (var j = 0; j < i; j++)
                        {
                            if (scratchLabels[j] == label)
                            {
                                alreadyHandled = true;
                                break;
                            }
                        }

                        if (alreadyHandled)
                        {
                            continue;
                        }

                        var caseIndex =
                            (scratchLabels[0] == label ? 8 : 0)
                            | (scratchLabels[1] == label ? 4 : 0)
                            | (scratchLabels[2] == label ? 2 : 0)
                            | (scratchLabels[3] == label ? 1 : 0);
                        if (caseIndex == 0 || caseIndex == 15)
                        {
                            continue;
                        }

                        if (!edgesByLabel.TryGetValue(label, out var edges))
                        {
                            edges = new List<EdgeSegment>(64);
                            edgesByLabel[label] = edges;
                        }

                        AddCaseEdges(edges, caseIndex, x, y);
                    }
                }
            }

            var result = new Dictionary<int, List<List<Vector2>>>(edgesByLabel.Count);
            foreach (var pair in edgesByLabel)
            {
                result[pair.Key] = ChainEdges(pair.Value);
            }

            return result;
        }

        private static void CollectEdgesForLabel(int[] labels, int width, int height, int targetLabel, List<EdgeSegment> edges)
        {
            for (var y = -1; y < height; y++)
            {
                for (var x = -1; x < width; x++)
                {
                    var tl = LabelAt(labels, width, height, x, y, targetLabel);
                    var tr = LabelAt(labels, width, height, x + 1, y, targetLabel);
                    var br = LabelAt(labels, width, height, x + 1, y + 1, targetLabel);
                    var bl = LabelAt(labels, width, height, x, y + 1, targetLabel);
                    var caseIndex = (tl ? 8 : 0) | (tr ? 4 : 0) | (br ? 2 : 0) | (bl ? 1 : 0);
                    if (caseIndex == 0 || caseIndex == 15)
                    {
                        continue;
                    }

                    AddCaseEdges(edges, caseIndex, x, y);
                }
            }
        }

        private static int LabelValue(int[] labels, int width, int height, int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return -1;
            }

            return labels[y * width + x];
        }

        private static bool LabelAt(int[] labels, int width, int height, int x, int y, int targetLabel)
        {
            return LabelValue(labels, width, height, x, y) == targetLabel;
        }

        private static void AddCaseEdges(List<EdgeSegment> edges, int caseIndex, int x, int y)
        {
            var top = new Vector2(x + 0.5f, y);
            var right = new Vector2(x + 1f, y + 0.5f);
            var bottom = new Vector2(x + 0.5f, y + 1f);
            var left = new Vector2(x, y + 0.5f);

            switch (caseIndex)
            {
                case 1: case 14: edges.Add(new EdgeSegment(left, bottom)); break;
                case 2: case 13: edges.Add(new EdgeSegment(bottom, right)); break;
                case 3: case 12: edges.Add(new EdgeSegment(left, right)); break;
                case 4: case 11: edges.Add(new EdgeSegment(top, right)); break;
                case 5: edges.Add(new EdgeSegment(left, top)); edges.Add(new EdgeSegment(bottom, right)); break;
                case 6: case 9: edges.Add(new EdgeSegment(top, bottom)); break;
                case 7: case 8: edges.Add(new EdgeSegment(left, top)); break;
                case 10: edges.Add(new EdgeSegment(top, right)); edges.Add(new EdgeSegment(left, bottom)); break;
            }
        }

        private static List<List<Vector2>> ChainEdges(List<EdgeSegment> edges)
        {
            var contours = new List<List<Vector2>>();
            if (edges.Count == 0)
            {
                return contours;
            }

            var map = new Dictionary<Vector2Int, List<int>>(edges.Count * 2);
            for (var i = 0; i < edges.Count; i++)
            {
                AddEdgeIndex(map, Quantize(edges[i].A), i);
                AddEdgeIndex(map, Quantize(edges[i].B), i);
            }

            var used = new bool[edges.Count];
            for (var i = 0; i < edges.Count; i++)
            {
                if (used[i])
                {
                    continue;
                }

                var contour = new List<Vector2>(32) { edges[i].A, edges[i].B };
                used[i] = true;

                var closed = ExtendChain(contour, map, edges, used);
                if (!closed)
                {
                    contour.Reverse();
                    closed = ExtendChain(contour, map, edges, used);
                }

                if (contour.Count < 3)
                {
                    continue;
                }

                // Unclosed fragments would be force-closed by SVG 'Z' with a giant
                // diagonal edge across the image. Only tiny gaps may self-close.
                var gapSq = (contour[0] - contour[contour.Count - 1]).sqrMagnitude;
                if (!closed && gapSq > 9f)
                {
                    continue;
                }

                contours.Add(contour);
            }

            return contours;
        }

        private static bool ExtendChain(List<Vector2> contour, Dictionary<Vector2Int, List<int>> map, List<EdgeSegment> edges, bool[] used)
        {
            var current = contour[contour.Count - 1];
            var guard = 0;
            while (guard++ < edges.Count * 4)
            {
                var key = Quantize(current);
                var nextIndex = -1;
                if (map.TryGetValue(key, out var candidates))
                {
                    for (var c = 0; c < candidates.Count; c++)
                    {
                        var candidateIndex = candidates[c];
                        if (!used[candidateIndex])
                        {
                            nextIndex = candidateIndex;
                            break;
                        }
                    }
                }

                if (nextIndex < 0)
                {
                    return false;
                }

                used[nextIndex] = true;
                current = edges[nextIndex].Other(current);
                if ((current - contour[0]).sqrMagnitude < 0.25f)
                {
                    return true;
                }

                contour.Add(current);
            }

            return false;
        }

        private static void AddEdgeIndex(Dictionary<Vector2Int, List<int>> map, Vector2Int key, int edgeIndex)
        {
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<int>(4);
                map[key] = list;
            }

            list.Add(edgeIndex);
        }

        private static Vector2Int Quantize(Vector2 p) => new Vector2Int(Mathf.RoundToInt(p.x * 2f), Mathf.RoundToInt(p.y * 2f));

        private readonly struct EdgeSegment
        {
            public EdgeSegment(Vector2 a, Vector2 b)
            {
                A = a;
                B = b;
            }

            public Vector2 A { get; }
            public Vector2 B { get; }

            public Vector2 Other(Vector2 point) => (point - A).sqrMagnitude < (point - B).sqrMagnitude ? B : A;
        }
    }
}

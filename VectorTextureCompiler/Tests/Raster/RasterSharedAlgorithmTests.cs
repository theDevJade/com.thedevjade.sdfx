using System.Collections.Generic;
using NUnit.Framework;
using SDFX.Rasterizer;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Tests.Raster
{
    public sealed class RasterSharedAlgorithmTests
    {
        [Test]
        public void PathSimplifier_DouglasPeucker_CollinearPointsCollapse()
        {
            var points = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(2f, 0f),
                new Vector2(3f, 0f)
            };

            var simplified = PathSimplifier.DouglasPeucker(points, 0.1f);
            Assert.AreEqual(2, simplified.Count);
            Assert.AreEqual(points[0], simplified[0]);
            Assert.AreEqual(points[points.Count - 1], simplified[1]);
        }

        [Test]
        public void PathSimplifier_SimplifyToEdgeBudget_RespectsMaxEdges()
        {
            var points = new List<Vector2>();
            for (var i = 0; i < 64; i++)
            {
                points.Add(new Vector2(i, Mathf.Sin(i * 0.4f) * 10f));
            }

            var issues = new List<RasterIssue>();
            var simplified = PathSimplifier.SimplifyToEdgeBudget(points, 8, issues);
            Assert.LessOrEqual(simplified.Count, 9);
            Assert.Greater(issues.Count, 0);
        }

        [Test]
        public void ContourTracer_FilledSquare_ProducesClosedContour()
        {
            const int w = 8;
            const int h = 8;
            var mask = new bool[w * h];
            for (var y = 2; y < 6; y++)
            {
                for (var x = 2; x < 6; x++)
                {
                    mask[y * w + x] = true;
                }
            }

            var contours = ContourTracer.TraceSuzukiAbe(mask, w, h, true);
            Assert.Greater(contours.Count, 0);
            Assert.Greater(contours[0].Count, 3);
        }

        [Test]
        public void ColorQuantizer_SolidColors_BuildExactPalette()
        {
            var red = new Color32(255, 0, 0, 255);
            var blue = new Color32(0, 0, 255, 255);
            var pixels = new Color32[16];
            for (var i = 0; i < 8; i++)
            {
                pixels[i] = red;
                pixels[i + 8] = blue;
            }

            var image = new RasterImageBuffer(pixels, 4, 4, null, null);
            var options = new RasterColorQuantOptions
            {
                ColorCount = 4,
                Method = RasterColorQuantMethod.MedianCut
            };

            var palette = ColorQuantizer.BuildPalette(image, options, 0.01f);
            Assert.GreaterOrEqual(palette.Length, 2);
            Assert.LessOrEqual(palette.Length, 4);

            var labels = ColorQuantizer.AssignLabels(image, palette, 0.01f);
            Assert.AreEqual(pixels.Length, labels.Length);
            Assert.AreNotEqual(labels[0], labels[8]);
        }

        [Test]
        public void AutoAlgorithmSelector_BinaryAlphaMask_PrefersContours()
        {
            var pixels = new Color32[32 * 32];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            for (var y = 8; y < 24; y++)
            {
                for (var x = 8; x < 24; x++)
                {
                    pixels[y * 32 + x] = new Color32(255, 255, 255, 255);
                }
            }

            var recommendation = RasterAutoAlgorithmSelector.Analyze(pixels, 32, 32);
            Assert.AreEqual(RasterVectorizationAlgorithm.SuzukiAbeContours, recommendation.Algorithm);
            Assert.IsFalse(string.IsNullOrEmpty(recommendation.Reason));
        }
    }
}

using NUnit.Framework;
using SDFX.Rasterizer;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Tests.Raster
{
    public sealed class RasterVectorizerTests
    {
        [Test]
        public void Registry_ResolvesAllAlgorithms()
        {
            foreach (RasterVectorizationAlgorithm algorithm in System.Enum.GetValues(typeof(RasterVectorizationAlgorithm)))
            {
                Assert.IsNotNull(RasterAlgorithmMetadata.Get(algorithm).Name);
            }
        }

        [Test]
        public void RasterToSvg_ColorQuant_ProducesSvg()
        {
            var texture = CreateTwoToneShapeTexture(32, 32);
            var options = new RasterParsingOptions
            {
                Algorithm = RasterVectorizationAlgorithm.ColorQuantMarchingSquares,
                UseComputeAcceleration = false,
                ColorQuant = { ColorCount = 4, MinRegionArea = 4 }
            };

            var result = RasterToSvg.Export(texture, options);
            Assert.IsFalse(string.IsNullOrEmpty(result.SvgText));
            Assert.IsTrue(result.SvgText.Contains("<svg"));
            Assert.Greater(result.PathCount, 0);
            RasterToSvg.DestroyPreview(result);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void MigrateTracingMode_MapsLegacyModes()
        {
            Assert.AreEqual(
                RasterVectorizationAlgorithm.GradientEdgeVectorization,
                RasterParsingOptions.MigrateTracingMode(RasterTracingMode.Edges));
            Assert.AreEqual(
                RasterVectorizationAlgorithm.SuzukiAbeContours,
                RasterParsingOptions.MigrateTracingMode(RasterTracingMode.Contours));
            Assert.AreEqual(
                RasterVectorizationAlgorithm.SuzukiAbeContours,
                RasterParsingOptions.MigrateTracingMode(RasterTracingMode.Strokes));
        }

        [Test]
        public void RasterToSvg_SuzukiAbe_ProducesSvgFromAlphaMask()
        {
            var texture = CreateAlphaMaskTexture(32, 32);
            var options = new RasterParsingOptions
            {
                Algorithm = RasterVectorizationAlgorithm.SuzukiAbeContours,
                UseComputeAcceleration = false,
                Contour = { ThresholdMode = RasterThresholdMode.Alpha, TraceHoles = true }
            };

            var result = RasterToSvg.Export(texture, options);
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.SvgText.Contains("<svg"));
            Assert.Greater(result.PathCount, 0);
            RasterToSvg.DestroyPreview(result);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void RasterToSvg_HybridContour_ProducesSvg()
        {
            var texture = CreateTwoToneShapeTexture(24, 24);
            var options = new RasterParsingOptions
            {
                Algorithm = RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf,
                UseComputeAcceleration = false,
                ColorQuant = { ColorCount = 4, MinRegionArea = 4 },
                Hybrid = { MinRegionArea = 4 }
            };

            var result = RasterToSvg.Export(texture, options);
            Assert.IsFalse(string.IsNullOrEmpty(result.SvgText));
            Assert.IsTrue(result.SvgText.Contains("<svg"));
            Assert.Greater(result.PathCount, 0);
            RasterToSvg.DestroyPreview(result);
            Object.DestroyImmediate(texture);
        }

        private static Texture2D CreateTwoToneShapeTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[width * height];
            var background = new Color32(255, 255, 255, 255);
            var foreground = new Color32(220, 40, 40, 255);
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = background;
            }

            var x0 = width / 4;
            var y0 = height / 4;
            var x1 = (width * 3) / 4;
            var y1 = (height * 3) / 4;
            for (var y = y0; y < y1; y++)
            {
                for (var x = x0; x < x1; x++)
                {
                    pixels[y * width + x] = foreground;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateAlphaMaskTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            var x0 = width / 4;
            var y0 = height / 4;
            var x1 = (width * 3) / 4;
            var y1 = (height * 3) / 4;
            for (var y = y0; y < y1; y++)
            {
                for (var x = x0; x < x1; x++)
                {
                    pixels[y * width + x] = new Color32(255, 255, 255, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
}

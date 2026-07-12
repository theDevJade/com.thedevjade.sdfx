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
            var texture = CreateSolidTexture(32, 32, Color.red);
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
        }

        private static Texture2D CreateSolidTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[width * height];
            var c = (Color32)color;
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = c;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
}

using NUnit.Framework;
using SDFX.VectorTextureCompiler.Core.Parsing;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Tests.Parsing
{
    public sealed class CustomFormatParserTests
    {
        [Test]
        public void Parse_CanvasAndShapes_ProducesNormalizedUvPrimitives()
        {
            const string source = @"
@canvas,100,200
rect,10,20,50,40,#ff0000
circle,50,100,20,20,#00ff00
";
            var result = CustomFormatParser.Parse(source, new ParserOptions
            {
                CoordinateModel = CoordinateModel.NormalizedUv,
                Strictness = ParserStrictness.Strict
            });

            Assert.IsFalse(result.HasErrors);
            Assert.AreEqual(2, result.Primitives.Count);
            Assert.AreEqual(PrimitiveKind.Rectangle, result.Primitives[0].Type);
            Assert.AreEqual(0.1f, result.Primitives[0].Position.x, 0.0001f);
            Assert.AreEqual(0.1f, result.Primitives[0].Position.y, 0.0001f);
            Assert.AreEqual(0.5f, result.Primitives[0].Size.x, 0.0001f);
            Assert.AreEqual(0.2f, result.Primitives[0].Size.y, 0.0001f);
            Assert.AreEqual(1f, result.Primitives[0].Color.r, 0.01f);
            Assert.AreEqual(PrimitiveKind.Circle, result.Primitives[1].Type);
        }

        [Test]
        public void Parse_SourceSpace_KeepsAbsoluteCoordinates()
        {
            var result = CustomFormatParser.Parse(
                "@canvas,512,512\nrect,10,20,100,50,#ffffff",
                new ParserOptions { CoordinateModel = CoordinateModel.SourceSpace });

            Assert.AreEqual(1, result.Primitives.Count);
            Assert.AreEqual(10f, result.Primitives[0].Position.x, 0.0001f);
            Assert.AreEqual(20f, result.Primitives[0].Position.y, 0.0001f);
            Assert.AreEqual(100f, result.Primitives[0].Size.x, 0.0001f);
        }

        [Test]
        public void Parse_UnsupportedShape_IsErrorInStrictMode()
        {
            var result = CustomFormatParser.Parse(
                "hexagon,0,0,10,10,#ffffff",
                new ParserOptions { Strictness = ParserStrictness.Strict });

            Assert.IsTrue(result.HasErrors);
            Assert.AreEqual(0, result.Primitives.Count);
            Assert.AreEqual(ParseIssueSeverity.Error, result.Issues[0].Severity);
        }

        [Test]
        public void Parse_ShortRow_IsWarningInPermissiveMode()
        {
            var result = CustomFormatParser.Parse(
                "rect,1,2,3",
                new ParserOptions { Strictness = ParserStrictness.Permissive });

            Assert.IsFalse(result.HasErrors);
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual(ParseIssueSeverity.Warning, result.Issues[0].Severity);
        }

        [Test]
        public void Parse_EmptySource_ReturnsError()
        {
            var result = CustomFormatParser.Parse("   ", new ParserOptions());
            Assert.IsTrue(result.HasErrors);
            Assert.AreEqual(0, result.Primitives.Count);
        }

        [Test]
        public void Parse_Aliases_MapToExpectedPrimitiveTypes()
        {
            var result = CustomFormatParser.Parse(
                "@canvas,1,1\nrectangle,0,0,1,1,#fff\nroundedrect,0,0,1,1,#fff\nroundrect,0,0,1,1,#fff",
                new ParserOptions { CoordinateModel = CoordinateModel.SourceSpace });

            Assert.AreEqual(3, result.Primitives.Count);
            Assert.AreEqual(PrimitiveKind.Rectangle, result.Primitives[0].Type);
            Assert.AreEqual(PrimitiveKind.RoundedRectangle, result.Primitives[1].Type);
            Assert.AreEqual(PrimitiveKind.RoundedRectangle, result.Primitives[2].Type);
        }
    }
}

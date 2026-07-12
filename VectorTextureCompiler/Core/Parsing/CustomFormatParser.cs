using System;
using System.Collections.Generic;
using System.Globalization;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Core.Parsing
{
    public static class CustomFormatParser
    {
        public static List<Primitive> Parse(string sourceText)
        {
            return Parse(sourceText, new ParserOptions()).Primitives;
        }

        public static ParseResult Parse(string sourceText, ParserOptions options)
        {
            options ??= new ParserOptions();

            var primitives = new List<Primitive>();
            var sourceData = new List<PrimitiveSourceData>();
            var issues = new List<ParseIssue>();
            var canvasWidth = 1f;
            var canvasHeight = 1f;

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                issues.Add(new ParseIssue(ParseIssueSeverity.Error, SdfxLanguage.Parsing.CustomSourceTextEmpty, SdfxLanguage.Parsing.CustomElementName, 0));
                return new ParseResult(primitives, sourceData, issues);
            }

            var lines = sourceText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex].Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (line.StartsWith("@canvas", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseCanvasDirective(line, out canvasWidth, out canvasHeight))
                    {
                        AddIssue(options, issues, SdfxLanguage.Parsing.CanvasElementName, SdfxLanguage.Parsing.InvalidCanvasDirective, lineIndex + 1);
                    }

                    continue;
                }

                var parts = line.Split(',');
                if (parts.Length < 6)
                {
                    AddIssue(options, issues, SdfxLanguage.Parsing.CustomElementName, SdfxLanguage.Parsing.InvalidCustomRow, lineIndex + 1);
                    continue;
                }

                if (!TryParsePrimitive(parts, options, canvasWidth, canvasHeight, out var primitive, out var metadata, out var error))
                {
                    AddIssue(options, issues, SdfxLanguage.Parsing.CustomElementName, error, lineIndex + 1);
                    continue;
                }

                primitives.Add(primitive);
                sourceData.Add(metadata);
            }

            return new ParseResult(primitives, sourceData, issues);
        }

        private static bool TryParsePrimitive(
            string[] parts,
            ParserOptions options,
            float canvasWidth,
            float canvasHeight,
            out Primitive primitive,
            out PrimitiveSourceData metadata,
            out string error)
        {
            primitive = default;
            metadata = new PrimitiveSourceData();
            error = string.Empty;

            var shape = parts[0].Trim().ToLowerInvariant();
            if (!TryParseFloat(parts, 1, out var x) || !TryParseFloat(parts, 2, out var y) || !TryParseFloat(parts, 3, out var width) || !TryParseFloat(parts, 4, out var height))
            {
                error = SdfxLanguage.Parsing.InvalidCustomBounds;
                return false;
            }

            var color = ParseColor(parts[5].Trim());
            var mappedType = PrimitiveKind.Rectangle;
            switch (shape)
            {
                case "rect":
                case "rectangle":
                    mappedType = PrimitiveKind.Rectangle;
                    break;
                case "circle":
                    mappedType = PrimitiveKind.Circle;
                    break;
                case "line":
                    mappedType = PrimitiveKind.Line;
                    break;
                case "bezier":
                    mappedType = PrimitiveKind.Bezier;
                    break;
                case "polygon":
                    mappedType = PrimitiveKind.Polygon;
                    break;
                case "roundrect":
                case "roundedrect":
                case "rounded_rectangle":
                    mappedType = PrimitiveKind.RoundedRectangle;
                    break;
                case "capsule":
                    mappedType = PrimitiveKind.Capsule;
                    break;
                case "star":
                    mappedType = PrimitiveKind.Star;
                    break;
                case "ring":
                    mappedType = PrimitiveKind.Ring;
                    break;
                case "arc":
                    mappedType = PrimitiveKind.Arc;
                    break;
                default:
                    error = SdfxLanguage.Parsing.UnsupportedCustomPrimitiveType(shape);
                    return false;
            }

            var sourcePos = new Vector2(x, y);
            var sourceSize = new Vector2(width, height);
            var uvPos = new Vector2(sourcePos.x / canvasWidth, sourcePos.y / canvasHeight);
            var uvSize = new Vector2(sourceSize.x / canvasWidth, sourceSize.y / canvasHeight);

            primitive = new Primitive
            {
                Type = mappedType,
                Position = options.CoordinateModel == CoordinateModel.SourceSpace ? sourcePos : uvPos,
                Size = options.CoordinateModel == CoordinateModel.SourceSpace ? sourceSize : uvSize,
                RotationDegrees = 0f,
                Color = color,
                ParameterIndex = -1,
                Softness = 0f,
                Layer = 0
            };

            metadata = new PrimitiveSourceData
            {
                SourcePosition = options.CoordinateModel == CoordinateModel.Hybrid ? sourcePos : primitive.Position,
                SourceSize = options.CoordinateModel == CoordinateModel.Hybrid ? sourceSize : primitive.Size,
                SourceElement = shape
            };

            return true;
        }

        private static bool TryParseCanvasDirective(string line, out float width, out float height)
        {
            width = 1f;
            height = 1f;

            var parts = line.Split(',');
            if (parts.Length != 3)
            {
                return false;
            }

            if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out width)
                || !float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out height))
            {
                return false;
            }

            width = Mathf.Max(0.0001f, width);
            height = Mathf.Max(0.0001f, height);
            return true;
        }

        private static bool TryParseFloat(string[] parts, int index, out float value)
        {
            value = 0f;
            if (index < 0 || index >= parts.Length)
            {
                return false;
            }

            return float.TryParse(parts[index].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static Color ParseColor(string raw)
        {
            if (ColorUtility.TryParseHtmlString(raw, out var color))
            {
                return color;
            }

            return Color.white;
        }

        private static void AddIssue(ParserOptions options, List<ParseIssue> issues, string elementName, string message, int lineNumber)
        {
            var severity = options.Strictness == ParserStrictness.Strict ? ParseIssueSeverity.Error : ParseIssueSeverity.Warning;
            issues.Add(new ParseIssue(severity, message, elementName, lineNumber));
        }
    }
}
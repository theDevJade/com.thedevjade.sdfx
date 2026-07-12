using System;
using System.Collections.Generic;
using System.Globalization;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using System.Xml;
using System.Xml.Linq;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Core.Parsing
{
    public static class SvgParser
    {
        public static List<Primitive> Parse(string svgText)
        {
            return Parse(svgText, new ParserOptions()).Primitives;
        }

        public static ParseResult Parse(string svgText, ParserOptions options)
        {
            return VectorGraphicsSvgAdapter.Parse(svgText, options);
        }

        internal static void AddBoundsPrimitive(
            List<Primitive> primitives,
            List<PrimitiveSourceData> sourceData,
            ParserOptions options,
            PrimitiveKind type,
            float sourceX,
            float sourceY,
            float sourceWidth,
            float sourceHeight,
            Color color,
            float canvasWidth,
            float canvasHeight,
            string sourceElement)
        {
            AddPrimitive(primitives, sourceData, options, type, sourceX, sourceY, sourceWidth, sourceHeight, color, canvasWidth, canvasHeight, sourceElement);
        }

        private static ParseResult ParseBaseline(string svgText, ParserOptions options)
        {
            options ??= new ParserOptions();

            var primitives = new List<Primitive>();
            var sourceData = new List<PrimitiveSourceData>();
            var issues = new List<ParseIssue>();

            if (string.IsNullOrWhiteSpace(svgText))
            {
                issues.Add(new ParseIssue(ParseIssueSeverity.Error, SdfxLanguage.Parsing.SvgTextEmpty, SdfxLanguage.Parsing.SvgElementName, 0));
                return new ParseResult(primitives, sourceData, issues);
            }

            XDocument xml;
            try
            {
                xml = XDocument.Parse(svgText, LoadOptions.SetLineInfo);
            }
            catch (XmlException ex)
            {
                issues.Add(new ParseIssue(ParseIssueSeverity.Error, ex.Message, SdfxLanguage.Parsing.SvgElementName, ex.LineNumber, ParseIssueCode.ParseFailure));
                return new ParseResult(primitives, sourceData, issues);
            }

            var svgElement = xml.Root;
            if (svgElement == null || !svgElement.Name.LocalName.Equals("svg", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ParseIssue(ParseIssueSeverity.Error, SdfxLanguage.Parsing.RootIsNotSvg, SdfxLanguage.Parsing.SvgElementName, 0, ParseIssueCode.InvalidDocument));
                return new ParseResult(primitives, sourceData, issues);
            }

            var view = ReadCanvasDimensions(svgElement);
            WalkNode(svgElement, view.width, view.height, options, primitives, sourceData, issues);

            return new ParseResult(primitives, sourceData, issues);
        }

        private static (float width, float height) ReadCanvasDimensions(XElement svg)
        {
            var width = ReadFloat(ReadAttribute(svg, "width"), 1f);
            var height = ReadFloat(ReadAttribute(svg, "height"), 1f);

            var viewBox = ReadAttribute(svg, "viewBox");
            if (!string.IsNullOrWhiteSpace(viewBox))
            {
                var parts = SplitNumbers(viewBox);
                if (parts.Count == 4)
                {
                    width = Mathf.Max(0.0001f, parts[2]);
                    height = Mathf.Max(0.0001f, parts[3]);
                }
            }

            return (Mathf.Max(0.0001f, width), Mathf.Max(0.0001f, height));
        }

        private static void WalkNode(
            XElement node,
            float canvasWidth,
            float canvasHeight,
            ParserOptions options,
            List<Primitive> primitives,
            List<PrimitiveSourceData> sourceData,
            List<ParseIssue> issues)
        {
            foreach (var element in node.Elements())
            {
                var elementName = element.Name.LocalName.ToLowerInvariant();

                if (HasTransform(element))
                {
                    ReportUnsupported(options, issues, elementName, SdfxLanguage.Parsing.UnsupportedTransform, ParseIssueCode.UnsupportedTransform, GetLineNumber(element));
                }

                if (HasGradientReference(element))
                {
                    ReportUnsupported(options, issues, elementName, SdfxLanguage.Parsing.UnsupportedGradientReference, ParseIssueCode.UnsupportedGradient, GetLineNumber(element));
                }

                if (HasInlineStyle(element))
                {
                    ReportUnsupported(options, issues, elementName, SdfxLanguage.Parsing.LimitedInlineStyle, ParseIssueCode.UnsupportedStyle, GetLineNumber(element));
                }

                switch (elementName)
                {
                    case "g":
                    case "svg":
                        WalkNode(element, canvasWidth, canvasHeight, options, primitives, sourceData, issues);
                        break;
                    case "path":
                        ReportUnsupported(options, issues, elementName, SdfxLanguage.Parsing.UnsupportedPath, ParseIssueCode.UnsupportedPath, GetLineNumber(element));
                        break;
                    case "defs":
                    case "lineargradient":
                    case "radialgradient":
                    case "pattern":
                    case "mask":
                    case "clippath":
                        ReportUnsupported(options, issues, elementName, SdfxLanguage.Parsing.UnsupportedDefinitionElement, ParseIssueCode.UnsupportedGradient, GetLineNumber(element));
                        break;
                    case "rect":
                        AddRect(element, canvasWidth, canvasHeight, options, primitives, sourceData);
                        break;
                    case "circle":
                        AddCircle(element, canvasWidth, canvasHeight, options, primitives, sourceData);
                        break;
                    case "ellipse":
                        AddEllipse(element, canvasWidth, canvasHeight, options, primitives, sourceData);
                        break;
                    case "line":
                        AddLine(element, canvasWidth, canvasHeight, options, primitives, sourceData);
                        break;
                    case "polygon":
                        AddPolygonBounds(element, canvasWidth, canvasHeight, options, primitives, sourceData, issues);
                        break;
                    default:
                        ReportUnsupported(options, issues, elementName, SdfxLanguage.Parsing.UnsupportedSvgElementIgnored, ParseIssueCode.UnsupportedElement, GetLineNumber(element));
                        break;
                }
            }
        }

        private static void AddRect(XElement element, float canvasWidth, float canvasHeight, ParserOptions options, List<Primitive> primitives, List<PrimitiveSourceData> sourceData)
        {
            var x = ReadFloat(ReadAttribute(element, "x"), 0f);
            var y = ReadFloat(ReadAttribute(element, "y"), 0f);
            var width = Mathf.Max(0f, ReadFloat(ReadAttribute(element, "width"), 0f));
            var height = Mathf.Max(0f, ReadFloat(ReadAttribute(element, "height"), 0f));
            var color = ReadFillColor(element);

            AddPrimitive(primitives, sourceData, options, PrimitiveKind.Rectangle, x, y, width, height, color, canvasWidth, canvasHeight, "rect");
        }

        private static void AddCircle(XElement element, float canvasWidth, float canvasHeight, ParserOptions options, List<Primitive> primitives, List<PrimitiveSourceData> sourceData)
        {
            var cx = ReadFloat(ReadAttribute(element, "cx"), 0f);
            var cy = ReadFloat(ReadAttribute(element, "cy"), 0f);
            var r = Mathf.Max(0f, ReadFloat(ReadAttribute(element, "r"), 0f));
            var color = ReadFillColor(element);

            AddPrimitive(primitives, sourceData, options, PrimitiveKind.Circle, cx - r, cy - r, r * 2f, r * 2f, color, canvasWidth, canvasHeight, "circle");
        }

        private static void AddEllipse(XElement element, float canvasWidth, float canvasHeight, ParserOptions options, List<Primitive> primitives, List<PrimitiveSourceData> sourceData)
        {
            var cx = ReadFloat(ReadAttribute(element, "cx"), 0f);
            var cy = ReadFloat(ReadAttribute(element, "cy"), 0f);
            var rx = Mathf.Max(0f, ReadFloat(ReadAttribute(element, "rx"), 0f));
            var ry = Mathf.Max(0f, ReadFloat(ReadAttribute(element, "ry"), 0f));
            var color = ReadFillColor(element);

            AddPrimitive(primitives, sourceData, options, PrimitiveKind.Circle, cx - rx, cy - ry, rx * 2f, ry * 2f, color, canvasWidth, canvasHeight, "ellipse");
        }

        private static void AddLine(XElement element, float canvasWidth, float canvasHeight, ParserOptions options, List<Primitive> primitives, List<PrimitiveSourceData> sourceData)
        {
            var x1 = ReadFloat(ReadAttribute(element, "x1"), 0f);
            var y1 = ReadFloat(ReadAttribute(element, "y1"), 0f);
            var x2 = ReadFloat(ReadAttribute(element, "x2"), 0f);
            var y2 = ReadFloat(ReadAttribute(element, "y2"), 0f);

            var minX = Mathf.Min(x1, x2);
            var minY = Mathf.Min(y1, y2);
            var width = Mathf.Abs(x2 - x1);
            var height = Mathf.Abs(y2 - y1);
            var stroke = ReadStrokeColor(element);

            AddPrimitive(primitives, sourceData, options, PrimitiveKind.Line, minX, minY, width, Mathf.Max(height, 0.001f), stroke, canvasWidth, canvasHeight, "line");
        }

        private static void AddPolygonBounds(
            XElement element,
            float canvasWidth,
            float canvasHeight,
            ParserOptions options,
            List<Primitive> primitives,
            List<PrimitiveSourceData> sourceData,
            List<ParseIssue> issues)
        {
            var points = ReadAttribute(element, "points");
            var values = SplitNumbers(points);
            if (values.Count < 6 || values.Count % 2 != 0)
            {
                ReportUnsupported(options, issues, "polygon", SdfxLanguage.Parsing.InvalidPolygonPoints, ParseIssueCode.InvalidGeometry, GetLineNumber(element));
                return;
            }

            var minX = float.PositiveInfinity;
            var minY = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxY = float.NegativeInfinity;

            for (var i = 0; i < values.Count; i += 2)
            {
                minX = Mathf.Min(minX, values[i]);
                minY = Mathf.Min(minY, values[i + 1]);
                maxX = Mathf.Max(maxX, values[i]);
                maxY = Mathf.Max(maxY, values[i + 1]);
            }

            var color = ReadFillColor(element);
            AddPrimitive(primitives, sourceData, options, PrimitiveKind.Polygon, minX, minY, maxX - minX, maxY - minY, color, canvasWidth, canvasHeight, "polygon");
        }

        private static void AddPrimitive(
            List<Primitive> primitives,
            List<PrimitiveSourceData> sourceData,
            ParserOptions options,
            PrimitiveKind type,
            float sourceX,
            float sourceY,
            float sourceWidth,
            float sourceHeight,
            Color color,
            float canvasWidth,
            float canvasHeight,
            string sourceElement)
        {
            var uvPos = new Vector2(sourceX / canvasWidth, sourceY / canvasHeight);
            var uvSize = new Vector2(sourceWidth / canvasWidth, sourceHeight / canvasHeight);

            var primitive = new Primitive
            {
                Type = type,
                Position = options.CoordinateModel == CoordinateModel.SourceSpace ? new Vector2(sourceX, sourceY) : uvPos,
                Size = options.CoordinateModel == CoordinateModel.SourceSpace ? new Vector2(sourceWidth, sourceHeight) : uvSize,
                RotationDegrees = 0f,
                Color = color,
                ParameterIndex = -1,
                Softness = 0f,
                Layer = 0
            };

            primitives.Add(primitive);

            if (options.CoordinateModel == CoordinateModel.Hybrid)
            {
                sourceData.Add(new PrimitiveSourceData
                {
                    SourcePosition = new Vector2(sourceX, sourceY),
                    SourceSize = new Vector2(sourceWidth, sourceHeight),
                    SourceElement = sourceElement
                });
            }
            else
            {
                sourceData.Add(new PrimitiveSourceData
                {
                    SourcePosition = primitive.Position,
                    SourceSize = primitive.Size,
                    SourceElement = sourceElement
                });
            }
        }

        private static Color ReadFillColor(XElement element)
        {
            var fill = ReadAttribute(element, "fill");
            return ParseColor(fill, Color.white);
        }

        private static Color ReadStrokeColor(XElement element)
        {
            var stroke = ReadAttribute(element, "stroke");
            return ParseColor(stroke, Color.white);
        }

        private static Color ParseColor(string value, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return fallback;
            }

            if (ColorUtility.TryParseHtmlString(value, out var color))
            {
                return color;
            }

            return fallback;
        }

        private static void ReportUnsupported(
            ParserOptions options,
            List<ParseIssue> issues,
            string elementName,
            string message,
            ParseIssueCode code,
            int lineNumber)
        {
            var severity = options.Strictness == ParserStrictness.Strict ? ParseIssueSeverity.Error : ParseIssueSeverity.Warning;
            issues.Add(new ParseIssue(severity, message, elementName, lineNumber, code));
        }

        private static bool HasTransform(XElement element)
        {
            return !string.IsNullOrWhiteSpace(ReadAttribute(element, "transform"));
        }

        private static bool HasGradientReference(XElement element)
        {
            var fill = ReadAttribute(element, "fill");
            var stroke = ReadAttribute(element, "stroke");
            return IsGradientValue(fill) || IsGradientValue(stroke);
        }

        private static bool HasInlineStyle(XElement element)
        {
            return !string.IsNullOrWhiteSpace(ReadAttribute(element, "style"));
        }

        private static bool IsGradientValue(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.IndexOf("url(", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ReadAttribute(XElement element, string name)
        {
            var attribute = element.Attribute(name);
            return attribute == null ? string.Empty : attribute.Value;
        }

        private static int GetLineNumber(XElement element)
        {
            var lineInfo = (IXmlLineInfo)element;
            return lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0;
        }

        private static float ReadFloat(string raw, float fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            raw = raw.Trim();
            var unitStart = raw.Length;
            for (var i = 0; i < raw.Length; i++)
            {
                var c = raw[i];
                if ((c < '0' || c > '9') && c != '-' && c != '+' && c != '.' && c != 'e' && c != 'E')
                {
                    unitStart = i;
                    break;
                }
            }

            if (unitStart != raw.Length)
            {
                raw = raw.Substring(0, unitStart);
            }

            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;
        }

        private static List<float> SplitNumbers(string source)
        {
            var values = new List<float>();
            if (string.IsNullOrWhiteSpace(source))
            {
                return values;
            }

            var tokens = source.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < tokens.Length; i++)
            {
                if (float.TryParse(tokens[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    values.Add(value);
                }
            }

            return values;
        }
    }
}
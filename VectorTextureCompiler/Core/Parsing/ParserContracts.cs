using System;
using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Core.Parsing
{
    public enum ParserStrictness
    {
        Strict = 0,
        Permissive = 1
    }

    public enum CoordinateModel
    {
        NormalizedUv = 0,
        SourceSpace = 1,
        Hybrid = 2
    }

    public enum ParseIssueSeverity
    {
        Warning = 0,
        Error = 1
    }

    public enum ParseIssueCode
    {
        Unknown = 0,
        ParseFailure = 1,
        InvalidDocument = 2,
        InvalidAttribute = 3,
        InvalidGeometry = 4,
        InvalidInput = 5,
        UnsupportedElement = 10,
        UnsupportedPath = 11,
        UnsupportedTransform = 12,
        UnsupportedGradient = 13,
        UnsupportedStyle = 14,
        PathDetailReduced = 15,
        UnsupportedRasterMode = 20,
        RasterComputeUnavailable = 21,
        RasterInferenceUnavailable = 22,
        RasterModelLoadFailed = 23,
        RasterModelActive = 24,
        RasterUsingFallbackSegmentation = 25
    }

    public readonly struct ParseIssue
    {
        public ParseIssue(ParseIssueSeverity severity, string message, string elementName, int lineNumber, ParseIssueCode code = ParseIssueCode.Unknown)
        {
            Severity = severity;
            Message = message ?? string.Empty;
            ElementName = elementName ?? string.Empty;
            LineNumber = lineNumber;
            Code = code;
        }

        public ParseIssueSeverity Severity { get; }
        public string Message { get; }
        public string ElementName { get; }
        public int LineNumber { get; }
        public ParseIssueCode Code { get; }
    }

    [Serializable]
    public sealed class ParserOptions
    {
        public ParserStrictness Strictness = ParserStrictness.Strict;
        public CoordinateModel CoordinateModel = CoordinateModel.Hybrid;

        public int MaxPathEdgesPerPrimitive = 512;
    }

    [Serializable]
    public sealed class PrimitiveSourceData
    {
        public Vector2 SourcePosition;
        public Vector2 SourceSize;
        public string SourceElement = string.Empty;
    }

    public readonly struct ParseResult
    {
        public ParseResult(List<Primitive> primitives, List<PrimitiveSourceData> sourceData, List<ParseIssue> issues)
            : this(primitives, sourceData, issues, null)
        {
        }

        public ParseResult(List<Primitive> primitives, List<PrimitiveSourceData> sourceData, List<ParseIssue> issues, List<Vector4> pathEdges)
        {
            Primitives = primitives ?? new List<Primitive>();
            SourceData = sourceData ?? new List<PrimitiveSourceData>();
            Issues = issues ?? new List<ParseIssue>();
            PathEdges = pathEdges ?? new List<Vector4>();
        }

        public List<Primitive> Primitives { get; }
        public List<PrimitiveSourceData> SourceData { get; }
        public List<ParseIssue> Issues { get; }

        public List<Vector4> PathEdges { get; }

        public bool HasErrors
        {
            get
            {
                for (var i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i].Severity == ParseIssueSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
